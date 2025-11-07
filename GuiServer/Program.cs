using GuiServer;
using GuiServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSignalR();
builder.Services.AddSingleton<BacktestRunner>();

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5000", "http://127.0.0.1:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<BacktestHub>("/backtestHub");

// Simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

Console.WriteLine("=== Options Backtesting Server ===");
Console.WriteLine($"Server starting on: http://localhost:5000");
Console.WriteLine("SignalR Hub: http://localhost:5000/backtestHub");
Console.WriteLine("Dashboard: http://localhost:5000/index.html");
Console.WriteLine("\nPress Ctrl+C to stop the server");

app.Run("http://localhost:5000");
