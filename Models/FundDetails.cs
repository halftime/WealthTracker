namespace WealthTracker.Models;

public sealed record FundDetails(
    string Ticker,
    string Name,
    string Category,
    string UnitName,
    string? Currency,
    IReadOnlyDictionary<string, string> Metadata)
{
    public string Subtitle
    {
        get
        {
            var currency = string.IsNullOrWhiteSpace(Currency) ? "price currency from API" : Currency;
            return $"{Category} - {UnitName} - {currency}";
        }
    }

    public static FundDetails FromDefinition(AssetDefinition definition, string? apiStatus = null)
    {
        var metadata = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(apiStatus))
        {
            metadata["api_status"] = apiStatus;
        }

        return new FundDetails(
            definition.Ticker,
            definition.Name,
            definition.Category,
            definition.UnitName,
            null,
            metadata);
    }
}
