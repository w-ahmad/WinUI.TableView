using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides methods to capture and apply <see cref="TableViewState"/> snapshots,
/// enabling persistence of a <see cref="TableView"/>'s sort, filter, and column layout state.
/// </summary>
/// <remarks>
/// This helper operates exclusively on state that can be round-tripped through serialization.
/// Runtime-only constructs (such as <see cref="FilterDescription.Predicate"/>) are intentionally
/// excluded — see the filter-related methods for details. Serialization itself (e.g. JSON) is
/// the responsibility of the consuming application.
/// </remarks>
internal static class TableViewStateHelper
{
    /// <summary>
    /// Captures the current sort, filter, and column layout state of <paramref name="tableView"/>
    /// into a new <see cref="TableViewState"/> instance.
    /// </summary>
    /// <param name="tableView">The table view whose state should be captured.</param>
    /// <returns>A <see cref="TableViewState"/> representing the current state.</returns>
    internal static TableViewState Capture(TableView tableView)
    {
        ArgumentNullException.ThrowIfNull(tableView);

        var state = new TableViewState();
        CaptureSort(tableView, state);
        CaptureFilter(tableView, state);
        CaptureColumns(tableView, state);
        return state;
    }

    /// <summary>
    /// Applies a previously captured <paramref name="state"/> to <paramref name="tableView"/>,
    /// restoring its sort, filter, and column layout state.
    /// Columns are restored first so that ordering is correct before sort and filter are applied.
    /// Unrecognised column keys are silently skipped; a <see langword="null"/>
    /// <paramref name="state"/> is a no-op.
    /// </summary>
    /// <param name="tableView">The target table view.</param>
    /// <param name="state">The state to apply, or <see langword="null"/> to skip.</param>
    internal static void Apply(TableView tableView, TableViewState? state)
    {
        ArgumentNullException.ThrowIfNull(tableView);

        if (state is null)
        {
            return;
        }

        ApplyColumns(tableView, state.Columns);
        ApplySort(tableView, state.SortDescriptions);
        ApplyFilter(tableView, state.FilterDescriptions);
    }

    // ── Sort ──────────────────────────────────────────────────────────────────

    private static void CaptureSort(TableView tableView, TableViewState state)
    {
        foreach (var sd in tableView.SortDescriptions)
        {
            state.SortDescriptions.Add(new TableViewSortDescriptionState
            {
                PropertyName = sd.PropertyName,
                Direction = sd.Direction,
            });
        }
    }

    private static void ApplySort(TableView tableView, IEnumerable<TableViewSortDescriptionState> sortDescriptions)
    {
        tableView.SortDescriptions.Clear();

        foreach (var sd in sortDescriptions)
        {
            if (string.IsNullOrWhiteSpace(sd.PropertyName))
            {
                continue;
            }

            tableView.SortDescriptions.Add(new SortDescription(sd.PropertyName, sd.Direction));
        }
    }

    // ── Filter ────────────────────────────────────────────────────────────────
    //
    // FilterDescription.Predicate is a runtime-only lambda and cannot be serialized.
    // Only the user-selected values from FilterHandler.SelectedValues are captured (as strings).
    // On restore, the predicate is reconstructed by calling FilterHandler.ApplyFilter,
    // which rebuilds it from the restored SelectedValues.

    private static void CaptureFilter(TableView tableView, TableViewState state)
    {
        foreach (var fd in tableView.FilterDescriptions)
        {
            // ColumnFilterDescription (internal) carries a direct column reference; use it
            // when available to avoid the slower property-name lookup.
            var column = fd is ColumnFilterDescription cfd
                ? cfd.Column
                : FindColumnByPropertyName(tableView, fd.PropertyName);

            if (column is null)
            {
                continue;
            }

            var filterState = new TableViewFilterDescriptionState
            {
                ColumnKey = GetColumnKey(column),
            };

            if (tableView.FilterHandler.SelectedValues.TryGetValue(column, out var selectedValues))
            {
                foreach (var value in selectedValues)
                {
                    filterState.SelectedValues.Add(value?.ToString());
                }
            }

            state.FilterDescriptions.Add(filterState);
        }
    }

    private static void ApplyFilter(TableView tableView, IEnumerable<TableViewFilterDescriptionState> filterDescriptions)
    {
        // Clear all existing column filters before restoring.
        tableView.FilterHandler?.ClearFilter(null);

        foreach (var filterState in filterDescriptions)
        {
            if (string.IsNullOrWhiteSpace(filterState.ColumnKey) || filterState.SelectedValues.Count == 0)
            {
                continue;
            }

            var column = FindColumnByKey(tableView, filterState.ColumnKey);
            if (column is null)
            {
                continue;
            }

            var targetType = FindColumnValueType(tableView, column);
            var selectedValues = filterState.SelectedValues
                .Select(v => ConvertFromString(v, targetType))
                .ToList<object?>();

            tableView.FilterHandler.SelectedValues[column] = selectedValues;
            tableView.FilterHandler.ApplyFilter(column);
        }
    }

