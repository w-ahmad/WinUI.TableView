using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;

namespace WinUI.TableView;

public partial class TableViewRowHeader : ContentControl
{
    private ContentPresenter? _contentPresenter;
    private TableViewCellsPresenter? _cellPresenter;

    public TableViewRowHeader()
    {
        DefaultStyleKey = typeof(TableViewRowHeader);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPresenter = GetTemplateChild("Content") as ContentPresenter;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is not null && TableViewRow is not null && _contentPresenter is not null && ContentTemplateRoot is FrameworkElement element)
        {
            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860           
            element.MaxWidth = double.PositiveInfinity;
            element.MaxHeight = double.PositiveInfinity;
            #endregion

            element.Measure(availableSize: new Size(double.PositiveInfinity, double.PositiveInfinity));

            var desiredWidth = element.DesiredSize.Width;
            desiredWidth += Padding.Left;
            desiredWidth += Padding.Right;
            desiredWidth += BorderThickness.Left;
            desiredWidth += BorderThickness.Right;
            desiredWidth = TableView.RowHeaderWidth is double.NaN ? desiredWidth : TableView.RowHeaderWidth;

            var actualWidth = Math.Max(TableView.RowHeaderActualWidth, desiredWidth);
            actualWidth = Math.Clamp(actualWidth, TableView.RowHeaderMinWidth, TableView.RowHeaderMaxWidth);

            TableView.SetValue(TableView.RowHeaderActualWidthProperty, actualWidth);

            #region TEMP_FIX_FOR_ISSUE https://github.com/microsoft/microsoft-ui-xaml/issues/9860
            var contentWidth = TableView.RowHeaderActualWidth;
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

    public TableView? TableView { get; internal set; }
    public TableViewRow? TableViewRow { get; internal set; }
}
