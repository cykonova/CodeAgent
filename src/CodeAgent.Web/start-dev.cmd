@echo off
echo Starting CodeAgent Web Development Environment...

REM Get the directory where this batch file is located
set "SCRIPT_DIR=%~dp0"
echo Script directory: %SCRIPT_DIR%

REM Change to the script directory (CodeAgent.Web)
cd /d "%SCRIPT_DIR%"

echo Starting Angular dev server on http://localhost:4200...
cd client

REM Check if package.json exists
if not exist "package.json" (
    echo ERROR: package.json not found in %cd%
    echo Please make sure you're running this script from the CodeAgent.Web directory
    pause
    exit /b 1
)

start "Angular Dev Server" cmd /c "npm start"

echo Waiting for Angular dev server to start...
timeout /t 8 /nobreak > nul

REM Go back to CodeAgent.Web directory
cd /d "%SCRIPT_DIR%"

echo Starting .NET server on http://localhost:5001...

REM Check if project file exists
if not exist "CodeAgent.Web.csproj" (
    echo ERROR: CodeAgent.Web.csproj not found in %cd%
    echo Please make sure you're running this script from the CodeAgent.Web directory
    pause
    exit /b 1
)

echo Both servers are starting...
echo Angular: http://localhost:4200
echo .NET API: http://localhost:5001
echo.
echo Press Ctrl+C to stop the .NET server
echo Close the Angular Dev Server window to stop Angular
echo.

dotnet run