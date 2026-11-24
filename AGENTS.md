# Agent Instructions for WinUI.TableView

This is the single source of instructions for every coding agent working in this repository (Claude Code, GitHub Copilot, Codex, Cursor, and others). Tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`) only point here. Edit this file, not those.

## Project Overview

**WinUI.TableView** is a lightweight and fast data grid control for WinUI apps with support for the Uno Platform. It derives from `ListView` and provides an Excel-like data table experience with column filtering, sorting, editing, and data export.

- **Repository**: https://github.com/w-ahmad/WinUI.TableView
- **Documentation**: https://w-ahmad.github.io/WinUI.TableView/
- **NuGet Package**: WinUI.TableView
- **License**: MIT

## Technology Stack

- **Primary Platform**: WinUI (Windows App SDK)
- **Cross-Platform Support**: Uno Platform (Desktop/Skia, WebAssembly, Android, iOS)
- **Library Target Frameworks**: `net8.0`, `net9.0`, `net10.0` (Uno) and `net8.0/9.0/10.0-windows10.0.19041.0` (WinUI)
- **SDK**: .NET 10 (see `global.json`), Uno.Sdk and MSTest.Sdk versions pinned in `global.json`
- **Languages**: C# with XAML
- **Central package management**: versions live in `Directory.Packages.props`

## Repository Structure

```
/
├── src/                                   # Library source
│   ├── Columns/                           # Column types
│   ├── Controls/                          # Custom controls
│   ├── Converters/                        # XAML value converters
│   ├── EventArgs/                         # Event argument classes
│   ├── Extensions/                        # Extension methods
│   ├── Helpers/                           # Helpers and utilities
│   ├── ItemsSource/                       # Data source handling
│   ├── Primitives/                        # Base/primitive controls
│   ├── Strings/                           # Localization (.resw)
│   ├── Themes/                            # Styles and control templates
│   ├── TableView.cs                       # Main control
│   └── WinUI.TableView.csproj
├── tests/
│   ├── WinUI.TableView.Tests/             # WinUI test host + shared test sources
│   └── WinUI.TableView.Tests.Uno/         # Uno test host linking the shared sources
├── samples/
│   ├── WinUI.TableView.SampleApp/         # WinUI sample app
│   └── WinUI.TableView.SampleApp.Uno/     # Uno sample app
├── docs/                                  # DocFX documentation
├── .github/                               # Workflows, issue and PR templates
└── WinUI.TableView.slnx
```

## Build and Test

**Prerequisites**: Visual Studio 2022 or later, Windows SDK 10.0.19041.0+, .NET 8, 9 and 10 SDKs.

**Build and pack the library**:
```powershell
msbuild /restore /t:Build,Pack src/WinUI.TableView.csproj /p:Configuration=Release
```
NuGet packages land in `artifacts/NuGet/Release/`.

**WinUI tests** (require an interactive Windows desktop, x64 only):
```powershell
dotnet test --project tests/WinUI.TableView.Tests/WinUI.TableView.Tests.csproj --configuration Release --arch x64
```

**Uno Desktop tests**:
```powershell
dotnet run --project tests/WinUI.TableView.Tests.Uno/WinUI.TableView.Tests.Uno.csproj --configuration Release --framework net10.0-desktop -p:UnoTestTargetFramework=net10.0-desktop -- --exit-after-tests
```

See `.github/workflows/ci-build.yml` for the exact CI commands, including WebAssembly and mobile test runs.

## Coding Standards

Formatting is defined in `.editorconfig`. Key conventions:

- 4-space indentation, CRLF line endings
- File-scoped namespaces
- Nullable reference types enabled
- Private fields use `_camelCase`; public members use PascalCase; interfaces start with `I`
- `using` directives outside the namespace
- No file headers

### Code Comments

- Keep code comments short and add them only when necessary.
- Don't comment what the code already says; comment only non-obvious intent, workarounds, or platform quirks.
- One short line is preferred; no multi-line explanations or narration of changes.
- Don't leave comments explaining removed code (e.g. `// removed X because...`); just delete it.

### XML Documentation

- All public types and members must have XML doc comments (CS1591 is an error).
- Keep them concise: a one-sentence `<summary>` is usually enough.

### Platform-Specific Code

- The library builds for both WinUI and Uno. Use conditional compilation (`#if WINDOWS`) for WinUI-only code.
- Keep changes working on both WinUI and Uno; test both when possible.

### XAML

- Styles and control templates live in `src/Themes/`; `Generic.xaml` is the main resource dictionary.

## Core Components

- **TableView**: main control, derived from `ListView`
- **Columns**: `TableViewTextColumn`, `TableViewCheckBoxColumn`, `TableViewComboBoxColumn`, `TableViewNumberColumn`, `TableViewToggleSwitchColumn`, `TableViewTemplateColumn`, `TableViewTimeColumn`, `TableViewDateColumn`
- **Supporting classes**: `TableViewCell`, `TableViewRow`, `TableViewColumnHeader`, `TableViewHeaderRow`, `TableViewColumnsCollection`

## Performance and Memory (Non-Negotiable)

This control must stay fast and memory-efficient with very large datasets. Performance and memory efficiency are never traded for convenience, brevity, or a quicker fix. A change that makes scrolling, layout, or memory use worse is not acceptable, even if it fixes a bug or adds a feature; find a design that doesn't regress.

- **Hot paths** (measure/arrange, scrolling, row and cell creation/recycling, binding updates, sorting, filtering) must not allocate per frame, per row, or per cell when it can be avoided. No LINQ, closures, boxing, string formatting, or temporary collections in these paths.
- **Virtualization** must stay intact: never force realization of all rows, cells, or columns, and never call `UpdateLayout()` or walk the full visual tree in loops.
- **Algorithms**: avoid O(n²) work over items or columns; cache what is expensive to recompute and invalidate it precisely.
- **Reflection**: cache property accessors; never reflect per cell or per value read.
- **Memory leaks**: unsubscribe event handlers, release references on `Unloaded`, and prefer weak references for long-lived subscriptions to item or collection events. Unloaded tables, rows, and cells must be collectable.
- **XAML element count**: keep templates lean; every extra element is multiplied by every realized cell.
- **Collections**: size them upfront when the count is known; reuse buffers instead of reallocating.
- When a change touches a hot path, state its performance and memory impact in the PR and measure it with a large dataset (100k+ rows) where practical.

## Making Changes

- Make minimal, focused changes; preserve existing behavior unless fixing a bug.
- Avoid breaking public API changes; discuss them in an issue first.
- New column types go in `src/Columns/`, inheriting from `TableViewBoundColumn` or `TableViewColumn`.
- Add or update tests in the shared test sources so they run on both WinUI and Uno.
- Update XML docs, `README.md` and `docs/` when public behavior changes.
- Localized strings are `.resw` files in `src/Strings/`, accessed through `TableViewLocalizedStrings`.
- Tests can access internals via `InternalsVisibleTo`.

## Pull Requests

- Never open a PR from `main`; use a feature branch.
- Fill in `.github/PULL_REQUEST_TEMPLATE.md`.
- CI (`ci-build.yml`) must pass.

## Naming

- Write "WinUI", never "WinUI 3".
