namespace WealthTracker.Models;

public sealed record PricePoint(string Ticker, DateOnly Date, decimal Price);
