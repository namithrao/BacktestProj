using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Shared;

namespace Backtester;

/// <summary>
/// Loads bar data from CSV files
/// </summary>
public class CsvDataLoader
{
    private readonly string _dataDirectory;

    public CsvDataLoader(string dataDirectory)
    {
        _dataDirectory = dataDirectory;
    }

    /// <summary>
    /// Load all bars for specified tickers, sorted chronologically
    /// </summary>
    public async Task<List<Bar>> LoadBarsAsync(List<string> tickers)
    {
        var allBars = new List<Bar>();

        foreach (var ticker in tickers)
        {
            Console.WriteLine($"Loading data for {ticker}...");

            // Find all CSV files for this ticker (in the ticker's subdirectory)
            var tickerDir = Path.Combine(_dataDirectory, ticker);

            if (!Directory.Exists(tickerDir))
            {
                Console.WriteLine($"Warning: No data directory found for {ticker}");
                continue;
            }

            var csvFiles = Directory.GetFiles(tickerDir, "*.csv");
            Console.WriteLine($"Found {csvFiles.Length} contract files for {ticker}");

            foreach (var csvFile in csvFiles)
            {
                var contractBars = await LoadBarsFromFileAsync(csvFile);
                allBars.AddRange(contractBars);
            }
        }

        // Sort all bars chronologically
        allBars = allBars.OrderBy(b => b.Timestamp).ToList();

        Console.WriteLine($"Loaded {allBars.Count} total bars across all tickers");
        return allBars;
    }

    /// <summary>
    /// Load bars from a single CSV file
    /// </summary>
    private async Task<List<Bar>> LoadBarsFromFileAsync(string filePath)
    {
        var bars = new List<Bar>();
        var ticker = Path.GetFileNameWithoutExtension(filePath);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var bar = new Bar
            {
                Timestamp = csv.GetField<long>("timestamp"),
                Open = csv.GetField<double>("open"),
                High = csv.GetField<double>("high"),
                Low = csv.GetField<double>("low"),
                Close = csv.GetField<double>("close"),
                Volume = csv.GetField<long>("volume"),
                Ticker = ticker
            };

            bars.Add(bar);
        }

        return bars;
    }

    /// <summary>
    /// Get list of available tickers in the data directory
    /// </summary>
    public List<string> GetAvailableTickers()
    {
        if (!Directory.Exists(_dataDirectory))
            return new List<string>();

        return Directory.GetDirectories(_dataDirectory)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
    }

    /// <summary>
    /// Get list of contract files for a specific ticker
    /// </summary>
    public List<string> GetContractFiles(string ticker)
    {
        var tickerDir = Path.Combine(_dataDirectory, ticker);

        if (!Directory.Exists(tickerDir))
            return new List<string>();

        return Directory.GetFiles(tickerDir, "*.csv")
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
    }
}
