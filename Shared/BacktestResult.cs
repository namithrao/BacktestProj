namespace Shared;

/// <summary>
/// Contains all backtest performance metrics and results
/// </summary>
public class BacktestResult
{
    /// <summary>
    /// Total profit/loss
    /// </summary>
    public double TotalPnl { get; set; }

    /// <summary>
    /// Total P&L as percentage of initial capital
    /// </summary>
    public double TotalPnlPercent { get; set; }

    /// <summary>
    /// Sharpe ratio (annualized)
    /// </summary>
    public double SharpeRatio { get; set; }

    /// <summary>
    /// Maximum drawdown (percentage)
    /// </summary>
    public double MaxDrawdown { get; set; }

    /// <summary>
    /// Maximum drawdown in dollar amount
    /// </summary>
    public double MaxDrawdownDollar { get; set; }

    /// <summary>
    /// Win rate (percentage of winning trades)
    /// </summary>
    public double WinRate { get; set; }

    /// <summary>
    /// Total number of trades executed
    /// </summary>
    public int NumberOfTrades { get; set; }

    /// <summary>
    /// Number of winning trades
    /// </summary>
    public int WinningTrades { get; set; }

    /// <summary>
    /// Number of losing trades
    /// </summary>
    public int LosingTrades { get; set; }

    /// <summary>
    /// Average profit per winning trade
    /// </summary>
    public double AverageWin { get; set; }

    /// <summary>
    /// Average loss per losing trade
    /// </summary>
    public double AverageLoss { get; set; }

    /// <summary>
    /// Profit factor (total wins / total losses)
    /// </summary>
    public double ProfitFactor { get; set; }

    /// <summary>
    /// Equity curve over time
    /// </summary>
    public List<EquityPoint> EquityCurve { get; set; } = new();

    /// <summary>
    /// All trades executed during backtest
    /// </summary>
    public List<Trade> Trades { get; set; } = new();

    /// <summary>
    /// Initial capital
    /// </summary>
    public double InitialCapital { get; set; }

    /// <summary>
    /// Final capital
    /// </summary>
    public double FinalCapital { get; set; }

    /// <summary>
    /// Start date of backtest
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of backtest
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Tickers included in backtest
    /// </summary>
    public List<string> Tickers { get; set; } = new();
}
