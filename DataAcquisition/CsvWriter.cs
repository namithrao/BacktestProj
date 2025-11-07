using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Shared;

namespace DataAcquisition;

/// <summary>
/// Handles writing bar data to CSV files
/// </summary>
public class CsvWriter
{
    private readonly string _dataDirectory;

    public CsvWriter(string dataDirectory)
    {
        _dataDirectory = dataDirectory;

        // Ensure data directory exists
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// Write bars to a CSV file for a specific ticker
    /// </summary>
    public async Task WriteBarsAsync(List<Bar> bars, string ticker)
    {
        if (bars.Count == 0)
        {
            Console.WriteLine($"No bars to write for {ticker}");
            return;
        }

        // Extract underlying ticker from option ticker (e.g., O:TSLA... -> TSLA)
        var underlying = ExtractUnderlying(ticker);
        var underlyingDir = Path.Combine(_dataDirectory, underlying);

        // Create underlying directory if it doesn't exist
        Directory.CreateDirectory(underlyingDir);

        var filePath = Path.Combine(underlyingDir, $"{ticker}.csv");

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvHelper.CsvWriter(writer, config);

        // Write header
        csv.WriteField("timestamp");
        csv.WriteField("open");
        csv.WriteField("high");
        csv.WriteField("low");
        csv.WriteField("close");
        csv.WriteField("volume");
        await csv.NextRecordAsync();

        // Write bars
        foreach (var bar in bars.OrderBy(b => b.Timestamp))
        {
            csv.WriteField(bar.Timestamp);
            csv.WriteField(bar.Open);
            csv.WriteField(bar.High);
            csv.WriteField(bar.Low);
            csv.WriteField(bar.Close);
            csv.WriteField(bar.Volume);
            await csv.NextRecordAsync();
        }

        Console.WriteLine($"Wrote {bars.Count} bars to {filePath}");
    }

    /// <summary>
    /// Check if a CSV file already exists for a ticker
    /// </summary>
    public bool FileExists(string ticker)
    {
        var underlying = ExtractUnderlying(ticker);
        var filePath = Path.Combine(_dataDirectory, underlying, $"{ticker}.csv");
        return File.Exists(filePath);
    }

    /// <summary>
    /// Get the number of bars already saved for a ticker
    /// </summary>
    public int GetBarCount(string ticker)
    {
        if (!FileExists(ticker))
            return 0;

        var underlying = ExtractUnderlying(ticker);
        var filePath = Path.Combine(_dataDirectory, underlying, $"{ticker}.csv");

        // Count lines in file (subtract 1 for header)
        return File.ReadLines(filePath).Count() - 1;
    }

    private string ExtractUnderlying(string ticker)
    {
        // Option tickers format: O:TSLA241115C00250000
        // Extract the underlying (TSLA)

        if (ticker.StartsWith("O:"))
        {
            var parts = ticker.Substring(2).Split(new[] { '2' }, 2); // Split on first '2' (year)
            return parts[0];
        }

        // Fallback: use the ticker as-is
        return ticker;
    }
}
