@echo off
echo --- Restoring Dependencies ---
dotnet restore

echo.
echo --- Building Solution ---
dotnet build diplom.sln -c Debug

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [!] Build Failed. Check the errors above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo --- Starting API Server (Background) ---
start "TimeFlow API" dotnet run --project diplom.API\diplom.API.csproj --no-build

echo.
echo --- Running WPF Application ---
dotnet run --project diplom\diplom.csproj --no-build

if %ERRORLEVEL% NEQ 0 (
    pause
)
