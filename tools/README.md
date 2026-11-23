# Tools

Scripts that build the sample app for each target the way users get it and open it for manual testing, the Store package build, and the recorder for the WebAssembly AOT profile.

| Script | Target | Result |
| --- | --- | --- |
| [`publish-and-serve-wasm.ps1`](publish-and-serve-wasm.ps1) | WebAssembly | Release publish with profile-guided AOT, served at http://127.0.0.1:8096 |
| [`publish-and-open-uno-desktop.ps1`](publish-and-open-uno-desktop.ps1) | Uno desktop (Skia) | Release publish, opened |
| [`publish-and-open-winui.ps1`](publish-and-open-winui.ps1) | WinUI (Windows App SDK) | Native AOT publish, opened |
| [`build-store-package.ps1`](build-store-package.ps1) | WinUI (Microsoft Store) | A `.msixupload` bundle of the sample against a released NuGet package |
| [`record-aot-profile.cjs`](record-aot-profile.cjs) | WebAssembly | A fresh `Platforms/WebAssembly/aot.profile` for the Uno sample |

The scripts find the repository from their own location, so they work from any folder. Everything they produce goes under `artifacts/`, which git ignores. The target framework comes from `NetVersion` in `Directory.Build.props`, so nothing needs editing when it changes.

## Prerequisites

- The .NET SDK pinned in `global.json`.
- For WebAssembly: nothing else. The first publish downloads Uno's Emscripten toolchain to `~/.uno/emsdk` and takes several minutes; later publishes are faster.
- For WinUI: Visual Studio with the **Desktop development with C++** workload, which Native AOT links with.
- For the AOT recorder: Node.js 18 or later and PowerShell 7 (`pwsh`). The recorder drives a browser through Playwright, and offers to install it on first use.

## Publish and serve: WebAssembly

```powershell
.\tools\publish-and-serve-wasm.ps1
```

Publishes `WinUI.TableView.SampleApp.Uno` for `browserwasm` in Release and serves it on the loopback address. Open http://127.0.0.1:8096 in a browser.

It always publishes, because only a publish applies the profile-guided AOT compilation. A Release *build* runs mostly interpreted, so it scrolls and loads far slower than what users get and isn't worth judging performance on.

The server sends `Cache-Control: no-store`, so after a new publish a plain reload shows it. Stop the server with Ctrl+C.

| Option | Effect |
| --- | --- |
| `-NoPublish` | Serve the last publish as it is |
| `-Port <n>` | Serve on another port (default 8096) |
| `-Out <dir>` | Publish to and serve from another folder (default `artifacts\wasm-publish`) |

If the server was left running in a window that is gone, end the `pwsh` process that runs it. Stopping "whatever holds port 8096" doesn't work: Windows' HTTP driver owns the port (it shows as PID 4), not the script.

## Publish and open: Uno desktop

```powershell
.\tools\publish-and-open-uno-desktop.ps1
```

Publishes the Uno sample for `desktop` (Skia on Windows) in Release and opens it. The desktop head runs on the JIT, so this is the same code users run; there is no AOT step as there is for WebAssembly.

| Option | Effect |
| --- | --- |
| `-NoPublish` | Open the last publish as it is |
| `-Out <dir>` | Publish to and open from another folder (default `artifacts\uno-desktop-publish`) |

## Publish and open: WinUI

```powershell
.\tools\publish-and-open-winui.ps1
```

Publishes the WinUI sample with Native AOT, unpackaged and self-contained, and opens it. Native AOT is how the sample ships, and it behaves differently from a JIT run in ways tests don't catch: reflection only finds what the trimmer kept, and WinRT interop uses generated code only. Pages that show blank cells or crash only in this build usually point at missing metadata or binding code.

Two gaps in the toolchain are handled for you:

- The native link step runs `vswhere.exe`, which lives in the Visual Studio Installer folder. The script puts that folder on `PATH` for the publish.
- An unpackaged publish leaves out the compiled XAML files (`.xbf`), and the app then fails at startup with `0xC000027B`. The script copies them from the build output the publish just produced.

