using System.Text.Json;
using System.Text.Json.Serialization;
using Shared;

namespace DataAcquisition;

/// <summary>
/// Client for interacting with Polygon.io API
/// </summary>
public class PolygonClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly int _callsPerMinute;

    public PolygonClient(string apiKey, int callsPerMinute = 5)
    {
        _apiKey = apiKey;
        _callsPerMinute = callsPerMinute;
        _rateLimiter = new SemaphoreSlim(callsPerMinute, callsPerMinute);

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.polygon.io")
        };
    }

    /// <summary>
    /// Get all options contracts for a given underlying ticker
    /// </summary>
    public async Task<List<OptionsContract>> GetOptionsContractsAsync(
        string underlying,
        DateTime? expirationDateGte = null,
        DateTime? expirationDateLte = null)
    {
        var contracts = new List<OptionsContract>();
        string? nextUrl = null;

        do
        {
            await RateLimitAsync();

            var url = nextUrl ?? BuildContractsUrl(underlying, expirationDateGte, expirationDateLte);

            Console.WriteLine($"Fetching contracts from: {url}");

            var response = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<OptionsContractsResponse>(response);

            if (result?.Results != null)
            {
                contracts.AddRange(result.Results);
                Console.WriteLine($"Fetched {result.Results.Count} contracts. Total so far: {contracts.Count}");
            }

            nextUrl = result?.NextUrl;
            if (!string.IsNullOrEmpty(nextUrl))
            {
                nextUrl += $"&apiKey={_apiKey}";
            }

        } while (!string.IsNullOrEmpty(nextUrl));

        return contracts;
    }

    /// <summary>
    /// Get minute-level OHLC bars for a specific option contract (with retry logic for rate limits)
    /// </summary>
    public async Task<List<Bar>> GetMinuteBarsAsync(
        string optionTicker,
        DateTime fromDate,
        DateTime toDate,
        int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await RateLimitAsync();

                var from = fromDate.ToString("yyyy-MM-dd");
                var to = toDate.ToString("yyyy-MM-dd");

                var url = $"/v2/aggs/ticker/{optionTicker}/range/1/minute/{from}/{to}?adjusted=true&sort=asc&limit=50000&apiKey={_apiKey}";

                if (attempt == 1)
                {
                    Console.WriteLine($"Fetching bars for {optionTicker} from {from} to {to}");
                }

                var response = await _httpClient.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<AggregatesResponse>(response);

                if (result?.Results == null || result.Results.Count == 0)
                {
                    Console.WriteLine($"No data returned for {optionTicker}");
                    return new List<Bar>();
                }

                var bars = result.Results.Select(r => new Bar
                {
                    Timestamp = r.T,
                    Open = r.O,
                    High = r.H,
                    Low = r.L,
                    Close = r.C,
                    Volume = r.V,
                    Ticker = optionTicker
                }).ToList();

                Console.WriteLine($"Fetched {bars.Count} bars for {optionTicker}");
                return bars;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"Error fetching bars for {optionTicker} after {maxRetries} attempts: {ex.Message}");
                    return new List<Bar>();
                }

                var waitSeconds = attempt * 30; // 30, 60, 90 seconds exponential backoff
                Console.WriteLine($"  Rate limited (429), waiting {waitSeconds}s before retry {attempt}/{maxRetries}");
                await Task.Delay(waitSeconds * 1000);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error fetching bars for {optionTicker}: {ex.Message}");
                return new List<Bar>();
            }
        }

        return new List<Bar>();
    }

    private string BuildContractsUrl(string underlying, DateTime? expirationDateGte, DateTime? expirationDateLte)
    {
        var url = $"/v3/reference/options/contracts?underlying_ticker={underlying}&limit=1000";

        if (expirationDateGte.HasValue)
        {
            url += $"&expiration_date.gte={expirationDateGte.Value:yyyy-MM-dd}";
        }

        if (expirationDateLte.HasValue)
        {
            url += $"&expiration_date.lte={expirationDateLte.Value:yyyy-MM-dd}";
        }

        url += $"&apiKey={_apiKey}";
        return url;
    }

    private async Task RateLimitAsync()
    {
        await _rateLimiter.WaitAsync();

        // Simple synchronous delay: e.g., 5 calls/min = 12 sec between calls
        // Add 3-second buffer to avoid edge cases and API processing time
        var delayMs = (int)(60000.0 / _callsPerMinute) + 3000; // 15 seconds for 5 calls/min
        await Task.Delay(delayMs);

        _rateLimiter.Release();
    }
}

// Polygon.io API response models
public class OptionsContractsResponse
{
    [JsonPropertyName("results")]
    public List<OptionsContract> Results { get; set; } = new();

    [JsonPropertyName("next_url")]
    public string? NextUrl { get; set; }
}

public class OptionsContract
{
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;

    [JsonPropertyName("underlying_ticker")]
    public string UnderlyingTicker { get; set; } = string.Empty;

    [JsonPropertyName("expiration_date")]
    public string ExpirationDate { get; set; } = string.Empty;

    [JsonPropertyName("strike_price")]
    public double StrikePrice { get; set; }

    [JsonPropertyName("contract_type")]
    public string ContractType { get; set; } = string.Empty; // "call" or "put"
}

public class AggregatesResponse
{
    [JsonPropertyName("results")]
    public List<AggregateBar> Results { get; set; } = new();

    [JsonPropertyName("resultsCount")]
    public int ResultsCount { get; set; }
}

public class AggregateBar
{
    [JsonPropertyName("t")]
    public long T { get; set; } // timestamp

    [JsonPropertyName("o")]
    public double O { get; set; } // open

    [JsonPropertyName("h")]
    public double H { get; set; } // high

    [JsonPropertyName("l")]
    public double L { get; set; } // low

    [JsonPropertyName("c")]
    public double C { get; set; } // close

    [JsonPropertyName("v")]
    public long V { get; set; } // volume
}
