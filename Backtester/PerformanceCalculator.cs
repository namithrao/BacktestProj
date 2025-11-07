using Shared;

namespace Backtester;

/// <summary>
/// Calculates performance metrics for backtesting
/// </summary>
public class PerformanceCalculator
{
    private readonly double _riskFreeRate;

    public PerformanceCalculator(double riskFreeRate = 0.04) // 4% annual risk-free rate
    {
        _riskFreeRate = riskFreeRate;
    }

    /// <summary>
    /// Calculate all performance metrics from a portfolio
    /// </summary>
    public BacktestResult CalculateMetrics(
        Portfolio portfolio,
        DateTime startDate,
        DateTime endDate,
        List<string> tickers)
    {
        var result = new BacktestResult
        {
            InitialCapital = portfolio.InitialCapital,
            FinalCapital = portfolio.TotalEquity,
            TotalPnl = portfolio.TotalEquity - portfolio.InitialCapital,
            StartDate = startDate,
            EndDate = endDate,
            Tickers = tickers,
            EquityCurve = portfolio.EquityHistory,
            Trades = portfolio.Trades
        };

        result.TotalPnlPercent = (result.TotalPnl / portfolio.InitialCapital) * 100;

        // Calculate trade statistics
        CalculateTradeStatistics(result);

        // Calculate Sharpe ratio
        result.SharpeRatio = CalculateSharpeRatio(portfolio.EquityHistory, portfolio.InitialCapital);

        // Calculate max drawdown
        CalculateDrawdown(portfolio.EquityHistory, result);

        return result;
    }

    private void CalculateTradeStatistics(BacktestResult result)
    {
        var trades = result.Trades;
        result.NumberOfTrades = trades.Count(t => t.Action == TradeAction.SELL); // Count closed positions

        if (result.NumberOfTrades == 0)
        {
            return;
        }

        var closedTrades = trades.Where(t => t.Action == TradeAction.SELL).ToList();

        result.WinningTrades = closedTrades.Count(t => t.Pnl > 0);
        result.LosingTrades = closedTrades.Count(t => t.Pnl <= 0);

        result.WinRate = result.NumberOfTrades > 0
            ? (result.WinningTrades / (double)result.NumberOfTrades) * 100
            : 0;

        var wins = closedTrades.Where(t => t.Pnl > 0).ToList();
        var losses = closedTrades.Where(t => t.Pnl < 0).ToList();

        result.AverageWin = wins.Any() ? wins.Average(t => t.Pnl) : 0;
        result.AverageLoss = losses.Any() ? losses.Average(t => Math.Abs(t.Pnl)) : 0;

        var totalWins = wins.Sum(t => t.Pnl);
        var totalLosses = Math.Abs(losses.Sum(t => t.Pnl));

        result.ProfitFactor = totalLosses > 0 ? totalWins / totalLosses : 0;
    }

    private double CalculateSharpeRatio(List<EquityPoint> equityHistory, double initialCapital)
    {
        if (equityHistory.Count < 2)
            return 0;

        // Calculate returns
        var returns = new List<double>();
        for (int i = 1; i < equityHistory.Count; i++)
        {
            var prevEquity = equityHistory[i - 1].Equity;
            var currentEquity = equityHistory[i].Equity;

            if (prevEquity > 0)
            {
                var ret = (currentEquity - prevEquity) / prevEquity;
                returns.Add(ret);
            }
        }

        if (returns.Count == 0)
            return 0;

        // Calculate mean and std dev of returns
        var meanReturn = returns.Average();
        var variance = returns.Sum(r => Math.Pow(r - meanReturn, 2)) / returns.Count;
        var stdDev = Math.Sqrt(variance);

        if (stdDev == 0)
            return 0;

        // Annualize
        // Assuming minute-level data: 252 trading days * 390 minutes per day
        var periodsPerYear = 252 * 390;
        var annualizedReturn = meanReturn * periodsPerYear;
        var annualizedStdDev = stdDev * Math.Sqrt(periodsPerYear);

        // Sharpe ratio
        var sharpeRatio = (annualizedReturn - _riskFreeRate) / annualizedStdDev;

        return sharpeRatio;
    }

    private void CalculateDrawdown(List<EquityPoint> equityHistory, BacktestResult result)
    {
        if (equityHistory.Count == 0)
        {
            result.MaxDrawdown = 0;
            result.MaxDrawdownDollar = 0;
            return;
        }

        double maxEquity = equityHistory[0].Equity;
        double maxDrawdown = 0;
        double maxDrawdownDollar = 0;

        foreach (var point in equityHistory)
        {
            if (point.Equity > maxEquity)
            {
                maxEquity = point.Equity;
            }

            var drawdown = maxEquity - point.Equity;
            var drawdownPercent = maxEquity > 0 ? (drawdown / maxEquity) * 100 : 0;

            if (drawdown > maxDrawdownDollar)
            {
                maxDrawdownDollar = drawdown;
            }

            if (drawdownPercent > maxDrawdown)
            {
                maxDrawdown = drawdownPercent;
            }
        }

        result.MaxDrawdown = maxDrawdown;
        result.MaxDrawdownDollar = maxDrawdownDollar;
    }
}
