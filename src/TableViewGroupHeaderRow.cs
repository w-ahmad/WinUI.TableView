#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Represents a group header row in a TableView control, used to display group information and provide expand/collapse functionality.
/// </summary>
public partial class TableViewGroupHeaderRow : ListViewHeaderItem
{
    /// <summary>
    /// The per-level indent step (in pixels). Shared with <see cref="TableViewRow"/> so rows line up with the
    /// deepest group header they belong to.
    /// </summary>
    internal const double GroupIndentSize = 24d;

    private Button? _expandCollapseButton;
    private Border? _indentPlaceholder;
    private Rectangle? _h_gridLine;

    /// <summary>
    /// Initializes a new instance of the TableViewGroupHeaderRow class.
    /// </summary>
    public TableViewGroupHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewGroupHeaderRow);

        Loaded += OnLoaded;
    }

    /// <summary>
    /// Gets or sets the owning <see cref="TableView"/>, used to keep this row's grid line and other
    /// TableView-driven appearance in sync.
    /// </summary>
    internal TableView? TableView { get; set; }

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _expandCollapseButton = GetTemplateChild("ExpandCollapseButton") as Button;
        _expandCollapseButton?.Click -= OnExpandCollapseButtonClick;
        _expandCollapseButton?.Click += OnExpandCollapseButtonClick;
        _indentPlaceholder = GetTemplateChild("IndentPlaceholder") as Border;
        _h_gridLine = GetTemplateChild("HorizontalGridLine") as Rectangle;

        EnsureGridLines();
        UpdateExpandCollapseButtonVisibility();

        if (Content is TableViewGroupInfo groupInfo)
        {
            UpdateExpandCollapseVisualState(groupInfo.IsExpanded);
        }
    }

    /// <summary>
    /// Registers this row with its owning <see cref="TableView"/> so it can be refreshed (e.g. grid line
    /// appearance) whenever a relevant TableView property changes.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureGridLines();
        UpdateExpandCollapseButtonVisibility();
    }

    /// <summary>
    /// Applies <see cref="TableView.HorizontalGridLinesStroke"/> and
    /// <see cref="TableView.HorizontalGridLinesStrokeThickness"/> to the divider below this row,
    /// matching every other horizontal grid line in the control.
    /// </summary>
    internal void EnsureGridLines()
    {
        if (_h_gridLine is null || TableView is null) return;

        _h_gridLine.Fill = TableView.HorizontalGridLinesStroke;
        _h_gridLine.Height = TableView.HorizontalGridLinesStrokeThickness;
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
    }

    /// <summary>
    /// Handles the Click event of the expand/collapse button, toggling the IsExpanded property of the associated TableViewGroupInfo.
    /// </summary>
    private void OnExpandCollapseButtonClick(object sender, RoutedEventArgs e)
    {
        if (Content is TableViewGroupInfo groupInfo)
        {
            groupInfo.IsExpanded = !groupInfo.IsExpanded;
            UpdateExpandCollapseVisualState(groupInfo.IsExpanded);
        }
    }

    /// <inheritdoc/>
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        if (Content is TableViewGroupInfo groupInfo)
        {
            _indentPlaceholder?.Width = groupInfo.Level * GroupIndentSize;
            UpdateExpandCollapseVisualState(groupInfo.IsExpanded);
            UpdateExpandCollapseButtonVisibility();
        }
    }

    /// <summary>
    /// Updates the visual state of the group header row based on whether it is expanded or collapsed.
    /// </summary>
    private void UpdateExpandCollapseVisualState(bool isExpanded)
    {
        VisualStates.GoToState(this, true, isExpanded ? VisualStates.StateExpanded : VisualStates.StateCollapsed);
    }
}
#endif
