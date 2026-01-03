using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;

namespace WinUI.TableView;

/// <summary>
/// Partial class for TableView that contains Events and firing event methods.
/// </summary>
partial class TableView
{
    /// <summary>
    /// Event triggered when a column is auto-generating.
    /// </summary>
    public event EventHandler<TableViewAutoGeneratingColumnEventArgs>? AutoGeneratingColumn;

    /// <summary>
    /// Called before the AutoGeneratingColumn event occurs.
    /// </summary>
    /// <param name="args">Cancelable event args.</param>
    protected virtual void OnAutoGeneratingColumn(TableViewAutoGeneratingColumnEventArgs args)
    {
        AutoGeneratingColumn?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when exporting all rows content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportAllContent;

    /// <summary>
    /// Called before the ExportAllContent event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnExportAllContent(TableViewExportContentEventArgs args)
    {
        ExportAllContent?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when exporting selected rows or cells content.
    /// </summary>
    public event EventHandler<TableViewExportContentEventArgs>? ExportSelectedContent;

    /// <summary>
    /// Called before the ExportSelectedContent event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnExportSelectedContent(TableViewExportContentEventArgs args)
    {
        ExportSelectedContent?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when copying selected rows or cell content to the clipboard.
    /// </summary>
    public event EventHandler<TableViewCopyToClipboardEventArgs>? CopyToClipboard;

    /// <summary>
    /// Called before the CopyToClipboard event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnCopyToClipboard(TableViewCopyToClipboardEventArgs args)
    {
        CopyToClipboard?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when the IsReadOnly property changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? IsReadOnlyChanged;

    /// <summary>
    /// Called before the <see cref="IsReadOnlyChanged"/> event occurs.
    /// </summary>
    protected virtual void OnIsReadOnlyChanged(DependencyPropertyChangedEventArgs args)
    {
        IsReadOnlyChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when the row context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewRowContextFlyoutEventArgs>? RowContextFlyoutOpening;

    /// <summary>
    /// Called before the <see cref="RowContextFlyoutOpening"/> event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnRowContextFlyoutOpening(TableViewRowContextFlyoutEventArgs args)
    {
        RowContextFlyoutOpening?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when a row is double-tapped/clicked.
    /// </summary>
    public event EventHandler<TableViewRowDoubleTappedEventArgs>? RowDoubleTapped;

    /// <summary>
    /// Called before the <see cref="RowDoubleTapped"/> event occurs.
    /// </summary>
    /// <param name="args">The event data.</param>
    protected virtual void OnRowDoubleTapped(TableViewRowDoubleTappedEventArgs args)
    {
        RowDoubleTapped?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when the cell context flyout is opening.
    /// </summary>
    public event EventHandler<TableViewCellContextFlyoutEventArgs>? CellContextFlyoutOpening;

    /// <summary>
    /// Called before the <see cref="CellContextFlyoutOpening"/> event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected virtual void OnCellContextFlyoutOpening(TableViewCellContextFlyoutEventArgs args)
    {
        CellContextFlyoutOpening?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs when a sorting is being applied to a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewSortingEventArgs>? Sorting;

    /// <summary>
    /// Called before the <see cref="Sorting"/> event occurs.
    /// </summary>
    /// <param name="args">Handleable event args.</param>
    protected internal virtual void OnSorting(TableViewSortingEventArgs args)
    {
        Sorting?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs when sorting is cleared from a column in the TableView.
    /// </summary>
    public event EventHandler<TableViewClearSortingEventArgs>? ClearSorting;

    /// <summary>
    /// Called before the <see cref="ClearSorting"/> event occurs.
    /// </summary>
    /// <param name="args">The event data.</param>
    protected internal virtual void OnClearSorting(TableViewClearSortingEventArgs args)
    {
        ClearSorting?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when selected cells change.
    /// </summary>
    public event EventHandler<TableViewCellSelectionChangedEventArgs>? CellSelectionChanged;

    /// <summary>
    /// Called before the <see cref="CellSelectionChanged"/> event occurs.
    /// </summary>
    protected virtual void OnCellSelectionChanged(TableViewCellSelectionChangedEventArgs args)
    {
        CellSelectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Event triggered when the current cell changes.
    /// </summary>
    public event DependencyPropertyChangedEventHandler? CurrentCellChanged;

    /// <summary>
    /// Called before the <see cref="CurrentCellChanged"/> event occurs.
    /// </summary>
    protected virtual void OnCurrentCellChanged(DependencyPropertyChangedEventArgs args)
    {
        CurrentCellChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs before a cell enters edit mode and allows the edit operation to be canceled.
    /// </summary>
    public event EventHandler<TableViewBeginningEditEventArgs>? BeginningEdit;

    /// <summary>
    /// Called before the <see cref="BeginningEdit"/> event occurs.
    /// </summary>
    /// <param name="args">Cancelable event args.</param>
    protected internal virtual void OnBeginningEdit(TableViewBeginningEditEventArgs args)
    {
        BeginningEdit?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs when a cell enters edit mode, after the editing element has been created.
    /// </summary>
    public event EventHandler<TableViewPreparingCellForEditEventArgs>? PreparingCellForEdit;

    /// <summary>
    /// Called before the <see cref="PreparingCellForEdit"/> event occurs.
    /// </summary>
    /// <param name="args">Event args.</param>
    protected internal virtual void OnPreparingCellForEdit(TableViewPreparingCellForEditEventArgs args)
    {
        PreparingCellForEdit?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs just before a cell edit is committed or canceled, and allows the action to be canceled.
    /// </summary>
    public event EventHandler<TableViewCellEditEndingEventArgs>? CellEditEnding;

    /// <summary>
    /// Called before the <see cref="CellEditEnding"/> event occurs.
    /// </summary>
    /// <param name="args">Cancelable event args.</param>
    protected virtual void OnCellEditEnding(TableViewCellEditEndingEventArgs args)
    {
        CellEditEnding?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs after a cell edit has been committed or canceled.
    /// </summary>
    public event EventHandler<TableViewCellEditEndedEventArgs>? CellEditEnded;

    /// <summary>
    /// Called before the <see cref="CellEditEnded"/> event occurs.
    /// </summary>
    /// <param name="args">Event args.</param>
    protected virtual void OnCellEditEnded(TableViewCellEditEndedEventArgs args)
    {
        CellEditEnded?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs before a column is reordered.
    /// </summary>
    public event EventHandler<TableViewColumnReorderingEventArgs>? ColumnReordering;

    /// <summary>
    /// Called before the <see cref="ColumnReordering"/> event occurs.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected internal virtual void OnColumnReordering(TableViewColumnReorderingEventArgs args)
    {
        ColumnReordering?.Invoke(this, args);
    }

    /// <summary>
    /// Occurs after a column has been reordered.
    /// </summary>
    public event EventHandler<TableViewColumnReorderedEventArgs>? ColumnReordered;

    /// <summary>
    /// Called before the <see cref="ColumnReordered"/> event occurs.
    /// </summary>
    /// <param name="args">The event arguments.</param>
    protected internal virtual void OnColumnReordered(TableViewColumnReorderedEventArgs args)
    {
        ColumnReordered?.Invoke(this, args);
    }
}