    // ── Columns ───────────────────────────────────────────────────────────────

    private static void CaptureColumns(TableView tableView, TableViewState state)
    {
        for (var index = 0; index < tableView.Columns.Count; index++)
        {
            var column = tableView.Columns[index];
            state.Columns.Add(new TableViewColumnState
            {
                Key = GetColumnKey(column),
                Header = column.Header?.ToString(),
                Visibility = column.Visibility,
                DisplayIndex = index,
                WidthValue = column.Width.Value,
                WidthUnitType = column.Width.GridUnitType,
            });
        }
    }

    private static void ApplyColumns(TableView tableView, IEnumerable<TableViewColumnState> columns)
    {
        foreach (var columnState in columns.OrderBy(c => c.DisplayIndex))
        {
            var column = FindColumnByKey(tableView, columnState.Key);
            if (column is null)
            {
                continue;
            }

            // Only restore the header if a non-empty string was saved, to avoid overwriting
            // complex header objects (e.g. DataTemplates set by code) with a plain string.
            if (!string.IsNullOrWhiteSpace(columnState.Header))
            {
                column.Header = columnState.Header;
            }

            column.Visibility = columnState.Visibility;
            column.Width = new GridLength(columnState.WidthValue, columnState.WidthUnitType);

            var currentIndex = tableView.Columns.IndexOf(column);
            var targetIndex = Math.Clamp(columnState.DisplayIndex, 0, tableView.Columns.Count - 1);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                tableView.Columns.Move(currentIndex, targetIndex);
            }
        }
    }

    // ── Key resolution ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the stable key used to identify a column across sessions.
    /// </summary>
    private static string GetColumnKey(TableViewColumn column)
    {
        // For bound columns the binding property path is the primary key because it is
        // semantically tied to the data model: if the binding changes, the column represents
        // different data and a state miss is the correct outcome.
        // Edge case: if two bound columns share the same property path, set Tag on both
        // to disambiguate; Tag takes precedence in that case (see the fallback below).
        if (column is TableViewBoundColumn boundColumn
            && !string.IsNullOrWhiteSpace(boundColumn.PropertyPath))
        {
            return boundColumn.PropertyPath!;
        }

        // For template columns (which have no binding), Tag is the primary key — it is
        // developer-assigned and intentional. Changing the Tag signals a deliberate identity
        // change, so a state miss is the correct outcome.
        // Header is a last resort and is fragile (may be localised or display-renamed
        // without any change to the column's purpose).
        return column.Tag?.ToString()
            ?? column.Header?.ToString()
            ?? string.Empty;
    }

    private static TableViewColumn? FindColumnByKey(TableView tableView, string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return tableView.Columns
            .FirstOrDefault(c => string.Equals(GetColumnKey(c), key, StringComparison.OrdinalIgnoreCase));
    }

    private static TableViewColumn? FindColumnByPropertyName(TableView tableView, string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        foreach (var column in tableView.Columns)
        {
            if (column is TableViewBoundColumn boundColumn
                && string.Equals(boundColumn.PropertyPath, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }

            if (string.Equals(column.SortMemberPath, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return column;
            }
        }

        return null;
    }

    // ── Filter value type resolution ───────────────────────────────────────────

    private static Type? FindColumnValueType(TableView tableView, TableViewColumn column)
    {
        if (tableView.ItemsSource is not IEnumerable items)
        {
            return null;
        }

        foreach (var item in items)
        {
            var value = column.GetCellContent(item);
            if (value is not null)
            {
                return value.GetType();
            }
        }

        return null;
    }

    private static object? ConvertFromString(string? value, Type? targetType)
    {
        if (value is null)
        {
            return null;
        }

        if (targetType is null || targetType == typeof(string))
        {
            return value;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(bool) && bool.TryParse(value, out var boolVal)) return boolVal;
        if (underlying == typeof(int) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)) return intVal;
        if (underlying == typeof(long) && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal)) return longVal;
        if (underlying == typeof(short) && short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortVal)) return shortVal;
        if (underlying == typeof(float) && float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatVal)) return floatVal;
        if (underlying == typeof(double) && double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleVal)) return doubleVal;
        if (underlying == typeof(decimal) && decimal.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var decimalVal)) return decimalVal;
        if (underlying == typeof(Guid) && Guid.TryParse(value, out var guidVal)) return guidVal;
        if (underlying == typeof(DateTime) && DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeVal)) return dateTimeVal;
        if (underlying.IsEnum && Enum.TryParse(underlying, value, ignoreCase: true, out var enumVal)) return enumVal;

        return value;
    }
}
