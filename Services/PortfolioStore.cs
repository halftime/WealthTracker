using System.Text.Json;
using Microsoft.Maui.Storage;
using WealthTracker.Models;

namespace WealthTracker.Services;

public sealed class PortfolioStore
{
    private const string InvestmentsKey = "wealthtracker.investments.v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public IReadOnlyList<InvestmentLot> Load()
    {
        try
        {
            var json = Preferences.Default.Get(InvestmentsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<InvestmentLot>();
            }

            return JsonSerializer.Deserialize<List<InvestmentLot>>(json, JsonOptions) ?? new List<InvestmentLot>();
        }
        catch
        {
            return Array.Empty<InvestmentLot>();
        }
    }

    public void Save(IEnumerable<InvestmentLot> investments)
    {
        var normalized = investments
            .Where(investment => investment.Quantity > 0 && !string.IsNullOrWhiteSpace(investment.Ticker))
            .OrderBy(investment => investment.Date)
            .ThenBy(investment => investment.Ticker)
            .ToList();

        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        Preferences.Default.Set(InvestmentsKey, json);
    }
}
