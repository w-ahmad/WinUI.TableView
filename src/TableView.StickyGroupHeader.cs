using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// The sticky group header: one header drawn over the top of the rows, showing the group the first visible row is in
/// once that group's own header has scrolled out of view.
/// </summary>
/// <remarks>
/// The header is not one of the rows. It sits in the scroll viewer's template, above the rows and below the
/// scrollbars, and follows the realized rows, so it works with any row layout. The next group's header pushes it up as
/// it arrives. It is left out of the automation tree and the tab order: the header in the rows stays the one that
/// keyboard and screen reader users reach.
/// </remarks>
public partial class TableView
{
    private Grid? _stickyGroupHeaderHost;
    private RectangleGeometry? _stickyGroupHeaderHostClip;
    private TableViewGroupHeaderRow? _stickyGroupHeader;
    private TranslateTransform? _stickyGroupHeaderTransform;
    private double _lastGroupHeaderHeight;

    /// <summary>
    /// Gets the header shown over the top of the rows, or null until one has been needed.
    /// </summary>
    internal TableViewGroupHeaderRow? StickyGroupHeader => _stickyGroupHeader;

    /// <summary>
    /// Finds the host of the sticky header in the scroll viewer's template. A template without one turns the feature
    /// off.
    /// </summary>
    private void AttachStickyGroupHeaderHost(ScrollViewer scrollViewer)
    {
        if (_stickyGroupHeaderHost is not null)
        {
            _stickyGroupHeaderHost.SizeChanged -= OnStickyGroupHeaderHostSizeChanged;
            _stickyGroupHeaderHost.PointerWheelChanged -= OnScrollContentPresenterPointerWheelChanged;
            _stickyGroupHeaderHost.Children.Clear();
        }

        _stickyGroupHeader = null;
        _stickyGroupHeaderHost = scrollViewer.FindDescendant<Grid>(x => x.Name is "StickyGroupHeaderHost");

        if (_stickyGroupHeaderHost is not null)
        {
            _stickyGroupHeaderHost.SizeChanged += OnStickyGroupHeaderHostSizeChanged;

            // The wheel scrolls horizontally over the header just as over the rows.
            _stickyGroupHeaderHost.PointerWheelChanged += OnScrollContentPresenterPointerWheelChanged;
        }

        UpdateStickyGroupHeader();
    }

    /// <summary>
    /// Clips the host, so a header pushed up by the next group slides out under the column headers.
    /// </summary>
    private void OnStickyGroupHeaderHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _stickyGroupHeaderHostClip ??= new RectangleGeometry();
        _stickyGroupHeaderHostClip.Rect = new(0, 0, e.NewSize.Width, e.NewSize.Height);
        _stickyGroupHeaderHost!.Clip = _stickyGroupHeaderHostClip;
    }

    /// <summary>
    /// Shows the header of the group the first visible row is in, pushed up by the next group's header, or hides it
    /// when that group's own header is in view.
    /// </summary>
    /// <remarks>
    /// Runs on every scroll and after every layout of the rows, so it only sets what changed.
    /// </remarks>
    internal void UpdateStickyGroupHeader()
    {
        if (!AreStickyGroupHeadersActive || !IsLoaded || _stickyGroupHeaderHost is null || _visualRows is null || _rowsRepeater is null)
        {
            HideStickyGroupHeader();
            return;
        }

        var top = RowsViewportTop;

        if (top <= 0d)
        {
            HideStickyGroupHeader();
            return;
        }

        var firstRow = RealizedRows.FindAtOffset(top, out _);

        if (firstRow < 0)
        {
            // Nothing is laid out yet; keep showing what is shown until something is.
            return;
        }

        var headerIndex = FindGroupHeaderAtOrBefore(firstRow);

        if (headerIndex < 0
            || (RealizedRows.TryGetBounds(headerIndex, out var headerTop, out _) && headerTop >= top)
            || _visualRows.GetGroupHeader(headerIndex) is not { } group)
        {
            HideStickyGroupHeader();
            return;
        }

        var header = EnsureStickyGroupHeader();

        if (!ReferenceEquals(header.Content, group))
        {
            header.ContentTemplate = GetGroupHeaderTemplate(group, header);
            group.PublishBindableValues();
            header.Content = group;
        }

        if (header.Visibility is not Visibility.Visible)
        {
            header.Visibility = Visibility.Visible;
        }

        var width = _rowsRepeater.ActualWidth;

        if (header.Width != width)
        {
            header.Width = width;
        }

        var margin = new Thickness(Padding.Left, 0, 0, 0);

        if (header.Margin != margin)
        {
            header.Margin = margin;
        }

        var y = 0d;

        if (FindGroupHeaderAfter(headerIndex) is >= 0 and var nextIndex && RealizedRows.TryGetBounds(nextIndex, out var nextTop, out _))
        {
            y = Math.Min(0d, nextTop - top - header.ActualHeight);
        }

        // Rows scroll horizontally inside the scroll viewer on Uno; on Windows the offset stays 0.
        var x = -(_scrollViewer?.HorizontalOffset ?? 0d);

        if (_stickyGroupHeaderTransform!.Y != y)
        {
            _stickyGroupHeaderTransform.Y = y;
        }

        if (_stickyGroupHeaderTransform.X != x)
        {
            _stickyGroupHeaderTransform.X = x;
        }
    }

    /// <summary>
    /// Creates the sticky header the first time one is shown.
    /// </summary>
    private TableViewGroupHeaderRow EnsureStickyGroupHeader()
    {
        if (_stickyGroupHeader is null)
        {
            _stickyGroupHeaderTransform = new TranslateTransform();
            _stickyGroupHeader = new TableViewGroupHeaderRow
            {
                TableView = this,
                IsPinned = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransform = _stickyGroupHeaderTransform
            };

            _stickyGroupHeaderHost!.Children.Add(_stickyGroupHeader);
        }

        return _stickyGroupHeader;
    }

    /// <summary>
    /// Hides the sticky header, letting it drop its group when the table is unloaded.
    /// </summary>
    private void HideStickyGroupHeader(bool releaseGroup = false)
    {
        if (_stickyGroupHeader is null)
        {
            return;
        }

        if (_stickyGroupHeader.Visibility is not Visibility.Collapsed)
        {
            _stickyGroupHeader.Visibility = Visibility.Collapsed;
        }

        if (releaseGroup)
        {
            _stickyGroupHeader.Content = null;
        }
    }

    /// <summary>
    /// Gets the height the sticky header covers at the top of the viewport.
    /// </summary>
    /// <remarks>
    /// The height of the group's header in the rows when it is realized, else the sticky header's own when it is
    /// shown, else the last one seen, since a header far out of view is not realized.
    /// </remarks>
    private double GetStickyGroupHeaderHeight(int headerIndex)
    {
        if (RealizedRows.TryGetBounds(headerIndex, out _, out var height) && height > 0d)
        {
            _lastGroupHeaderHeight = height;
        }
        else if (_stickyGroupHeader is { ActualHeight: > 0d } header)
        {
            _lastGroupHeaderHeight = header.ActualHeight;
        }

        return _lastGroupHeaderHeight;
    }
}
