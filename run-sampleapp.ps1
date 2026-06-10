# Baut die WinUI SampleApp mit VS-MSBuild (das dotnet-CLI-SDK fehlt hier das MSIX/PRI-Tooling)
# und startet sie unpackaged. Aufruf:  .\run-sampleapp.ps1
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild (Visual Studio) nicht gefunden." }

$proj = Join-Path $root "samples\WinUI.TableView.SampleApp\WinUI.TableView.SampleApp.csproj"

Write-Host "Baue SampleApp (unpackaged) ..." -ForegroundColor Cyan
& $msbuild $proj -t:Restore`;Build -p:Configuration=Debug -p:Platform=x64 `
    -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -v:m -nologo
if ($LASTEXITCODE -ne 0) { throw "Build fehlgeschlagen (Exit $LASTEXITCODE)." }

$exe = Join-Path $root "samples\WinUI.TableView.SampleApp\bin\x64\Debug\net10.0-windows10.0.22621.0\WinUI.TableView.SampleApp.exe"
Write-Host "Starte $exe" -ForegroundColor Green
Start-Process -FilePath $exe
