using System.Text.Json;
using DataAcquisition;

Console.WriteLine("=== Options Data Acquisition Tool ===\n");

// Load configuration
var config = LoadConfiguration();

// Initialize clients
var polygonClient = new PolygonClient(config.PolygonApiKey, config.CallsPerMinute);
var csvWriter = new CsvWriter(config.DataDirectory);

// Define parameters
var underlyings = new[] { "NVDA", "TSLA", "AAPL" };
var endDate = DateTime.UtcNow.Date;
var startDate = endDate.AddYears(-1); // 1 year of historical data

Console.WriteLine($"Download Parameters:");
Console.WriteLine($"  Underlyings: {string.Join(", ", underlyings)}");
Console.WriteLine($"  Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
Console.WriteLine($"  Data Directory: {config.DataDirectory}");
Console.WriteLine($"  Rate Limit: {config.CallsPerMinute} calls/minute\n");

foreach (var underlying in underlyings)
{
    Console.WriteLine($"\n--- Processing {underlying} ---\n");

    try
    {
        // Step 1: Get all options contracts for this underlying
        // We want contracts that were ACTIVE during our backtest period (startDate to endDate)
        // Get contracts that expire between 1-6 months after our data collection end date
        // This ensures they were actively traded during the full year
        Console.WriteLine($"Fetching options contracts for {underlying}...");
        var contracts = await polygonClient.GetOptionsContractsAsync(
            underlying,
            expirationDateGte: startDate.AddMonths(1),  // Contracts that expired after we started collecting
            expirationDateLte: endDate.AddMonths(6)      // Contracts with enough time for active trading
        );

        Console.WriteLine($"Found {contracts.Count} contracts for {underlying}\n");

        if (contracts.Count == 0)
        {
            Console.WriteLine($"No contracts found for {underlying}, skipping...");
            continue;
        }

        // Filter contracts if needed (optional: only ATM options, only specific DTE, etc.)
        // For now, we'll download all contracts but you may want to filter to reduce API calls
        var contractsToDownload = FilterContracts(contracts, config);

        Console.WriteLine($"Downloading data for {contractsToDownload.Count} contracts...\n");

        // Step 2: Download minute bars for each contract
        var downloaded = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var contract in contractsToDownload)
        {
            try
            {
                // Skip if already downloaded (optional: check file existence)
                if (config.SkipExisting && csvWriter.FileExists(contract.Ticker))
                {
                    var existingBars = csvWriter.GetBarCount(contract.Ticker);
                    Console.WriteLine($"[{downloaded + skipped + failed + 1}/{contractsToDownload.Count}] Skipping {contract.Ticker} (already has {existingBars} bars)");
                    skipped++;
                    continue;
                }

                Console.WriteLine($"[{downloaded + skipped + failed + 1}/{contractsToDownload.Count}] Downloading {contract.Ticker}...");

                // Download bars in chunks (Polygon has a 50,000 result limit per call)
                var allBars = new List<Shared.Bar>();
                var chunkStartDate = startDate;
                var chunkEndDate = startDate.AddMonths(3); // Download in 3-month chunks

                while (chunkStartDate < endDate)
                {
                    if (chunkEndDate > endDate)
                        chunkEndDate = endDate;

                    var bars = await polygonClient.GetMinuteBarsAsync(
                        contract.Ticker,
                        chunkStartDate,
                        chunkEndDate
                    );

                    allBars.AddRange(bars);

                    chunkStartDate = chunkEndDate.AddDays(1);
                    chunkEndDate = chunkStartDate.AddMonths(3);
                }

                if (allBars.Count > 0)
                {
                    await csvWriter.WriteBarsAsync(allBars, contract.Ticker);
                    downloaded++;
                }
                else
                {
                    Console.WriteLine($"  No data available for {contract.Ticker}");
                    failed++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error downloading {contract.Ticker}: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"\n{underlying} Summary:");
        Console.WriteLine($"  Downloaded: {downloaded}");
        Console.WriteLine($"  Skipped: {skipped}");
        Console.WriteLine($"  Failed: {failed}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {underlying}: {ex.Message}");
    }
}

Console.WriteLine("\n=== Download Complete ===");

// Helper methods
static Configuration LoadConfiguration()
{
    var configPath = "appsettings.json";

    if (!File.Exists(configPath))
    {
        Console.WriteLine("Configuration file not found. Creating default appsettings.json...");
        var defaultConfig = new Configuration
        {
            PolygonApiKey = "YOUR_API_KEY_HERE",
            DataDirectory = "../Data",
            CallsPerMinute = 5,
            SkipExisting = true,
            MaxContractsPerUnderlying = 50
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"Created {configPath}. Please update with your Polygon.io API key and run again.");
        Environment.Exit(1);
    }

    var json = File.ReadAllText(configPath);
    var config = JsonSerializer.Deserialize<Configuration>(json) ?? new Configuration();

    if (string.IsNullOrEmpty(config.PolygonApiKey) || config.PolygonApiKey == "YOUR_API_KEY_HERE")
    {
        Console.WriteLine("Error: Please set your Polygon.io API key in appsettings.json");
        Environment.Exit(1);
    }

    return config;
}

static List<OptionsContract> FilterContracts(List<OptionsContract> contracts, Configuration config)
{
    // Filter for contracts with better chance of having trading data:
    // 1. Prefer contracts expiring 30-180 days from now (more likely to be actively traded)
    // 2. Take contracts from various expiration dates to diversify
    // 3. Sort by strike price for variety

    var today = DateTime.UtcNow;

    // Filter: Contracts expiring 30-180 days from today
    var filtered = contracts
        .Where(c => {
            if (!DateTime.TryParse(c.ExpirationDate, out var exp))
                return false;

            var daysToExpiry = (exp - today).TotalDays;
            return daysToExpiry >= 30 && daysToExpiry <= 180;
        })
        .OrderBy(c => c.StrikePrice) // Sort by strike for diversity
        .ToList();

    Console.WriteLine($"Filtered to {filtered.Count} contracts with 30-180 days to expiry (from {contracts.Count} total)");

    // If no contracts match the filter, fallback to all contracts
    if (filtered.Count == 0)
    {
        Console.WriteLine("No contracts in 30-180 day range, using all available contracts");
        filtered = contracts;
    }

    if (config.MaxContractsPerUnderlying > 0 && filtered.Count > config.MaxContractsPerUnderlying)
    {
        Console.WriteLine($"Further filtering to {config.MaxContractsPerUnderlying} contracts for diversity");

        // Take diverse sample: some calls, some puts, various strikes
        var calls = filtered.Where(c => c.ContractType.ToLower() == "call")
            .Take(config.MaxContractsPerUnderlying / 2).ToList();
        var puts = filtered.Where(c => c.ContractType.ToLower() == "put")
            .Take(config.MaxContractsPerUnderlying / 2).ToList();

        return calls.Concat(puts).ToList();
    }

    return filtered;
}

// Configuration class
class Configuration
{
    public string PolygonApiKey { get; set; } = string.Empty;
    public string DataDirectory { get; set; } = "../Data";
    public int CallsPerMinute { get; set; } = 5;
    public bool SkipExisting { get; set; } = true;
    public int MaxContractsPerUnderlying { get; set; } = 50; // Limit to avoid excessive API calls
}
