# Wealth Tracker

.NET MAUI app for tracking a multi asset portfolio.

<p align="center">
  <img width="22%" src="https://github.com/user-attachments/assets/d0d8c7b1-b160-4ad9-a44a-e8462bb85b17" />
  <img width="22%" src="https://github.com/user-attachments/assets/1491a93f-f75d-4166-abae-da499128a777" />
  <img width="22%" src="https://github.com/user-attachments/assets/e2b716ae-fb3b-4bb7-b604-9fd5bf2b882d" />
  <img width="22%" src="https://github.com/user-attachments/assets/00c92b14-11be-47bd-874f-396c116af7bb" />
</p>

## Supported tickers

- Funds: `V3AA`, `VWCE`, `IWDA`, `SXRS`, `VOLT`
- Precious metals: `XAU`, `XAG`, `XPT`

The app reads market data with HTTP GET requests against:

- `http://ignc.dev:8080/fund/{ticker}` for optional investment/fund details.
- `http://ignc.dev:8080/prices/{ticker}` for daily price history.

`/fund/{ticker}` is treated as optional because it currently returns an empty 404 for `VWCE` from this environment. The app falls back to local names and still values the portfolio from `/prices/{ticker}`.

## Features

- Add an investment lot by ticker, quantity, and date.
- Uses shares for funds and troy ounces for metals.
- Persists investment lots with MAUI Preferences.
- Computes current value, cost basis, gain/loss, and holding allocation.
- Draws a portfolio breakdown pie chart with `GraphicsView`.
- Draws a wealth-over-time line chart from daily prices and dated lots.
- Enables cleartext HTTP for Android, iOS, and Mac Catalyst because the API URL is `http://`. (to be fixed to https only)

## Build

Install the .NET SDK and MAUI workloads, then run:

```bash
cd WealthTracker
dotnet workload install maui
dotnet build -f net10.0-maccatalyst
```

Verification in this workspace:

- `dotnet build -f net10.0-maccatalyst -p:TargetFrameworks=net10.0-maccatalyst -t:Compile` succeeds with 0 warnings and 0 errors.
- `dotnet build -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -t:Compile` succeeds with 0 warnings and 0 errors.
- Android debug packaging succeeds with OpenJDK 17 and Android SDK API 36:

```bash
JAVA_HOME="/path/to/jdk" \
ANDROID_SDK_ROOT="/path/to/android-sdk" \
dotnet build -f net10.0-android \
  -p:AndroidSdkDirectory="$ANDROID_SDK_ROOT" \
  -p:JavaSdkDirectory="$JAVA_HOME"
```

On Windows, `capture-android-screenshots.cmd` builds the Android app, deploys it to the emulator, opens the Overview, Allocation, Chart, and Buy/Sell screens, and saves PNG screenshots under `screenshots/`.
