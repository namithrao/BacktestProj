namespace Shared;

/// <summary>
/// Represents a single OHLC bar (candle) for options data
/// </summary>
public class Bar
{
    /// <summary>
    /// Unix timestamp in milliseconds
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Opening price
    /// </summary>
    public double Open { get; set; }

    /// <summary>
    /// Highest price during the period
    /// </summary>
    public double High { get; set; }

    /// <summary>
    /// Lowest price during the period
    /// </summary>
    public double Low { get; set; }

    /// <summary>
    /// Closing price
    /// </summary>
    public double Close { get; set; }

    /// <summary>
    /// Trading volume
    /// </summary>
    public long Volume { get; set; }

    /// <summary>
    /// Option ticker symbol (e.g., O_TSLA211119C00080000)
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// DateTime representation of the timestamp (UTC)
    /// </summary>
    public DateTime DateTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}
