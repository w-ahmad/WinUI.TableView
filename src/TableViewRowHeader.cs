using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Represents a header for row in TableView.
/// </summary>
public partial class TableViewRowHeader : ContentControl
{
    private ContentPresenter? _contentPresenter;
    private TableViewCellsPresenter? _cellPresenter;

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

            var desiredWidth = Math.Clamp(GetContentDesiredWidth(element), TableView.RowHeaderMinWidth, TableView.RowHeaderMaxWidth);
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
        desiredWidth = Math.Clamp(desiredWidth, TableView.RowHeaderMinWidth, TableView.RowHeaderMaxWidth);

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
        var rowHeight = TableViewRow?.Height is double.NaN ? double.PositiveInfinity : TableViewRow?.Height ?? 0;
        var rowMaxHeight = TableViewRow?.MaxHeight ?? double.PositiveInfinity;
        var contentHeight = Math.Min(rowHeight, rowMaxHeight);
        contentHeight -= element?.Margin.Top ?? 0;
        contentHeight -= element?.Margin.Bottom ?? 0;
        contentHeight -= Padding.Top;
        contentHeight -= Padding.Bottom;
        contentHeight -= BorderThickness.Top;
        contentHeight -= BorderThickness.Bottom;
        contentHeight -= GetHorizonalGridlineHeight();
        return contentHeight;
    }

    /// <summary>
    /// Retrieves the height of the horizontal gridline.
    /// </summary>
    private double GetHorizonalGridlineHeight()
    {
        _cellPresenter ??= this?.FindAscendant<TableViewCellsPresenter>();
        return _cellPresenter?.GetHorizonalGridlineHeight() ?? 0d;
    }

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableView? TableView { get; internal set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; internal set; }
}
