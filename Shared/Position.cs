namespace Shared;

/// <summary>
/// Represents an open position in a single option contract
/// </summary>
public class Position
{
    /// <summary>
    /// Option ticker symbol
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Number of contracts held (positive for long, negative for short)
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Average entry price
    /// </summary>
    public double EntryPrice { get; set; }

    /// <summary>
    /// Unix timestamp when position was opened
    /// </summary>
    public long EntryTime { get; set; }

    /// <summary>
    /// Current market price of the position
    /// </summary>
    public double CurrentPrice { get; set; }

    /// <summary>
    /// Current market value of the position
    /// </summary>
    public double CurrentValue => Quantity * CurrentPrice * 100; // Options are 100 shares per contract

    /// <summary>
    /// Unrealized P&L
    /// </summary>
    public double UnrealizedPnl => (CurrentPrice - EntryPrice) * Quantity * 100;

    /// <summary>
    /// DateTime representation of entry time (UTC)
    /// </summary>
    public DateTime EntryDateTime => DateTimeOffset.FromUnixTimeMilliseconds(EntryTime).UtcDateTime;
}
