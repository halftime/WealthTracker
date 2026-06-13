using System.Globalization;
using System.Text.Json;
using WealthTracker.Models;

namespace WealthTracker.Services;

public sealed class MarketDataClient
{
    public const string DefaultBaseUrl = "http://ignc.dev:8080/";

    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, PriceSeries> _priceSeriesCache = new();
    private readonly Dictionary<string, FundDetails> _fundDetailsCache = new();

    public MarketDataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<FundDetails> GetFundDetailsAsync(string ticker, CancellationToken cancellationToken = default)
    {
        ticker = NormalizeTicker(ticker);
        var definition = AssetDefinition.ForTicker(ticker);

        if (_fundDetailsCache.TryGetValue(ticker, out var cachedDetails))
        {
            return cachedDetails;
        }

        try
        {
            using var response = await _httpClient.GetAsync($"fund/{ticker}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return FundDetails.FromDefinition(definition, $"fund/{ticker} returned {(int)response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(body))
            {
                return FundDetails.FromDefinition(definition, $"fund/{ticker} returned an empty body");
            }

            using var document = JsonDocument.Parse(body);
            var element = UnwrapObject(document.RootElement);
            if (element.ValueKind != JsonValueKind.Object)
            {
                return FundDetails.FromDefinition(definition, $"fund/{ticker} did not return an object");
            }

            var metadata = FlattenObject(element);
            var name = FirstString(element, "name", "longName", "shortName", "description", "fundName")
                ?? definition.Name;
            var category = FirstString(element, "category", "assetClass", "type")
                ?? definition.Category;
            var currency = FirstString(element, "currency", "quoteCurrency", "priceCurrency");

            var details = new FundDetails(ticker, name, category, definition.UnitName, currency, metadata);
            _fundDetailsCache[ticker] = details;
            return details;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException)
        {
            return FundDetails.FromDefinition(definition, error.Message);
        }
    }

    public async Task<PriceSeries> GetPricesAsync(string ticker, CancellationToken cancellationToken = default)
    {
        ticker = NormalizeTicker(ticker);
        
        if (_priceSeriesCache.TryGetValue(ticker, out var cachedSeries))
        {
            return cachedSeries;
        }

        using var response = await _httpClient.GetAsync($"prices/{ticker}", cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return new PriceSeries(ticker, Array.Empty<PricePoint>());
        }

        var points = new List<PricePoint>();
        foreach (var row in document.RootElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var rowTicker = FirstString(row, "symbol", "ticker") ?? ticker;
            var dateText = FirstString(row, "date", "time", "timestamp");
            var price = FirstDecimal(row, "price", "close", "value", "nav");

            if (dateText is null || price is null)
            {
                continue;
            }

            if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                continue;
            }

            points.Add(new PricePoint(NormalizeTicker(rowTicker), date, price.Value));
        }

        var series = new PriceSeries(ticker, points);
        _priceSeriesCache[ticker] = series;
        return series;
    }

    private static string NormalizeTicker(string ticker)
    {
        return ticker.Trim().ToUpperInvariant();
    }

    private static JsonElement UnwrapObject(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().FirstOrDefault();
        }

        return element;
    }

    private static string? FirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static decimal? FirstDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!element.TryGetProperty(propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String
                && decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number))
            {
                return number;
            }
        }

        return null;
    }

    private static Dictionary<string, string> FlattenObject(JsonElement element)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            {
                continue;
            }

            output[property.Name] = property.Value.ToString();
        }

        return output;
    }
}
