using Shared;

namespace Backtester;

/// <summary>
/// Main backtesting engine that orchestrates the backtest
/// </summary>
public class BacktestEngine
{
    private readonly IStrategy _strategy;
    private readonly Portfolio _portfolio;
    private readonly OrderExecutor _orderExecutor;
    private readonly PerformanceCalculator _perfCalculator;
    private readonly int _equityUpdateInterval;

    public event Action<Bar>? OnBarProcessed;
    public event Action<Trade>? OnTradeExecuted;
    public event Action<EquityPoint>? OnEquityUpdate;
    public event Action<int, int>? OnProgress; // (current, total)

    public BacktestEngine(
        IStrategy strategy,
        double initialCapital = 100000,
        double feePerTrade = 1.0,
        double riskFreeRate = 0.04,
        int equityUpdateInterval = 100)
    {
        _strategy = strategy;
        _equityUpdateInterval = equityUpdateInterval;

        _portfolio = new Portfolio
        {
            Cash = initialCapital,
            InitialCapital = initialCapital
        };

        _orderExecutor = new OrderExecutor(_portfolio, feePerTrade);
        _perfCalculator = new PerformanceCalculator(riskFreeRate);
    }

    /// <summary>
    /// Run the backtest on historical data
    /// </summary>
    public async Task<BacktestResult> RunAsync(
        List<Bar> bars,
        List<string> tickers,
        DateTime startDate,
        DateTime endDate)
    {
        Console.WriteLine($"\nStarting backtest...");
        Console.WriteLine($"Strategy: {_strategy.Name}");
        Console.WriteLine($"Initial Capital: {_portfolio.InitialCapital:C}");
        Console.WriteLine($"Bars to process: {bars.Count}");
        Console.WriteLine($"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}\n");

        _strategy.Reset();

        var barCount = 0;
        var totalBars = bars.Count;

        foreach (var bar in bars)
        {
            barCount++;

            // Update position prices
            _orderExecutor.UpdatePositionPrices(bar);

            // Process bar through strategy
            var signal = _strategy.OnBar(bar);

            // Execute signal if generated
            if (signal != null)
            {
                var trade = _orderExecutor.ExecuteSignal(signal);

                if (trade != null)
                {
                    Console.WriteLine($"[{bar.DateTime:yyyy-MM-dd HH:mm}] {trade.Action} {trade.Ticker} @ {trade.Price:F2} | P&L: {trade.Pnl:F2} | Cash: {_portfolio.Cash:F2}");
                    OnTradeExecuted?.Invoke(trade);
                }
            }

            // Record equity at intervals
            if (barCount % _equityUpdateInterval == 0 || barCount == totalBars)
            {
                var equityPoint = new EquityPoint
                {
                    Timestamp = bar.Timestamp,
                    Equity = _portfolio.TotalEquity
                };

                _portfolio.EquityHistory.Add(equityPoint);
                OnEquityUpdate?.Invoke(equityPoint);
            }

            // Report progress
            if (barCount % 1000 == 0 || barCount == totalBars)
            {
                var progress = (int)((barCount / (double)totalBars) * 100);
                Console.WriteLine($"Progress: {progress}% ({barCount}/{totalBars}) | Equity: {_portfolio.TotalEquity:C}");
                OnProgress?.Invoke(barCount, totalBars);
            }

            OnBarProcessed?.Invoke(bar);

            // Simulate async processing (allows other tasks to run)
            if (barCount % 100 == 0)
            {
                await Task.Delay(1);
            }
        }

        // Close all remaining positions
        Console.WriteLine("\nClosing all open positions...");
        var closingTrades = _orderExecutor.CloseAllPositions(bars.Last().Timestamp);

        foreach (var trade in closingTrades)
        {
            Console.WriteLine($"Close {trade.Ticker} @ {trade.Price:F2} | P&L: {trade.Pnl:F2}");
            OnTradeExecuted?.Invoke(trade);
        }

        // Calculate performance metrics
        Console.WriteLine("\nCalculating performance metrics...");
        var result = _perfCalculator.CalculateMetrics(_portfolio, startDate, endDate, tickers);

        PrintResults(result);

        return result;
    }

    private void PrintResults(BacktestResult result)
    {
        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("BACKTEST RESULTS");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Strategy: {_strategy.Name}");
        Console.WriteLine($"Period: {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}");
        Console.WriteLine($"Tickers: {string.Join(", ", result.Tickers)}");
        Console.WriteLine();
        Console.WriteLine($"Initial Capital:    {result.InitialCapital,15:C}");
        Console.WriteLine($"Final Capital:      {result.FinalCapital,15:C}");
        Console.WriteLine($"Total P&L:          {result.TotalPnl,15:C} ({result.TotalPnlPercent:F2}%)");
        Console.WriteLine();
        Console.WriteLine($"Sharpe Ratio:       {result.SharpeRatio,15:F3}");
        Console.WriteLine($"Max Drawdown:       {result.MaxDrawdown,15:F2}% ({result.MaxDrawdownDollar:C})");
        Console.WriteLine();
        Console.WriteLine($"Total Trades:       {result.NumberOfTrades,15}");
        Console.WriteLine($"Winning Trades:     {result.WinningTrades,15}");
        Console.WriteLine($"Losing Trades:      {result.LosingTrades,15}");
        Console.WriteLine($"Win Rate:           {result.WinRate,15:F2}%");
        Console.WriteLine();
        Console.WriteLine($"Average Win:        {result.AverageWin,15:C}");
        Console.WriteLine($"Average Loss:       {result.AverageLoss,15:C}");
        Console.WriteLine($"Profit Factor:      {result.ProfitFactor,15:F2}");
        Console.WriteLine(new string('=', 60));
    }
}
