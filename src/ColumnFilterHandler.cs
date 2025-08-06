using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Fast filter and sorting implementation with unified single code path for Options Flyout and header clicks.
/// </summary>
public class ColumnFilterHandler : IColumnFilterHandler
{
    private readonly TableView _tableView;

    // SMART CACHE: Uses intelligent cache keys that include filter state
    private readonly ConcurrentDictionary<string, HashSet<object?>> _smartUniqueValuesCache = new();
    private readonly ConcurrentDictionary<TableViewColumn, HashSet<object>> _activeFilters = new();
    private readonly ConcurrentDictionary<TableViewColumn, Func<object, object?>> _propertyAccessors = new();

    // UNIFIED SORTING ENGINE - SINGLE CODE PATH
    private TableViewColumn? _currentSortColumn;
    private SortDirection? _currentSortDirection;
    private readonly FastSortingEngine _sortingEngine;

    private readonly FastFilterEngine _filterEngine;
    private object? _lastItemsSource;
    private IList? _originalItemsSource;
    private IList? _currentFilteredSource;

    // CRITICAL: Prevent infinite loops between filtering and sorting
    private volatile bool _isApplyingFilterOrSort;

    // SCROLL POSITION PRESERVATION: Store scroll position for restoration
    private double _preservedHorizontalOffset = 0;
    private ScrollViewer? _cachedScrollViewer = null;

    public ColumnFilterHandler(TableView tableView)
    {
        _tableView = tableView;
        _filterEngine = new FastFilterEngine();
        _sortingEngine = new FastSortingEngine();
        SelectedValues = new SelectedValuesWrapper(this);

        // Hook into ALL sorting events to unify the code path
        HookIntoAllSortingEvents();
    }

    /// <summary>
    /// UNIFIED: Hook into all sorting events to create a single fast code path
    /// </summary>
    private void HookIntoAllSortingEvents()
    {
        // Hook into TableView sorting event (header clicks)
        _tableView.Sorting += OnTableViewSorting;

        // Hook into ClearSorting event
        _tableView.ClearSorting += OnTableViewClearSorting;
    }

    /// <summary>
    /// UNIFIED: Single entry point for all sorting operations
    /// </summary>
    private void OnTableViewSorting(object? sender, TableViewSortingEventArgs e)
    {
        // CRITICAL: Prevent infinite loops
        if (_isApplyingFilterOrSort)
        {
            return;
        }

        // Mark event as handled so TableView doesn't process it
        e.Handled = true;

        // CRITICAL: Ensure we have original data before any sorting operation
        EnsureOriginalDataSource();

        // Handle the sorting with our unified fast engine
        HandleUnifiedSort(e.Column);
    }

    /// <summary>
    /// UNIFIED: Single entry point for clearing sorts
    /// </summary>
    private void OnTableViewClearSorting(object? sender, TableViewClearSortingEventArgs e)
    {
        // CRITICAL: Prevent infinite loops
        if (_isApplyingFilterOrSort)
        {
            return;
        }

        // Mark event as handled so TableView doesn't process it
        e.Handled = true;

        // Clear our internal sort state
        ClearAllSorting();
    }

    /// <summary>
    /// PUBLIC API: UNIFIED fast sorting entry point
    /// Used by Options Flyout - triggers the same TableView event as header clicks
    /// </summary>
    public void ApplyUnifiedSort(TableViewColumn column, SortDirection direction)
    {
        if (column?.TableView == null) return;

        // CRITICAL: Ensure we have original data
        EnsureOriginalDataSource();

        // Set the column sort direction first
        column.SortDirection = direction;

        // UNIFIED PATH: Use the same event mechanism as header clicks
        var eventArgs = new TableViewSortingEventArgs(column);
        _tableView.OnSorting(eventArgs);
    }

    /// <summary>
    /// PUBLIC API: UNIFIED fast sort clearing
    /// </summary>
    public void ClearUnifiedSort(TableViewColumn column)
    {
        if (column?.TableView == null) return;

        // Clear the column sort direction
        column.SortDirection = null;

        // UNIFIED PATH: Use the same event mechanism
        var eventArgs = new TableViewClearSortingEventArgs();
        _tableView.OnClearSorting(eventArgs);
    }

    /// <summary>
    /// CRITICAL: Ensure we have the original data source captured
    /// </summary>
    private void EnsureOriginalDataSource()
    {
        if (_originalItemsSource == null && _tableView.ItemsSource != null)
        {
            _originalItemsSource = _tableView.ItemsSource as IList ?? _tableView.ItemsSource.Cast<object>().ToList();
            _currentFilteredSource = _originalItemsSource;
            _lastItemsSource = _tableView.ItemsSource;

            System.Diagnostics.Debug.WriteLine($"EnsureOriginalDataSource: Captured {_originalItemsSource.Count} items");
        }
    }

