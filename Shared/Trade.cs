namespace Shared;

/// <summary>
/// Represents an executed trade
/// </summary>
public class Trade
{
    /// <summary>
    /// Unix timestamp in milliseconds when the trade was executed
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Option ticker symbol
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Trade action: BUY or SELL
    /// </summary>
    public TradeAction Action { get; set; }

    /// <summary>
    /// Execution price
    /// </summary>
    public double Price { get; set; }

    /// <summary>
    /// Number of contracts
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Profit/Loss for this trade (calculated on close)
    /// </summary>
    public double Pnl { get; set; }

    /// <summary>
    /// Fee paid for this trade
    /// </summary>
    public double Fee { get; set; }

    /// <summary>
    /// DateTime representation of the timestamp (UTC)
    /// </summary>
    public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}

public enum TradeAction
{
    BUY,
    SELL
}
