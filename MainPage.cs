using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls.Shapes;
using WealthTracker.Charts;
using WealthTracker.Models;
using WealthTracker.Services;

namespace WealthTracker;

public sealed class MainPage : ContentPage
{
    private readonly MarketDataClient _marketDataClient;
    private readonly PortfolioStore _store;
    private readonly PortfolioCalculator _calculator;
    private readonly ObservableCollection<HoldingSnapshot> _holdings = new();
    private readonly ObservableCollection<InvestmentLot> _investments = new();
    private readonly PortfolioPieDrawable _pieDrawable = new();
    private readonly WealthLineDrawable _lineDrawable = new();

    private readonly Picker _tickerPicker = new()
    {
        Title = "Ticker",
        TitleColor = Color.FromArgb("#94A3B8"),
        TextColor = Colors.White,
    };
    private readonly Entry _quantityEntry = new()
    {
        Placeholder = "0.00",
        PlaceholderColor = Color.FromArgb("#94A3B8"),
        Keyboard = Keyboard.Numeric,
        TextColor = Colors.White,
    };
    private readonly DatePicker _datePicker = new()
    {
        MaximumDate = DateTime.Today,
        Date = DateTime.Today,
        TextColor = Colors.White,
    };
    private readonly Label _unitLabel = MutedLabel("shares");
    private readonly Label _assetPreview = MutedLabel("Select a ticker.");
    private readonly Label _statusLabel = MutedLabel("Ready.");
    private readonly Label _totalValueLabel = ValueLabel("0.00");
    private readonly Label _investedLabel = ValueLabel("0.00");
    private readonly Label _gainLossLabel = ValueLabel("0.00");
    private readonly Label _updatedLabel = MutedLabel("--");
    private readonly Label _overviewHoldingsLabel = MutedLabel("No holdings yet.");
    private readonly Label _overviewTopHoldingLabel = MutedLabel("Top holding --");
    private readonly Label _overviewInvestmentsLabel = MutedLabel("0 investment lots");
    private readonly Label _overviewStatusLabel = MutedLabel("Ready.");
    private readonly ActivityIndicator _busy = new() { Color = Color.FromArgb("#38BDF8") };
    private readonly GraphicsView _pieView;
    private readonly GraphicsView _lineView;

    private bool _loaded;

    public MainPage(MarketDataClient marketDataClient, PortfolioStore store, PortfolioCalculator calculator)
    {
        _marketDataClient = marketDataClient;
        _store = store;
        _calculator = calculator;
        _pieView = new GraphicsView { Drawable = _pieDrawable, HeightRequest = 250 };
        _lineView = new GraphicsView { Drawable = _lineDrawable, HeightRequest = 260 };

        Title = "Wealth Tracker";
        BackgroundColor = Color.FromArgb("#101214");
        BuildPage();
        LoadInvestments();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await RefreshAsync();
        await UpdateAssetPreviewAsync();
    }

    private void BuildPage()
    {
        _tickerPicker.ItemsSource = AssetDefinition.Supported.ToList();
        _tickerPicker.ItemDisplayBinding = new Binding(nameof(AssetDefinition.DisplayName));
        _tickerPicker.SelectedIndex = 0;
        _tickerPicker.SelectedIndexChanged += async (_, _) =>
        {
            UpdateUnitLabel();
            await UpdateAssetPreviewAsync();
        };
        UpdateUnitLabel();

        var breakdownPage = BuildSubPage("Allocation", BuildAssetBreakdownContent());
        var wealthPage = BuildSubPage("Chart", BuildWealthHistoryContent());
        var investmentsPage = BuildSubPage("Buy/Sell", BuildInvestmentsContent());

        var refreshButton = PrimaryButton("Refresh prices");
        refreshButton.Clicked += async (_, _) => await RefreshAsync();

        Title = "Overview";
        Content = BuildOverviewContent(
            refreshButton,
            breakdownPage,
            wealthPage,
            investmentsPage);
    }

