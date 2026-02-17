@echo off
echo --- Restoring Dependencies ---
dotnet restore

echo.
echo --- Building Project ---
dotnet build diplom\diplom.csproj -c Debug

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [!] Build Failed. Check the errors above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo --- Running Application ---
dotnet run --project diplom\diplom.csproj --no-build

if %ERRORLEVEL% NEQ 0 (
    pause
)
