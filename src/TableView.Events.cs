using Microsoft.UI.Xaml;
using System;

namespace WinUI.TableView;

partial class TableView
{
    /// <summary>
    /// Event triggered when a column is auto-generating.
    /// </summary>
    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;

    /// <summary>
    /// Event triggered when exporting all rows content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportAllContent;

    /// <summary>
    /// Event triggered when exporting selected rows or cells content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportSelectedContent;

    /// <summary>
    /// Event triggered when copying selected rows or cell content to the clipboard.
    /// </summary>
    public event EventHandler<TableViewCopyToClipboardEventArgs>? CopyToClipboard;

    /// <summary>
    /// Event triggered when the IsReadOnly property changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? IsReadOnlyChanged;

    /// <summary>
    /// Event triggered when the row context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewRowContextFlyoutEventArgs>? RowContextFlyoutOpening;

    /// <summary>
    /// Event triggered when the cell context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewCellContextFlyoutEventArgs>? CellContextFlyoutOpening;

    /// <summary>
    /// Occurs when a sorting is being applied to a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewSortingEventArgs>? Sorting;

    /// <summary>
    /// Occurs when sorting is cleared from a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewClearSortingEventArgs>? ClearSorting;

    /// <summary>
    /// Event triggered when selected cells change.
    /// </summary>
    public event EventHandler<TableViewCellSelectionChangedEventArgs>? CellSelectionChanged;

    /// <summary>
    /// Event triggered when the current cell changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? CurrentCellChanged;
}