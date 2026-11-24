# Agent Instructions for WinUI.TableView

This file is the single source of instructions for every coding agent working in this repository. `CLAUDE.md` imports it and `.github/copilot-instructions.md` points to it, so edit this file, not those.

## Project

**WinUI.TableView** is a fast data grid control for WinUI with Uno Platform support. `TableView` derives from `Control` and hosts its rows in an `ItemsRepeater` with the platform's virtualizing `StackLayout`. It provides an Excel-like experience: column filtering, sorting, grouping, editing, selection, clipboard, and export.

- Docs: https://w-ahmad.dev/WinUI.TableView/ (source in `docs/`, DocFX)
- Targets: WinUI (Windows App SDK) and Uno (Desktop/Skia, WebAssembly, Android, iOS). Target frameworks are in `src/WinUI.TableView.csproj`; the SDK and MSBuild SDK versions are pinned in `global.json`.
- Packages use central version management (`Directory.Packages.props`).

## Layout

- `src/` library. `TableView.*.cs` are partials of the main control. Key folders:
  - `Columns/`: column types (bound columns derive from `TableViewBoundColumn`, others from `TableViewColumn`)
  - `ItemsSource/`: `CollectionView`, grouping, and the flattened visual row sequence (`TableViewVisualRows`)
  - `Layout/`: row host, realized rows index, and the pooling row element factory
  - `Selection/`: range-based row and cell selection (`TableViewSelectionModel`)
  - `AutomationPeers/`, `Strings/` (`.resw`, accessed through `TableViewLocalizedStrings`), `Themes/` (`Generic.xaml` and control templates)
- `tests/WinUI.TableView.Tests/`: WinUI test host and the shared test sources. `tests/WinUI.TableView.Tests.Uno/` links the same sources.
- `samples/`: WinUI and Uno sample apps.
- `tools/`: publish, AOT profile, and Store package scripts (see `tools/README.md`).

## Build and Test

```powershell
msbuild /restore /t:Build,Pack src/WinUI.TableView.csproj /p:Configuration=Release
dotnet test --project tests/WinUI.TableView.Tests/WinUI.TableView.Tests.csproj --configuration Release --arch x64
dotnet run --project tests/WinUI.TableView.Tests.Uno/WinUI.TableView.Tests.Uno.csproj --configuration Release --framework net10.0-desktop -p:UnoTestTargetFramework=net10.0-desktop -- --exit-after-tests
```

- WinUI tests need an interactive Windows desktop.
- WebAssembly and mobile test runs, test filtering, and platform exclusions are in `CONTRIBUTING.md` ("Running Tests"). CI commands are in `.github/workflows/ci-build.yml`.

## Implementation Principles

- Prefer simple code over new abstractions. Reuse existing helpers and infrastructure.
- Don't add dependencies without a clear justification.
- Don't refactor code unrelated to the task.
- Fix root causes; don't mask symptoms.
- Preserve public API and behavior. Discuss breaking changes in an issue first and document them in `docs/docs/breaking-changes.md`.
- Never remove, skip, or weaken tests to make a change pass.
- Never claim a build, test, or benchmark passed unless you ran it. Say what you didn't run.
- Don't assume WinUI and Uno behave the same. Verify platform-specific behavior on each, and use `#if WINDOWS` / `#if !WINDOWS` for platform code.
- When changing UI behavior, keep accessibility (automation peers), keyboard navigation, and localization working.
- Add tests to the shared test sources so they run on both WinUI and Uno.
- Update XML docs, `README.md`, and `docs/` when public behavior changes.

## Performance and Memory (Non-Negotiable)

The control must stay fast and memory-efficient with very large datasets. Never trade performance or memory for implementation convenience, and never trade correctness for performance. A change that regresses scrolling, layout, or memory is not acceptable even if it fixes a bug or adds a feature; find a design that doesn't regress.

- Hot paths (measure/arrange, scrolling, row and cell realization and recycling, binding updates, sorting, filtering) must avoid per-frame, per-row, and per-cell allocations: no LINQ, closures, boxing, uncached reflection, string formatting, or temporary collections there.
- Outside hot paths, prefer simple, readable code. Don't add complex optimizations without evidence they matter.
- Preserve row virtualization and recycling. Never force realization of all rows or cells, and don't add `UpdateLayout()` calls or full visual-tree walks in loops or hot paths.
- Avoid O(n²) work over items or columns. Cache expensive results and invalidate them precisely.
- Bound every cache, collection, and object pool; none may grow without limit.
- Balance every event subscription with an unsubscription. Use weak subscriptions where the source outlives the subscriber.
- Handle unload, reload, and recycling correctly: release references on `Unloaded`, restore state on reload, and clear stale references and state when an element is recycled. Unloaded tables must be collectable.
- Keep templates lean; every element is multiplied by every realized cell.

## Native AOT and Trimming

The library is `IsAOTCompatible`, and its AOT and trimming compatibility must be preserved.

- No runtime code generation (`Reflection.Emit`, `Expression.Compile`) or reflection that the trimmer can't see.
- Prefer compiled bindings (`{x:Bind}`) and source generators where they fit. Classes that implement WinRT interfaces or derive from WinRT types must be `partial` so CsWinRT can generate their AOT marshaling code.
- Don't suppress AOT or trimming warnings (`UnconditionalSuppressMessage`, `NoWarn`) without a justification in the code or PR.
- When changing reflection, bindings, or how objects are created, verify the result under Native AOT: `tools/publish-and-open-winui.ps1`, and for WebAssembly `tools/publish-and-serve-wasm.ps1`. Consumer-facing rules are in `docs/docs/aot-compatibility.md`.

## Performance Validation

For changes to performance-critical paths, measure before and after, covering what the change can affect:

- scrolling responsiveness and frame times
- allocations and retained memory
- realized row and cell counts
- sorting and filtering times
- datasets from 100K to 1M rows
- WinUI and Uno, especially WebAssembly
- recycling correctness and leaks

Use the publish scripts and the `TABLEVIEW_FRAME_LOG` frame log described in `tools/README.md`. Judge WebAssembly only on a published AOT build, not a Release build. Unrelated changes don't need benchmarks, but never claim a performance improvement without measurements.

## Coding Standards

`.editorconfig` defines the formatting and naming rules; follow it. Key points:

- File-scoped namespaces; `using` directives outside the namespace; no file headers.
- Private and internal instance fields use `_camelCase`.
- Nullable reference types are enabled.
- Every public type and member needs an XML doc comment (CS1591 is an error). Keep it concise; a one-sentence `<summary>` is usually enough.
- Keep code comments short, and add them only for non-obvious intent, workarounds, or platform quirks. Don't restate the code, narrate changes, or leave comments explaining removed code.
- Write "WinUI", never "WinUI 3".

## Pull Requests

- Never open a PR from `main`; use a feature branch.
- Fill in `.github/PULL_REQUEST_TEMPLATE.md`. Include performance and memory measurements when performance-critical paths change.
- CI (`ci-build.yml`) must pass.
