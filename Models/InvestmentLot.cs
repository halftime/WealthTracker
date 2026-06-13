using System.Globalization;
using System.Text.Json.Serialization;

namespace WealthTracker.Models;

public sealed class InvestmentLot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Ticker { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [JsonIgnore]
    public AssetDefinition Definition => AssetDefinition.ForTicker(Ticker);

    [JsonIgnore]
    public string UnitName => Definition.UnitName;

    [JsonIgnore]
    public string Title => $"{Ticker} - {Quantity.ToString("N4", CultureInfo.CurrentCulture)} {UnitName}";

    [JsonIgnore]
    public string Detail => $"Added {Date:yyyy-MM-dd}";
}
