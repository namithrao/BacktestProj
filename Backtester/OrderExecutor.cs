using Shared;

namespace Backtester;

/// <summary>
/// Executes orders and manages portfolio positions
/// </summary>
public class OrderExecutor
{
    private readonly double _feePerTrade;
    private readonly Portfolio _portfolio;

    public OrderExecutor(Portfolio portfolio, double feePerTrade = 1.0)
    {
        _portfolio = portfolio;
        _feePerTrade = feePerTrade;
    }

    /// <summary>
    /// Execute a trading signal
    /// </summary>
    public Trade? ExecuteSignal(StrategySignal signal)
    {
        if (signal.SignalType == SignalType.BUY)
        {
            return ExecuteBuy(signal);
        }
        else if (signal.SignalType == SignalType.SELL)
        {
            return ExecuteSell(signal);
        }

        return null;
    }

    private Trade? ExecuteBuy(StrategySignal signal)
    {
        // Check if we already have a position in this ticker
        if (_portfolio.Positions.ContainsKey(signal.Ticker))
        {
            // Already have a position, skip
            return null;
        }

        // Calculate cost (options are 100 shares per contract)
        var cost = signal.Price * signal.Quantity * 100 + _feePerTrade;

        // Check if we have enough cash
        if (_portfolio.Cash < cost)
        {
            Console.WriteLine($"Insufficient cash for {signal.Ticker}: Need {cost:C}, Have {_portfolio.Cash:C}");
            return null;
        }

        // Create position
        var position = new Position
        {
            Ticker = signal.Ticker,
            Quantity = signal.Quantity,
            EntryPrice = signal.Price,
            EntryTime = signal.Timestamp,
            CurrentPrice = signal.Price
        };

        // Update portfolio
        _portfolio.Positions[signal.Ticker] = position;
        _portfolio.Cash -= cost;

        // Create trade record
        var trade = new Trade
        {
            Timestamp = signal.Timestamp,
            Ticker = signal.Ticker,
            Action = TradeAction.BUY,
            Price = signal.Price,
            Quantity = signal.Quantity,
            Fee = _feePerTrade,
            Pnl = 0 // P&L is calculated on sell
        };

        _portfolio.Trades.Add(trade);

        return trade;
    }

    private Trade? ExecuteSell(StrategySignal signal)
    {
        // Check if we have a position to sell
        if (!_portfolio.Positions.TryGetValue(signal.Ticker, out var position))
        {
            // No position to sell
            return null;
        }

        // Calculate proceeds
        var proceeds = signal.Price * signal.Quantity * 100 - _feePerTrade;

        // Calculate P&L
        var cost = position.EntryPrice * position.Quantity * 100;
        var pnl = proceeds - cost - _feePerTrade; // Subtract buy fee as well

        // Update portfolio
        _portfolio.Cash += proceeds;
        _portfolio.Positions.Remove(signal.Ticker);

        // Create trade record
        var trade = new Trade
        {
            Timestamp = signal.Timestamp,
            Ticker = signal.Ticker,
            Action = TradeAction.SELL,
            Price = signal.Price,
            Quantity = signal.Quantity,
            Fee = _feePerTrade,
            Pnl = pnl
        };

        _portfolio.Trades.Add(trade);

        return trade;
    }

    /// <summary>
    /// Update current prices for all positions
    /// </summary>
    public void UpdatePositionPrices(Bar bar)
    {
        if (_portfolio.Positions.TryGetValue(bar.Ticker, out var position))
        {
            position.CurrentPrice = bar.Close;
        }
    }

    /// <summary>
    /// Close all open positions at current prices
    /// </summary>
    public List<Trade> CloseAllPositions(long timestamp)
    {
        var trades = new List<Trade>();

        foreach (var position in _portfolio.Positions.Values.ToList())
        {
            var signal = new StrategySignal
            {
                Timestamp = timestamp,
                Ticker = position.Ticker,
                SignalType = SignalType.SELL,
                Price = position.CurrentPrice,
                Quantity = position.Quantity,
                Reason = "Close all positions at end of backtest"
            };

            var trade = ExecuteSell(signal);
            if (trade != null)
            {
                trades.Add(trade);
            }
        }

        return trades;
    }
}
