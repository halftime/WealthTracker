namespace WealthTracker.Models;

public sealed class PriceSeries
{
    public PriceSeries(string ticker, IEnumerable<PricePoint> points)
    {
        Ticker = ticker;
        Points = points
            .Where(point => point.Price > 0)
            .OrderBy(point => point.Date)
            .ToArray();
    }

    public string Ticker { get; }
    public IReadOnlyList<PricePoint> Points { get; }
    public PricePoint? Latest => Points.Count == 0 ? null : Points[^1];

    public PricePoint? PriceOnOrBefore(DateOnly date)
    {
        PricePoint? last = null;
        foreach (var point in Points)
        {
            if (point.Date > date)
            {
                break;
            }

            last = point;
        }

        return last;
    }

    public PricePoint? PriceOnOrAfter(DateOnly date)
    {
        return Points.FirstOrDefault(point => point.Date >= date);
    }

    public PricePoint? PriceNearest(DateOnly date)
    {
        return PriceOnOrBefore(date) ?? PriceOnOrAfter(date);
    }
}
