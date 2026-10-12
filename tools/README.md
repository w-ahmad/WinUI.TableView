# WASM scroll profiling

Install the existing browser test dependencies and full Chromium:

```powershell
npm ci --prefix tests/WinUI.TableView.Tests.Uno
node tests/WinUI.TableView.Tests.Uno/node_modules/playwright/cli.js install chromium
```

Publish before measuring. A regular Release build does not apply profile-guided AOT.

```powershell
dotnet publish samples/WinUI.TableView.SampleApp.Uno/WinUI.TableView.SampleApp.Uno.csproj -c Release -f net10.0-browserwasm -o artifacts/scroll-publish
node tools/scroll-profile.cjs
```

The harness serves only the selected output on a temporary loopback port, starts a fresh browser context with service workers blocked, and sends 40 wheel events of 80 pixels over each table. The viewport is fixed at 1280 × 900 so the navigation coordinates are reproducible. It measures Overview (1,000 rows), Grouping (100 rows), and Virtualization (10,000 rows). The scroll distance stays within Grouping's extent.

Results, screenshots, browser logs, and graphics device information go to `artifacts/scroll-profile`. Inspect the screenshots to confirm the sample is populated and the expected pages were selected. The harness also checks that pixels in the row area change after scrolling. Windows uses full Chromium with D3D11; confirm `graphics.json` reports hardware WebGL before comparing runs. Avoid running builds or other CPU-intensive work during measurements.

The command exits with code 2 when more than 10% of Overview frame intervals exceed 34 ms. This is a local investigation budget, not a universal performance guarantee. Compare the same build, hardware, viewport, wheel sequence, and font settings across multiple runs. Sample data is generated randomly, so compare distributions rather than individual maximum delays.

Pass a different published root and output directory to compare saved builds. Add `--cpu` to capture `scroll.cpuprofile`, which Chrome DevTools can open. CPU profiling adds overhead, so the separate `overview-profiled` result should not be used for the frame comparison.

Add `--smoke` to also capture selecting all 10,000 rows, clearing selection, and grouping by last name. These actions run after the timed measurements.

Add `--warm` to measure a second scroll on Overview after its row pool has grown. Compare it with the first scroll to distinguish initial cache growth from continued row recycling.

Add `--fast` to capture 20 Overview wheel steps of 600 pixels and check for blank row-sized bands. This uses the sample's current light theme and row/font settings. It validates captured viewports, not every transient animation frame.

```powershell
node tools/scroll-profile.cjs path/to/baseline/wwwroot artifacts/baseline
node tools/scroll-profile.cjs artifacts/scroll-publish/wwwroot artifacts/candidate --cpu
```

To record newly used methods, first build the sample with `WasmShellGenerateAOTProfile=true`, `WasmShellEnableJiterpreter=false`, and `UnoXamlResourcesTrimming=false`, following [Uno's profile-guided AOT instructions](https://platform.uno/docs/articles/external/uno.wasm.bootstrap/doc/runtime-execution-modes.html#profile-guided-aot). Then run:

```powershell
node tools/scroll-profile.cjs samples/WinUI.TableView.SampleApp.Uno/bin/Release/net10.0-browserwasm/wwwroot artifacts/scroll-recording --record-aot
```

This exercises scrolling across common sample templates and saves `artifacts/scroll-recording/aot.profile` using Uno's Alt+Shift+P hotkey. It does not replace the project's profile automatically. Retain coverage for features not exercised by this recording, and republish with profiling disabled after updating `Platforms/WebAssembly/aot.profile`.

When comparing profile variants, verify that their native WASM files differ and that the expected method symbols are present. In this investigation, changing the profile selection alone reused existing native outputs. We moved the worktree's generated `obj/Release/net10.0-browserwasm/wasm/for-publish` cache aside, switched the actual profile file, and republished to force a separate baseline. Restore the intended profile and matching native cache after such a comparison.
