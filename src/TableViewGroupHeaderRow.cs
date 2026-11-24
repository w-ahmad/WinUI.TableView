using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;
using WinUI.TableView.AutomationPeers;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a group header row in a TableView control, used to display group information and provide expand/collapse functionality.
/// </summary>
/// <remarks>
/// Realized and recycled by the table's row host alongside its data rows, so an instance shows many different
/// groups over its lifetime. Its <see cref="ContentControl.Content"/> is the <see cref="TableViewGroupInfo"/> of the
/// group it currently shows, and its <see cref="ContentControl.ContentTemplate"/> comes from the table's
/// <see cref="TableView.GroupStyle"/> for the group's nesting level.
/// </remarks>
[TemplateVisualState(Name = VisualStates.StateExpanded, GroupName = VisualStates.GroupExpandCollapse)]
[TemplateVisualState(Name = VisualStates.StateCollapsed, GroupName = VisualStates.GroupExpandCollapse)]
[TemplateVisualState(Name = VisualStates.StateUnpinned, GroupName = VisualStates.GroupPin)]
[TemplateVisualState(Name = VisualStates.StatePinned, GroupName = VisualStates.GroupPin)]
public partial class TableViewGroupHeaderRow : ContentControl
{
    /// <summary>
    /// The per-level indent step (in pixels). Shared with <see cref="TableViewRow"/> so rows line up with the
    /// deepest group header they belong to.
    /// </summary>
    internal const double GroupIndentSize = 24d;

    private Button? _expandCollapseButton;
    private Border? _indentPlaceholder;
    private Border? _h_gridLine;
    private FrameworkElement? _h_gridLineSpace;
    private bool _isPinned;

    /// <summary>
    /// Initializes a new instance of the TableViewGroupHeaderRow class.
    /// </summary>
    public TableViewGroupHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewGroupHeaderRow);
    }

    /// <summary>
    /// Gets or sets the owning <see cref="TableView"/>, used to keep this row's grid line and other
    /// TableView-driven appearance in sync.
    /// </summary>
    internal TableView? TableView { get; set; }

    /// <summary>
    /// Gets or sets the header's index in the flattened visual row sequence, or -1 when it is not realized.
    /// </summary>
    internal int VisualIndex { get; set; } = -1;

    /// <summary>
    /// Gets the group this header currently shows.
    /// </summary>
    public TableViewGroupInfo? GroupInfo => Content as TableViewGroupInfo;

    /// <summary>
    /// Gets or sets a value indicating whether this header is pinned to the top of the viewport by
    /// <see cref="TableView.AreStickyGroupHeadersEnabled"/>, drawing over the rows scrolling beneath it.
    /// </summary>
    internal bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned != value)
            {
                _isPinned = value;
                VisualStates.GoToState(this, false, value ? VisualStates.StatePinned : VisualStates.StateUnpinned);
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Rows are measured at an unbounded width, and the rows' width is the table's horizontal extent. A header spans
    /// the viewport, so it is measured no wider than that: a long group key must not make the table scroll sideways.
    /// </remarks>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width) && TableView?.RowsViewportWidth is > 0d and var viewportWidth)
        {
            availableSize.Width = viewportWidth;
        }

        var size = base.MeasureOverride(availableSize);

        return double.IsInfinity(availableSize.Width) ? size : new Size(Math.Min(size.Width, availableSize.Width), size.Height);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _expandCollapseButton?.Click -= OnExpandCollapseButtonClick;
        _expandCollapseButton = GetTemplateChild("ExpandCollapseButton") as Button;
        _expandCollapseButton?.Click += OnExpandCollapseButtonClick;
        _indentPlaceholder = GetTemplateChild("IndentPlaceholder") as Border;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Border;
        _h_gridLineSpace = GetTemplateChild("HorizontalGridLineSpace") as FrameworkElement;

        EnsureGridLines();
        UpdateExpandCollapseButtonVisibility();
        UpdateGroupVisuals(useTransitions: false);
        VisualStates.GoToState(this, false, _isPinned ? VisualStates.StatePinned : VisualStates.StateUnpinned);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new TableViewGroupHeaderRowAutomationPeer(this);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        // A header is not a data row: pressing it must not start a row selection or a drag rectangle, which is
        // what the table does with a press that reaches it unhandled.
        e.Handled = true;
    }

    /// <summary>
    /// Applies <see cref="TableView.HorizontalGridLinesStroke"/> and
    /// <see cref="TableView.HorizontalGridLinesStrokeThickness"/> to the divider below this row,
    /// matching every other horizontal grid line in the control.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_h_gridLine is null || TableView is null) return;

        GridLineSprite.SetBottomEdge(_h_gridLine, TableView.HorizontalGridLinesStroke, TableView.HorizontalGridLinesStrokeThickness);
        _h_gridLineSpace?.Height = TableView.HorizontalGridLinesStrokeThickness;
    }

    /// <summary>
    /// Shows or hides the expand/collapse button per <see cref="TableView.ShowGroupExpandCollapseButton"/>.
    /// The button's layout slot - and with it, this row's level-based indent, applied via the button's own
    /// Margin - is kept either way; only its visual/interactivity is toggled, so hiding it doesn't flatten
    /// every level's indentation back to zero.
    /// </summary>
    internal void UpdateExpandCollapseButtonVisibility()
    {
        if (_expandCollapseButton is null) return;

        var showButton = TableView?.ShowGroupExpandCollapseButton != false;
        _expandCollapseButton.Opacity = showButton ? 1 : 0;
        _expandCollapseButton.IsHitTestVisible = showButton;
        // The sticky header is a copy drawn over the rows; keyboard users reach the header in the rows.
        _expandCollapseButton.IsTabStop = showButton && !IsPinned;
    }

    /// <summary>
    /// Toggles the group this header shows.
    /// </summary>
    internal void ToggleExpanded()
    {
        if (GroupInfo is { } groupInfo)
        {
            // The owning CollectionView rebuilds the view in response, which replaces this header's content.
            var wasExpanded = groupInfo.IsExpanded;
            groupInfo.IsExpanded = !wasExpanded;
#if WINDOWS
            if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged) && FrameworkElementAutomationPeer.FromElement(this) is { } peer)
            {
                peer.RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    wasExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    wasExpanded ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded);
            }
#endif
        }
    }

    /// <summary>
    /// Handles the Click event of the expand/collapse button, toggling the IsExpanded property of the associated TableViewGroupInfo.
    /// </summary>
    private void OnExpandCollapseButtonClick(object sender, RoutedEventArgs e)
    {
        ToggleExpanded();
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        UpdateGroupVisuals(useTransitions: false);
        UpdateExpandCollapseButtonVisibility();
    }

    /// <summary>
    /// Brings the indent and the expanded or collapsed state up to date with the group shown, without transitions.
    /// </summary>
    internal void RefreshGroupVisuals()
    {
        UpdateGroupVisuals(useTransitions: false);
    }

    /// <summary>
    /// Brings the indent and the expanded/collapsed visual state in line with the group being shown.
    /// </summary>
    private void UpdateGroupVisuals(bool useTransitions)
    {
        if (GroupInfo is not { } groupInfo)
        {
            return;
        }

        _indentPlaceholder?.Width = groupInfo.Level * GroupIndentSize;
        VisualStates.GoToState(this, useTransitions, groupInfo.IsExpanded ? VisualStates.StateExpanded : VisualStates.StateCollapsed);
    }
}
