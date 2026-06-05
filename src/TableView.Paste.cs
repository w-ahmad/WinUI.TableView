using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Provides clipboard paste functionality for the TableView control, allowing users to paste tabular data from the clipboard into the table.
/// </summary>
public partial class TableView
{
    /// <summary>
    /// Attempts to initiate a paste operation from the clipboard. 
    /// </summary>
    /// <returns>True if the paste operation was initiated successfully; otherwise, false.</returns>
    internal bool TryStartPasteFromClipboard()
    {
        var args = new TableViewPasteFromClipboardEventArgs();
        OnPasteFromClipboard(args);

        if (args.Handled || !CanHandlePasteRequest())
        {
            return false;
        }

        PasteFromClipboardAsync();
        return true;
    }

    /// <summary>
    /// Handles the actual paste operation from the clipboard.
    /// </summary>
    private async void PasteFromClipboardAsync()
    {
        try
        {
            var content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
            {
                Debug.WriteLine("TableView: Clipboard does not contain text data to paste.");
                return;
            }

            _collectionView.AllowLiveShaping = false;

            var text = await content.GetTextAsync();
            var pastedAnyValue = PasteClipboardTextInternal(text);

            if (pastedAnyValue)
            {
                // Refresh the view to ensure any changes are reflected in the UI
                if (SortDescriptions.Count > 0 || FilterDescriptions.Count > 0)
                    RefreshView();
            }

            _collectionView.AllowLiveShaping = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TableView: Clipboard.GetText failed: {ex}");
        }
    }

    /// <summary>
    /// Processes the clipboard text and pastes it into the table starting from the determined anchor cell.
    /// </summary>
    /// <param name="text">The text content from the clipboard to be pasted into the table.</param>
    /// <returns>True if any value was successfully pasted; otherwise, false.</returns>
    private bool PasteClipboardTextInternal(string? text)
    {
        var rows = ParseClipboardText(text);
        if (rows.Count == 0)
        {
            return false;
        }

        if (!TryGetPasteAnchor(out var anchor))
        {
            Debug.WriteLine("TableView: Paste requires a current cell or a selected row/cell.");
            return false;
        }

        var pastedAnyValue = false;
        for (var rowOffset = 0; rowOffset < rows.Count; rowOffset++)
        {
            var rowIndex = anchor.Row + rowOffset;
            if (rowIndex < 0 || rowIndex >= Items.Count)
            {
                break;
            }

            if (Items[rowIndex] is not object item)
            {
                continue;
            }

            var values = rows[rowOffset];
            var columnIndex = anchor.Column;
            for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
            {
                if (columnIndex >= Columns.VisibleColumns.Count) break;

                var column = Columns.VisibleColumns[columnIndex];
                var value = values[valueIndex];

                if (column.IsReadOnly) continue;

                pastedAnyValue = column.SetClipboardContent(item, value);
                columnIndex++;
            }
        }

        return pastedAnyValue;
    }

    /// <summary>
    /// Parses the clipboard text into a list of rows, where each row is an array of values.
    /// </summary>
    /// <param name="text">The text content from the clipboard to be parsed.</param>
    /// <returns>A list of rows, where each row is an array of values.</returns>
    private static IReadOnlyList<string[]> ParseClipboardText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalizedText = text.ReplaceLineEndings("\n");
        var rows = normalizedText.Split('\n');

        if (rows.Length > 0 && rows[^1].Length == 0)
        {
            rows = rows[..^1];
        }

        return [.. rows.Select(row => row.Split('\t'))];
    }

    /// <summary>
    /// Determines whether the TableView can handle a paste request based on its current state.
    /// </summary>
    /// <returns>True if the TableView can handle the paste request; otherwise, false.</returns>
    internal bool CanHandlePasteRequest()
    {
        var focused = FocusManager.GetFocusedElement(XamlRoot!) as FrameworkElement;
        if (focused is TextBox or PasswordBox or RichEditBox)
        {
            return false;
        }

        return CanPaste && !IsReadOnly && !IsEditing && Columns.VisibleColumns.Count != 0 && Items.Count != 0;
    }

    /// <summary>
    /// Attempts to determine the anchor cell for pasting based on the current state of the TableView.
    /// </summary>
    /// <param name="anchor">The determined anchor cell for pasting.</param>
    /// <returns>True if a valid paste anchor is found; otherwise, false.</returns>
    private bool TryGetPasteAnchor(out TableViewCellSlot anchor)
    {
        if (CurrentCellSlot is { } currentCell && IsValidPasteAnchor(currentCell))
        {
            anchor = currentCell;
            return true;
        }

        if (SelectedCells.Count > 0)
        {
            anchor = SelectedCells.OrderBy(slot => slot.Row)
                                  .ThenBy(slot => slot.Column)
                                  .First();
            return true;
        }

        var rowIndex = SelectedRanges.Any()
            ? SelectedRanges.Min(range => range.FirstIndex)
            : CurrentRowIndex;

        if (rowIndex is >= 0)
        {
            var columnIndex = GetFirstWritableVisibleColumnIndex();
            if (columnIndex >= 0)
            {
                anchor = new TableViewCellSlot(rowIndex.Value, columnIndex);
                return true;
            }
        }

        anchor = default;
        return false;
    }

    /// <summary>
    /// Validates whether the given TableViewCellSlot is a valid anchor for pasting.
    /// </summary>
    /// <param name="slot">The TableViewCellSlot to validate.</param>
    /// <returns>True if the slot is a valid paste anchor; otherwise, false.</returns>
    private bool IsValidPasteAnchor(TableViewCellSlot slot)
    {
        return slot.Row >= 0
            && slot.Row < Items.Count
            && slot.Column >= 0
            && slot.Column < Columns.VisibleColumns.Count;
    }

    /// <summary>
    /// Finds the index of the first visible column that is writable (not read-.
    /// </summary>
    /// <returns>The index of the first writable visible column, or -1 if none are found.</returns>
    private int GetFirstWritableVisibleColumnIndex()
    {
        for (var i = 0; i < Columns.VisibleColumns.Count; i++)
        {
            if (Columns.VisibleColumns[i] is { IsReadOnly: false, ClipboardContentBinding: { } })
            {
                return i;
            }
        }

        return -1;
    }
}
