using Backtester;
using GuiServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace GuiServer;

/// <summary>
/// Service that runs backtests and broadcasts updates via SignalR
/// </summary>
public class BacktestRunner
{
    private readonly IHubContext<BacktestHub> _hubContext;
    private readonly IConfiguration _configuration;
    private bool _isRunning;
    private int _progress;
    private string _statusMessage = string.Empty;

    public BacktestRunner(IHubContext<BacktestHub> hubContext, IConfiguration configuration)
    {
        _hubContext = hubContext;
        _configuration = configuration;
    }

    public async Task RunBacktestAsync(BacktestRequest request, string connectionId)
    {
        if (_isRunning)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("OnError", "A backtest is already running");
            return;
        }

        _isRunning = true;
        _progress = 0;
        _statusMessage = "Starting backtest...";

        try
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("OnProgress", 0, "Loading data...");

            // Load configuration
            var dataDirectory = _configuration["BacktestSettings:DataDirectory"] ?? "../Data";
            var feePerTrade = _configuration.GetValue<double>("BacktestSettings:FeePerTrade", 1.0);
            var riskFreeRate = _configuration.GetValue<double>("BacktestSettings:RiskFreeRate", 0.04);

            // Load data
            var dataLoader = new CsvDataLoader(dataDirectory);
            var bars = await dataLoader.LoadBarsAsync(request.Tickers);

            if (bars.Count == 0)
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("OnError", "No data found for specified tickers");
                _isRunning = false;
                return;
            }

            await _hubContext.Clients.Client(connectionId).SendAsync("OnProgress", 10, $"Loaded {bars.Count} bars");

            // Filter bars by date range
            bars = bars.Where(b => b.DateTime >= request.StartDate && b.DateTime <= request.EndDate).ToList();

            await _hubContext.Clients.Client(connectionId).SendAsync("OnProgress", 15, $"Filtered to {bars.Count} bars in date range");

            // Create strategy
            var strategy = new MovingAverageCrossoverStrategy(request.ShortPeriod, request.LongPeriod);

            // Create engine
            var engine = new BacktestEngine(
                strategy,
                request.InitialCapital,
                feePerTrade,
                riskFreeRate,
                equityUpdateInterval: 100
            );

            // Subscribe to events
            var barCount = 0;
            var throttleCounter = 0;
            var totalBars = bars.Count;

            engine.OnBarProcessed += async (bar) =>
            {
                barCount++;
                throttleCounter++;

                // Throttle bar updates (send every 100th bar to avoid overwhelming the client)
                if (throttleCounter >= 100 || barCount == totalBars)
                {
                    throttleCounter = 0;
                    await _hubContext.Clients.Client(connectionId).SendAsync("OnBarProcessed", new
                    {
                        timestamp = bar.Timestamp,
                        ticker = bar.Ticker,
                        close = bar.Close,
                        dateTime = bar.DateTime
                    });
                }
            };

            engine.OnTradeExecuted += async (trade) =>
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("OnTrade", trade);
            };

            engine.OnEquityUpdate += async (equityPoint) =>
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("OnEquityUpdate", equityPoint);
            };

            engine.OnProgress += async (current, total) =>
            {
                var progressPercent = (int)((current / (double)total) * 100);
                _progress = progressPercent;
                _statusMessage = $"Processing bars: {current}/{total}";
                await _hubContext.Clients.Client(connectionId).SendAsync("OnProgress", progressPercent, _statusMessage);
            };

            // Run backtest
            var result = await engine.RunAsync(bars, request.Tickers, request.StartDate, request.EndDate);

            // Send final result
            await _hubContext.Clients.Client(connectionId).SendAsync("OnBacktestComplete", result);

            _statusMessage = "Backtest complete";
            _progress = 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunBacktestAsync: {ex}");
            await _hubContext.Clients.Client(connectionId).SendAsync("OnError", $"Backtest failed: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    public BacktestStatus GetStatus()
    {
        return new BacktestStatus
        {
            IsRunning = _isRunning,
            Progress = _progress,
            Message = _statusMessage
        };
    }
}
