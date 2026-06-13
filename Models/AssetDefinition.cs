namespace WealthTracker.Models;

public sealed record AssetDefinition(
    string Ticker,
    string Name,
    string Category,
    string UnitName,
    string ColorHex)
{
    public string DisplayName => $"{Ticker} - {Name}";
    public bool IsPreciousMetal => Category == PreciousMetalsCategory;

    public const string FundsCategory = "Funds";
    public const string PreciousMetalsCategory = "Precious metals";

    public static readonly IReadOnlyList<AssetDefinition> Supported = new[]
    {
        new AssetDefinition("V3AA", "Vanguard ESG Global All Cap", FundsCategory, "shares", "#2F80ED"),
        new AssetDefinition("VWCE", "Vanguard FTSE All-World", FundsCategory, "shares", "#27AE60"),
        new AssetDefinition("IWDA", "iShares Core MSCI World", FundsCategory, "shares", "#9B51E0"),
        new AssetDefinition("SXRS", "iShares Diversified Commodity Swap", FundsCategory, "shares", "#F2994A"),
        new AssetDefinition("VOLT", "WisdomTree Battery Solutions", FundsCategory, "shares", "#EB5757"),
        new AssetDefinition("XAU", "Gold", PreciousMetalsCategory, "troy ounces", "#D4AF37"),
        new AssetDefinition("XAG", "Silver", PreciousMetalsCategory, "troy ounces", "#A7B1BC"),
        new AssetDefinition("XPT", "Platinum", PreciousMetalsCategory, "troy ounces", "#7F8FA6"),
    };

    public static AssetDefinition ForTicker(string ticker)
    {
        return Supported.FirstOrDefault(asset => asset.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
            ?? new AssetDefinition(ticker.ToUpperInvariant(), ticker.ToUpperInvariant(), FundsCategory, "shares", "#64748B");
    }
}
