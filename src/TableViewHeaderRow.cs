using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents the header row in a TableView.
/// </summary>
[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateNoButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllButtonDisabled, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllCheckBox, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateSelectAllCheckBoxDisabled, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateOptionsButton, GroupName = VisualStates.GroupCornerButton)]
[TemplateVisualState(Name = VisualStates.StateOptionsButtonDisabled, GroupName = VisualStates.GroupCornerButton)]
public partial class TableViewHeaderRow : Control
{
    private Panel? _cornerButtonPanel;
    private Button? _optionsButton;
    private Button? _selectAllButton;
    private CheckBox? _selectAllCheckBox;
    private bool _isSyncingSelectAllCheckBox;
    private FrameworkElement? _v_gridLine;
    private FrameworkElement? _h_gridLine;
    private Panel? _frozenHeadersPanel; // a custom template may use a StackPanel for either
    private Panel? _scrollableHeadersPanel;
    private readonly Dictionary<(TableViewColumn Column, int Occurrence), TableViewColumnHeader> _headersByColumn = [];
    private readonly List<TableViewColumnHeader> _leavingHeaders = [];
    private Border? _columnDropIndicator;
    private TranslateTransform? _columnDropIndicatorTransform;
    private Image? _dragHeaderImage;
    private DispatcherTimer? _desiredWidthTimer;
    private bool _calculatingHeaderWidths;
    private RectangleGeometry? _scrollableHeadersClip;
    private bool _cellsAvailableWidthPending;
    private bool _isPaddingApplied;
    private int _dropColumnIndex;
    private bool _isValidDropTarget;
    private readonly Dictionary<DependencyProperty, long> _callbackTokens = [];

    /// <summary>
    /// Initializes a new instance of the TableViewHeaderRow class.
    /// </summary>
    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _selectAllButton?.Tapped -= OnSelectAllButtonClicked;
        _selectAllCheckBox?.Checked -= OnSelectAllCheckBoxChecked;
        _selectAllCheckBox?.Unchecked -= OnSelectAllCheckBoxUnchecked;

        _cornerButtonPanel = GetTemplateChild("CornerButtonPanel") as Panel;
        _optionsButton = GetTemplateChild("optionsButton") as Button;
        _selectAllButton = GetTemplateChild("selectAllButton") as Button;
        _selectAllCheckBox = GetTemplateChild("selectAllCheckBox") as CheckBox;
        _v_gridLine = GetTemplateChild("VerticalGridLine") as FrameworkElement;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as FrameworkElement;
        _frozenHeadersPanel = GetTemplateChild("FrozenHeadersPanel") as Panel;
        _scrollableHeadersPanel = GetTemplateChild("ScrollableHeadersPanel") as Panel;
        ForgetHeaders(); // those of the previous template
#if !WINDOWS
        // Uno re-arranges a resized panel at its layout slot without calling this ArrangeOverride, dropping the scroll offset.
        _scrollableHeadersPanel?.SizeChanged += (_, _) => InvalidateArrange();
#endif
        _columnDropIndicator = GetTemplateChild("ColumnDropIndicator") as Border;
        _columnDropIndicatorTransform = GetTemplateChild("ColumnDropIndicatorTransform") as TranslateTransform;
        _dragHeaderImage = GetTemplateChild("DragHeaderImage") as Image;

        if (TableView is null)
        {
            return;
        }

        _selectAllButton?.Tapped += OnSelectAllButtonClicked;
        _selectAllCheckBox?.Checked += OnSelectAllCheckBoxChecked;
        _selectAllCheckBox?.Unchecked += OnSelectAllCheckBoxUnchecked;

        (_scrollableHeadersPanel as TableViewCellsPanel)?.TableView = TableView;

        if (TableView.Columns is not null)
        {
            SyncHeaders();
            CalculateHeaderWidths();
        }

        _cornerButtonPanel?.SetBinding(WidthProperty, new Binding
        {
            Source = TableView,
            Path = new PropertyPath(nameof(TableView.CellsHorizontalOffset))
        });

        SetOptionCommands();
        SetExportOptionsVisibility();
        SetCornerButtonState();
        SetHeadersVisibility();
        SetCellsHorizontalOffset();
        TableView.UpdateHorizontalScrollBarMargin();
        EnsureGridLines();
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        if (_scrollableHeadersPanel is not null && _frozenHeadersPanel is not null && TableView is not null && _scrollableHeadersPanel.ActualWidth > 0)
        {
            var frozenOffset = _frozenHeadersPanel.ActualOffset.X + _frozenHeadersPanel.ActualWidth;
            var headersOffset = -TableView.HorizontalOffset + frozenOffset;
            // This reduces to TableView.HorizontalOffset, which is read live from the scroll viewer,
            // while ActualWidth is left over from the previous arrange. Narrowing a column while the
            // view is scrolled right can therefore leave the offset past the panel's own extent, which
            // would give the clip below a zero or negative width - not a valid rectangle, and it hides
            // every header. Clamp it into the panel's extent so the clip is always well formed.
            var xClip = Math.Clamp((headersOffset * -1) + frozenOffset, 0, _scrollableHeadersPanel.ActualWidth);

            _scrollableHeadersPanel.Arrange(new Rect(headersOffset, 0, _scrollableHeadersPanel.ActualWidth, _scrollableHeadersPanel.ActualHeight));

            if (headersOffset >= frozenOffset)
            {
                _scrollableHeadersPanel.Clip = null;
            }
            else
            {
                // Cached rather than reallocated: horizontal scrolling re-arranges this panel every frame
                // (see TableView.OnHorizontalOffsetChanged).
                _scrollableHeadersClip ??= new RectangleGeometry();
                _scrollableHeadersClip.Rect = new Rect(xClip, 0, _scrollableHeadersPanel.ActualWidth - xClip, finalSize.Height);
                _scrollableHeadersPanel.Clip = _scrollableHeadersClip;
            }

#if !WINDOWS
            UpdateRenderCulling(cullHeaders: !TableView.IsColumnResizing);
#endif
        }