    private View BuildHeader(Button refreshButton)
    {
        var actions = new HorizontalStackLayout
        {
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            Children = { _busy, refreshButton },
        };
        Grid.SetColumn(actions, 1);

        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Label
                        {
                            Text = "Wealth Tracker",
                            FontSize = 30,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Colors.White,
                        },
                        MutedLabel("V3AA, VWCE, IWDA, SXRS, VOLT, XAU, XAG, and XPT via ignc.dev prices."),
                    },
                },
                actions,
            },
        };
    }

    private View BuildSummaryGrid()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 12,
        };

        grid.Add(SummaryCard("Current value", _totalValueLabel), 0, 0);
        grid.Add(SummaryCard("Cost basis", _investedLabel), 1, 0);
        grid.Add(SummaryCard("Gain / loss", _gainLossLabel, _updatedLabel), 2, 0);
        return grid;
    }

    private View BuildOverviewContent(
        Button refreshButton,
        Page breakdownPage,
        Page wealthPage,
        Page investmentsPage)
    {
        var breakdownButton = SecondaryButton("Allocation");
        breakdownButton.Clicked += async (_, _) => await Navigation.PushAsync(breakdownPage);

        var wealthButton = SecondaryButton("Chart");
        wealthButton.Clicked += async (_, _) => await Navigation.PushAsync(wealthPage);

        var investmentsButton = PrimaryButton("Buy/sell");
        investmentsButton.Clicked += async (_, _) => await Navigation.PushAsync(investmentsPage);

        return PageStack(
            BuildHeader(refreshButton),
            Card("Actions", new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    investmentsButton,
                    breakdownButton,
                    wealthButton,
                },
            }),
            BuildSummaryGrid(),
            Card("Small overview", new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    _overviewHoldingsLabel,
                    _overviewTopHoldingLabel,
                    _overviewInvestmentsLabel,
                    _overviewStatusLabel,
                },
            }));
    }

    private View BuildAssetBreakdownContent()
    {
        return PageStack(
            Card("Asset breakdown", _pieView),
            Card("Holdings", BuildHoldingsView()));
    }

    private View BuildWealthHistoryContent()
    {
        return PageStack(Card("Wealth over time", _lineView));
    }

    private View BuildInvestmentsContent()
    {
        var addButton = PrimaryButton("Add investment");
        addButton.Clicked += async (_, _) => await AddInvestmentAsync();

        var clearButton = SecondaryButton("Clear");
        clearButton.Clicked += async (_, _) => await ClearPortfolioAsync();

        return PageStack(
            Card("Add investment", new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(GridLength.Star),
                            new ColumnDefinition(GridLength.Star),
                        },
                        ColumnSpacing = 12,
                        Children =
                        {
                            Field("Asset", _tickerPicker, 0, 0),
                            Field("Date", _datePicker, 1, 0),
                        },
                    },
                    Field("Shares or troy ounces", _quantityEntry),
                    _unitLabel,
                    _assetPreview,
                    new HorizontalStackLayout { Spacing = 10, Children = { addButton, clearButton } },
                    _statusLabel,
                },
            }),
            Card("Investments", BuildInvestmentsView()));
    }

    private static ContentPage BuildSubPage(string title, View content)
    {
        return new ContentPage
        {
            Title = title,
            BackgroundColor = Color.FromArgb("#101214"),
            Content = new ScrollView
            {
                Content = content,
            },
        };
    }

    private static VerticalStackLayout PageStack(params View[] children)
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 18),
            Spacing = 16,
        };

        foreach (var child in children)
        {
            stack.Children.Add(child);
        }

        return stack;
    }

    private CollectionView BuildHoldingsView()
    {
        return new CollectionView
        {
            ItemsSource = _holdings,
            EmptyView = MutedLabel("No holdings yet."),
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.White, FontSize = 15 };
                title.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.Title));

                var detail = MutedLabel("");
                detail.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.DetailText));

                var quantity = MutedLabel("");
                quantity.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.QuantityText));

                var value = new Label { FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#86EFAC"), HorizontalTextAlignment = TextAlignment.End };
                value.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.ValueText));

                var allocation = MutedLabel("");
                allocation.HorizontalTextAlignment = TextAlignment.End;
                allocation.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.AllocationText));

                var gain = MutedLabel("");
                gain.HorizontalTextAlignment = TextAlignment.End;
                gain.SetBinding(Label.TextProperty, nameof(HoldingSnapshot.GainLossText));

                var grid = new Grid
                {
                    Padding = new Thickness(0, 10),
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    RowDefinitions =
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                    },
                    ColumnSpacing = 12,
                };

                grid.Add(title, 0, 0);
                grid.Add(value, 1, 0);
                grid.Add(detail, 0, 1);
                grid.Add(allocation, 1, 1);
                grid.Add(quantity, 0, 2);
                grid.Add(gain, 1, 2);

                return grid;
            }),
        };
    }

    private CollectionView BuildInvestmentsView()
    {
        return new CollectionView
        {
            ItemsSource = _investments,
            EmptyView = MutedLabel("Add a fund share purchase or precious-metal troy-ounce purchase."),
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
                title.SetBinding(Label.TextProperty, nameof(InvestmentLot.Title));

                var detail = MutedLabel("");
                detail.SetBinding(Label.TextProperty, nameof(InvestmentLot.Detail));

                var delete = SecondaryButton("Delete");
                delete.Clicked += async (sender, _) =>
                {
                    if (sender is Button { BindingContext: InvestmentLot lot })
                    {
                        _investments.Remove(lot);
                        SaveInvestments();
                        await RefreshAsync();
                    }
                };

                var grid = new Grid
                {
                    Padding = new Thickness(0, 8),
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto),
                    },
                    RowDefinitions =
                    {
                        new RowDefinition(GridLength.Auto),
                        new RowDefinition(GridLength.Auto),
                    },
                    ColumnSpacing = 12,
                };

                grid.Add(title, 0, 0);
                grid.Add(detail, 0, 1);
                grid.Add(delete, 1, 0);
                Grid.SetRowSpan(delete, 2);
                return grid;
            }),
        };
    }

    private async Task AddInvestmentAsync()
    {
        if (_tickerPicker.SelectedItem is not AssetDefinition definition)
        {
            _statusLabel.Text = "Select an asset first.";
            return;
        }

        if (!decimal.TryParse(_quantityEntry.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity)
            && !decimal.TryParse(_quantityEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
        {
            _statusLabel.Text = "Enter a valid quantity.";
            return;
        }

        if (quantity <= 0)
        {
            _statusLabel.Text = "Quantity must be greater than zero.";
            return;
        }

        var date = DateOnly.FromDateTime(_datePicker.Date ?? DateTime.Today);
        _investments.Add(new InvestmentLot
        {
            Ticker = definition.Ticker,
            Quantity = quantity,
            Date = date,
        });
        _quantityEntry.Text = string.Empty;
        SaveInvestments();
        await RefreshAsync();
    }

    private async Task ClearPortfolioAsync()
    {
        _investments.Clear();
        SaveInvestments();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _busy.IsRunning = true;
        _busy.IsVisible = true;
        try
        {
            var snapshot = await _calculator.BuildSnapshotAsync(_investments.ToArray());
            Replace(_holdings, snapshot.Holdings);

            _totalValueLabel.Text = FormatNumber(snapshot.TotalValue);
            _investedLabel.Text = FormatNumber(snapshot.TotalInvested);
            _gainLossLabel.Text = $"{FormatSigned(snapshot.GainLoss)} ({FormatPercent(snapshot.GainLossPercent)})";
            _gainLossLabel.TextColor = snapshot.GainLoss >= 0 ? Color.FromArgb("#86EFAC") : Color.FromArgb("#FCA5A5");
            _updatedLabel.Text = $"Updated {snapshot.GeneratedAt.LocalDateTime:t}";

            _pieDrawable.Holdings = snapshot.Holdings;
            _lineDrawable.Points = snapshot.WealthHistory;
            _pieView.Invalidate();
            _lineView.Invalidate();

            var statusText = snapshot.Holdings.Count == 0
                ? "Add an investment to start tracking."
                : $"Loaded {snapshot.Holdings.Count} holdings and {snapshot.WealthHistory.Count} wealth points.";
            _statusLabel.Text = statusText;
            _overviewStatusLabel.Text = statusText;
            _overviewInvestmentsLabel.Text = _investments.Count == 1
                ? "1 investment lot"
                : $"{_investments.Count} investment lots";

            var topHolding = snapshot.Holdings.FirstOrDefault();
            _overviewHoldingsLabel.Text = snapshot.Holdings.Count == 0
                ? "No holdings yet."
                : $"{snapshot.Holdings.Count} holdings tracked.";
            _overviewTopHoldingLabel.Text = topHolding is null
                ? "Top holding --"
                : $"Top holding {topHolding.Ticker}: {topHolding.ValueText} ({topHolding.AllocationText})";
        }
        catch (Exception error)
        {
            _statusLabel.Text = $"Refresh failed: {error.Message}";
            _overviewStatusLabel.Text = _statusLabel.Text;
        }
        finally
        {
            _busy.IsRunning = false;
            _busy.IsVisible = false;
        }
    }

    private async Task UpdateAssetPreviewAsync()
    {
        if (_tickerPicker.SelectedItem is not AssetDefinition definition)
        {
            return;
        }

        try
        {
            _assetPreview.Text = "Loading asset details...";
            var details = await _marketDataClient.GetFundDetailsAsync(definition.Ticker);
            var prices = await _marketDataClient.GetPricesAsync(definition.Ticker);
            var latest = prices.Latest;
            var latestText = latest is null
                ? "No price rows returned."
                : $"Latest price {FormatNumber(latest.Price)} on {latest.Date:yyyy-MM-dd}.";

            _assetPreview.Text = $"{details.Name} - {details.Subtitle}\n{latestText}";
        }
        catch (Exception error)
        {
            _assetPreview.Text = $"{definition.Name}. Details unavailable: {error.Message}";
        }
    }

    private void LoadInvestments()
    {
        foreach (var investment in _store.Load())
        {
            _investments.Add(investment);
        }
    }

    private void SaveInvestments()
    {
        _store.Save(_investments);
    }

    private void UpdateUnitLabel()
    {
        if (_tickerPicker.SelectedItem is AssetDefinition definition)
        {
            _unitLabel.Text = definition.IsPreciousMetal
                ? "Enter troy ounces for this metal."
                : "Enter share count for this fund.";
        }
    }

    private static View Field(string label, View control, int column = 0, int row = 0)
    {
        var layout = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new Label { Text = label, TextColor = Color.FromArgb("#94A3B8"), FontSize = 12 },
                control,
            },
        };
        Grid.SetColumn(layout, column);
        Grid.SetRow(layout, row);
        return layout;
    }

    private static Border Card(string title, View content)
    {
        return new Border
        {
            Padding = 16,
            Stroke = Color.FromArgb("#38312C"),
            BackgroundColor = Color.FromArgb("#1B1B1F"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.White,
                        FontSize = 18,
                    },
                    content,
                },
            },
        };
    }

    private static Border SummaryCard(string title, Label value, Label? caption = null)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = title, TextColor = Color.FromArgb("#94A3B8"), FontSize = 12 },
                value,
            },
        };

        if (caption is not null)
        {
            stack.Children.Add(caption);
        }

        return new Border
        {
            Padding = 16,
            Stroke = Color.FromArgb("#38312C"),
            BackgroundColor = Color.FromArgb("#1B1B1F"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = stack,
        };
    }

    private static Button PrimaryButton(string text)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#38BDF8"),
            TextColor = Color.FromArgb("#082F49"),
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
        };
    }

    private static Button SecondaryButton(string text)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#2A2522"),
            TextColor = Colors.White,
            CornerRadius = 12,
        };
    }

    private static Label ValueLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontAttributes = FontAttributes.Bold,
            FontSize = 22,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation,
        };
    }

    private static Label MutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            TextColor = Color.FromArgb("#94A3B8"),
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap,
        };
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private static string FormatNumber(decimal value)
    {
        return value.ToString("N2", CultureInfo.CurrentCulture);
    }

    private static string FormatSigned(decimal value)
    {
        return value.ToString("+0.00;-0.00;0.00", CultureInfo.CurrentCulture);
    }

    private static string FormatPercent(decimal? value)
    {
        return value?.ToString("+0.0%;-0.0%;0.0%", CultureInfo.CurrentCulture) ?? "0.0%";
    }
}