| Option | Effect |
| --- | --- |
| `-NoPublish` | Open the last publish as it is |
| `-Platform x64\|ARM64` | Architecture to publish (default: this machine's). A publish for another architecture isn't opened |
| `-Out <dir>` | Publish to and open from another folder (default `artifacts\winui-publish`) |

x86 isn't offered: the sample turns Native AOT off for x86.

### Frame log

To measure scrolling on Windows, set `TABLEVIEW_FRAME_LOG` to a file path before opening the published app, use it, and read the CSV it leaves: a row per rendered frame with the elapsed time, the interval since the previous frame, how much total GC pause time grew in that interval, and the collection counts per generation. A long interval with a matching pause delta is a GC stall; one without is UI-thread work. The file is appended in chunks, so stop interacting for a moment before reading it.

```powershell
$env:TABLEVIEW_FRAME_LOG = "$env:TEMP\tableview-frames.csv"
.\tools\publish-and-open-winui.ps1 -NoPublish
```

Both "publish and open" scripts first close a copy of the app already running from their output folder, because its files are locked while it runs. Security software that scans launched programs can keep the `.exe` open for a few minutes after the app has closed; the scripts then say what they are waiting for and carry on once it is released (or give up after five minutes; `-Out` publishes to a fresh folder instead). After opening the app they wait for its window and report an error if it exits during startup.

## Build the Store package

```powershell
.\tools\build-store-package.ps1 1.5.1
```

Builds the `.msixupload` bundle (x86, x64 and ARM64) of the sample app for the Microsoft Store, referencing the given **released** WinUI.TableView package from NuGet rather than the library project, so the Store app is what users of that package version get. The version is also written into `Package.appxmanifest` (as `1.5.1.0`; the Store requires a fourth component of 0) for the duration of the build only, and the manifest is restored afterwards. The sample's `bin` and `obj` folders are removed first, because switching between a project reference and a package reference leaves restore assets behind that break the build.

Upload the `.msixupload` it reports to the app's submission in Partner Center.

| Option | Effect |
| --- | --- |
| `-Configuration <name>` | Build configuration (default `Release`) |
| `-OutputDirectory <dir>` | Where the bundle goes (default `artifacts\store`) |

It needs Visual Studio with the Windows App SDK workload, which provides the MSBuild packaging targets; the .NET SDK alone cannot produce a Store bundle.

## Record the WebAssembly AOT profile

```powershell
node tools\record-aot-profile.cjs
```

The WebAssembly sample is compiled ahead of time only for the methods listed in `samples/WinUI.TableView.SampleApp.Uno/Platforms/WebAssembly/aot.profile`; everything else runs in the interpreter. The profile is recorded from a real session of the app, so it goes stale.

**When to run it:**

- After every upgrade of the Uno packages (`Uno.Sdk`, `Uno.WinUI`). Method signatures in Uno change between versions, and a profile recorded against the old ones leaves the new hot paths interpreted. Scrolling then stutters badly even though nothing in this repository changed.
- After significant changes to the table's hot paths: creating, reusing or laying out rows and cells.

**What it does** (about ten minutes):

1. Checks that Playwright and its Chromium build are installed in `tests/WinUI.TableView.Tests.Uno`, and offers to install what is missing.
2. Publishes a profiling build of the sample (interpreter only, no linker) to `artifacts/aot-record/app`. That build has its own `obj` and `bin` under `artifacts/aot-record/build`: the WebAssembly build caches processed assemblies, and sharing the checkout's would mix a normal publish's trimmed assemblies into the profiling app, which then fails to start.
3. Records three sessions in headless Chromium, each from a fresh start: **scrolling** (every common page, a jump to row 5,000, a table of 100 columns scrolled sideways, grouping, a theme switch), **controls** (editing a cell, sorting, the filter flyout, row headers, row details, compact sizing) and **grouping**. Pages are opened by their names in the navigation menu, read through Uno's accessibility layer. That layer does not report the pages inside a menu group, so those are opened by typing their name into the search box. The browser window grows until the whole menu fits, because that layer only reports the menu's items in the right place while the menu is unscrolled.
4. Merges the three recordings, and writes `aot.profile` only if each recording holds the table's own methods (a recording of an app that never started doesn't) and the merged file read back still contains every recorded method.

**Then:** run `.\tools\publish-and-serve-wasm.ps1`, check that scrolling on the Overview and Grid Lines pages is smooth, and commit the new `aot.profile`.

| Option | Effect |
| --- | --- |
| `--setup` | Install missing browser dependencies without asking |
| `--no-build` | Record against the last profiling build as it is |
| `--only <name>` | Re-record one session (`scrolling`, `controls` or `grouping`), keeping the other recordings |
| `--merge-only` | Merge the recordings already on disk without opening a browser |
| `--out <file>` | Write the merged profile somewhere else, for example to compare it with the committed one |

**When something goes wrong,** look in `artifacts/aot-record/recordings`: each session leaves a screenshot of the app at its start and end, and the browser's console log.

- *"The app did not start"*: the console log shows why the .NET runtime or Uno failed to load.
- *"its accessibility layer could not be switched on"* or *"its navigation menu never appeared"*: the app is up (the startup screenshot shows it) but the script can't see the menu, so it can't find the pages. Uno's accessibility layer may have changed in a new version.
- *"holds almost no WinUI.TableView methods"*: the app loaded but the table never ran; the startup screenshot usually shows what it got stuck on. The committed profile is left as it was.
- *"no '…' page in this sample - skipped"*: the menu has no entry of that name. That's expected for a page this branch doesn't have; if the page was renamed, update the name in the script's recording functions.
- *"could not open the '…' page"* (a warning at the end): the menu entry was found, but clicking it didn't change the page title, so that page's steps ran on whatever page was showing.
- Clicks *inside* a page (the Group by button, a column header, an editable cell) are positions from the page's top-left corner in a 1280-wide window. If a page's layout changes, compare the end screenshots with what the session was meant to do.
- **Check the end screenshots after every recording.** The controls session should end on Compact Sizing and the other two on Grouping. A session that ends elsewhere opened the wrong pages, and the profile is missing their code even though the recording "succeeded".
