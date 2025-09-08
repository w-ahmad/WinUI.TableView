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
        var desiredWidth = 0d;
        if (TableView is not null && TableViewRow is not null && _contentPresenter is not null && ContentTemplateRoot is FrameworkElement element)
        {
            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860           
            element.MaxWidth = double.PositiveInfinity;
            element.MaxHeight = double.PositiveInfinity;
            #endregion

            element.Measure(availableSize: new Size(double.PositiveInfinity, double.PositiveInfinity));

            desiredWidth += element.DesiredSize.Width;
            desiredWidth += Padding.Left;
            desiredWidth += Padding.Right;
            desiredWidth += BorderThickness.Left;
            desiredWidth += BorderThickness.Right;
            desiredWidth = TableView.RowHeaderWidth is double.NaN ? desiredWidth : TableView.RowHeaderWidth;
            desiredWidth = Math.Max(TableView.RowHeaderActualWidth, desiredWidth);
            desiredWidth = Math.Clamp(desiredWidth, TableView.RowHeaderMinWidth, TableView.RowHeaderMaxWidth);

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            var contentWidth = desiredWidth;
            contentWidth -= element.Margin.Left;
            contentWidth -= element.Margin.Right;
            contentWidth -= Padding.Left;
            contentWidth -= Padding.Right;
            contentWidth -= BorderThickness.Left;
            contentWidth -= BorderThickness.Right;

            var rowHeight = TableViewRow.Height is double.NaN ? double.PositiveInfinity : TableViewRow.Height;
            var rowMaxHeight = TableViewRow.MaxHeight;
            var contentHeight = Math.Min(rowHeight, rowMaxHeight);
            contentHeight -= element.Margin.Top;
            contentHeight -= element.Margin.Bottom;
            contentHeight -= Padding.Top;
            contentHeight -= Padding.Bottom;
            contentHeight -= BorderThickness.Top;
            contentHeight -= BorderThickness.Bottom;
            contentHeight -= GetHorizonalGridlineHeight();

            if (contentWidth < 0 || contentHeight < 0)
            {
                _contentPresenter.Visibility = Visibility.Collapsed;
            }
            else
            {
                element.MaxWidth = contentWidth;
                element.MaxHeight = contentHeight;
                _contentPresenter.Visibility = Visibility.Visible;
            }
            #endregion
        }

        desiredWidth = Math.Clamp(desiredWidth, TableView?.RowHeaderMinWidth ?? 0, TableView?.RowHeaderMaxWidth ?? double.PositiveInfinity);
        TableView?.SetValue(TableView.RowHeaderActualWidthProperty, desiredWidth);

        return base.MeasureOverride(availableSize);
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
