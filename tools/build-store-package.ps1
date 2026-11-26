<#
.SYNOPSIS
Builds the Microsoft Store upload package (.msixupload) of the WinUI sample against a released NuGet package.

.DESCRIPTION
The Store build of the sample app references the WinUI.TableView package from NuGet, not the library project,
so what goes to the Store is what users of that package version get. The version given is written into
Package.appxmanifest (with a fourth component of 0, as the Store requires) for the duration of the build only;
the manifest is restored afterwards, so the repository stays clean.

The NuGet package version is the same as the Store version unless -NuGetVersion is given. When it is not given,
the script asks for it; press Enter to keep the Store version. Use it when the two differ, such as a preview
package (2.0.0-preview1) shipped as Store version 1.9.1.0.

The sample's bin and obj folders are removed first, because switching between a ProjectReference and a
PackageReference leaves restore assets behind that break the build.

.EXAMPLE
.\tools\build-store-package.ps1 1.5.1            # package the sample against WinUI.TableView 1.5.1
.\tools\build-store-package.ps1 v1.5.1.0         # the same, in the Store's own version form
.\tools\build-store-package.ps1 1.5.1 -OutputDirectory some\dir
.\tools\build-store-package.ps1 1.9.1 -NuGetVersion 2.0.0-preview1   # Store version 1.9.1.0, WinUI.TableView 2.0.0-preview1
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Version,

    [string]$NuGetVersion,

    [string]$Configuration = "Release",

    [string]$OutputDirectory = "artifacts\store"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "samples\WinUI.TableView.SampleApp\WinUI.TableView.SampleApp.csproj"
$manifestPath = Join-Path $repoRoot "samples\WinUI.TableView.SampleApp\Package.appxmanifest"
$bundlePlatforms = "x86|x64|ARM64"

function Get-MSBuildPath {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

    if (Test-Path $vswhere) {
        $path = & $vswhere `
            -latest `
            -products * `
            -requires Microsoft.Component.MSBuild `
            -find "MSBuild\**\Bin\MSBuild.exe" |
            Select-Object -First 1

        if ($path) {
            return $path
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw "MSBuild could not be found. Install Visual Studio with the Windows App SDK/.NET desktop development workloads, or run this script from a Visual Studio Developer PowerShell."
}

# Accept:
#   1.5.0
#   v1.5.0
#   1.5.0.0
#   v1.5.0.0
#
# NuGet package version:
#   1.5.0
#
# Microsoft Store package version:
#   1.5.0.0
$normalizedVersion = $Version.Trim() -replace '^v', ''

if ($normalizedVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $build = [int]$Matches[3]

    $tableViewVersion = "$major.$minor.$build"
    $packageVersion = "$major.$minor.$build.0"
}
elseif ($normalizedVersion -match '^(\d+)\.(\d+)\.(\d+)\.(\d+)$') {
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $build = [int]$Matches[3]
    $revision = [int]$Matches[4]

    if ($revision -ne 0) {
        throw "Microsoft Store package versions must use 0 as the fourth component. Use '$major.$minor.$build.0'."
    }

    $tableViewVersion = "$major.$minor.$build"
    $packageVersion = "$major.$minor.$build.0"
}
else {
    throw "Invalid version '$Version'. Use a version such as '1.5.0' or '1.5.0.0'."
}

foreach ($part in @($major, $minor, $build)) {
    if ($part -lt 0 -or $part -gt 65535) {
        throw "Each package version component must be between 0 and 65535. Version: $packageVersion"
    }
}

if ($major -lt 1) {
    throw "The major version must be at least 1. Version: $packageVersion"
}

if (-not $PSBoundParameters.ContainsKey('NuGetVersion')) {
    $NuGetVersion = Read-Host "WinUI.TableView NuGet version (press Enter to use $tableViewVersion)"
}

if (-not [string]::IsNullOrWhiteSpace($NuGetVersion)) {
    $tableViewVersion = $NuGetVersion.Trim() -replace '^v', ''

    if ($tableViewVersion -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z][0-9A-Za-z.-]*)?$') {
        throw "Invalid NuGet version '$NuGetVersion'. Use a version such as '1.5.0' or '2.0.0-preview1'."
    }
}

