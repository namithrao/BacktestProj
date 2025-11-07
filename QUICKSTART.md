# Quick Start Guide

## Get Started in 3 Steps

### 1. Configure Your API Key

Edit `DataAcquisition/appsettings.json`:
```json
{
  "PolygonApiKey": "YOUR_API_KEY_HERE"
}
```

Get your free API key at: https://polygon.io/

### 2. Download Sample Data (Start Small!)

For testing, reduce `MaxContractsPerUnderlying` to 5 in `appsettings.json`:

```bash
cd DataAcquisition
dotnet run
```

Wait 5-10 minutes for sample data to download.

### 3. Run the Dashboard

```bash
cd ../GuiServer
dotnet run
```

Open browser: http://localhost:5000/index.html

Select tickers, date range, and click "Start Backtest"!

## Project File Overview

### Solution & Projects
- `BacktestProj.sln` - Main solution file
- `Shared/` - Domain models (Bar, Trade, Portfolio, etc.)
- `DataAcquisition/` - Downloads data from Polygon.io
- `Backtester/` - Backtesting engine with MA crossover strategy
- `GuiServer/` - Web server with SignalR real-time updates

### Key Files

**Shared Project (Domain Models)**
- `Bar.cs` - OHLC candle data
- `Trade.cs` - Executed trade record
- `Position.cs` - Open position tracking
- `Portfolio.cs` - Cash, positions, equity history
- `BacktestResult.cs` - Performance metrics
- `StrategySignal.cs` - Trading signals (BUY/SELL/HOLD)

**DataAcquisition**
- `Program.cs` - Main download orchestration
- `PolygonClient.cs` - Polygon.io API client with rate limiting
- `CsvWriter.cs` - Saves bars to CSV files

**Backtester**
- `BacktestEngine.cs` - Main backtest orchestrator
- `MovingAverageCrossoverStrategy.cs` - MA crossover logic
- `OrderExecutor.cs` - Executes trades and manages portfolio
- `PerformanceCalculator.cs` - Calculates Sharpe, drawdown, etc.
- `CsvDataLoader.cs` - Loads bars from CSV files
- `IStrategy.cs` - Strategy interface

**GuiServer**
- `Program.cs` - ASP.NET Core startup
- `BacktestRunner.cs` - Runs backtest and broadcasts via SignalR
- `Hubs/BacktestHub.cs` - SignalR hub for real-time communication
- `wwwroot/index.html` - Web dashboard UI
- `wwwroot/styles.css` - Dashboard styling
- `wwwroot/app.js` - SignalR client and chart logic

### Data Storage
```
Data/
├── NVDA/
│   ├── O:NVDA241115C00500000.csv
│   └── ...
├── TSLA/
│   └── O:TSLA241115C00250000.csv
└── AAPL/
    └── O:AAPL241115C00180000.csv
```

Each CSV contains minute-level OHLC data:
```csv
timestamp,open,high,low,close,volume
1634567400000,82.5,83.2,82.1,82.8,1500
```

## Common Issues

**"dotnet: command not found"**
- Install .NET 8 SDK: https://dotnet.microsoft.com/download

**"No data found"**
- Run DataAcquisition first to download data
- Check that `Data/` folder exists with CSV files

**SignalR not connecting**
- Ensure GuiServer is running
- Check console for errors
- Try refreshing browser

## Tips for Testing

1. **Start Small**: Set `MaxContractsPerUnderlying: 5` for quick testing
2. **Short Date Range**: Test with 1 month of data initially
3. **One Ticker**: Uncheck other tickers to speed up backtest
4. **Console Output**: Watch terminal for detailed backtest progress

## Next Steps

1. Download full dataset (may take hours with free API tier)
2. Experiment with MA periods (short: 5-20, long: 20-50)
3. Analyze results in the dashboard
4. Review trade log to understand strategy behavior

## Architecture Flow

```
1. DataAcquisition downloads from Polygon.io → CSV files
2. GuiServer starts ASP.NET Core + SignalR hub
3. Browser connects to SignalR hub
4. User clicks "Start Backtest"
5. BacktestRunner:
   - Loads CSV data
   - Creates strategy and engine
   - Processes bars chronologically
   - Broadcasts updates via SignalR
6. Browser receives real-time updates:
   - Progress bar
   - Equity chart
   - Trade log
   - Final metrics
```

## Key Concepts

**Moving Average Crossover**
- Short MA crosses above Long MA = BUY signal
- Short MA crosses below Long MA = SELL signal
- Works on individual option contracts

**Sharpe Ratio**
- Measures risk-adjusted returns
- Higher is better (>1 good, >2 excellent)
- Accounts for volatility

**Max Drawdown**
- Largest peak-to-trough decline
- Indicates risk/volatility
- Lower is better

## Learning Path

1. Run the complete system end-to-end
2. Read through domain models in `Shared/`
3. Study `MovingAverageCrossoverStrategy.cs`
4. Understand `BacktestEngine.cs` event loop
5. Review SignalR hub in `BacktestHub.cs`
6. Examine web dashboard JavaScript in `app.js`

---

**Ready to start? Edit the API key and run DataAcquisition!**
