# Options Backtesting System

A C# application for backtesting options trading strategies using historical data from Polygon.io. This project demonstrates network programming concepts with ASP.NET Core and SignalR for real-time communication.

## Features

- **Data Acquisition**: Download historical options data from Polygon.io API
- **Moving Average Crossover Strategy**: Automated trading signals based on MA crossovers
- **Real-time Web Dashboard**: Live backtest results via SignalR
- **Performance Metrics**: Sharpe ratio, max drawdown, win rate, P&L tracking
- **Multi-ticker Support**: Backtest NVDA, TSLA, and AAPL options simultaneously

## Project Structure

```
BacktestProj/
├── Shared/                    # Domain models (Bar, Trade, Portfolio, etc.)
├── DataAcquisition/          # Console app to download Polygon.io data
├── Backtester/               # Backtesting engine and MA crossover strategy
├── GuiServer/                # ASP.NET Core web app with SignalR
│   └── wwwroot/              # Web dashboard (HTML/CSS/JS)
└── Data/                     # CSV storage for historical data
    ├── NVDA/
    ├── TSLA/
    └── AAPL/
```

## Prerequisites

- **.NET 8 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Polygon.io API Key**: [Sign up for free](https://polygon.io/)
  - Free tier includes: 5 API calls/minute, 2 years historical data, minute-level bars

## Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/namithrao/BacktestProject.git
cd BacktestProject
```

### 2. Configure API Credentials

```bash
# Copy template files to create your local config
cp DataAcquisition/appsettings.template.json DataAcquisition/appsettings.json
cp GuiServer/appsettings.template.json GuiServer/appsettings.json
```

### 3. Add Your Polygon.io API Key

Edit `DataAcquisition/appsettings.json` and replace `YOUR_POLYGON_API_KEY_HERE` with your actual API key:

```json
{
  "PolygonApiKey": "your-actual-api-key-here",
  "DataDirectory": "../Data",
  "CallsPerMinute": 4,
  "SkipExisting": true,
  "MaxContractsPerUnderlying": 50
}
```

**Security Note**: Your `appsettings.json` files are automatically ignored by git (they're in `.gitignore`). Never commit files containing your API key!

Get your free API key at: https://polygon.io/

### 4. Install Dependencies

The project uses NuGet packages that will be restored automatically:

- **DataAcquisition**: `CsvHelper`, `System.Text.Json`
- **Backtester**: `CsvHelper`, `MathNet.Numerics`
- **GuiServer**: `Microsoft.AspNetCore.SignalR`

## Usage

### Step 1: Download Historical Data

**Important**: Downloading 1 year of data for 3 tickers can take significant time due to API rate limits (5 calls/minute). Start with a smaller dataset for testing.

```bash
cd DataAcquisition
dotnet run
```

This will:
- Fetch all options contracts for NVDA, TSLA, AAPL
- Download minute-level OHLC bars for the past year
- Save data to CSV files in `Data/{Ticker}/{ContractSymbol}.csv`
- Resume from where it left off if interrupted (when `SkipExisting: true`)

**Expected Duration**: With 50 contracts per ticker and API limits, expect ~30-60 minutes for initial download.

**Tips**:
- Reduce `MaxContractsPerUnderlying` to download fewer contracts for testing
- The program displays progress and saves as it downloads
- You can stop and restart - it will skip existing files

### Step 2: Run the Backtest Server

```bash
cd GuiServer
dotnet run
```

The server will start on `http://localhost:5000`

### Step 3: Open the Dashboard

Open your browser and navigate to:

```
http://localhost:5000/index.html
```

### Step 4: Run a Backtest

1. **Select Tickers**: Check NVDA, TSLA, and/or AAPL
2. **Set Date Range**: Choose start and end dates (up to 1 year)
3. **Configure Strategy**:
   - Short MA Period: e.g., 10 minutes
   - Long MA Period: e.g., 30 minutes
4. **Set Initial Capital**: e.g., $100,000
5. Click **"Start Backtest"**

The dashboard will show:
- Real-time progress updates
- Live equity curve chart
- Trade log as trades execute
- Final performance metrics (P&L, Sharpe ratio, drawdown, win rate)

## Strategy Explanation

### Moving Average Crossover

The strategy uses two simple moving averages (SMA) on option prices:

- **Short MA**: Fast-moving average (e.g., 10 minutes)
- **Long MA**: Slow-moving average (e.g., 30 minutes)

**Trading Rules**:
- **BUY Signal**: When short MA crosses above long MA (bullish crossover)
- **SELL Signal**: When short MA crosses below long MA (bearish crossover)
- **Position Sizing**: 1 contract per trade
- **Entry/Exit**: Market orders at bar close price

### Performance Metrics

- **Total P&L**: Final equity - initial capital
- **Return %**: (Total P&L / Initial capital) × 100
- **Sharpe Ratio**: Risk-adjusted return (annualized)
  - Formula: `(Mean Return - Risk Free Rate) / Std Dev of Returns`
  - Higher is better (>1 is good, >2 is excellent)
- **Max Drawdown**: Largest peak-to-trough decline in equity (%)
- **Win Rate**: Percentage of profitable trades
- **Profit Factor**: Total wins / Total losses

## Configuration

### DataAcquisition Settings

`DataAcquisition/appsettings.json`:
- `PolygonApiKey`: Your Polygon.io API key
- `DataDirectory`: Where to save CSV files
- `CallsPerMinute`: API rate limit (5 for free tier)
- `SkipExisting`: Skip already downloaded files
- `MaxContractsPerUnderlying`: Limit contracts to download

### GuiServer Settings

`GuiServer/appsettings.json`:
- `DataDirectory`: Path to CSV data folder
- `FeePerTrade`: Commission per trade ($1.00)
- `RiskFreeRate`: Annual risk-free rate for Sharpe ratio (0.04 = 4%)

## Data Format

CSV files are stored as: `Data/{Ticker}/{ContractSymbol}.csv`

Example: `Data/TSLA/O:TSLA241115C00250000.csv`

Format:
```csv
timestamp,open,high,low,close,volume
1634567400000,82.5,83.2,82.1,82.8,1500
1634567460000,82.8,83.0,82.6,82.9,1200
```

- **timestamp**: Unix timestamp in milliseconds
- **open, high, low, close**: Option prices
- **volume**: Trading volume

## Troubleshooting

### "No data found for specified tickers"

- Ensure you've run DataAcquisition and data exists in `Data/{Ticker}/`
- Check that selected tickers match folder names (NVDA, TSLA, AAPL)

### "Insufficient cash" messages during backtest

- Option prices can be high ($50-$500+ per contract × 100 shares)
- Increase initial capital or reduce number of simultaneous positions

### Slow data download

- This is expected with free tier (5 API calls/minute)
- Reduce `MaxContractsPerUnderlying` for faster testing
- Consider upgrading Polygon.io plan for faster downloads

### SignalR connection errors

- Ensure GuiServer is running (`dotnet run` in GuiServer directory)
- Check browser console for connection errors
- Try refreshing the page

## Learning Objectives

This project demonstrates:

1. **Network Programming**:
   - HTTP REST API calls (Polygon.io)
   - Real-time WebSocket communication (SignalR)
   - Client-server architecture

2. **ASP.NET Core**:
   - SignalR hubs for real-time updates
   - Static file serving
   - CORS configuration

3. **Backtesting Concepts**:
   - Event-driven architecture
   - Time-series data processing
   - Portfolio management
   - Performance metrics calculation

4. **Data Handling**:
   - CSV file I/O
   - Data streaming for large datasets
   - Chronological data sorting

## Limitations (Free Tier)

- **API Rate Limit**: 5 calls/minute (slow downloads)
- **Historical Data**: 2 years maximum
- **No Real-time**: Free tier has delayed data
- **Minute-level Only**: Daily and minute bars (no tick data)

## Future Enhancements

- [ ] Add more strategies (RSI, Bollinger Bands, Greeks-based)
- [ ] Implement options Greeks calculation
- [ ] Add multi-leg strategies (spreads, straddles)
- [ ] Support for backtesting equity stocks
- [ ] Export results to PDF/Excel
- [ ] Parameter optimization (grid search)
- [ ] Walk-forward analysis
- [ ] Convert to Protocol Buffers for faster data loading
- [ ] Add ZeroMQ for distributed backtesting

## Resources

- [Polygon.io API Documentation](https://polygon.io/docs)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr/)
- [Chart.js Documentation](https://www.chartjs.org/docs/)
- [Options Trading Basics](https://www.investopedia.com/options-basics-tutorial-4583012)

## License

This project is for educational purposes. Use at your own risk. Not financial advice.

## Support

For issues or questions:
1. Check this README
2. Review code comments
3. Check Polygon.io API status
4. Verify .NET 8 SDK is installed

---

**Happy Backtesting! 📈**
