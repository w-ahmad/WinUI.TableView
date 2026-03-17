using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Foundation;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents a header for row in TableView.
/// </summary>
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewRowHeader : ContentControl
{
    private ContentPresenter? _contentPresenter;
    private ToggleButton? _hierarchyToggleButton;
    private bool _isHierarchyExpanderVisible;
    private bool _isHierarchyExpanded;
    private bool _isUpdatingHierarchyToggle;

    /// <summary>
    /// Initializes a new instance of the TableViewRowHeader class.
    /// </summary>
    public TableViewRowHeader()
    {
        DefaultStyleKey = typeof(TableViewRowHeader);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPresenter = GetTemplateChild("Content") as ContentPresenter;
        _hierarchyToggleButton = GetTemplateChild("HierarchyToggleButton") as ToggleButton;

        if (_hierarchyToggleButton is not null)
        {
            _hierarchyToggleButton.Checked -= OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Unchecked -= OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Checked += OnHierarchyToggleButtonChanged;
            _hierarchyToggleButton.Unchecked += OnHierarchyToggleButtonChanged;
        }

        UpdateHierarchyState();
    }

    private void OnHierarchyToggleButtonChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingHierarchyToggle)
        {
            return;
        }

        if (TableViewRow?.Content is null || TableView is null || _hierarchyToggleButton is null)
        {
            return;
        }

        TableView.SetItemExpanded(TableViewRow.Content, _hierarchyToggleButton.IsChecked is true);
    }

    private void UpdateHierarchyState()
    {
        if (_hierarchyToggleButton is null)
        {
            return;
        }

        _isUpdatingHierarchyToggle = true;
        _hierarchyToggleButton.Visibility = _isHierarchyExpanderVisible ? Visibility.Visible : Visibility.Collapsed;
        _hierarchyToggleButton.IsChecked = _isHierarchyExpanded;
        _hierarchyToggleButton.Content = _isHierarchyExpanded ? "▼" : "▶";
        _isUpdatingHierarchyToggle = false;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is not null && TableViewRow is not null && _contentPresenter is not null)
        {
            var element = ContentTemplateRoot as FrameworkElement;
            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860           
            if (element is not null)
            {
                element.MaxWidth = double.PositiveInfinity;
                element.MaxHeight = double.PositiveInfinity;
            }
            #endregion

            element?.Measure(availableSize: new Size(double.PositiveInfinity, double.PositiveInfinity));

            var desiredWidth = GetContentDesiredWidth(element);
            TableView?.SetValue(TableView.RowHeaderActualWidthProperty, desiredWidth);

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            var contentWidth = GetContentWidth(desiredWidth, element);
            var contentHeight = GetContentHeight(element);

            if (contentWidth < 0 || contentHeight < 0)
            {
                _contentPresenter.Visibility = Visibility.Collapsed;
            }
            else if (element is not null)
            {
                element.MaxWidth = contentWidth;
                element.MaxHeight = contentHeight;
                _contentPresenter.Visibility = Visibility.Visible;
            }
            #endregion
        }

        return base.MeasureOverride(availableSize);
    }

    private double GetContentDesiredWidth(FrameworkElement? element)
    {
        if (TableView is null) return 0d;

        var desiredWidth = element?.DesiredSize.Width ?? 0;
        desiredWidth += Padding.Left;
        desiredWidth += Padding.Right;
        desiredWidth += BorderThickness.Left;
        desiredWidth += BorderThickness.Right;
        desiredWidth = TableView.RowHeaderWidth is double.NaN ? desiredWidth : TableView.RowHeaderWidth;
        desiredWidth = Math.Max(TableView.RowHeaderActualWidth, desiredWidth);

        return desiredWidth;
    }

    private double GetContentWidth(double desiredWidth, FrameworkElement? element)
    {
        var contentWidth = desiredWidth;
        contentWidth -= element?.Margin.Left ?? 0;
        contentWidth -= element?.Margin.Right ?? 0;
        contentWidth -= Padding.Left;
        contentWidth -= Padding.Right;
        contentWidth -= BorderThickness.Left;
        contentWidth -= BorderThickness.Right;
        return contentWidth;
    }

    private double GetContentHeight(FrameworkElement? element)
    {
        var height = Height is double.NaN ? double.PositiveInfinity : Height;
        var contentHeight = Math.Min(height, MaxHeight);
        contentHeight -= element?.Margin.Top ?? 0;
        contentHeight -= element?.Margin.Bottom ?? 0;
        contentHeight -= Padding.Top;
        contentHeight -= Padding.Bottom;
        contentHeight -= BorderThickness.Top;
        contentHeight -= BorderThickness.Bottom;
        contentHeight -= GetHorizontalGridlineHeight();
        return contentHeight;
    }

    /// <summary>
    /// Gets the height of the horizontal gridlines.
    /// </summary>
    private double GetHorizontalGridlineHeight()
    {
        return TableView?.GridLinesVisibility is TableViewGridLinesVisibility.All or TableViewGridLinesVisibility.Horizontal
            ? TableView.HorizontalGridLinesStrokeThickness : 0d;
    }

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableView? TableView { get; internal set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; internal set; }

    /// <summary>
    /// Gets or sets a value indicating whether the hierarchy expander is visible.
    /// </summary>
    internal bool IsHierarchyExpanderVisible
    {
        get => _isHierarchyExpanderVisible;
        set
        {
            _isHierarchyExpanderVisible = value;
            UpdateHierarchyState();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the hierarchy expander is expanded.
    /// </summary>
    internal bool IsHierarchyExpanded
    {
        get => _isHierarchyExpanded;
        set
        {
            _isHierarchyExpanded = value;
            UpdateHierarchyState();
        }
    }
}
