# WinUI tests

This project runs as an unpackaged WinUI 3 executable hosting MSTest and
Microsoft.Testing.Platform (MTP). No MSIX registration, signing certificate,
Developer Mode, VSTest installation, or WinApp CLI is needed.

## Prerequisites

- Windows with an interactive desktop for the WinUI window and composition tests.
- The .NET 10 SDK selected by the repository's `global.json` (10.0.100 with patch
  roll-forward).
- Windows SDK / WinUI build prerequisites, normally installed through Visual
  Studio's WinUI application development workload.

The existing Windows App SDK version and target framework are retained. The
Windows App SDK runtime is copied beside the executable by
`WindowsAppSDKSelfContained=true`; no separate Windows App Runtime installation
is required. The .NET runtime is still framework-dependent and is supplied by
the .NET SDK on development and CI machines.

## Run from the repository root

```powershell
dotnet test --project tests/WinUI.TableView.Tests/WinUI.TableView.Tests.csproj `
  --configuration Release --arch x64 `
  --report-trx --results-directory "$PWD/TestResults/winui" --timeout 20m
```

The command restores, builds, launches the test window, runs the tests and exits.
The process returns nonzero on failure. TRX reports are written to the explicit
results directory. GitHub Actions runs this same command and uploads
`TestResults` even when testing fails.

Use `--list-tests` to check discovery, or `--filter` for a focused run:

```powershell
dotnet test --project tests/WinUI.TableView.Tests/WinUI.TableView.Tests.csproj `
  --configuration Release --arch x64 --list-tests

dotnet test --project tests/WinUI.TableView.Tests/WinUI.TableView.Tests.csproj `
  --configuration Release --arch x64 --no-build `
  --filter "FullyQualifiedName~UITestLifecycleTests" --report-trx
```

`--no-build` requires a previous build with the same configuration and
architecture. x86 and ARM64 remain supported build targets; use an architecture
that your machine can execute. The examples and CI use x64.

For direct execution after building, run
`tests/WinUI.TableView.Tests/bin/Release/net10.0-windows10.0.26100.0/win-x64/WinUI.TableView.Tests.exe`.
Pass MTP options such as `--list-tests` or `--report-trx` to that executable.
Do not use `dotnet exec` or launch the DLL through `dotnet`: WinUI resource
resolution needs the application's own executable. The unpackaged launch
profile also supports `dotnet run --project ... --configuration Release --arch x64`.

## Test host lifecycle

WinUI generates the entry point from `UnitTestApp.xaml`. `MSTest.Sdk` detects the
application definition and generates the reusable
`MicrosoftTestingPlatformApplication.RunAsync` helper instead of a second entry
point. `OnLaunched` activates `UnitTestAppWindow`, publishes its dispatcher to
`UITestMethodAttribute.DispatcherQueue`, and passes process arguments to MTP.
It preserves the runner result in `Environment.ExitCode`, then closes the window
and exits the application, including when the runner throws.

Keep `[UITestMethod]` on tests that touch XAML objects. MSTest dispatches test
initialization, the test body, async continuations, and cleanup on the window's
UI thread. Do not add `WinUITestTarget`: the self-hosted app already started
WinUI. Tests share one window, so the assembly explicitly disables parallelism.

`LoadTestContentAsync` waits for `Loaded` and a composition rendering callback
before returning. `UnloadTestContentAsync` waits for `Unloaded`. Continue to use
these helpers for real layout tests, and unload content in `finally` or test
cleanup. Keep the test window available for rendering while a run is active.
`Host/UITestLifecycleTests.cs` verifies dispatcher access across asynchronous
setup/body/cleanup, rendered dimensions and unloading.

`global.json` pins MSTest.Sdk 4.5.1, which supplies compatible MSTest 4.5.1 and
MTP 2.5.1 packages, including their central package version entries. The project
enables only TRX reporting beyond the standard runner. It retains the native
`app.manifest` for Windows compatibility and DPI awareness; the MSIX manifest
and package artwork have been removed.

## Uno and troubleshooting

The separate `WinUI.TableView.Tests.Uno` project still uses Uno.Sdk and its own
in-app runner. Its centrally managed MSTest 3.9.3 dependencies and CI commands
are unchanged. It links the shared top-level test sources; the WinUI-only
`Host` directory is not linked. Use the explicit `--project` command above for
MTP rather than attempting a solution-wide run that includes Uno applications.

- After switching from the packaged project, remove stale test `bin` and `obj`
  directories before rebuilding if an old `AppxManifest.xml` remains in output.
- MTP uses `--report-trx`, not VSTest's `--logger:trx` or `.build.appxrecipe`.
- The documented MrtCore `PRI263` warnings about MSTest satellite resources are
  benign. They do not indicate missing application resources.
- If a layout test hangs, confirm that the window can render on the current
  Windows desktop. `--timeout 20m` bounds the run, and the CI job also has a
  30-minute limit.

## Official references

- [MSTest WinUI announcement](https://devblogs.microsoft.com/dotnet/testing-uwp-and-winui-3-apps-with-mstest/)
- [Unpackaged WinUI sample](https://github.com/microsoft/testfx/tree/main/samples/public/WinUIMtpUnpackagedApp)
- [WinUI testing guide](https://github.com/microsoft/testfx/blob/main/docs/winui-testing.md)
- [MSTest.Sdk 4.5.1](https://www.nuget.org/packages/MSTest.Sdk/4.5.1)
- [MTP 2.5.1](https://www.nuget.org/packages/Microsoft.Testing.Platform/2.5.1)
- [WinApp CLI announcement](https://devblogs.microsoft.com/ifdef-windows/winappcli-v0-7-0-release-announcement/)
  (optional tooling; not a dependency of these unpackaged in-process tests)
