using Microsoft.Extensions.DependencyInjection;
using WealthTracker.Services;

namespace WealthTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(MarketDataClient.DefaultBaseUrl),
            Timeout = TimeSpan.FromSeconds(20),
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WealthTracker/1.0");

        builder.Services.AddSingleton(httpClient);
        builder.Services.AddSingleton<MarketDataClient>();
        builder.Services.AddSingleton<PortfolioStore>();
        builder.Services.AddSingleton<PortfolioCalculator>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
