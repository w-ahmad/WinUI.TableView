using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Foundation;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents a header for row in TableView.
/// </summary>
public partial class TableViewRowHeader : ContentControl
{
    private FrameworkElement? _contentPresenter; // not ContentPresenter: Native AOT can wrap it as a base class

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

        _contentPresenter = GetTemplateChild("Content") as FrameworkElement;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TableView is not null && TableViewRow is not null && _contentPresenter is not null)
        {
            var element = GetTemplateRoot();

            // A template that was just set has no content until the presenter measures.
            if (element is null && ContentTemplate is not null)
            {
                _contentPresenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                element = GetTemplateRoot();
            }

            element?.Measure(availableSize: new Size(double.PositiveInfinity, double.PositiveInfinity));

            var desiredWidth = GetContentDesiredWidth(element);

            // Every row header measures on every pass, and the shared width rarely changes.
            if (TableView is not null && desiredWidth != TableView.RowHeaderActualWidth)
            {
                TableView.SetValue(TableView.RowHeaderActualWidthProperty, desiredWidth);
            }
        }

        return base.MeasureOverride(availableSize);
    }

    private FrameworkElement? GetTemplateRoot()
    {
#if WINDOWS
        return ContentTemplateRoot as FrameworkElement;
#else
        // Uno only exposes the template's root on the presenter that realizes it.
        return (_contentPresenter as ContentPresenter)?.GetContentTemplateRoot();
#endif
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

    /// <summary>
    /// Gets or sets the TableViewRow associated with the presenter.
    /// </summary>
    public TableView? TableView { get; internal set; }

    /// <summary>
    /// Gets or sets the TableView associated with the presenter.
    /// </summary>
    public TableViewRow? TableViewRow { get; internal set; }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewRowHeaderAutomationPeer(this);
    }
}
