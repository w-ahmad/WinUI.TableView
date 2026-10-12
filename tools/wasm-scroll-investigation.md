# WASM scrolling investigation — 12 October 2026

The application already publishes with mixed interpreter/AOT execution. The checked-in AOT profile predates the ItemsRepeater migration: its last Git change was in April 2026. It has no records for `TableViewRowElementFactory`, `OnRowElementPrepared`, `AttachRow`, or `ApplyRowVisuals`. A freshly recorded profile includes these methods and both sides of row recycling.

The change updates `samples/WinUI.TableView.SampleApp.Uno/Platforms/WebAssembly/aot.profile` and halves the browser row cache in `TableView.ItemsHost.cs` from 1 to 0.5. Other targets retain cache length 2. The recording exercises common sample templates and scrolling. It is merged with the previous profile using Uno Bootstrap's `Mono.Profiler.Aot.ProfileReader` and `ProfileWriter`. The resulting profile has 49,202 method records. After writing and reading it again, every original and recorded method key, including generic instantiations, is present. Existing worktree edits are preserved.

This follows [Uno's recommendation to recreate AOT profiles after substantial method changes](https://platform.uno/docs/articles/external/uno.wasm.bootstrap/doc/runtime-execution-modes.html#profile-guided-aot). A Release build alone does not apply the profile; publish the application.

## Measurement

The browser harness in `scroll-profile.cjs` uses full Chromium with hardware D3D11/WebGL on Intel Arc graphics, a 1280 × 900 viewport, a fresh context with service workers blocked, and 40 real wheel events of 80 pixels over each populated table. Screenshots confirm visible rows change and Grouping stays within its scroll extent. Frame intervals are measured with `requestAnimationFrame`, a proxy for browser main-thread stalls rather than a count of actual Skia canvas paints.

The baseline and profile-only candidate use identical managed assembly hashes. Only the AOT profile differs in that comparison. A second experiment changes only browser cache length while retaining the complete profile. Native output caches were separated because changing profile selection alone reused the previous native output. The original-profile WASM has none of the four new method symbols above; the updated-profile WASM has all four, and its CPU profile shows them executing as compiled WASM.

Software-rendered headless timings and the earlier saved publish were excluded from the final comparison. The saved publish predates some of Claude's latest changes. CPU-profiled runs are also excluded from frame comparisons because profiling adds overhead.

Three clean baseline runs with the original profile:

| Page | Rows | p95 frame interval, ms | Frames over 34 ms |
| --- | ---: | --- | --- |
| Overview | 1,000 | 33.5 / 49.9 / 33.4 | 6/146 / 7/139 / 5/142 |
| Grouping | 100 | 16.8 / 16.9 / 16.9 | 1/127 / 2/128 / 2/122 |
| Virtualization | 10,000 | 16.8 / 16.8 / 16.8 | 0/124 / 0/156 / 0/127 |

Three runs with the updated profile and original browser cache length 1:

| Page | p95 frame interval, ms | Frames over 34 ms |
| --- | --- | --- |
| Overview | 33.5 / 33.4 / 33.4 | 6/136 / 3/131 / 5/135 |
| Grouping | 16.8 / 16.8 / 16.8 | 1/127 / 2/127 / 1/126 |
| Virtualization | 16.8 / 16.8 / 16.8 | 0/166 / 0/130 / 0/124 |

Three runs with the updated profile and browser cache length 0.5, the final change:

| Page | p95 frame interval, ms | Frames over 34 ms |
| --- | --- | --- |
| Overview | 33.3 / 16.8 / 33.2 | 4/135 / 2/139 / 4/133 |
| Grouping | 16.9 / 16.8 / 16.8 | 0/127 / 0/125 / 0/130 |
| Virtualization | 16.8 / 16.8 / 16.9 | 0/131 / 0/164 / 0/162 |

Overview's combined share of frame intervals over 34 ms is 4.2% with the original profile/cache, 3.5% with the refreshed profile alone, and 2.5% with both changes. The median duration of the wheel sequence falls from 2,767 to 2,433 ms, about 12%. The maximum intervals across those three original runs are 117/133/167 ms; the final runs are 50/50/67 ms. These are small local samples, not a universal speedup guarantee.

A second Overview scroll after its pool has grown has no intervals over 34 ms with either original or final settings; p95 is about 16.8–16.9 ms. This concentrates the remaining issue in first-scroll work such as cache growth. Halving the browser cache limits extra row creation while the viewport starts moving.

Fast-scroll QA sends 20 wheel events of 600 pixels on Overview. Both cache sizes keep the sampled viewports populated. PNG analysis finds no text-free band taller than 27 pixels in the interior row area (a blank row-sized band over 40 pixels fails). This checks captured viewports, not every transient display frame. Interaction screenshots also confirm selecting all 10,000 rows as one range, clearing selection, and grouping by last name. The WASM publish and Windows library build pass. Existing browser regression suites pass: 19 row-host/layout tests, 11 recycling tests, and 4 row-header-width tests (34 total, zero failures).

The final native WASM download is 9,690,110 bytes with Brotli compression versus 9,433,411 bytes for the matching original-profile baseline, an increase of about 0.26 MB (2.7%). The profile itself is build input.

## Findings and limits

Ungrouped index conversion is constant time, and row pools/cache size track the viewport. The reviewed scrolling path does not scan the full dataset. Grouping also has fewer columns than Overview, so row count alone is not a controlled comparison. Columns are not virtualized: every realized row creates every visible column's cell, including columns horizontally outside the viewport.

Refreshing the AOT profile addresses a confirmed coverage gap; reducing the browser cache provides an additional measured gain. Neither eliminates all rendering, binding, text-formatting, or auto-width work. In particular, the existing 20-point Overview font setting remains in place. Results apply to these sample pages on this machine; sample data is generated randomly, and occasional long frames require distributions across runs rather than conclusions from one maximum. A smaller cache also reduces headroom for a very fast fling while the UI thread is blocked; the captured wheel checks pass, but touch and touchpad behavior should be checked on the deployed app and slower devices.

If further improvement is needed, the next measurements should compare the same rows and columns with fixed versus automatic widths, then quantify offscreen-column binding/layout cost. Reducing that work or adding column virtualization needs a separate behavior-preserving change and browser measurements. [Uno 6.7's damage-region rendering](https://platform.uno/blog/uno-platform-6-7/) is another investigation candidate, not a verified fix for this application.

See [the harness instructions](README.md) to reproduce measurements. Diagnostic screenshots, CPU profiles, build logs, and published comparison builds are under `artifacts/scroll-*`. The final sample is in `artifacts/scroll-publish/wwwroot`; the profile-only comparison is in `artifacts/scroll-profile-only-publish/wwwroot`. A local preview is served at `http://127.0.0.1:8091` for this session.
