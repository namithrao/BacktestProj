using Shared;

namespace Backtester;

/// <summary>
/// Interface for trading strategies
/// </summary>
public interface IStrategy
{
    /// <summary>
    /// Process a new bar and optionally generate a trading signal
    /// </summary>
    StrategySignal? OnBar(Bar bar);

    /// <summary>
    /// Get the strategy name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Reset the strategy state
    /// </summary>
    void Reset();
}