    /// <summary>
    /// UNIFIED: Handle all sorting with Excel-like single-column behavior
    /// </summary>
    private void HandleUnifiedSort(TableViewColumn column)
    {
        if (column == null) return;

        // EXCEL BEHAVIOR: Single column sorting only
        // Clear all other columns first
        foreach (var col in _tableView.Columns)
        {
            if (col != column && col != null)
            {
                col.SortDirection = null;
            }
        }

        // Determine next sort direction for this column
        SortDirection? nextDirection;

        // logic for header clicks vs options flyout
        var currentDirection = column.SortDirection;

        // Check if this is from Options Flyout (direction already set) or Header click (needs cycling)
        // Options flyout calls ApplyUnifiedSort which sets the direction BEFORE calling this method
        // Header clicks go through OnTableViewSorting and need Excel-like cycling

        if (currentDirection.HasValue)
        {
            // This could be either:
            // 1. Options flyout (keep the direction that was just set)
            // 2. Header click (cycle to next direction)

            // We need to distinguish: if direction was just set by ApplyUnifiedSort, keep it
            // Otherwise, cycle to next direction for header clicks

            // For header clicks: cycle through directions using CURRENT state
            var previousDirection = _currentSortColumn == column ? _currentSortDirection : null;

            if (previousDirection == currentDirection)
            {
                // This is a header click - cycle to next direction
                nextDirection = GetNextSortDirection(currentDirection);
                column.SortDirection = nextDirection;
            }
            else
            {
                // Direction was just changed (by options flyout) - keep it
                nextDirection = currentDirection;
            }
        }
        else
        {
            // No direction set - this is first click (unsorted → ascending)
            nextDirection = SortDirection.Ascending;
            column.SortDirection = nextDirection;
        }

        // Update our internal state
        _currentSortColumn = nextDirection.HasValue ? column : null;
        _currentSortDirection = nextDirection;

        // Apply fast sorting
        ApplyFastSorting();
    }

    /// <summary>
    /// UNIFIED: Clear all sorting
    /// </summary>
    private void ClearAllSorting()
    {
        // Clear all column sort directions
        foreach (var col in _tableView.Columns)
        {
            if (col != null)
            {
                col.SortDirection = null;
            }
        }

        // Clear our internal state
        _currentSortColumn = null;
        _currentSortDirection = null;

        // Re-apply filtering without sorting (restores original order within filtered data)
        ApplyFilteringAndSorting();
    }

    /// <summary>
    /// Get next sort direction following Excel-like behavior
    /// </summary>
    private SortDirection? GetNextSortDirection(SortDirection? current)
    {
        return current switch
        {
            null => SortDirection.Ascending,              // Unsorted → ↑ Ascending
            SortDirection.Ascending => SortDirection.Descending,   // ↑ → ↓ Descending  
            SortDirection.Descending => null,             // ↓ → Unsorted
            _ => SortDirection.Ascending
        };
    }

    /// <summary>
    /// Apply fast sorting with no scrolling.
    /// </summary>
    private void ApplyFastSorting()
    {
        // CRITICAL: Prevent infinite loops
        if (_isApplyingFilterOrSort)
        {
            return;
        }

        // CRITICAL: Ensure we have data to work with
        if (_originalItemsSource == null)
        {
            EnsureOriginalDataSource();
            if (_originalItemsSource == null)
            {
                return;
            }
        }

        // SCROLL PRESERVATION: Capture current position before any data changes
        PreserveHorizontalScrollPosition();

        _isApplyingFilterOrSort = true;

        Task.Run(() =>
        {
            try
            {
                // Step 1: Apply filters first (smaller dataset to sort)
                var sourceForSorting = _activeFilters.Any()
                    ? _filterEngine.FilterFast(_originalItemsSource, _activeFilters, _propertyAccessors)
                    : _originalItemsSource;

                // Step 2: Apply sorting if we have a sort column
                var finalItems = (_currentSortColumn != null && _currentSortDirection.HasValue)
                    ? _sortingEngine.SortFast(sourceForSorting, _currentSortColumn, _currentSortDirection.Value, _propertyAccessors)
                    : sourceForSorting;

                _tableView.DispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        _tableView.DeselectAll();
                        
                        SuppressScrollEventsDuringOperation(() =>
                        {
                            BypassCollectionViewAndSetSource(finalItems);
                            _currentFilteredSource = finalItems;
                            UpdateTableViewSortDescriptions();
                        });

                        // Final restoration
                        RestoreHorizontalScrollPosition();
                    }
                    catch
                    {
                        // Fallback: restore to unsorted state
                        _currentSortColumn = null;
                        _currentSortDirection = null;
                        try
                        {
                            BypassCollectionViewAndSetSource(sourceForSorting);
                            // Still attempt to restore scroll position even in fallback
                            RestoreHorizontalScrollPosition();
                        }
                        catch (Exception ex2)
                        {
                            System.Diagnostics.Debug.WriteLine($"Sort fallback error: {ex2.Message}");
                        }
                    }
                    finally
                    {
                        _isApplyingFilterOrSort = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sort background error: {ex.Message}");
                _isApplyingFilterOrSort = false;

                // Reset sort state on error
                _tableView.DispatcherQueue?.TryEnqueue(() =>
                {
                    _currentSortColumn = null;
                    _currentSortDirection = null;
                    // Ensure scroll position is reset on error to prevent stale state
                    _preservedHorizontalOffset = 0;
                });
            }
        });
    }