        return finalSize;
    }

#if !WINDOWS
    /// <summary>
    /// Shows the column headers in view and hides the scrollable ones scrolled out of view sideways (see
    /// <see cref="TableView.UpdateRenderCulling"/>).
    /// </summary>
    /// <param name="cullHeaders">Whether to hide the headers out of view, rather than show every header.</param>
    internal void UpdateRenderCulling(bool cullHeaders)
    {
        if (_scrollableHeadersPanel is null || _frozenHeadersPanel is null || TableView is null || ElementVisuals.AreKnownNotComposed)
        {
            return;
        }

        foreach (var header in _frozenHeadersPanel.Children)
        {
            ElementVisuals.SetInView(header, isInView: true);
        }

        // The scrollable panel is arranged at the frozen headers' right edge less the horizontal offset and clipped
        // there, so in its own coordinates the part in view starts at the offset and runs to this row's right edge.
        var start = TableView.HorizontalOffset;
        var end = start + ActualWidth - (_frozenHeadersPanel.ActualOffset.X + _frozenHeadersPanel.ActualWidth);
        cullHeaders &= ActualWidth > 0d;

        var columns = TableView.Columns as TableViewColumnsCollection;
        var firstOffset = columns?.GetVisibleColumnOffset(Math.Clamp(TableView.FrozenColumnCount, 0, columns.VisibleColumns.Count)) ?? 0d;

        foreach (var header in _scrollableHeadersPanel.Children)
        {
            var x = (double)header.ActualOffset.X;
            var right = x + header.ActualSize.X;

            // A header that has just been given its column is not arranged there yet; the column says where it will be.
            if (columns is not null
                && header is TableViewColumnHeader { Column: { } column } columnHeader
                && columns.GetVisibleColumnIndex(column, columnHeader.ColumnOccurrence) is >= 0 and var index)
            {
                x = columns.GetVisibleColumnOffset(index) - firstOffset;
                right = columns.GetVisibleColumnOffset(index + 1) - firstOffset;
            }

            ElementVisuals.SetInView(header, isInView: !cullHeaders || (right >= start && x <= end));
        }
    }
