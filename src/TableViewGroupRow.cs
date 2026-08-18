using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;
using Windows.System;

namespace WinUI.TableView;

/// <summary>
/// Represents a group header row in a <see cref="WinUI.TableView.TableView"/>.
/// </summary>
/// <remarks>
/// Group header rows are visual only: they carry no cell slot, take no part in item selection, and are stepped
/// over by cell navigation. They are realized and recycled by the same row host as data rows, so a collapsed
/// group costs one realized element regardless of how many items it hides.
/// </remarks>
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
[TemplateVisualState(Name = VisualStates.StateExpanded, GroupName = VisualStates.GroupExpandCollapse)]
[TemplateVisualState(Name = VisualStates.StateCollapsed, GroupName = VisualStates.GroupExpandCollapse)]
public partial class TableViewGroupRow : Control
{
    private const double DefaultIndentSize = 20d;
    private ToggleButton? _expanderButton;

    /// <summary>
    /// Identifies the <see cref="Indent"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IndentProperty = DependencyProperty.Register(
        nameof(Indent), typeof(double), typeof(TableViewGroupRow), new PropertyMetadata(0d));

    /// <summary>
    /// Identifies the <see cref="ItemCount"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ItemCountProperty = DependencyProperty.Register(
        nameof(ItemCount), typeof(int), typeof(TableViewGroupRow), new PropertyMetadata(0));

    /// <summary>
    /// Identifies the <see cref="IsExpanded"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded), typeof(bool), typeof(TableViewGroupRow), new PropertyMetadata(true, OnIsExpandedChanged));

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupRow"/> class.
    /// </summary>
    public TableViewGroupRow()
    {
        DefaultStyleKey = typeof(TableViewGroupRow);
    }

    /// <summary>
    /// Gets the group this row is the header for.
    /// </summary>
    public TableViewGroup? Group { get; private set; }

    /// <summary>
    /// Gets the index of this row within the flattened visual row sequence, or -1 when not realized.
    /// </summary>
    internal int VisualIndex { get; set; } = -1;

    /// <summary>
    /// Gets the <see cref="WinUI.TableView.TableView"/> that owns this row.
    /// </summary>
    public TableView? TableView { get; internal set; }

    /// <summary>
    /// Gets the horizontal indent, in pixels, produced by the group's nesting level.
    /// </summary>
    public double Indent
    {
        get => (double)GetValue(IndentProperty);
        private set => SetValue(IndentProperty, value);
    }

    /// <summary>
    /// Gets the number of items the group covers.
    /// </summary>
    public int ItemCount
    {
        get => (int)GetValue(ItemCountProperty);
        private set => SetValue(ItemCountProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the group's contents are shown.
    /// </summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>
    /// Binds this row to a group. Called by the row host when the element is prepared or reused.
    /// </summary>
    internal void PrepareForGroup(TableViewGroup group, DataTemplate? headerTemplate, bool isExpanded)
    {
        Group = group;
        Indent = group.Level * DefaultIndentSize;
        ItemCount = group.ItemCount;
        Content = group;
        ContentTemplate = headerTemplate;

        SetValue(IsExpandedProperty, isExpanded);
        UpdateVisualState();
    }

    /// <summary>
    /// Clears the per-group state so the element can be reused for a different group.
    /// </summary>
    internal void PrepareForRecycle()
    {
        Group = null;
        Content = null;
    }

    /// <summary>
    /// Gets or sets the content shown in the header, which is the group itself.
    /// </summary>
    internal object? Content
    {
        get;
        set
        {
            field = value;

            if (GetTemplateChild(HeaderPresenterPart) is ContentPresenter presenter)
            {
                presenter.Content = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the template used to present the group key.
    /// </summary>
    internal DataTemplate? ContentTemplate
    {
        get;
        set
        {
            field = value;

            if (GetTemplateChild(HeaderPresenterPart) is ContentPresenter presenter)
            {
                presenter.ContentTemplate = value;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_expanderButton is not null)
        {
            _expanderButton.Click -= OnExpanderButtonClick;
        }

        _expanderButton = GetTemplateChild(ExpanderButtonPart) as ToggleButton;

        if (_expanderButton is not null)
        {
            _expanderButton.Click += OnExpanderButtonClick;
        }

        if (GetTemplateChild(HeaderPresenterPart) is ContentPresenter presenter)
        {
            presenter.Content = Content;
            presenter.ContentTemplate = ContentTemplate;
        }

        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        // Space and Enter toggle the group, which is the only interaction a header row offers.
        if (e.Key is VirtualKey.Space or VirtualKey.Enter)
        {
            Toggle();
            e.Handled = true;

            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
    {
        base.OnDoubleTapped(e);

        Toggle();
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new AutomationPeers.TableViewGroupRowAutomationPeer(this);
    }

    /// <summary>
    /// Expands the group when it is collapsed, and collapses it when it is expanded.
    /// </summary>
    internal void Toggle()
    {
        if (Group is { } group)
        {
            TableView?.ToggleGroup(group);
        }
    }

    private void OnExpanderButtonClick(object sender, RoutedEventArgs e)
    {
        Toggle();
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewGroupRow row)
        {
            row.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (_expanderButton is not null)
        {
            _expanderButton.IsChecked = IsExpanded;
        }

        VisualStates.GoToState(this, true, IsExpanded ? VisualStates.StateExpanded : VisualStates.StateCollapsed);
    }

    private const string ExpanderButtonPart = "ExpanderButton";
    private const string HeaderPresenterPart = "HeaderPresenter";
}
