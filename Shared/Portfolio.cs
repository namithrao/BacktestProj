namespace Shared;

/// <summary>
/// Represents the portfolio state at a point in time
/// </summary>
public class Portfolio
{
    /// <summary>
    /// Available cash
    /// </summary>
    public double Cash { get; set; }

    /// <summary>
    /// All open positions
    /// </summary>
    public Dictionary<string, Position> Positions { get; set; } = new();

    /// <summary>
    /// History of equity values over time (timestamp -> equity)
    /// </summary>
    public List<EquityPoint> EquityHistory { get; set; } = new();

    /// <summary>
    /// All executed trades
    /// </summary>
    public List<Trade> Trades { get; set; } = new();

    /// <summary>
    /// Total value of all positions
    /// </summary>
    public double PositionsValue => Positions.Values.Sum(p => p.CurrentValue);

    /// <summary>
    /// Total equity (cash + positions)
    /// </summary>
    public double TotalEquity => Cash + PositionsValue;

    /// <summary>
    /// Initial capital at start of backtest
    /// </summary>
    public double InitialCapital { get; set; }
}

/// <summary>
/// Represents a point in the equity curve
/// </summary>
public class EquityPoint
{
    public long Timestamp { get; set; }
    public double Equity { get; set; }

    public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}
