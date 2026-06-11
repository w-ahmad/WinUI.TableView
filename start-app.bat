@echo off
setlocal
rem Baut die WinUI SampleApp und startet sie (unpackaged).
rem Build laeuft ueber VS-MSBuild, weil dem dotnet-CLI-SDK das MSIX/PRI-Tooling fehlt.

set "ROOT=%~dp0"
set "PROJ=%ROOT%samples\WinUI.TableView.SampleApp\WinUI.TableView.SampleApp.csproj"
set "EXE=%ROOT%samples\WinUI.TableView.SampleApp\bin\x64\Debug\net10.0-windows10.0.22621.0\WinUI.TableView.SampleApp.exe"

rem MSBuild von Visual Studio finden
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do set "MSBUILD=%%i"
if not defined MSBUILD (
    echo FEHLER: Visual Studio MSBuild nicht gefunden.
    pause
    exit /b 1
)

rem WindowsAppSDKSelfContained: Windows App Runtime liegt neben der EXE,
rem sonst sucht der Bootstrapper eine installierte Runtime und scheitert bei Versions-Mismatch
set "FLAGS=-p:Configuration=Debug -p:Platform=x64 -p:WindowsAppSDKSelfContained=true"

echo Baue SampleApp ...
"%MSBUILD%" "%PROJ%" -t:Build %FLAGS% -v:m -nologo -clp:ErrorsOnly
if errorlevel 1 (
    echo Erster Build fehlgeschlagen, zweiter Versuch ^(XAML-Compiler braucht manchmal zwei Durchlaeufe^) ...
    "%MSBUILD%" "%PROJ%" -t:Build %FLAGS% -v:m -nologo -clp:ErrorsOnly
    if errorlevel 1 (
        echo FEHLER: Build fehlgeschlagen.
        pause
        exit /b 1
    )
)

if not exist "%EXE%" (
    echo FEHLER: %EXE% nicht gefunden.
    pause
    exit /b 1
)

echo Starte App ...
start "" "%EXE%"
endlocal
