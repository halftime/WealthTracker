using WealthTracker.Models;

namespace WealthTracker.Services;

public sealed class PortfolioCalculator
{
    private readonly MarketDataClient _marketDataClient;

    public PortfolioCalculator(MarketDataClient marketDataClient)
    {
        _marketDataClient = marketDataClient;
    }

    public async Task<PortfolioSnapshot> BuildSnapshotAsync(
        IEnumerable<InvestmentLot> investments,
        CancellationToken cancellationToken = default)
    {
        var lots = investments
            .Where(investment => investment.Quantity > 0)
            .Select(investment => new InvestmentLot
            {
                Id = investment.Id,
                Ticker = investment.Ticker.Trim().ToUpperInvariant(),
                Quantity = investment.Quantity,
                Date = investment.Date,
            })
            .OrderBy(investment => investment.Date)
            .ToArray();

        if (lots.Length == 0)
        {
            return new PortfolioSnapshot(
                Array.Empty<HoldingSnapshot>(),
                Array.Empty<WealthPoint>(),
                0,
                0,
                DateTimeOffset.Now);
        }

        var tickers = lots.Select(investment => investment.Ticker).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var details = new Dictionary<string, FundDetails>(StringComparer.OrdinalIgnoreCase);
        var series = new Dictionary<string, PriceSeries>(StringComparer.OrdinalIgnoreCase);

        foreach (var ticker in tickers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            details[ticker] = await _marketDataClient.GetFundDetailsAsync(ticker, cancellationToken);
            series[ticker] = await _marketDataClient.GetPricesAsync(ticker, cancellationToken);
        }

        var totalValue = 0m;
        var totalInvested = 0m;
        var holdings = new List<HoldingSnapshot>();

        foreach (var ticker in tickers)
        {
            var assetLots = lots.Where(investment => investment.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase)).ToArray();
            var quantity = assetLots.Sum(investment => investment.Quantity);
            var assetSeries = series[ticker];
            var latest = assetSeries.Latest;
            if (latest is null || quantity <= 0)
            {
                continue;
            }

            var investedValue = assetLots.Sum(investment =>
            {
                var purchasePrice = assetSeries.PriceNearest(investment.Date)?.Price ?? latest.Price;
                return investment.Quantity * purchasePrice;
            });

            var currentValue = quantity * latest.Price;
            totalValue += currentValue;
            totalInvested += investedValue;

            var definition = AssetDefinition.ForTicker(ticker);
            var detail = details[ticker];
            holdings.Add(new HoldingSnapshot
            {
                Ticker = ticker,
                Name = detail.Name,
                Category = detail.Category,
                UnitName = definition.UnitName,
                ColorHex = definition.ColorHex,
                Quantity = quantity,
                CurrentPrice = latest.Price,
                LatestPriceDate = latest.Date,
                CurrentValue = currentValue,
                InvestedValue = investedValue,
            });
        }

        holdings = holdings
            .OrderByDescending(holding => holding.CurrentValue)
            .Select(holding => holding.WithAllocation(totalValue))
            .ToList();

        return new PortfolioSnapshot(
            holdings,
            BuildWealthHistory(lots, series),
            totalValue,
            totalInvested,
            DateTimeOffset.Now);
    }

    private static IReadOnlyList<WealthPoint> BuildWealthHistory(
        IReadOnlyList<InvestmentLot> lots,
        IReadOnlyDictionary<string, PriceSeries> series)
    {
        if (lots.Count == 0 || series.Count == 0)
        {
            return Array.Empty<WealthPoint>();
        }

        var firstInvestmentDate = lots.Min(investment => investment.Date);
        var dates = series.Values
            .SelectMany(item => item.Points.Select(point => point.Date))
            .Where(date => date >= firstInvestmentDate)
            .Distinct()
            .OrderBy(date => date)
            .ToArray();

        var history = new List<WealthPoint>(dates.Length);
        foreach (var date in dates)
        {
            var value = 0m;
            foreach (var group in lots.GroupBy(investment => investment.Ticker, StringComparer.OrdinalIgnoreCase))
            {
                if (!series.TryGetValue(group.Key, out var assetSeries))
                {
                    continue;
                }

                var quantity = group
                    .Where(investment => investment.Date <= date)
                    .Sum(investment => investment.Quantity);
                if (quantity <= 0)
                {
                    continue;
                }

                var price = assetSeries.PriceOnOrBefore(date)?.Price;
                if (price is not null)
                {
                    value += quantity * price.Value;
                }
            }

            if (value > 0)
            {
                history.Add(new WealthPoint(date, value));
            }
        }

        return history;
    }
}

file static class HoldingSnapshotExtensions
{
    public static HoldingSnapshot WithAllocation(this HoldingSnapshot holding, decimal totalValue)
    {
        return new HoldingSnapshot
        {
            Ticker = holding.Ticker,
            Name = holding.Name,
            Category = holding.Category,
            UnitName = holding.UnitName,
            ColorHex = holding.ColorHex,
            Quantity = holding.Quantity,
            CurrentPrice = holding.CurrentPrice,
            LatestPriceDate = holding.LatestPriceDate,
            CurrentValue = holding.CurrentValue,
            InvestedValue = holding.InvestedValue,
            Allocation = totalValue > 0 ? holding.CurrentValue / totalValue : 0,
        };
    }
}
