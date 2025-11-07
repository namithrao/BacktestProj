using Shared;

namespace Backtester;

/// <summary>
/// Moving Average Crossover Strategy
/// Generates BUY signal when short MA crosses above long MA
/// Generates SELL signal when short MA crosses below long MA
/// </summary>
public class MovingAverageCrossoverStrategy : IStrategy
{
    private readonly int _shortPeriod;
    private readonly int _longPeriod;
    private readonly Dictionary<string, ContractData> _contractDataMap;

    public string Name => $"MA Crossover ({_shortPeriod}/{_longPeriod})";

    public MovingAverageCrossoverStrategy(int shortPeriod = 10, int longPeriod = 30)
    {
        _shortPeriod = shortPeriod;
        _longPeriod = longPeriod;
        _contractDataMap = new Dictionary<string, ContractData>();
    }

    public StrategySignal? OnBar(Bar bar)
    {
        // Get or create data for this contract
        if (!_contractDataMap.ContainsKey(bar.Ticker))
        {
            _contractDataMap[bar.Ticker] = new ContractData(_shortPeriod, _longPeriod);
        }

        var data = _contractDataMap[bar.Ticker];

        // Add price to buffers
        data.PriceBuffer.Add(bar.Close);

        // Need enough data to calculate both MAs
        if (data.PriceBuffer.Count < _longPeriod)
        {
            return null;
        }

        // Calculate moving averages
        var shortMA = CalculateMA(data.PriceBuffer, _shortPeriod);
        var longMA = CalculateMA(data.PriceBuffer, _longPeriod);

        // Store previous MA values to detect crossover
        var prevShortMA = data.PreviousShortMA;
        var prevLongMA = data.PreviousLongMA;

        data.PreviousShortMA = shortMA;
        data.PreviousLongMA = longMA;

        // Need previous values to detect crossover
        if (!prevShortMA.HasValue || !prevLongMA.HasValue)
        {
            return null;
        }

        // Detect crossover
        var signal = DetectCrossover(
            prevShortMA.Value,
            prevLongMA.Value,
            shortMA,
            longMA,
            data.CurrentPosition
        );

        if (signal != SignalType.HOLD)
        {
            // Update position state
            if (signal == SignalType.BUY)
            {
                data.CurrentPosition = true;
            }
            else if (signal == SignalType.SELL)
            {
                data.CurrentPosition = false;
            }

            return new StrategySignal
            {
                Timestamp = bar.Timestamp,
                Ticker = bar.Ticker,
                SignalType = signal,
                Price = bar.Close,
                Quantity = 1,
                Reason = $"MA Crossover: Short={shortMA:F2}, Long={longMA:F2}"
            };
        }

        return null;
    }

    public void Reset()
    {
        _contractDataMap.Clear();
    }

    private double CalculateMA(List<double> prices, int period)
    {
        if (prices.Count < period)
            return 0;

        return prices.Skip(prices.Count - period).Take(period).Average();
    }

    private SignalType DetectCrossover(
        double prevShortMA,
        double prevLongMA,
        double currentShortMA,
        double currentLongMA,
        bool hasPosition)
    {
        // Bullish crossover: short MA crosses above long MA
        if (prevShortMA <= prevLongMA && currentShortMA > currentLongMA && !hasPosition)
        {
            return SignalType.BUY;
        }

        // Bearish crossover: short MA crosses below long MA
        if (prevShortMA >= prevLongMA && currentShortMA < currentLongMA && hasPosition)
        {
            return SignalType.SELL;
        }

        return SignalType.HOLD;
    }

    private class ContractData
    {
        public List<double> PriceBuffer { get; }
        public double? PreviousShortMA { get; set; }
        public double? PreviousLongMA { get; set; }
        public bool CurrentPosition { get; set; } // true if we have a position

        public ContractData(int shortPeriod, int longPeriod)
        {
            // Keep extra history for safety
            var bufferSize = Math.Max(shortPeriod, longPeriod) * 2;
            PriceBuffer = new List<double>(bufferSize);
        }
    }
}