    /// <summary>
    /// SCROLL PRESERVATION: Captures current horizontal scroll position before data operations
    /// </summary>
    private void PreserveHorizontalScrollPosition()
    {
        try
        {
            // Access the private _scrollViewer field using reflection
            var scrollViewerField = typeof(TableView).GetField("_scrollViewer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (scrollViewerField?.GetValue(_tableView) is ScrollViewer scrollViewer)
            {
                _preservedHorizontalOffset = scrollViewer.HorizontalOffset;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PreserveScrollPosition error: {ex.Message}");
            _preservedHorizontalOffset = 0; // Safe fallback
        }
    }

    /// <summary>
    /// SCROLL RESTORATION: Restores previously captured horizontal scroll position
    /// Uses improved timing and multiple restoration attempts to eliminate visual jumping
    /// </summary>
    private void RestoreHorizontalScrollPosition()
    {
        if (_preservedHorizontalOffset <= 0) return;

        try
        {
            var scrollViewerField = typeof(TableView).GetField("_scrollViewer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (scrollViewerField?.GetValue(_tableView) is ScrollViewer scrollViewer)
            {
                // IMMEDIATE RESTORATION: Set scroll position synchronously first to minimize jumping
                // This prevents the initial jump to column 0
                try
                {
                    scrollViewer.ChangeView(_preservedHorizontalOffset, null, null, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Immediate scroll restore error: {ex.Message}");
                }

                // DELAYED RESTORATION: Use multiple priority levels to ensure restoration
                // This handles cases where the immediate restoration might be overridden
                var capturedOffset = _preservedHorizontalOffset;

                // High priority restoration (fastest response)
                _tableView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    try
                    {
                        if (Math.Abs(scrollViewer.HorizontalOffset - capturedOffset) > 1)
                        {
                            scrollViewer.ChangeView(capturedOffset, null, null, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"High priority scroll restore error: {ex.Message}");
                    }
                });

                // Normal priority restoration (secondary safety net)
                _tableView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    try
                    {
                        if (Math.Abs(scrollViewer.HorizontalOffset - capturedOffset) > 1)
                        {
                            scrollViewer.ChangeView(capturedOffset, null, null, true);
                        }
                    }
                    catch { /* Ignore errors in fallback */ }
                });

                // Low priority final restoration (final safety net)
                _tableView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    try
                    {
                        if (Math.Abs(scrollViewer.HorizontalOffset - capturedOffset) > 1)
                        {
                            scrollViewer.ChangeView(capturedOffset, null, null, true);
                        }
                    }
                    catch { /* Ignore errors in final fallback */ }
                    finally
                    {
                        // Reset preserved offset only after all restoration attempts
                        _preservedHorizontalOffset = 0;
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RestoreScrollPosition dispatch error: {ex.Message}");
            _preservedHorizontalOffset = 0; // Reset on error
        }
    }

    /// <summary>
    /// Apply both filtering and sorting in optimal order with scroll position preservation.
    /// </summary>
    private void ApplyFilteringAndSorting()
    {
        // CRITICAL: Prevent infinite loops
        if (_isApplyingFilterOrSort)
        {
            return;
        }

        // CRITICAL: Ensure we have data to work with
        if (_originalItemsSource == null)
        {
            EnsureOriginalDataSource();
            if (_originalItemsSource == null)
            {
                return;
            }
        }

        // SCROLL PRESERVATION: Capture current position before any data changes
        PreserveHorizontalScrollPosition();

        _isApplyingFilterOrSort = true;

        Task.Run(() =>
        {
            try
            {
                // Step 1: Apply filters first (smaller dataset to sort)
                var filteredItems = _activeFilters.Any()
                    ? _filterEngine.FilterFast(_originalItemsSource, _activeFilters, _propertyAccessors)
                    : _originalItemsSource;

                // Step 2: Apply sorting to filtered data
                var finalItems = (_currentSortColumn != null && _currentSortDirection.HasValue)
                    ? _sortingEngine.SortFast(filteredItems, _currentSortColumn, _currentSortDirection.Value, _propertyAccessors)
                    : filteredItems;

                _tableView.DispatcherQueue?.TryEnqueue(() =>
                {
                    try
                    {
                        _tableView.DeselectAll();
                        BypassCollectionViewAndSetSource(finalItems);
                        _currentFilteredSource = finalItems;

                        // SCROLL RESTORATION: Restore position after data has been updated
                        RestoreHorizontalScrollPosition();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Filter+Sort error: {ex.Message}");
                        // Still attempt to restore scroll position even on error
                        RestoreHorizontalScrollPosition();
                    }
                    finally
                    {
                        _isApplyingFilterOrSort = false;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyFilteringAndSorting background error: {ex.Message}");
                _isApplyingFilterOrSort = false;
                // Ensure scroll position is reset on error
                _tableView.DispatcherQueue?.TryEnqueue(() => _preservedHorizontalOffset = 0);
            }
        });
    }

    /// <summary>
    /// Set source and bypass all CollectionView processing (filters AND sorting)
    /// </summary>
    private void BypassCollectionViewAndSetSource(IList items)
    {
        try
        {
            var collectionViewField = typeof(TableView).GetField("_collectionView",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (collectionViewField?.GetValue(_tableView) is not CollectionView collectionView)
            {
                return;
            }

            // CRITICAL: Preserve scroll position BEFORE any CollectionView operations
            var preservedOffset = _preservedHorizontalOffset;
            
            // SCROLL SUPPRESSION: Temporarily disable scroll change notifications if possible
            var scrollViewerField = typeof(TableView).GetField("_scrollViewer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            ScrollViewer? scrollViewer = null;
            bool wasScrollingEnabled = true;
            
            if (scrollViewerField?.GetValue(_tableView) is ScrollViewer sv)
            {
                scrollViewer = sv;
                // Temporarily disable horizontal scrolling to prevent jumping
                wasScrollingEnabled = scrollViewer.HorizontalScrollMode != ScrollMode.Disabled;
                if (wasScrollingEnabled && preservedOffset > 0)
                {
                    // Don't disable scrolling as it might cause layout issues
                    // Instead, we'll use the multi-level restoration approach
                }
            }

            using (collectionView.DeferRefresh())
            {
                // Clear both filters AND sorts to prevent any CollectionView processing
                collectionView.FilterDescriptions.Clear();
                collectionView.SortDescriptions.Clear();

                // Set pre-processed source directly
                collectionView.Source = items;
            }

            // IMMEDIATE SCROLL RESTORATION: Restore scroll position as soon as possible
            if (scrollViewer != null && preservedOffset > 0)
            {
                try
                {
                    scrollViewer.ChangeView(preservedOffset, null, null, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BypassCollectionView scroll restore error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BypassCollectionViewAndSetSource error: {ex.Message}");
        }
    }

    /// <summary>
    /// Update TableView SortDescriptions to reflect our internal sorting state
    /// Maintains UI consistency between internal state and TableView state
    /// </summary>
    private void UpdateTableViewSortDescriptions()
    {
        try
        {
            // Clear existing sorts
            _tableView.SortDescriptions.Clear();

            // Add our current sort if any
            if (_currentSortColumn != null && _currentSortDirection.HasValue)
            {
                var propertyPath = (_currentSortColumn as TableViewBoundColumn)?.PropertyPath;
                var sortDesc = new ColumnSortDescription(_currentSortColumn, propertyPath, _currentSortDirection.Value);
                _tableView.SortDescriptions.Add(sortDesc);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateTableViewSortDescriptions error: {ex.Message}");
        }
    }

    // INTERFACE IMPLEMENTATION: IColumnFilterHandler.GetFilterItems
    public virtual IList<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText = default)
    {
        if (column?.TableView?.ItemsSource is not { } itemsSource)
        {
            return new List<TableViewFilterItem>();
        }

        // Store original source for fast filtering
        if (_originalItemsSource == null)
        {
            _originalItemsSource = itemsSource as IList ?? itemsSource.Cast<object>().ToList();
            _currentFilteredSource = _originalItemsSource;
        }

        // Only rebuild cache when ItemsSource actually changes
        if (_lastItemsSource != itemsSource)
        {
            _smartUniqueValuesCache.Clear();
            _lastItemsSource = itemsSource;
            _originalItemsSource = itemsSource as IList ?? itemsSource.Cast<object>().ToList();
            _currentFilteredSource = _originalItemsSource;
        }

        // SMART CACHE: Get unique values using intelligent cache key
        var uniqueValues = GetUniqueValuesWithSmartCache(column);

        var filteredValues = string.IsNullOrEmpty(searchText)
            ? uniqueValues
            : uniqueValues.Where(value =>
                {
                    // Handle blank values in search
                    if (value == null)
                    {
                        var blankText = TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)";
                        return blankText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                    }
                    return value.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
                });

        return filteredValues
            .Select(value => new TableViewFilterItem(
                GetSelectionState(value, column),
                // Use the same representation for blank values everywhere
                value ?? TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)"))
            .OrderBy(item => 
                {
                    // Sort blank values to the end
                    if (item.Value?.ToString() == (TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)"))
                        return "zzz_blank";
                    return item.Value?.ToString() ?? "";
                })
            .ToList();
    }

    /// <summary>
    /// SMART CACHE: Gets unique values using cache key that includes filter state
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private HashSet<object?> GetUniqueValuesWithSmartCache(TableViewColumn column)
    {
        var smartCacheKey = GenerateSmartCacheKey(column);

        if (_smartUniqueValuesCache.TryGetValue(smartCacheKey, out var cached))
        {
            return cached;
        }

        IList? sourceForUniqueValues = !HasAnyActiveFilters()
            ? _originalItemsSource
            : _currentFilteredSource;

        var values = new ConcurrentBag<object?>();
        var accessor = GetOrCreatePropertyAccessor(column);
        var source = sourceForUniqueValues?.Cast<object>().ToArray() ?? Array.Empty<object>();

        if (source.Length > 2000)
        {
            Parallel.ForEach(source, item =>
            {
                try
                {
                    var value = accessor(item);
                    // Always add the actual value, null for blank values
                    values.Add(IsBlank(value) ? null : value);
                }
                catch
                {
                    // Skip items that cause errors
                }
            });
        }
        else
        {
            foreach (var item in source)
            {
                try
                {
                    var value = accessor(item);
                    // Always add the actual value, null for blank values
                    values.Add(IsBlank(value) ? null : value);
                }
                catch
                {
                    // Skip items that cause errors
                }
            }
        }

        var result = new HashSet<object?>(values);
        _smartUniqueValuesCache[smartCacheKey] = result;

        return result;
    }

    private string GenerateSmartCacheKey(TableViewColumn column)
    {
        var columnId = column.GetHashCode().ToString();
        var dataCount = (_currentFilteredSource?.Count ?? 0).ToString();

        var otherFiltersHash = string.Join("|", _activeFilters
            .Where(kvp => kvp.Key != column)
            .OrderBy(kvp => kvp.Key.GetHashCode())
            .Select(kvp => $"{kvp.Key.GetHashCode()}:{string.Join(",", kvp.Value.OrderBy(v => v.GetHashCode()))}"));

        return $"{columnId}:{dataCount}:{otherFiltersHash}";
    }

    public virtual void ApplyFilter(TableViewColumn column)
    {
        if (column?.TableView is null) return;

        if (SelectedValues.TryGetValue(column, out var selectedList))
        {
            // Handle the conversion from UI values to filter values
            var normalizedValues = selectedList.Select(value =>
            {
                // Handle blank values
                if (value == null || 
                    (value is string str && str == (TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)")))
                {
                    return "<BLANK>";
                }
                return NormalizeValue(value);
            }).ToHashSet();
            
            _activeFilters[column] = normalizedValues;            
        }

        ApplyFilteringAndSorting();

        if (!column.IsFiltered)
        {
            column.IsFiltered = true;
            column.TableView.FilterDescriptions.Add(new ColumnFilterDescription(
                column,
                (column as TableViewBoundColumn)?.PropertyPath,
                _ => true));
        }

        _tableView.EnsureAlternateRowColors();
    }

    public virtual void ClearFilter(TableViewColumn? column)
    {
        if (column?.TableView is null) return;

        if (column is not null)
        {
            column.IsFiltered = false;
            column.TableView.FilterDescriptions.RemoveWhere(x =>
                x is ColumnFilterDescription cf && cf.Column == column);

            SelectedValues.RemoveWhere(x => x.Key == column);
            _activeFilters.TryRemove(column, out _);
        }
        else
        {
            SelectedValues.Clear();
            _tableView.FilterDescriptions.Clear();
            _activeFilters.Clear();

            // Clear all column sort directions when clearing all filters
            foreach (var col in _tableView.Columns)
            {
                if (col is not null)
                {
                    col.IsFiltered = false;
                }
            }

            //  When clearing all filters, also restore original order
            _currentSortColumn = null;
            _currentSortDirection = null;
        }

        ApplyFilteringAndSorting();
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private Func<object, object?> GetOrCreatePropertyAccessor(TableViewColumn column)
    {
        return _propertyAccessors.GetOrAdd(column, col =>
        {
            if (col is TableViewBoundColumn boundColumn && !string.IsNullOrEmpty(boundColumn.PropertyPath))
            {
                var propertyPath = boundColumn.PropertyPath;
                if (propertyPath.Contains('.'))
                {
                    var parts = propertyPath.Split('.');
                    return item =>
                    {
                        var current = item;
                        foreach (var part in parts)
                        {
                            if (current == null) return null;
                            try
                            {
                                var property = current.GetType().GetProperty(part);
                                current = property?.GetValue(current);
                            }
                            catch
                            {
                                return null;
                            }
                        }
                        return current;
                    };
                }
                else
                {
                    var propertyCache = new ConcurrentDictionary<Type, System.Reflection.PropertyInfo?>();
                    return item =>
                    {
                        if (item == null) return null;

                        var type = item.GetType();
                        var property = propertyCache.GetOrAdd(type, t => t.GetProperty(propertyPath));

                        try
                        {
                            return property?.GetValue(item);
                        }
                        catch
                        {
                            return null;
                        }
                    };
                }
            }

            // SAFE FALLBACK: Never use GetCellContent - it's unreliable
            return item => item?.ToString();
        });
    }

    private bool HasAnyActiveFilters() => _activeFilters.Any(kvp => kvp.Value.Count > 0);

    private bool GetSelectionState(object? value, TableViewColumn column)
    {
        if (!column.IsFiltered) return true;

        // Handle both null values and normalized values correctly
        if (value == null)
        {
            // For null values, check if "<BLANK>" is in the active filters
            return _activeFilters.TryGetValue(column, out var filters) && filters.Contains("<BLANK>");
        }
        
        // For non-null values, use the actual value
        return _activeFilters.TryGetValue(column, out var nonNullFilters) && nonNullFilters.Contains(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBlank(object? value)
    {
        return value == null || value == DBNull.Value ||
               (value is string str && string.IsNullOrWhiteSpace(str)) ||
               (value is Guid guid && guid == Guid.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object NormalizeValue(object? value)
    {
        return IsBlank(value) ? "<BLANK>" : value!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Filter(TableViewColumn column, object? item) => true;

    public void ClearCache()
    {
        _smartUniqueValuesCache.Clear();
        _activeFilters.Clear();
        _propertyAccessors.Clear();
        _currentSortColumn = null;
        _currentSortDirection = null;
        _lastItemsSource = null;
        _originalItemsSource = null;
        _currentFilteredSource = null;
    }

    public IDictionary<TableViewColumn, IList<object>> SelectedValues { get; }

    /// <summary>
    /// SelectedValuesWrapper to handle blank values correctly
    /// </summary>
    private class SelectedValuesWrapper : Dictionary<TableViewColumn, IList<object>>
    {
        private readonly ColumnFilterHandler _handler;

        public SelectedValuesWrapper(ColumnFilterHandler handler)
        {
            _handler = handler;
        }

        public new IList<object> this[TableViewColumn key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                // Handle blank values correctly in the setter
                var normalizedValues = value.Select(val =>
                {
                    if (val == null || 
                        (val is string str && str == (TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)")))
                    {
                        return "<BLANK>";
                    }
                    return NormalizeValue(val);
                }).ToHashSet();
                _handler._activeFilters[key] = normalizedValues;
            }
        }

        public new bool TryGetValue(TableViewColumn key, out IList<object> value)
        {
            var result = base.TryGetValue(key, out value!);
            if (result && !_handler._activeFilters.ContainsKey(key))
            {
                // Handle blank values correctly
                var normalizedValues = value.Select(val =>
                {
                    if (val == null || 
                        (val is string str && str == (TableViewLocalizedStrings.BlankFilterValue ?? "(Blank)")))
                    {
                        return "<BLANK>";
                    }
                    return NormalizeValue(val);
                }).ToHashSet();
                _handler._activeFilters[key] = normalizedValues;
            }
            return result;
        }

        public new bool Remove(TableViewColumn key)
        {
            _handler._activeFilters.TryRemove(key, out _);
            return base.Remove(key);
        }

        public void RemoveWhere(Func<KeyValuePair<TableViewColumn, IList<object>>, bool> predicate)
        {
            var keysToRemove = this.Where(predicate).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _handler._activeFilters.TryRemove(key, out _);
                base.Remove(key);
            }
        }

        public new void Clear()
        {
            _handler._activeFilters.Clear();
            base.Clear();
        }

        private static object NormalizeValue(object? value)
        {
            return ColumnFilterHandler.NormalizeValue(value);
        }
    }

    /// <summary>
    /// Fast Single-column sorting engine optimized 500k+ of rows
    /// </summary>
    private class FastSortingEngine
    {
        // Pre-compiled property accessors cache for maximum performance
        private static readonly ConcurrentDictionary<string, Func<object, IComparable?>> _compiledAccessors = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public IList SortFast(
            IList sourceItems,
            TableViewColumn sortColumn,
            SortDirection direction,
            ConcurrentDictionary<TableViewColumn, Func<object, object?>> propertyAccessors)
        {
            if (sourceItems.Count == 0)
            {
                return sourceItems;
            }

            var sourceArray = sourceItems.Cast<object>().ToArray();
            var accessor = propertyAccessors.GetValueOrDefault(sortColumn) ?? (item => item?.ToString());

            try
            {
                // Use parallel sorting for large datasets
                if (sourceArray.Length > 10000)
                {
                    return ParallelSortFast(sourceArray, accessor, direction);
                }
                else
                {
                    // Single-threaded for smaller datasets
                    var comparer = new FastSingleColumnComparer(accessor, direction);
                    Array.Sort(sourceArray, comparer);
                    return sourceArray.ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sort error: {ex.Message}");
                return sourceItems;
            }
        }

        /// <summary>
        /// Parallel merge sort for 500k+ of rows
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private IList ParallelSortFast(object[] sourceArray, Func<object, object?> accessor, SortDirection direction)
        {
            var processorCount = Environment.ProcessorCount;
            var chunkSize = Math.Max(sourceArray.Length / processorCount, 1000);
            var chunks = new List<object[]>();

            // Step 1: Divide into chunks for parallel processing
            for (int i = 0; i < sourceArray.Length; i += chunkSize)
            {
                var size = Math.Min(chunkSize, sourceArray.Length - i);
                var chunk = new object[size];
                Array.Copy(sourceArray, i, chunk, 0, size);
                chunks.Add(chunk);
            }

            // Step 2: Sort each chunk in parallel
            var sortedChunks = new object[chunks.Count][];
            var comparer = new FastSingleColumnComparer(accessor, direction);

            Parallel.For(0, chunks.Count, i =>
            {
                Array.Sort(chunks[i], comparer);
                sortedChunks[i] = chunks[i];
            });

            // Step 3: Merge all sorted chunks using k-way merge
            return MergeChunksFast(sortedChunks, comparer);
        }

        /// <summary>
        /// FAST: K-way merge algorithm for combining sorted chunks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private List<object> MergeChunksFast(object[][] sortedChunks, IComparer<object> comparer)
        {
            var result = new List<object>(sortedChunks.Sum(c => c.Length));
            var heap = new PriorityQueue<ChunkPointer, object>(comparer);

            // Initialize heap with first element from each chunk
            for (int i = 0; i < sortedChunks.Length; i++)
            {
                if (sortedChunks[i].Length > 0)
                {
                    var pointer = new ChunkPointer { ChunkIndex = i, ElementIndex = 0 };
                    heap.Enqueue(pointer, sortedChunks[i][0]);
                }
            }

            // Extract minimum and add next element from same chunk
            while (heap.Count > 0)
            {
                var minPointer = heap.Dequeue();
                var chunk = sortedChunks[minPointer.ChunkIndex];
                result.Add(chunk[minPointer.ElementIndex]);

                // Add next element from the same chunk if available
                if (minPointer.ElementIndex + 1 < chunk.Length)
                {
                    minPointer.ElementIndex++;
                    heap.Enqueue(minPointer, chunk[minPointer.ElementIndex]);
                }
            }

            return result;
        }

        /// <summary>
        /// Helper struct for k-way merge
        /// </summary>
        private struct ChunkPointer
        {
            public int ChunkIndex;
            public int ElementIndex;
        }

        /// <summary>
        /// Single-column comparer with type-specific fast paths
        /// </summary>
        private class FastSingleColumnComparer : IComparer<object>
        {
            private readonly Func<object, object?> _accessor;
            private readonly SortDirection _direction;
            private readonly int _directionMultiplier;

            public FastSingleColumnComparer(Func<object, object?> accessor, SortDirection direction)
            {
                _accessor = accessor;
                _direction = direction;
                _directionMultiplier = direction == SortDirection.Ascending ? 1 : -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -_directionMultiplier;
                if (y == null) return _directionMultiplier;

                try
                {
                    var xValue = _accessor(x);
                    var yValue = _accessor(y);

                    return CompareValuesFast(xValue, yValue) * _directionMultiplier;
                }
                catch
                {
                    // fast fallback
                    return CompareToStringFast(x, y) * _directionMultiplier;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int CompareValuesFast(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                // FAST: Type-specific comparisons with minimal boxing
                var xType = x.GetType();
                var yType = y.GetType();

                if (xType == yType)
                {
                    // Same types - fast path
                    return Type.GetTypeCode(xType) switch
                    {
                        TypeCode.String => string.Compare((string)x, (string)y, StringComparison.OrdinalIgnoreCase),
                        TypeCode.Int32 => ((int)x).CompareTo((int)y),
                        TypeCode.Int64 => ((long)x).CompareTo((long)y),
                        TypeCode.Double => ((double)x).CompareTo((double)y),
                        TypeCode.Decimal => ((decimal)x).CompareTo((decimal)y),
                        TypeCode.DateTime => ((DateTime)x).CompareTo((DateTime)y),
                        TypeCode.Boolean => ((bool)x).CompareTo((bool)y),
                        TypeCode.Single => ((float)x).CompareTo((float)y),
                        TypeCode.Int16 => ((short)x).CompareTo((short)y),
                        TypeCode.Byte => ((byte)x).CompareTo((byte)y),
                        _ => (x as IComparable)?.CompareTo(y) ?? CompareToStringFast(x, y)
                    };
                }

                // Different types - try IComparable first
                if (x is IComparable comparable)
                {
                    try { return comparable.CompareTo(y); }
                    catch { /* Fall through */ }
                }

                // Ultimate fallback
                return CompareToStringFast(x, y);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static int CompareToStringFast(object x, object y)
            {
                return string.Compare(x?.ToString() ?? "", y?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>
    /// Fast filter engine optimized for 500k+ of rows
    /// </summary>
    private class FastFilterEngine
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public IList FilterFast(
            IList? originalSource,
            ConcurrentDictionary<TableViewColumn, HashSet<object>> activeFilters,
            ConcurrentDictionary<TableViewColumn, Func<object, object?>> propertyAccessors)
        {
            if (originalSource == null || originalSource.Count == 0)
            {
                return new List<object>();
            }

            if (activeFilters.Count == 0)
            {
                return originalSource;
            }

            var sourceArray = originalSource.Cast<object>().ToArray();
            var result = new ConcurrentBag<object>();

            Parallel.ForEach(
                Partitioner.Create(sourceArray, true),
                item =>
                {
                    bool passesAllFilters = true;

                    foreach (var (column, allowedValues) in activeFilters)
                    {
                        if (!propertyAccessors.TryGetValue(column, out var accessor))
                        {
                            continue;
                        }

                        try
                        {
                            var value = accessor(item);
                            var normalizedValue = IsBlank(value) ? "<BLANK>" : value!;

                            if (!allowedValues.Contains(normalizedValue))
                            {
                                passesAllFilters = false;
                                break;
                            }
                        }
                        catch
                        {
                            // Skip items that cause errors
                            passesAllFilters = false;
                            break;
                        }
                    }

                    if (passesAllFilters)
                    {
                        result.Add(item);
                    }
                });

            return result.ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBlank(object? value)
        {
            return value == null || value == DBNull.Value ||
                   (value is string str && string.IsNullOrWhiteSpace(str)) ||
                   (value is Guid guid && guid == Guid.Empty);
        }
    }

    /// <summary>
    /// Temporarily suppress scroll events during data operations
    /// </summary>
    private void SuppressScrollEventsDuringOperation(Action operation)
    {
        try
        {
            // Cache the scroll viewer reference for better performance
            if (_cachedScrollViewer == null)
            {
                var scrollViewerField = typeof(TableView).GetField("_scrollViewer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                _cachedScrollViewer = scrollViewerField?.GetValue(_tableView) as ScrollViewer;
            }

            if (_cachedScrollViewer != null)
            {
                // Store scroll position before operation
                var originalOffset = _cachedScrollViewer.HorizontalOffset;
                
                // Execute the operation
                operation();
                
                // Restore immediately after operation
                _cachedScrollViewer.ChangeView(originalOffset, null, null, true);
                
                // Schedule additional restorations for improved reliability
                _tableView.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    try
                    {
                        if (_cachedScrollViewer != null && Math.Abs(_cachedScrollViewer.HorizontalOffset - originalOffset) > 1)
                        {
                            _cachedScrollViewer.ChangeView(originalOffset, null, null, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Scroll restoration error: {ex.Message}");
                    }
                });
            }
            else
            {
                // Fallback: execute operation without scroll suppression
                operation();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SuppressScrollEventsDuringOperation error: {ex.Message}");
            operation(); // Execute operation anyway
        }
    }
}
