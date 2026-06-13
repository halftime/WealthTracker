using System.Globalization;

namespace WealthTracker.Models;

public sealed class HoldingSnapshot
{
    public string Ticker { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public string ColorHex { get; init; } = "#64748B";
    public decimal Quantity { get; init; }
    public decimal CurrentPrice { get; init; }
    public DateOnly LatestPriceDate { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal InvestedValue { get; init; }
    public decimal GainLoss => CurrentValue - InvestedValue;
    public decimal? GainLossPercent => InvestedValue > 0 ? GainLoss / InvestedValue : null;
    public decimal Allocation { get; init; }

    public string Title => $"{Ticker} - {Name}";
    public string QuantityText => $"{Quantity.ToString("N4", CultureInfo.CurrentCulture)} {UnitName}";
    public string ValueText => CurrentValue.ToString("N2", CultureInfo.CurrentCulture);
    public string DetailText => $"Latest {CurrentPrice.ToString("N4", CultureInfo.CurrentCulture)} on {LatestPriceDate:yyyy-MM-dd}";
    public string AllocationText => Allocation.ToString("P1", CultureInfo.CurrentCulture);
    public string GainLossText => $"{GainLoss.ToString("+0.00;-0.00;0.00", CultureInfo.CurrentCulture)} ({(GainLossPercent ?? 0).ToString("+0.0%;-0.0%;0.0%", CultureInfo.CurrentCulture)})";
}