#endif

    /// <summary>
    /// Handles the collection changed event for the columns.
    /// </summary>
    private void OnTableViewColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncHeaders(reordered: e.Action is NotifyCollectionChangedAction.Move);

        if (e.Action is not NotifyCollectionChangedAction.Move)
        {
            CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Handles the property changed event for a column.
    /// </summary>
    private void OnColumnPropertyChanged(object? sender, TableViewColumnPropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TableViewColumn.Visibility))
        {
            // Star columns share the width a hidden column leaves, and give it back to one that is shown.
            SyncHeaders();
            CalculateHeaderWidths();
        }
        else if ((e.PropertyName is nameof(TableViewColumn.Order) ||
            e.PropertyName is nameof(TableViewColumn.IsFrozen)) &&
            e.Column.Visibility is Visibility.Visible)
        {
            SyncHeaders(reordered: true);
            CalculateHeaderWidths();
        }
        else if (e.PropertyName is nameof(TableViewColumn.Width) or
                                   nameof(TableViewColumn.MinWidth) or
                                   nameof(TableViewColumn.MaxWidth))
        {
            if (!_calculatingHeaderWidths)
            {
                CalculateHeaderWidths();
            }
        }
        else if (e.PropertyName is nameof(TableViewColumn.ActualWidth))
        {
            // A column added twice is sized through its first header; the others follow.
            if (TableView?.Columns is TableViewColumnsCollection { HasDuplicateColumns: true })
            {
                foreach (var header in _headersByColumn.Values)
                {
                    if (header.Column == e.Column && header.ColumnOccurrence > 0 && header.Width != e.Column.ActualWidth)
                    {
                        header.Width = e.Column.ActualWidth;
                    }
                }
            }
        }
        else if (e.PropertyName is nameof(TableViewColumn.DesiredWidth))
        {
            // Coalesce multiple updates (see in-loop Measure() call in CalculateHeaderWidths) to avoid excessive header width calculations.
            _desiredWidthTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _desiredWidthTimer.Tick -= OnDesiredWidthTimerTick;
            _desiredWidthTimer.Tick += OnDesiredWidthTimerTick;
            _desiredWidthTimer.Stop();
            _desiredWidthTimer.Start();
        }
    }

    /// <summary>
    /// Recalculates header widths after deferred desired-width updates, once the view is not scrolling.
    /// </summary>
    /// <remarks>
    /// Applying grown widths changes every affected column's cells in every realized row, and the re-measure of
    /// all that text lands in one or two frames - long ones, in the middle of whatever scroll let the timer
    /// expire. The widths themselves are already up to a debounce stale, so while a scroll is in flight the
    /// timer re-arms and the same widths are applied on a frame where nothing is moving.
    /// </remarks>
    private void OnDesiredWidthTimerTick(object? sender, object e)
    {
        _desiredWidthTimer?.Stop();

        if (TableView?.IsViewChangeInFlight is true)
        {
            _desiredWidthTimer?.Start();
            return;
        }

        if (!_calculatingHeaderWidths)
        {
            CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Brings the headers in line with the columns: one for every frozen column, and one for every scrollable
    /// column in or near the viewport (see <see cref="TableView.RealizedColumns"/>).
    /// </summary>
    /// <remarks>
    /// A scrollable panel that is not a <see cref="TableViewCellsPanel"/>, as in a custom template, keeps a header
    /// for every column. Headers out of view are kept while a column is being resized or dragged.
    /// </remarks>
    /// <param name="reordered">Whether the columns may have changed places, which moves headers that stay.</param>
    internal void SyncHeaders(bool reordered = false)
    {
        if (TableView is null || _scrollableHeadersPanel is null || _frozenHeadersPanel is null)
        {
            return;
        }

        var columns = (TableViewColumnsCollection)TableView.Columns;
        var visibleColumns = columns.VisibleColumns;
        var count = visibleColumns.Count;
        var frozenCount = Math.Clamp(TableView.FrozenColumnCount, 0, count);
        var (first, last) = _scrollableHeadersPanel is TableViewCellsPanel ? TableView.RealizedHeaderColumns : (frozenCount, count - 1);
        var canRemove = !TableView.IsColumnResizing;
        var changed = reordered;
        var frozenChanged = reordered;

        foreach (var header in _headersByColumn.Values)
        {
            canRemove &= !header.IsReordering;
        }

        // Headers whose column is gone, out of view, or now on the other side of the frozen boundary.
        _leavingHeaders.Clear();

        foreach (var header in Headers)
        {
            var index = header.Column is { } column ? columns.GetVisibleColumnIndex(column, header.ColumnOccurrence) : -1;
            var isWanted = index >= 0 && (index < frozenCount || (index >= first && index <= last) || !canRemove);

            if (!isWanted || header.IsInFrozenPanel != (index < frozenCount))
            {
                _leavingHeaders.Add(header);
            }
        }

        foreach (var header in _leavingHeaders)
        {
            frozenChanged |= header.IsInFrozenPanel;
            Forget(header);
        }

        for (var i = 0; i < count; i++)
        {
            if (i == frozenCount)
            {
                i = Math.Max(first, frozenCount);
            }

            if (i >= count || (i >= frozenCount && i > last))
            {
                break;
            }

            var column = visibleColumns[i];

            var occurrence = columns.GetColumnOccurrence(i);

            if (_headersByColumn.ContainsKey((column, occurrence)))
            {
                continue;
            }

            var isFrozen = i < frozenCount;
            TableViewColumnHeader? header = null;

            // A header that is leaving the same panel stays there and takes over the column.
            for (var j = _leavingHeaders.Count - 1; j >= 0; j--)
            {
                if (_leavingHeaders[j].IsInFrozenPanel == isFrozen)
                {
                    header = _leavingHeaders[j];
                    _leavingHeaders.RemoveAt(j);
                    header.ColumnOccurrence = occurrence;
                    header.Reuse(column);
                    break;
                }
            }

            if (header is null)
            {
                header = new TableViewColumnHeader { DataContext = column, Column = column, Content = column.Header, ColumnOccurrence = occurrence };

                // A column that has been sized keeps its width; a new one is sized with the others.
                if (column.ActualWidth > 0d)
                {
                    header.Width = column.ActualWidth;
                }

                AddToPanel(header, isFrozen);
            }

            // A column added twice goes by its first header.
            if (occurrence == 0)
            {
                column.HeaderControl = header;
            }

            _headersByColumn[(column, occurrence)] = header;

            if (occurrence > 0)
            {
                column.EnsureHeaderStyle();
            }

            frozenChanged |= isFrozen;
            changed = true;
        }

        // Headers no column took over.
        foreach (var header in _leavingHeaders)
        {
            RemoveFromPanel(header);
            changed = true;
        }

        _leavingHeaders.Clear();

        if (changed)
        {
            if (frozenChanged)
            {
                EnsureStackOrder(_frozenHeadersPanel, isFrozen: true);
            }

            EnsureStackOrder(_scrollableHeadersPanel, isFrozen: false);
            _scrollableHeadersPanel.InvalidateArrange();
        }
    }

    /// <summary>
    /// Shows the column's current <see cref="TableViewColumn.Header"/> in its headers.
    /// </summary>
    internal void UpdateHeaderContent(TableViewColumn column)
    {
        foreach (var header in Headers)
        {
            if (header.Column == column)
            {
                header.Content = column.Header;
            }
        }
    }

    private void AddToPanel(TableViewColumnHeader header, bool isFrozen)
    {
        header.IsInFrozenPanel = isFrozen;
        (isFrozen ? _frozenHeadersPanel : _scrollableHeadersPanel)?.Children.Add(header);
    }

    /// <summary>
    /// Puts the headers of a panel that stacks its children in column order. A <see cref="TableViewCellsPanel"/>
    /// places headers by their column and needs no order.
    /// </summary>
    private void EnsureStackOrder(Panel panel, bool isFrozen)
    {
        if (panel is TableViewCellsPanel)
        {
            return;
        }

        var position = 0;
        var inOrder = true;
        var headers = Headers;

        foreach (var header in headers)
        {
            if (header.IsInFrozenPanel == isFrozen)
            {
                if (position >= panel.Children.Count || !ReferenceEquals(panel.Children[position], header))
                {
                    inOrder = false;
                    break;
                }

                position++;
            }
        }

        if (inOrder && position == panel.Children.Count)
        {
            return;
        }

        panel.Children.Clear();

        foreach (var header in headers)
        {
            if (header.IsInFrozenPanel == isFrozen)
            {
                panel.Children.Add(header);
            }
        }
    }

    /// <summary>
    /// Calculates the widths of the headers.
    /// </summary>
    internal void CalculateHeaderWidths()
    {
        if (TableView?.ActualWidth > 0)
        {
            _calculatingHeaderWidths = true;

            var allColumns = TableView.Columns.VisibleColumns;
            var tableMinWidth = TableView.MinColumnWidth;
            var tableMaxWidth = TableView.MaxColumnWidth;
            var starUnitWeight = 0d;
            var fixedWidth = 0d;
            var startColumnsToAdjust = new List<TableViewColumn>();

            foreach (var column in allColumns)
            {
                var columnWidth = column.Width;

                if (columnWidth.IsStar)
                {
                    starUnitWeight += columnWidth.Value;
                    startColumnsToAdjust.Add(column);
                }
                else
                {
                    fixedWidth += ClampColumnWidth(column, columnWidth.IsAbsolute ? columnWidth.Value : GetColumnDesiredWidth(column), tableMinWidth, tableMaxWidth);
                }
            }

            var availableWidth = GetCellsAvailableWidth() - fixedWidth;
            var starUnitWidth = starUnitWeight > 0 ? availableWidth / starUnitWeight : 0;

            var starWidthAdjusted = false;
            do
            {
                starWidthAdjusted = false;

                foreach (var column in startColumnsToAdjust)
                {
                    var width = starUnitWidth * column.Width.Value;
                    var minWidth = column.MinWidth ?? tableMinWidth;
                    var maxWidth = column.MaxWidth ?? tableMaxWidth;

                    width = width < minWidth ? minWidth : width;
                    width = width > maxWidth ? maxWidth : width;

                    if (width != starUnitWidth * column.Width.Value)
                    {
                        availableWidth -= width;
                        starUnitWeight -= column.Width.Value;
                        // With no star column left to share, the unit stays so the limited ones resolve to their limit again below.
                        starUnitWidth = starUnitWeight > 0 ? availableWidth / starUnitWeight : starUnitWidth;
                        startColumnsToAdjust.Remove(column);
                        starWidthAdjusted = true;
                        break;
                    }
                }

            } while (starWidthAdjusted);


            var scale = XamlRoot?.RasterizationScale ?? 1d;
            var starEdge = 0d;
            var starPixels = 0d;

            foreach (var column in allColumns)
            {
                var width = column.Width.IsStar
                            ? starUnitWidth * column.Width.Value
                            : column.Width.IsAbsolute ? column.Width.Value
                            : GetColumnDesiredWidth(column);

                var minWidth = column.MinWidth ?? tableMinWidth;
                var maxWidth = column.MaxWidth ?? tableMaxWidth;
                var isShared = column.Width.IsStar && width >= minWidth && width <= maxWidth;

                width = width < minWidth ? minWidth : width;
                width = width > maxWidth ? maxWidth : width;

                // Snap to whole pixels so layout rounding can't push the columns past the viewport.
                if (isShared)
                {
                    starEdge += width * scale;
                    width = ClampColumnWidth(column, (Math.Floor(starEdge + 0.001) - starPixels) / scale, tableMinWidth, tableMaxWidth);
                    starPixels += width * scale;
                }

                // A column out of view has no header to take its width from.
                if (column.HeaderControl is { } header)
                {
                    header.Width = width;
                }
                else if (!column.IsResizing && column.ActualWidth != width)
                {
                    column.ActualWidth = width;
                }
            }

            _calculatingHeaderWidths = false;

            TableView.UpdateHorizontalScrollBarMargin();
        }
    }

    /// <summary>
    /// Limits a width to the column's minimum and maximum widths.
    /// </summary>
    private static double ClampColumnWidth(TableViewColumn column, double width, double tableMinWidth, double tableMaxWidth)
    {
        var minWidth = column.MinWidth ?? tableMinWidth;
        var maxWidth = column.MaxWidth ?? tableMaxWidth;

        width = width < minWidth ? minWidth : width;
        return width > maxWidth ? maxWidth : width;
    }

    /// <summary>
    /// Gets the width the columns can share without overflowing the rows.
    /// </summary>
    private double GetCellsAvailableWidth()
    {
        if (TableView is null) return 0d;

        var offset = TableView.CellsHorizontalOffset;
        offset = double.IsNaN(offset) ? _cornerButtonPanel?.ActualWidth ?? 0d : offset;
        offset += _v_gridLine?.Visibility is Visibility.Visible ? _v_gridLine.Width : 0d;

        var border = TableView.BorderThickness;
        var padding = TableView.Padding;

        return Math.Max(0d, TableView.ActualWidth - border.Left - border.Right - padding.Left - padding.Right - offset);
    }

    /// <summary>
    /// Insets the header row by the TableView's horizontal padding, which its rows are already inset by.
    /// </summary>
    private void ApplyTableViewPadding()
    {
        var padding = TableView?.Padding ?? default;

        if (!_isPaddingApplied && padding.Left == 0 && padding.Right == 0) return;

        Margin = new Thickness(padding.Left, 0, padding.Right, 0);
        _isPaddingApplied = true;
    }

    /// <summary>
    /// Handles changes to the properties that set the width left for the cells.
    /// </summary>
    private void OnTableViewCellsAvailableWidthChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (dp == PaddingProperty)
        {
            ApplyTableViewPadding();
        }

        if (_cellsAvailableWidthPending || TableView?.Columns.VisibleColumns.Any(x => x.Width.IsStar) is not true) return;

        // Deferred: the offset changes while the rows are being arranged.
        _cellsAvailableWidthPending = DispatcherQueue.TryEnqueue(() =>
        {
            _cellsAvailableWidthPending = false;
            CalculateHeaderWidths();
        });
    }

    // The width of an auto column nothing of which has been measured; the collection estimates with the same.
    private const double UnmeasuredColumnWidth = 100d;

    /// <summary>
    /// Gets the desired width of a column based on its header and cells.
    /// </summary>
    internal double GetColumnDesiredWidth(TableViewColumn column)
    {
        var autoWidthMode = column.ColumnAutoWidthMode ?? TableView?.ColumnAutoWidthMode;
        var width = column.DesiredWidth;

        if (column.HeaderControl is { } header && autoWidthMode is not TableViewColumnAutoWidthMode.Cells)
        {
            if (header.CachedDesiredWidth is null)
            {
                header.Width = double.NaN;
                header.Measure(new Size(double.PositiveInfinity, ActualHeight));
            }

            width = Math.Max(width, header.CachedDesiredWidth ?? 0d);
        }
        else if (column.HeaderControl is null && width <= 0d)
        {
            // Nothing of the column has been measured yet; it is sized for real when it scrolls into view.
            width = column.ActualWidth > 0d ? column.ActualWidth : UnmeasuredColumnWidth;
        }

        return width;
    }

    /// <summary>
    /// Handles the selection changed event for the TableView.
    /// </summary>
    private void OnTableViewSelectionChanged()
    {
        if (TableView is not null && _selectAllCheckBox is not null)
        {
            // Setting IsChecked below can itself raise Checked/Unchecked (WinUI doesn't distinguish a
            // programmatic set from a user click) - those handlers call back into TableView.SelectAll()/
            // DeselectAll(), which re-enters this same method via the resulting SelectionChanged, and so on
            // forever until the stack overflows. _isSyncingSelectAllCheckBox marks this update as our own
            // so OnSelectAllCheckBoxChecked/Unchecked can ignore it instead of re-triggering selection.
            _isSyncingSelectAllCheckBox = true;

            if (TableView.Items.Count == 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = false;
            }
            else if (TableView.SelectedItems.Count == TableView.Items.Count)
            {
                _selectAllCheckBox.IsChecked = true;
                _selectAllCheckBox.IsEnabled = true;
            }
            else if (TableView.SelectedItems.Count > 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = false;
                _selectAllCheckBox.IsEnabled = true;
            }

            _isSyncingSelectAllCheckBox = false;
        }
    }

    /// <summary>
    /// Handles the selection changed event of the associated TableView.
    /// </summary>
    private void OnTableViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OnTableViewSelectionChanged();
    }

    /// <summary>
    /// Handles vector changes in the associated TableView item collection.
    /// </summary>
    private void OnTableViewItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs e)
    {
        OnTableViewSelectionChanged();
    }

    /// <summary>
    /// Handles changes to the SelectionMode property of the associated TableView.
    /// </summary>
    private void OnTableViewSelectionModePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        SetHeadersVisibility();
        OnTableViewSelectionChanged();
    }

    /// <summary>
    /// Handles changes to the CornerButtonMode property of the associated TableView.
    /// </summary>
    private void OnTableViewCornerButtonModePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        SetCornerButtonState();
    }

    /// <summary>
    /// Handles changes to the ItemsSource property of the associated TableView.
    /// </summary>
    private void OnTableViewItemsSourcePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        OnTableViewSelectionChanged();
    }

    /// <summary>
    /// Sets the state of the select all button.
    /// </summary>
    internal void SetCornerButtonState()
    {
        var stateName = VisualStates.StateNoButton;

        if (TableView is { SelectionMode: ListViewSelectionMode.Multiple })
        {
            stateName = TableView.IsEditing ? VisualStates.StateSelectAllCheckBoxDisabled : VisualStates.StateSelectAllCheckBox;
        }
        else if (TableView is { CornerButtonMode: TableViewCornerButtonMode.Options })
        {
            stateName = TableView.IsEditing ? VisualStates.StateOptionsButtonDisabled : VisualStates.StateOptionsButton;
        }
        else if (TableView is { CornerButtonMode: TableViewCornerButtonMode.SelectAll })
        {
            stateName = TableView.IsEditing ? VisualStates.StateSelectAllButtonDisabled : VisualStates.StateSelectAllButton;
        }

        VisualStates.GoToState(this, false, stateName);
    }

    /// <summary>
    /// Sets the horizontal offset of the cells in the header row based on the TableView settings.
    /// </summary>
    internal void SetCellsHorizontalOffset()
    {
        // if there are no items, we want to reset the horizontal offset
        // so that the header row corner can take up the desired width.
        if (TableView?.CollectionView.Count == 0)
        {
            if (TableView.SelectionMode is ListViewSelectionMode.Multiple)
            {
                TableView.CellsHorizontalOffset = double.NaN;
            }
            else if (TableView.HeadersVisibility is TableViewHeadersVisibility.All)
            {
                var rowHeaderWidth = TableView.RowHeaderWidth is double.NaN ? 0 : TableView.RowHeaderWidth;
                TableView.CellsHorizontalOffset = Math.Min(TableView.RowHeaderMaxWidth,
                    Math.Max(TableView.RowHeaderMinWidth, rowHeaderWidth));
            }
            else
            {
                TableView.CellsHorizontalOffset = 0;
            }
        }
    }

    /// <summary>
    /// Handles the SelectAllButton clicked event.
    /// </summary>
    private void OnSelectAllButtonClicked(object sender, TappedRoutedEventArgs e)
    {
        TableView?.SelectAll();
    }

    /// <summary>
    /// Handles the Checked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxChecked(object sender, RoutedEventArgs e)
    {
        // Ignore our own programmatic update from OnTableViewSelectionChanged - only react to a real
        // user click, otherwise this and DeselectAll() below recurse into each other via SelectionChanged
        // until the stack overflows.
        if (_isSyncingSelectAllCheckBox) return;

        TableView?.SelectAll();
    }

    /// <summary>
    /// Handles the Unchecked event for the select all checkbox.
    /// </summary>
    private void OnSelectAllCheckBoxUnchecked(object sender, RoutedEventArgs e)
    {
        if (_isSyncingSelectAllCheckBox) return;

        TableView?.DeselectAll();
    }

    /// <summary>
    /// Ensures grid lines are applied to the header row.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_h_gridLine is not null && TableView is not null)
        {
            GridLineSprite.SetShapeFill(_h_gridLine, TableView.HorizontalGridLinesStroke);
            _h_gridLine.Height = TableView.HorizontalGridLinesStrokeThickness;
            _h_gridLine.Visibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
                                     ? Visibility.Visible : Visibility.Collapsed;

            if (_v_gridLine is not null)
            {
                var vGridLinesVisibility = TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                           || TableView.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical;
                var areHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
                var isMultiSelection = TableView is { SelectionMode: ListViewSelectionMode.Multiple };
                var isDetailsToggleButtonVisible = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded
                                                    && (TableView.RowDetailsTemplate is not null || TableView.RowDetailsTemplateSelector is not null);


                GridLineSprite.SetShapeFill(_v_gridLine, TableView.HeaderGridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Vertical
                                                         ? TableView.VerticalGridLinesStroke : SharedResources.TransparentBrush);
                _v_gridLine.Width = TableView.VerticalGridLinesStrokeThickness;
                _v_gridLine.Visibility = vGridLinesVisibility && (areHeadersVisible || isMultiSelection || isDetailsToggleButtonVisible) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        foreach (var header in Headers)
        {
            header.EnsureGridLines();
        }

        if (!_calculatingHeaderWidths)
        {
            CalculateHeaderWidths();
        }
    }

    /// <summary>
    /// Gets the previous header in the header row.
    /// </summary>
    internal TableViewColumnHeader? GetPreviousHeader(TableViewColumnHeader currentHeader)
    {
        if (TableView is { Columns: { } } && currentHeader is { Column: { } })
        {
            var index = TableView.Columns.VisibleColumns.IndexOf(currentHeader.Column);

            if (index > 0)
            {
                return TableView.Columns.VisibleColumns[index - 1].HeaderControl;
            }
        }

        return default;
    }

    /// <summary>
    /// Sets the visibility of the row header based on the TableView settings.
    /// </summary>
    internal void SetHeadersVisibility()
    {
        if (_cornerButtonPanel is not null && TableView is not null)
        {
            var areRowHeadersVisible = TableView.HeadersVisibility is TableViewHeadersVisibility.All or TableViewHeadersVisibility.Rows;
            var isMultiSelection = TableView is { SelectionMode: ListViewSelectionMode.Multiple };
            var isRowDetailExpandable = TableView.RowDetailsVisibilityMode is TableViewRowDetailsVisibilityMode.VisibleWhenExpanded;

            _cornerButtonPanel.Visibility = areRowHeadersVisible || isMultiSelection || isRowDetailExpandable
                ? Visibility.Visible : Visibility.Collapsed;
        }

        SetCornerButtonState();
        EnsureGridLines();
    }

    /// <summary>
    /// Shows the column drop indicator at the specified position.
    /// </summary>
    internal void ShowColumnDropIndicator(double position, RenderTargetBitmap headerVisuals)
    {
        if (_columnDropIndicator is not null && FindHeader(new(position, ActualHeight / 2)) is { Column: { } dropColumn } dropHeader)
        {
            _isValidDropTarget = true;
            _dropColumnIndex = TableView?.Columns.VisibleColumns.IndexOf(dropColumn) ?? 0;
            var transform = dropHeader.TransformToVisual(this);
            var dropHeaderX = transform.TransformPoint(new Point(0, 0)).X;
            var midPoint = dropHeaderX + (dropHeader.ActualWidth / 2);
            var x = dropHeaderX;
            x += midPoint < position ? dropHeader.ActualWidth : 0d;
            x -= _columnDropIndicator.ActualWidth / 2;
            _dropColumnIndex += midPoint > position ? -1 : 0;
            _columnDropIndicator.Visibility = Visibility.Visible;
            _columnDropIndicatorTransform?.X = x;
            _dragHeaderImage?.Source = headerVisuals;
        }
        else
        {
            _isValidDropTarget = false;
            _columnDropIndicator?.Visibility = Visibility.Collapsed;
        }
    }

    internal void ColumnDropCompleted(TableViewColumn column, bool applyDrop)
    {
        if (TableView is not null && _columnDropIndicator is not null)
        {
            _columnDropIndicator.Visibility = Visibility.Collapsed;
            var isValidDropTarget = _isValidDropTarget;
            _isValidDropTarget = false;

            if (!isValidDropTarget || !applyDrop)
            {
                return;
            }

            var sourceIndex = TableView.Columns.VisibleColumns.IndexOf(column);
            var dropIndex = sourceIndex > _dropColumnIndex ? _dropColumnIndex + 1 : _dropColumnIndex;
            dropIndex = Math.Clamp(dropIndex, 0, TableView.Columns.VisibleColumns.Count - 1);

            if (dropIndex == sourceIndex) return;

            var reorderingArgs = new TableViewColumnReorderingEventArgs(column, dropIndex);
            TableView.OnColumnReordering(reorderingArgs);

            if (reorderingArgs.Cancel) return;

            TableView.DeselectAll();
            TableView.Columns.Move(sourceIndex, dropIndex);

            var reorderedArgs = new TableViewColumnReorderedEventArgs(column, dropIndex);
            TableView.OnColumnReordered(reorderedArgs);
        }
    }

    /// <summary>
    /// Finds the header at the specified position.
    /// </summary>
    private TableViewColumnHeader? FindHeader(Point position)
    {
        var transformedPoint = TransformToVisual(null).TransformPoint(position);
#if WINDOWS
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, this)
#else
        return VisualTreeHelper.FindElementsInHostCoordinates(transformedPoint, TableView, true)
                               .OfType<ContentPresenter>()
                               .Where(x => x.Name is "ContentPresenter")
                               .Select(x => x.FindAscendant<TableViewColumnHeader>() is { } header ? header : default)
#endif
                               .OfType<TableViewColumnHeader>()
                               .FirstOrDefault();
    }

    /// <summary>
    /// Inserts a header at the place of its column.
    /// </summary>
    /// <param name="header">The header to insert.</param>
    public void InsertHeader(TableViewColumnHeader header)
    {
        if (TableView is null || header is not { Column: { } column } || _frozenHeadersPanel is null || _scrollableHeadersPanel is null) return;

        if (_headersByColumn.TryGetValue((column, header.ColumnOccurrence), out var existing) && !ReferenceEquals(existing, header))
        {
            RemoveHeader(existing);
        }

        _headersByColumn[(column, header.ColumnOccurrence)] = header;
        AddToPanel(header, column.IsFrozen);
        EnsureStackOrder(_frozenHeadersPanel, isFrozen: true);
        EnsureStackOrder(_scrollableHeadersPanel, isFrozen: false);
    }

    /// <summary>
    /// Removes a header from the header row.
    /// </summary>
    /// <param name="header">The header to remove.</param>
    public void RemoveHeader(TableViewColumnHeader header)
    {
        Forget(header);
        RemoveFromPanel(header);
    }

    // Takes a header out of the lookup, unless another header has taken its place there.
    private void Forget(TableViewColumnHeader header)
    {
        if (header.Column is { } column)
        {
            if (column.HeaderControl == header)
            {
                column.HeaderControl = null;
            }

            if (_headersByColumn.TryGetValue((column, header.ColumnOccurrence), out var current) && ReferenceEquals(current, header))
            {
                _headersByColumn.Remove((column, header.ColumnOccurrence));
            }
        }
    }

    private void RemoveFromPanel(TableViewColumnHeader header)
    {
        (header.IsInFrozenPanel ? _frozenHeadersPanel : _scrollableHeadersPanel)?.Children.Remove(header);
    }

    /// <summary>
    /// Clears all headers from the header row.
    /// </summary>
    public void ClearHeaders()
    {
        ForgetHeaders();
        _frozenHeadersPanel?.Children.Clear();
        _scrollableHeadersPanel?.Children.Clear();
    }

    private void ForgetHeaders()
    {
        foreach (var header in _headersByColumn.Values)
        {
            if (header.Column?.HeaderControl == header)
            {
                header.Column.HeaderControl = null;
            }
        }

        _headersByColumn.Clear();
    }

    /// <summary>
    /// Gets the header of a column, or of one of its places when it is in the columns more than once.
    /// </summary>
    internal TableViewColumnHeader? GetHeader(TableViewColumn column, int occurrence = 0)
    {
        return _headersByColumn.TryGetValue((column, occurrence), out var header) ? header : null;
    }

    /// <summary>
    /// Handles the SizeChanged event for the TableView.
    /// </summary>
    private void OnTableViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        CalculateHeaderWidths();
    }

    /// <summary>
    /// Handles changes to the TableView property.
    /// </summary>
    private static void OnTableViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewHeaderRow headerRow)
        {
            headerRow.OnTableViewChanged(e);
        }
    }

    /// <summary>
    /// Handles changes to the TableView property.
    /// </summary>
    private void OnTableViewChanged(DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TableView oldTableView)
        {
            oldTableView.SizeChanged -= OnTableViewSizeChanged;
            oldTableView.SelectionChanged -= OnTableViewSelectionChanged;
            oldTableView.CollectionView.VectorChanged -= OnTableViewItemsVectorChanged;
            oldTableView.Columns.CollectionChanged -= OnTableViewColumnsCollectionChanged;
            oldTableView.Columns.ColumnPropertyChanged -= OnColumnPropertyChanged;

            UnregisterPropertyChangedCallback(oldTableView, TableView.SelectionModeProperty);
            UnregisterPropertyChangedCallback(oldTableView, TableView.CornerButtonModeProperty);
            UnregisterPropertyChangedCallback(oldTableView, TableView.ItemsSourceProperty);
            UnregisterPropertyChangedCallback(oldTableView, TableView.CellsHorizontalOffsetProperty);
            UnregisterPropertyChangedCallback(oldTableView, BorderThicknessProperty);
            UnregisterPropertyChangedCallback(oldTableView, PaddingProperty);
        }

        if (e.NewValue is TableView newTableView)
        {
            newTableView.SizeChanged += OnTableViewSizeChanged;
            newTableView.SelectionChanged += OnTableViewSelectionChanged;
            newTableView.CollectionView.VectorChanged += OnTableViewItemsVectorChanged;
            newTableView.Columns.CollectionChanged += OnTableViewColumnsCollectionChanged;
            newTableView.Columns.ColumnPropertyChanged += OnColumnPropertyChanged;

            _callbackTokens[TableView.SelectionModeProperty] =
                newTableView.RegisterPropertyChangedCallback(TableView.SelectionModeProperty, OnTableViewSelectionModePropertyChanged);
            _callbackTokens[TableView.CornerButtonModeProperty] =
                newTableView.RegisterPropertyChangedCallback(TableView.CornerButtonModeProperty, OnTableViewCornerButtonModePropertyChanged);
            _callbackTokens[TableView.ItemsSourceProperty] =
                newTableView.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, OnTableViewItemsSourcePropertyChanged);

            foreach (var property in new[] { TableView.CellsHorizontalOffsetProperty, BorderThicknessProperty, PaddingProperty })
            {
                _callbackTokens[property] = newTableView.RegisterPropertyChangedCallback(property, OnTableViewCellsAvailableWidthChanged);
            }
        }

        ApplyTableViewPadding();
    }

    /// <summary>
    /// Unregisters a property changed callback from the associated TableView.
    /// </summary>
    /// <param name="tableView">The TableView that owns the callback registration.</param>
    /// <param name="property">The dependency property to unregister.</param>
    private void UnregisterPropertyChangedCallback(TableView tableView, DependencyProperty property)
    {
        if (_callbackTokens.Remove(property, out var callbackToken))
        {
            tableView.UnregisterPropertyChangedCallback(property, callbackToken);
        }
    }

    /// <summary>
    /// Gets the list of headers in the header row.
    /// </summary>
    /// <remarks>Only the columns in or near the viewport have headers. The list is in column order.</remarks>
    public IReadOnlyList<TableViewColumnHeader> Headers
    {
        get
        {
            var headers = new TableViewColumnHeader[_headersByColumn.Count];
            _headersByColumn.Values.CopyTo(headers, 0);

            if (TableView?.Columns is TableViewColumnsCollection columns)
            {
                Array.Sort(headers, (x, y) => GetOrder(x).CompareTo(GetOrder(y)));

                int GetOrder(TableViewColumnHeader header)
                {
                    return header.Column is { } column && columns.GetVisibleColumnIndex(column, header.ColumnOccurrence) is >= 0 and var index ? index : int.MaxValue;
                }
            }

            return headers;
        }
    }

    /// <summary>
    /// Gets or sets the TableView associated with the header row.
    /// </summary>
    public TableView? TableView
    {
        get => (TableView?)GetValue(TableViewProperty);
        set => SetValue(TableViewProperty, value);
    }

    /// <summary>
    /// Identifies the TableView dependency property.
    /// </summary>
    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewHeaderRow), new PropertyMetadata(null, OnTableViewChanged));
}
