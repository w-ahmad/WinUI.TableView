<#
.SYNOPSIS
Publishes the Uno desktop (Skia) sample in Release and opens it for manual testing.

.DESCRIPTION
The desktop head runs on CoreCLR with the JIT, so a Release publish is what users get; there is no AOT
step as on WebAssembly. A copy already running from the output folder is closed first, since its files
are locked while it runs.

.EXAMPLE
.\tools\publish-and-open-uno-desktop.ps1                 # publish, then open the app
.\tools\publish-and-open-uno-desktop.ps1 -NoPublish      # open the last publish as it is
.\tools\publish-and-open-uno-desktop.ps1 -Out some\dir   # publish to and open from another folder
#>
[CmdletBinding()]
param(
    [string]$Out = 'artifacts\uno-desktop-publish',
    [switch]$NoPublish
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($repoRoot, $Out))
$project = Join-Path $repoRoot 'samples\WinUI.TableView.SampleApp.Uno\WinUI.TableView.SampleApp.Uno.csproj'
$netVersion = ([xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props'))).Project.PropertyGroup.NetVersion | Select-Object -First 1
$appName = 'WinUI.TableView.SampleApp'
$exe = Join-Path $outPath "$appName.exe"

$running = Get-Process -Name $appName -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($outPath + '\', [System.StringComparison]::OrdinalIgnoreCase) }

if ($running) {
    Write-Host "Closing the copy already running from $outPath (PID $($running.Id -join ', ')) ..."
    foreach ($process in $running) { [void]$process.CloseMainWindow() }
    $running | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
    $running | Where-Object { -not $_.HasExited } | Stop-Process -Force
    $running | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue
}

function Test-Writable([string]$Path) {
    try { [System.IO.File]::Open($Path, 'Open', 'ReadWrite', 'None').Dispose(); return $true } catch { return $false }
}

if (-not $NoPublish -and (Test-Path $exe) -and -not (Test-Writable $exe)) {
    # Security software that scans launched programs can keep the file open for minutes after the app has
    # closed (longer after a forced close), and the publish can't replace it until then.
    Write-Host "Waiting for another process to release $exe ..."
    $deadline = (Get-Date).AddMinutes(5)

    while (-not (Test-Writable $exe)) {
        if ((Get-Date) -gt $deadline) {
            Write-Error "$exe is still in use by another process. Try again later, or publish to another folder with -Out."
            exit 1
        }

        Start-Sleep -Seconds 2
    }
}

if (-not $NoPublish) {
    Write-Host "Publishing $project ($netVersion-desktop) -> $outPath ..."
    dotnet publish $project -c Release -f "$netVersion-desktop" -o $outPath -v m

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
}

if (-not (Test-Path $exe)) {
    Write-Error "No $appName.exe under $outPath - publish first (run without -NoPublish)."
    exit 1
}

Write-Host "Opening $exe ..."
$process = Start-Process -FilePath $exe -WorkingDirectory $outPath -PassThru

# Wait for the window, so a crash at startup is reported here rather than passing silently.
for ($i = 0; $i -lt 60 -and -not $process.HasExited; $i++) {
    $process.Refresh()
    if ($process.MainWindowHandle -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 250
}

if ($process.HasExited) {
    Write-Error ("The app exited during startup with code 0x{0:X8}." -f $process.ExitCode)
    exit 1
}

Write-Host "Running (PID $($process.Id))."
