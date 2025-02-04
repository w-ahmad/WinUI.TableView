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

    public virtual void PrepareFilterItems(TableViewColumn column, string? searchText = default)
    {
        if (column is { TableView.ItemsSource: { } })
        {
            var collectionView = new CollectionView(column.TableView.ItemsSource);
            collectionView.FilterDescriptions.AddRange(
                column.TableView.FilterDescriptions.Where(
                x => x is not ColumnFilterDescription columnFilter || columnFilter.Column != column));

            var filterValues = new SortedSet<object?>();

            foreach (var item in collectionView)
            {
                var value = column.GetCellContent(item);
                filterValues.Add(value);
            }

            FilterItems = filterValues.Select(value =>
            {
                value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
                var isSelected = !column.IsFiltered || !string.IsNullOrEmpty(searchText) ||
                  (column.IsFiltered && SelectedValues.Contains(value));

                return string.IsNullOrEmpty(searchText)
                      || value?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true
                      ? new TableViewFilterItem(isSelected, value)
                      : null;

            }).OfType<TableViewFilterItem>()
              .ToList();
        }
    }

    public virtual void SearchTextChanged(TableViewColumn column, string? searchText)
    {
        PrepareFilterItems(column, searchText);
    }

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

    public virtual void ClearFilter(TableViewColumn? column)
    {
        if (column is { TableView: { } })
        {
            column.IsFiltered = false;
            column.TableView.FilterDescriptions.RemoveWhere(x => x is ColumnFilterDescription columnFilter && columnFilter.Column == column);
            column.TableView.RefreshFilter();
        }
        else
        {
            _tableView.ClearAllFilters();
        }
    }

    public virtual bool Filter(TableViewColumn column, object? item)
    {
        var value = column?.GetCellContent(item);
        value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
        return SelectedValues.Contains(value);
    }

    public IList<TableViewFilterItem> FilterItems { get; set; } = new List<TableViewFilterItem>();

    public IList<object> SelectedValues { get; set; } = new List<object>();
}
