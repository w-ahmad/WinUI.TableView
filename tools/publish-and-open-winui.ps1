<#
.SYNOPSIS
Publishes the WinUI sample with Native AOT (unpackaged, self-contained) and opens it for manual testing.

.DESCRIPTION
Native AOT is how the sample ships, and it takes different code paths than a JIT run: reflection needs
metadata the trimmer kept, and WinRT interop uses generated code only. Test it this way before a release.

Two gaps in the toolchain are covered here:
- The native link step calls vswhere.exe, which lives in the Visual Studio Installer folder; that folder is
  put on PATH for this run. Native AOT also needs Visual Studio's "Desktop development with C++" workload.
- An unpackaged publish leaves out the compiled XAML (.xbf), and the app then fails at startup with
  0xC000027B. They are copied over from the build output, which the publish has just produced.

A copy already running from the output folder is closed first, since its files are locked while it runs.

.EXAMPLE
.\tools\publish-and-open-winui.ps1                   # publish for this machine's architecture, then open
.\tools\publish-and-open-winui.ps1 -NoPublish        # open the last publish as it is
.\tools\publish-and-open-winui.ps1 -Platform ARM64   # publish for another architecture (not opened elsewhere)
.\tools\publish-and-open-winui.ps1 -Out some\dir     # publish to and open from another folder
#>
[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = $(if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'ARM64' } else { 'x64' }),
    [string]$Out = 'artifacts\winui-publish',
    [switch]$NoPublish
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Out))
$projectDir = Join-Path $repoRoot 'samples\WinUI.TableView.SampleApp'
$project = Join-Path $projectDir 'WinUI.TableView.SampleApp.csproj'
$runtime = "win-$($Platform.ToLowerInvariant())"
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
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'

    if (Test-Path (Join-Path $installer 'vswhere.exe')) {
        $env:PATH = "$installer;$env:PATH"
    }
    else {
        Write-Warning "vswhere.exe was not found in $installer; the Native AOT link step needs Visual Studio with the C++ desktop workload."
    }

    Write-Host "Publishing $project (Native AOT, $runtime) -> $outPath ..."
    dotnet publish $project -c Release "-p:Platform=$Platform" -r $runtime -p:PublishAot=true -p:WindowsPackageType=None -o $outPath -v m

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }

    # The build output the publish just refreshed: bin\<Platform>\Release\<framework>\<runtime>.
    $buildOutput = Get-ChildItem (Join-Path $projectDir "bin\$Platform\Release") -Directory -ErrorAction SilentlyContinue |
        ForEach-Object { Join-Path $_.FullName $runtime } |
        Where-Object { Test-Path $_ } |
        Sort-Object { (Get-Item $_).LastWriteTime } -Descending |
        Select-Object -First 1

    if (-not $buildOutput) {
        Write-Error "No build output under $(Join-Path $projectDir "bin\$Platform\Release") to take the compiled XAML from."
        exit 1
    }

    # The publish never writes .xbf files, so refresh all of them; a copy left from an earlier run would be stale.
    # The .pri is taken only if the publish didn't produce one.
    $copied = 0
    foreach ($file in Get-ChildItem $buildOutput -Recurse -Include '*.xbf', "$appName.pri") {
        $target = Join-Path $outPath $file.FullName.Substring($buildOutput.Length + 1)

        if ($file.Extension -eq '.xbf' -or -not (Test-Path $target)) {
            New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
            Copy-Item $file.FullName $target -Force
            $copied++
        }
    }

    Write-Host "Copied $copied compiled XAML/resource file(s) the publish leaves out."
}

if (-not (Test-Path $exe)) {
    Write-Error "No $appName.exe under $outPath - publish first (run without -NoPublish)."
    exit 1
}

$machine = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'ARM64' } else { 'x64' }

if ($Platform -ne $machine) {
    Write-Host "Published for $Platform; not opening it on this $machine machine."
    exit 0
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
    $code = '0x{0:X8}' -f $process.ExitCode
    $hint = if ($code -eq '0xC000027B') { ' (a XAML resource failed to load - check that the .xbf files were copied)' } else { '' }
    Write-Error "The app exited during startup with code $code$hint."
    exit 1
}

Write-Host "Running (PID $($process.Id))."
