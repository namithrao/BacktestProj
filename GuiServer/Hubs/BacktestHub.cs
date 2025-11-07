using Microsoft.AspNetCore.SignalR;
using Shared;

namespace GuiServer.Hubs;

/// <summary>
/// SignalR hub for real-time backtest updates
/// </summary>
public class BacktestHub : Hub
{
    private readonly BacktestRunner _backtestRunner;

    public BacktestHub(BacktestRunner backtestRunner)
    {
        _backtestRunner = backtestRunner;
    }

    /// <summary>
    /// Start a new backtest with specified parameters
    /// </summary>
    public async Task StartBacktest(BacktestRequest request)
    {
        Console.WriteLine($"Starting backtest for: {string.Join(", ", request.Tickers)}");

        try
        {
            await _backtestRunner.RunBacktestAsync(request, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("OnError", $"Error running backtest: {ex.Message}");
            Console.WriteLine($"Error in backtest: {ex}");
        }
    }

    /// <summary>
    /// Get current backtest status
    /// </summary>
    public async Task GetStatus()
    {
        var status = _backtestRunner.GetStatus();
        await Clients.Caller.SendAsync("OnStatus", status);
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// Request model for starting a backtest
/// </summary>
public class BacktestRequest
{
    public List<string> Tickers { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int ShortPeriod { get; set; } = 10;
    public int LongPeriod { get; set; } = 30;
    public double InitialCapital { get; set; } = 100000;
}

/// <summary>
/// Status information for a running backtest
/// </summary>
public class BacktestStatus
{
    public bool IsRunning { get; set; }
    public int Progress { get; set; }
    public string Message { get; set; } = string.Empty;
}
