# DataGrid Feature Comparison: WPF DataGrid, WCT DataGrid, and WinUI.TableView

DataGrids are heavily used in business desktop applications. Many WPF and UWP applications depend on a grid control for editing, sorting, filtering, row details, validation, clipboard operations, and large data-entry screens.

When developers decide to move their existing WPF or UWP applications to WinUI, one of the first challenges they face is finding a suitable replacement for DataGrid. WinUI does not provide a built-in DataGrid control, so developers need to evaluate community or third-party options based on the features their applications depend on.

This is where WinUI.TableView comes in. It provides a lightweight and feature-rich DataGrid-like control for WinUI and Uno Platform applications, making it easier to adopt WinUI without rebuilding common tabular data scenarios from scratch.

Microsoft’s WPF-to-WinUI migration guidance lists **WinUI.TableView** as one of the community-maintained options for DataGrid scenarios.

The Windows Community Toolkit DataGrid was previously available for UWP and Uno Platform apps, but it is now archived and is not available in the current version of the Windows Community Toolkit. The archived Toolkit documentation recommends using newer alternatives such as **DataTable** or **WinUI.TableView** for new development.

Because of this, developers migrating from WPF DataGrid or WCT DataGrid to WinUI need a clear way to compare the available features.

## Why this document exists

This document provides a practical feature comparison between:

- WPF DataGrid
- Windows Community Toolkit DataGrid
- WinUI.TableView

The goal is to help developers quickly understand:

- which features are already available in WinUI.TableView
- which WPF or WCT DataGrid features have similar support
- which features may require custom implementation
- which areas are currently missing or not yet verified

WinUI.TableView is actively maintained and continues to add new features based on real-world usage. If a feature you need is missing, you are welcome to open an issue, start a discussion, or contribute directly on GitHub.

## Feature comparison

| Feature | WPF DataGrid | WCT DataGrid | WinUI.TableView | Notes |
|---|---|---|---|---|
| Auto-generate columns | ✅ | ✅ | ✅ | |
| Text column | ✅ | ✅ | ✅ |  |
| CheckBox column | ✅ | ✅ | ✅ |  |
| ComboBox column | ✅ | ✅ | ✅ |  |
| Template column | ✅ | ✅ | ✅ |  |
| Hyperlink column | ✅ | ❌ | ✅ | |
| Number column | ❌ | ❌ | ✅ | |
| Date column | ❌ | ❌ | ✅ | |
| Time column | ❌ | ❌ | ✅ | |
| ToggleSwitch column | ❌ | ❌ | ✅ |  |
| Cell editing | ✅ | ✅ | ✅ |  |
| Editing lifecycle events | ✅ | ✅ | ✅ | |
| Add new row | ✅ | ❌ | ❌ | |
| Delete row | ✅ | ❌ | ❌ | |
| Sorting | ✅ | ⚠️ | ✅ | No built-in sorting in WCT DataGrid but exposes sorting events. |
| Filtering | ⚠️ | ❌ | ✅ | WPF commonly uses collection view/app logic. WCT docs explicitly say there is no built-in filtering. TableView provides Excel-like column filtering. |
| Grouping | ✅ | ✅ | ❌ |  |
| Row selection | ✅ | ✅ | ✅ |  |
| Cell selection | ✅ | ❌ | ✅ | |
| Clipboard copy | ✅ | ✅ | ✅ |  |
| Custom clipboard content | ✅ | ✅ | ✅ |  |
| Clipboard paste | ❌ | ❌ | ✅ | TableView includes paste support. |
| CSV export | ❌ | ❌ | ✅ | TableView has built-in CSV export support. |
| Column resize | ✅ | ✅ | ✅ |  |
| Row resize | ✅ | ❌ | ❌ |  |
| Column reorder | ✅ | ✅ | ✅ |  |
| Frozen columns | ✅ | ✅ | ✅ |  |
| Row details | ✅ | ✅ | ✅ |  |
| Frozen row details | ✅ | ✅ | ✅ | |
| Row headers | ✅ | ✅ | ✅ | |
| Grid lines | ✅ | ✅ | ✅ |  |
| Alternating rows | ✅ | ✅ | ✅ |  |
| Conditional styling | ❌| ❌ | ✅ | TableView supports conditional cell styling. |
| Cell and row context flyouts | ❌ | ❌ | ✅ | TableView has built-in support for row and cell context flyouts. |
| Accessibility / Narrator | Not verified | ✅ | ❌ |  |
| Validation | ✅ | ❌ | ❌ | WPF has strong cell and row validation support. TableView editing events may help. |
| Row virtualization | ✅ | ✅ | ✅ |  |

| Value | Meaning | 
|---|---|
| ✅ | Built-in or directly supported by the control. |
| ⚠️ | Partial. The control provides some support, but the full behavior requires app-side logic or does not fully match the other controls. |
| ❌ | Not available as a built-in or first-class feature. |
| Not verified | Not confirmed from the checked documentation or source. |

## Filling the gaps

WinUI.TableView is actively maintained and continues to evolve.

If you find a missing feature that is important for migration from WPF DataGrid or WCT DataGrid, please consider contributing in one of these ways:

- open a GitHub issue with your scenario
- start a GitHub discussion for API design
- share a small sample that shows the missing behavior
- improve the documentation
- contribute a pull request
- help test new preview features

Real migration scenarios are very valuable because they help shape the control around practical application needs instead of assumptions.

## References

- Microsoft WPF to WinUI 3 migration guidance:  
  https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/wpf-patterns-winui3

- Archived Windows Community Toolkit DataGrid documentation:  
  https://learn.microsoft.com/en-us/dotnet/communitytoolkit/archive/windows/datagrid

- WPF DataGrid documentation:  
  https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/datagrid

- WPF DataGrid API reference:  
  https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.datagrid

- WCT DataGrid grouping, sorting, and filtering guidance:  
  https://learn.microsoft.com/en-us/dotnet/communitytoolkit/archive/windows/datagrid-guidance/group-sort-filter

- WCT DataGrid API reference:  
  https://learn.microsoft.com/en-us/dotnet/api/microsoft.toolkit.uwp.ui.controls.datagrid