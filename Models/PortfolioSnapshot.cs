namespace WealthTracker.Models;

public sealed record PortfolioSnapshot(
    IReadOnlyList<HoldingSnapshot> Holdings,
    IReadOnlyList<WealthPoint> WealthHistory,
    decimal TotalValue,
    decimal TotalInvested,
    DateTimeOffset GeneratedAt)
{
    public decimal GainLoss => TotalValue - TotalInvested;
    public decimal? GainLossPercent => TotalInvested > 0 ? GainLoss / TotalInvested : null;
}
