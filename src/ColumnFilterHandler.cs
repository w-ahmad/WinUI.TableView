using System;
using System.Collections.Generic;
using System.Linq;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Default implementation of the IColumnFilterHandler interface.
/// </summary>
public class ColumnFilterHandler : IColumnFilterHandler
{
    private readonly TableView _tableView;

    /// <summary>
    /// Initializes a new instance of the ColumnFilterHandler class.
    /// </summary>
    public ColumnFilterHandler(TableView tableView)
    {
        _tableView = tableView;
    }

    /// <inheritdoc/>
    public virtual IList<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText = default)
    {
        if (column is { TableView.ItemsSource: { } })
        {
            var collectionView = new CollectionView(column.TableView.ItemsSource);
            collectionView.FilterDescriptions.AddRange(
                column.TableView.FilterDescriptions.Where(
                x => x is not ColumnFilterDescription columnFilter || columnFilter.Column != column));

            return [.. collectionView.Select(column.GetCellContent)
                                     .Select(x => IsBlank(x) ? null : x)
                                     .GroupBy(x => x)
                                     .OrderBy(x => x.Key)
                                     .Select(x =>
                                     {
                                         var value = x.Key;
                                         value ??= TableViewLocalizedStrings.BlankFilterValue;
                                         var isSelected = !column.IsFiltered || !string.IsNullOrEmpty(searchText) ||
                                           (column.IsFiltered && SelectedValues[column].Contains(value));

                                         return string.IsNullOrEmpty(searchText)
                                               || value?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true
                                               ? new TableViewFilterItem(isSelected, value, x.Count())
                                               : null;

                                     })
                                     .OfType<TableViewFilterItem>()
                                     .OrderByDescending(x => _tableView.ShowFilterItemsCount ? x.Count : 0)];
        }

        return [];
    }

    private static bool IsBlank(object? value)
    {
        return value == null ||
               value == DBNull.Value ||
               (value is string str && string.IsNullOrWhiteSpace(str)) ||
               (value is Guid guid && guid == Guid.Empty);
    }

    /// <inheritdoc/>
    public virtual void ApplyFilter(TableViewColumn column)
    {
        if (column is { TableView: { } })
        {
            column.TableView.DeselectAll();

            if (column.IsFiltered)
            {
                column.TableView.RefreshFilter();
            }
            else
            {
                var boundColumn = column as TableViewBoundColumn;

                column.IsFiltered = true;
                column.TableView.FilterDescriptions.Add(new ColumnFilterDescription(
                    column,
                    boundColumn?.PropertyPath,
                    (o) => Filter(column, o)));
            }
            column.TableView.RefreshFilter();
            column.TableView.EnsureAlternateRowColors();
        }
    }

    /// <inheritdoc/>
    public virtual void ClearFilter(TableViewColumn? column)
    {
        if (column is { TableView: { } })
        {
            column.IsFiltered = false;
            column.TableView.FilterDescriptions.RemoveWhere(x => x is ColumnFilterDescription columnFilter && columnFilter.Column == column);
            SelectedValues.RemoveWhere(x => x.Key == column);
            column.TableView.RefreshFilter();
        }
        else
        {
            SelectedValues.Clear();
            _tableView.FilterDescriptions.Clear();

            foreach (var col in _tableView.Columns)
            {
                if (col is not null)
                {
                    col.IsFiltered = false;
                }
            }
        }
    }

    /// <inheritdoc/>
    public virtual bool Filter(TableViewColumn column, object? item)
    {
        var value = column.GetCellContent(item);
        value = IsBlank(value) ? TableViewLocalizedStrings.BlankFilterValue : value!;
        return SelectedValues[column].Contains(value);
    }

    /// <inheritdoc/>
    public IDictionary<TableViewColumn, IList<object>> SelectedValues { get; } = new Dictionary<TableViewColumn, IList<object>>();
}
