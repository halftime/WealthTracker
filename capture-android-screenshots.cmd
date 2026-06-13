@echo off
setlocal EnableExtensions EnableDelayedExpansion

if /I "%~1"=="/?" goto :usage
if /I "%~1"=="-h" goto :usage
if /I "%~1"=="--help" goto :usage

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "AVD_NAME=%~1"
if not defined AVD_NAME set "AVD_NAME=WealthTracker_API_36"

if not defined ANDROID_SDK_ROOT if exist "%LOCALAPPDATA%\Android\Sdk" set "ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk"
if not defined JAVA_HOME call :detect_java_home

if not defined ANDROID_SDK_ROOT (
    echo ANDROID_SDK_ROOT is not set and no default SDK was found.
    exit /b 1
)

if not defined JAVA_HOME (
    echo JAVA_HOME is not set and no local JDK was found.
    exit /b 1
)

set "ADB=%ANDROID_SDK_ROOT%\platform-tools\adb.exe"
set "EMULATOR=%ANDROID_SDK_ROOT%\emulator\emulator.exe"
set "PROJECT=%ROOT%\WealthTracker.csproj"
set "APK=%ROOT%\bin\Debug\net10.0-android\com.enlilasko.wealthtracker-Signed.apk"
set "SCREENSHOT_DIR=%ROOT%\screenshot"
set "WINDOW_DUMP=%TEMP%\wealthtracker-window-dump.xml"
set "PACKAGE=com.enlilasko.wealthtracker"
set "ACTIVITY=crc64573c98363ff5d3c0.MainActivity"

if not exist "%ADB%" (
    echo adb.exe was not found at "%ADB%".
    exit /b 1
)

if not exist "%EMULATOR%" (
    echo emulator.exe was not found at "%EMULATOR%".
    exit /b 1
)

if not exist "%SCREENSHOT_DIR%" mkdir "%SCREENSHOT_DIR%"

call :ensure_device || exit /b 1

echo Building Android package...
dotnet build "%PROJECT%" -f net10.0-android -p:AndroidSdkDirectory="%ANDROID_SDK_ROOT%" -p:JavaSdkDirectory="%JAVA_HOME%" -p:EmbedAssembliesIntoApk=true -p:AndroidFastDeploymentType=None
if errorlevel 1 exit /b 1

if not exist "%APK%" (
    echo Expected APK not found at "%APK%".
    exit /b 1
)

echo Installing APK...
"%ADB%" install -r "%APK%"
if errorlevel 1 exit /b 1

echo Launching app...
"%ADB%" shell am start -n %PACKAGE%/%ACTIVITY%
if errorlevel 1 exit /b 1

call :wait_for_text "Actions" || exit /b 1

call :capture_screen overview.png || exit /b 1

call :open_page "Allocation" "Asset breakdown" || exit /b 1
call :capture_screen allocation.png || exit /b 1
call :go_back || exit /b 1

call :open_page "Chart" "Wealth over time" || exit /b 1
call :capture_screen chart.png || exit /b 1
call :go_back || exit /b 1

call :open_page "Buy/sell" "Add investment" || exit /b 1
call :capture_screen buy-sell.png || exit /b 1

echo Screenshots saved to "%SCREENSHOT_DIR%".
exit /b 0

:usage
echo Usage: %~nx0 [AVD_NAME]
echo Builds the Android app, deploys it to an emulator, opens the app pages, and saves screenshots in the screenshot folder.
exit /b 0

:detect_java_home
for /d %%D in ("%LOCALAPPDATA%\Java\jdk-*") do (
    if exist "%%~fD\bin\java.exe" (
        set "JAVA_HOME=%%~fD"
        goto :eof
    )
)
for /d %%D in ("%ProgramFiles%\Microsoft\jdk-*") do (
    if exist "%%~fD\bin\java.exe" (
        set "JAVA_HOME=%%~fD"
        goto :eof
    )
)
goto :eof

:ensure_device
"%ADB%" get-state 1>nul 2>nul
if errorlevel 1 (
    echo Starting emulator "%AVD_NAME%"...
    start "WealthTracker Emulator" "%EMULATOR%" -avd "%AVD_NAME%"
)