if (-not (Test-Path $projectPath)) {
    throw "Project not found: $projectPath"
}

if (-not (Test-Path $manifestPath)) {
    throw "Package manifest not found: $manifestPath"
}

$msbuild = Get-MSBuildPath
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$manifestFullPath = (Resolve-Path $manifestPath).Path
$projectDirectory = Split-Path $projectPath

Write-Host ""
Write-Host "Microsoft Store package build"
Write-Host "-----------------------------"
Write-Host "NuGet version  : $tableViewVersion"
Write-Host "Store version  : $packageVersion"
Write-Host "Configuration  : $Configuration"
Write-Host "Architectures  : x86, x64, ARM64"
Write-Host "Project        : $projectPath"
Write-Host "Output         : $outputPath"
Write-Host "MSBuild        : $msbuild"
Write-Host ""

# Keep the repository clean: update the manifest only for the duration of the build.
# Its bytes are restored as they were, byte order mark included.
$originalManifest = [System.IO.File]::ReadAllBytes($manifestFullPath)

try {
    [xml]$manifest = [System.IO.File]::ReadAllText($manifestFullPath)
    $manifest.Package.Identity.Version = $packageVersion
    $manifest.Save($manifestFullPath)

    # Clear previous restore/build state so switching between ProjectReference
    # and PackageReference cannot leave stale assets behind.
    foreach ($directory in @("bin", "obj")) {
        $path = Join-Path $projectDirectory $directory

        if (Test-Path $path) {
            Write-Host "Removing stale $directory directory..."
            Remove-Item $path -Recurse -Force
        }
    }

    if (Test-Path $outputPath) {
        Remove-Item $outputPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

    Write-Host ""
    Write-Host "Restoring Sample App with WinUI.TableView $tableViewVersion from NuGet..."

    & $msbuild $projectPath `
        /t:Restore `
        /p:Configuration=$Configuration `
        /p:StorePackaging=true `
        /p:UseTableViewNuGet=true `
        /p:TableViewPackageVersion=$tableViewVersion

    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE."
    }

    Write-Host ""
    Write-Host "Building Store upload package..."

    & $msbuild $projectPath `
        /p:Configuration=$Configuration `
        /p:Platform=x86 `
        /p:StorePackaging=true `
        /p:UseTableViewNuGet=true `
        /p:TableViewPackageVersion=$tableViewVersion `
        "/p:AppxBundlePlatforms=$bundlePlatforms" `
        /p:AppxBundle=Always `
        /p:UapAppxPackageBuildMode=StoreUpload `
        "/p:AppxPackageDir=$outputPath\" `
        /p:AppxPackageSigningEnabled=false `
        /p:GenerateTemporaryStoreCertificate=false `
        /p:GenerateAppxPackageOnBuild=true

    if ($LASTEXITCODE -ne 0) {
        throw "Package build failed with exit code $LASTEXITCODE."
    }

    $packages = @(
        Get-ChildItem `
            -Path $outputPath `
            -Filter "*.msixupload" `
            -File `
            -Recurse
    )

    if ($packages.Count -eq 0) {
        Write-Host ""
        Write-Host "Generated files:"

        Get-ChildItem -Path $outputPath -File -Recurse |
            ForEach-Object {
                Write-Host "  $($_.FullName)"
            }

        throw "Build completed, but no .msixupload file was found."
    }

    if ($packages.Count -gt 1) {
        Write-Host ""
        Write-Host "Multiple .msixupload files were generated:"

        $packages |
            ForEach-Object {
                Write-Host "  $($_.FullName)"
            }

        throw "Expected exactly one .msixupload file."
    }

    $package = $packages[0]

    Write-Host ""
    Write-Host "Store package created successfully:"
    Write-Host "  $($package.FullName)"
    Write-Host ""
    Write-Host "NuGet dependency : WinUI.TableView $tableViewVersion"
    Write-Host "Store version    : $packageVersion"
    Write-Host ""
    Write-Host "Upload the .msixupload file above to the existing app submission in Partner Center."
}
finally {
    [System.IO.File]::WriteAllBytes($manifestFullPath, $originalManifest)

    Write-Host ""
    Write-Host "Restored the original Package.appxmanifest."
}
