<#
.SYNOPSIS
Publishes the WASM sample and serves it for manual testing.

.DESCRIPTION
A Release build alone does not apply profile-guided AOT, so this always runs `dotnet publish`.
The server sends Cache-Control: no-store, so a plain browser reload always picks up a fresh publish.
Stop it with Ctrl+C.

.EXAMPLE
.\tools\publish-and-serve-wasm.ps1                 # publish, then serve on http://127.0.0.1:8096
.\tools\publish-and-serve-wasm.ps1 -NoPublish      # serve the last publish as it is
.\tools\publish-and-serve-wasm.ps1 -Port 8097      # use another port
.\tools\publish-and-serve-wasm.ps1 -Out some\dir   # publish to and serve another folder
#>
[CmdletBinding()]
param(
    [int]$Port = 8096,
    [string]$Out = 'artifacts\wasm-publish',
    [switch]$NoPublish
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$outPath = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($repoRoot, $Out))
$project = Join-Path $repoRoot 'samples\WinUI.TableView.SampleApp.Uno\WinUI.TableView.SampleApp.Uno.csproj'
$netVersion = ([xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props'))).Project.PropertyGroup.NetVersion | Select-Object -First 1

if (-not $NoPublish) {
    Write-Host "Publishing $project ($netVersion-browserwasm) -> $outPath ..."
    dotnet publish $project -c Release -f "$netVersion-browserwasm" -o $outPath -v m

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }
}

$wwwroot = Join-Path $outPath 'wwwroot'

if (-not (Test-Path (Join-Path $wwwroot 'index.html'))) {
    Write-Error "No index.html under $wwwroot - publish first (run without -NoPublish)."
    exit 1
}

$types = @{
    '.html' = 'text/html; charset=utf-8'; '.js' = 'text/javascript'; '.mjs' = 'text/javascript'
    '.wasm' = 'application/wasm'; '.json' = 'application/json'; '.css' = 'text/css'
    '.webmanifest' = 'application/manifest+json'; '.ico' = 'image/x-icon'; '.png' = 'image/png'
    '.jpg' = 'image/jpeg'; '.svg' = 'image/svg+xml'; '.ttf' = 'font/ttf'; '.otf' = 'font/otf'
    '.woff' = 'font/woff'; '.woff2' = 'font/woff2'; '.dat' = 'application/octet-stream'
    '.dll' = 'application/octet-stream'; '.pdb' = 'application/octet-stream'
    '.csv' = 'text/csv'; '.txt' = 'text/plain'
}

$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add("http://127.0.0.1:$Port/")

try {
    $listener.Start()
}
catch [System.Net.HttpListenerException] {
    Write-Error "Port $Port could not be bound (already in use?) - stop what holds it, or pass -Port <other>."
    exit 1
}

Write-Host "Serving $wwwroot"
Write-Host "  -> http://127.0.0.1:$Port/  (Ctrl+C stops it)"

try {
    while ($listener.IsListening) {
        # Poll the async accept so Ctrl+C is honored between requests.
        $accept = $listener.GetContextAsync()
        while (-not $accept.AsyncWaitHandle.WaitOne(250)) { }
        $context = $accept.GetAwaiter().GetResult()
        $response = $context.Response

        try {
            $relative = [System.Uri]::UnescapeDataString($context.Request.Url.AbsolutePath).TrimStart('/') -replace '/', '\'
            $target = [System.IO.Path]::GetFullPath((Join-Path $wwwroot $relative))

            if ($target -ne $wwwroot -and -not $target.StartsWith($wwwroot + [System.IO.Path]::DirectorySeparatorChar)) {
                $response.StatusCode = 403
            }
            else {
                if (Test-Path $target -PathType Container) {
                    $target = Join-Path $target 'index.html'
                }

                if (Test-Path $target -PathType Leaf) {
                    $extension = [System.IO.Path]::GetExtension($target).ToLowerInvariant()
                    $response.ContentType = if ($types.ContainsKey($extension)) { $types[$extension] } else { 'application/octet-stream' }
                    $response.Headers['Cache-Control'] = 'no-store'
                    $bytes = [System.IO.File]::ReadAllBytes($target)
                    $response.ContentLength64 = $bytes.Length
                    $response.OutputStream.Write($bytes, 0, $bytes.Length)
                }
                else {
                    $response.StatusCode = 404
                }
            }
        }
        catch {
            $response.StatusCode = 500
        }
        finally {
            $response.Close()
        }
    }
}
finally {
    $listener.Stop()
    $listener.Close()
}