echo Waiting for emulator device...
"%ADB%" wait-for-device
if errorlevel 1 exit /b 1

echo Waiting for Android boot completion...
set "BOOT_COMPLETED="
for /l %%I in (1,1,60) do (
    for /f "usebackq delims=" %%B in (`"%ADB%" shell getprop sys.boot_completed`) do set "BOOT_COMPLETED=%%B"
    if "!BOOT_COMPLETED!"=="1" goto :device_ready
    call :wait_seconds 2
)

echo Emulator did not report boot completion in time.
exit /b 1

:device_ready
call :wait_seconds 3
exit /b 0

:wait_seconds
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Sleep -Seconds %~1"
exit /b 0

:wait_for_text
set "TARGET_TEXT=%~1"
for /l %%I in (1,1,20) do (
    call :dump_window
    powershell -NoProfile -ExecutionPolicy Bypass -Command "$dump = [Environment]::GetEnvironmentVariable('WINDOW_DUMP'); $target = [Environment]::GetEnvironmentVariable('TARGET_TEXT'); [xml]$xml = Get-Content -LiteralPath $dump; $node = $xml.SelectNodes('//node') | Where-Object { $_.text -eq $target -or $_.'content-desc' -eq $target } | Select-Object -First 1; if ($node) { exit 0 } exit 1"
    if not errorlevel 1 exit /b 0
    call :wait_seconds 2
)

echo Timed out waiting for "%TARGET_TEXT%".
exit /b 1

:dump_window
for /l %%I in (1,1,5) do (
    "%ADB%" shell uiautomator dump /sdcard/window_dump.xml 1>nul 2>nul
    if not errorlevel 1 (
        "%ADB%" pull /sdcard/window_dump.xml "%WINDOW_DUMP%" 1>nul 2>nul
        if not errorlevel 1 exit /b 0
    )
    "%ADB%" shell input keyevent 82 1>nul 2>nul
    call :wait_seconds 1
)

echo Unable to dump the Android UI hierarchy.
exit /b 1

:open_page
set "TARGET_TEXT=%~1"
set "DESTINATION_TEXT=%~2"
call :wait_for_text "%TARGET_TEXT%" || exit /b 1
echo Opening %TARGET_TEXT% page...
call :tap_text "%TARGET_TEXT%" || exit /b 1
call :wait_for_text "%DESTINATION_TEXT%" || exit /b 1
exit /b 0

:go_back
"%ADB%" shell input keyevent 4
if errorlevel 1 exit /b 1
call :wait_for_text "Actions" || exit /b 1
exit /b 0

:capture_screen
set "SHOT_NAME=%~1"
echo Capturing %SHOT_NAME%...
"%ADB%" exec-out screencap -p > "%SCREENSHOT_DIR%\%SHOT_NAME%"
if errorlevel 1 exit /b 1
exit /b 0

:tap_text
set "TARGET_TEXT=%~1"
call :dump_window || exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -Command "$adb = [Environment]::GetEnvironmentVariable('ADB'); $dump = [Environment]::GetEnvironmentVariable('WINDOW_DUMP'); [xml]$xml = Get-Content -LiteralPath $dump; $node = $xml.SelectNodes('//node') | Where-Object { $_.text -eq $env:TARGET_TEXT -or $_.'content-desc' -eq $env:TARGET_TEXT } | Select-Object -First 1; if (-not $node) { Write-Error ('Could not find UI element: ' + $env:TARGET_TEXT); exit 2 }; if ($node.bounds -notmatch '\[(\d+),(\d+)\]\[(\d+),(\d+)\]') { Write-Error ('Could not parse bounds for: ' + $env:TARGET_TEXT); exit 3 }; $x = [int](($matches[1] + $matches[3]) / 2); $y = [int](($matches[2] + $matches[4]) / 2); & $adb shell input tap $x $y | Out-Null"
if errorlevel 1 exit /b 1
call :wait_seconds 2
exit /b 0