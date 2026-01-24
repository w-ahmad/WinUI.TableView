using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            var collectionView = new CollectionView(liveShapingEnabled: false);
            collectionView.FilterDescriptions.AddRange(
                column.TableView.FilterDescriptions.Where(
                x => x is not ColumnFilterDescription columnFilter || columnFilter.Column != column));
            if (searchText is { Length: > 0 })
            {
                collectionView.FilterDescriptions.Add(new FilterDescription(default, item =>
                    {
                        var value = column.GetCellContent(item);
                        return string.IsNullOrEmpty(searchText) ||
                               value?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
                    }));
            }

            collectionView.Source = column.TableView.ItemsSource;

            var items = _tableView.ShowFilterItemsCount ?
                        GetFilterItemsWithCount(column, searchText, collectionView) :
                        GetFilterItems(column, searchText, collectionView);

            return [.. items];
        }

        return [];
    }

    private IEnumerable<TableViewFilterItem> GetFilterItemsWithCount(TableViewColumn column, string? searchText, CollectionView collectionView)
    {
        var nullCount = 0;
        var isNullItemSelected = !column.IsFiltered || !string.IsNullOrEmpty(searchText) ||
                                 (column.IsFiltered && SelectedValues[column].Contains(null));
        var filterValues = new SortedDictionary<object, int>();

        foreach (var item in collectionView)
        {
            var value = column.GetCellContent(item);

            if (IsBlank(value)) nullCount++;
            else if (filterValues.TryGetValue(value, out var count)) filterValues[value] = ++count;
            else filterValues.Add(value, 1);
        }

        IEnumerable<TableViewFilterItem> nullFilterItem = nullCount > 0 ? [new TableViewFilterItem(isNullItemSelected, null, nullCount)] : [];

        return [.. nullFilterItem,.. filterValues.Select(x =>
        {
            var isSelected = !column.IsFiltered || !string.IsNullOrEmpty(searchText) ||
                             (column.IsFiltered && SelectedValues[column].Contains(x.Key));
            return new TableViewFilterItem(isSelected, x.Key, x.Value);
        }) .OrderByDescending(x=>x.Count)];
    }

    private IEnumerable<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText, CollectionView collectionView)
    {
        var filterValues = new SortedSet<object?>();

        foreach (var item in collectionView)
        {
            var value = column.GetCellContent(item);
            value = IsBlank(value) ? null : value;
            filterValues.Add(value);
        }

        return [.. filterValues.Select(x =>
        {
            var isSelected = !column.IsFiltered || !string.IsNullOrEmpty(searchText) ||
                             (column.IsFiltered && SelectedValues[column].Contains(x));
            return new TableViewFilterItem(isSelected, x, 0);
        })];
    }

    private static bool IsBlank([NotNullWhen(false)] object? value)
    {
        return value == null ||
               value == DBNull.Value ||
               (value is string str && string.IsNullOrWhiteSpace(str)) ||
               (value is Guid guid && guid == Guid.Empty);
    }

    /// <inheritdoc/>
    public virtual void ApplyFilter(TableViewColumn column)
    {
        if (column is { TableView.CollectionView: CollectionView { } collectionView })
        {
            using var defer = collectionView.DeferRefresh();
            column.TableView.DeselectAll();

            if (!column.IsFiltered)
            {
                var boundColumn = column as TableViewBoundColumn;

                column.IsFiltered = true;
                collectionView.FilterDescriptions.Add(new ColumnFilterDescription(
                    column,
                    boundColumn?.PropertyPath,
                    (o) => Filter(column, o)));
            }
        }
    }

    /// <inheritdoc/>
    public virtual void ClearFilter(TableViewColumn? column)
    {
        if (column is { TableView.CollectionView: CollectionView { } collectionView })
        {
            using var defer = collectionView.DeferRefresh();
            column.IsFiltered = false;
            collectionView.FilterDescriptions.RemoveWhere(x => x is ColumnFilterDescription columnFilter && columnFilter.Column == column);
            SelectedValues.RemoveWhere(x => x.Key == column);
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
        value = IsBlank(value) ? null : value!;
        return SelectedValues[column].Contains(value);
    }

    /// <inheritdoc/>
    public IDictionary<TableViewColumn, ICollection<object?>> SelectedValues { get; } = new Dictionary<TableViewColumn, ICollection<object?>>();
}
