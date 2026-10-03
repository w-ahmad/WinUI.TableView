using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace WinUI.TableView.AutomationPeers;

/// <summary>
/// Exposes <see cref="TableViewColumnHeader"/> to UI Automation.
/// Implements <see cref="IInvokeProvider"/> so that automation clients can trigger sorting
/// by invoking the column header, and surfaces sort state and filter status in the
/// accessible name and help text.
/// </summary>
public partial class TableViewColumnHeaderAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    private readonly TableViewColumnHeader _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewColumnHeaderAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The <see cref="TableViewColumnHeader"/> that is associated with this peer.</param>
    public TableViewColumnHeaderAutomationPeer(TableViewColumnHeader owner) : base(owner)
    {
        _owner = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore()
    {
        return nameof(TableViewColumnHeader);
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.HeaderItem;
    }

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
    {
        return TableViewLocalizedStrings.ColumnHeaderControlType;
    }

    /// <inheritdoc/>
    protected override bool IsContentElementCore()
    {
        return false;
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore()
    {
        return true;
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(_owner);
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (_owner.Column is null)
        {
            return base.GetNameCore();
        }

        var headerText = TableViewRowAutomationPeer.GetColumnHeaderText(_owner.Column);

        if (string.IsNullOrEmpty(headerText))
        {
            return base.GetNameCore();
        }

        // Append sort and filter state to the name for screen readers.
        var sortSuffix = _owner.Column.SortDirection switch
        {
            SortDirection.Ascending => $", {TableViewLocalizedStrings.SortAscending}",
            SortDirection.Descending => $", {TableViewLocalizedStrings.SortDescending}",
            _ => string.Empty
        };

        var filterSuffix = _owner.Column.IsFiltered
            ? $", {TableViewLocalizedStrings.Filtered}"
            : string.Empty;

        return $"{headerText}{sortSuffix}{filterSuffix}";
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var column = _owner.Column;
        if (column is null)
        {
            return base.GetHelpTextCore();
        }

        var hints = new System.Collections.Generic.List<string>();

        if (column.CanSort)
        {
            hints.Add(TableViewLocalizedStrings.SortAscending);
        }

        if (column.CanFilter)
        {
            hints.Add(TableViewLocalizedStrings.ClearFilter);
        }

        return hints.Count > 0 ? string.Join(", ", hints) : base.GetHelpTextCore();
    }

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke && (_owner.Column?.CanSort ?? false))
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }

    // IInvokeProvider

    /// <summary>
    /// Invokes the column header, cycling through sort directions (ascending → descending → unsorted).
    /// </summary>
    public void Invoke()
    {
        _owner.InvokeSortCycle();
    }
}
