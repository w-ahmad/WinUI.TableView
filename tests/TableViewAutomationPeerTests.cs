using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;
using WinUI.TableView.AutomationPeers;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewAutomationPeerTests
{
    // ─── TableViewAutomationPeer ────────────────────────────────────────────

    [UITestMethod]
    public void TableViewAutomationPeer_ControlType_IsDataGrid()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);

        Assert.AreEqual(AutomationControlType.DataGrid, peer.GetAutomationControlType());
    }

    [UITestMethod]
    public void TableViewAutomationPeer_ClassName_IsTableView()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);

        Assert.AreEqual("TableView", peer.GetClassName());
    }

    [UITestMethod]
    public void TableViewAutomationPeer_RowCount_MatchesItemsCount()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);

        Assert.AreEqual(tableView.Items.Count, peer.RowCount);
    }

    [UITestMethod]
    public void TableViewAutomationPeer_ColumnCount_MatchesVisibleColumns()
    {
        var tableView = new TableView
        {
            AutoGenerateColumns = false
        };
        tableView.Columns.Add(new TableViewTextColumn { Header = "A" });
        tableView.Columns.Add(new TableViewTextColumn { Header = "B" });

        var peer = new TableViewAutomationPeer(tableView);

        Assert.AreEqual(2, peer.ColumnCount);
    }

    [UITestMethod]
    public void TableViewAutomationPeer_SupportsGridPattern()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);

        var gridProvider = peer.GetPattern(PatternInterface.Grid);

        Assert.IsNotNull(gridProvider);
        Assert.IsInstanceOfType(gridProvider, typeof(IGridProvider));
    }

    [UITestMethod]
    public void TableViewAutomationPeer_SupportsTablePattern()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);

        var tableProvider = peer.GetPattern(PatternInterface.Table);

        Assert.IsNotNull(tableProvider);
        Assert.IsInstanceOfType(tableProvider, typeof(ITableProvider));
    }

    [UITestMethod]
    public void TableViewAutomationPeer_RowOrColumnMajor_IsRowMajor()
    {
        var tableView = new TableView();
        var peer = new TableViewAutomationPeer(tableView);
        var tableProvider = (ITableProvider)peer.GetPattern(PatternInterface.Table)!;

        Assert.AreEqual(RowOrColumnMajor.RowMajor, tableProvider.RowOrColumnMajor);
    }

    // ─── TableViewRowAutomationPeer ─────────────────────────────────────────

    [UITestMethod]
    public void TableViewRowAutomationPeer_ControlType_IsDataItem()
    {
        var row = new TableViewRow();
        var peer = new TableViewRowAutomationPeer(row);

        Assert.AreEqual(AutomationControlType.DataItem, peer.GetAutomationControlType());
    }

    [UITestMethod]
    public void TableViewRowAutomationPeer_ClassName_IsTableViewRow()
    {
        var row = new TableViewRow();
        var peer = new TableViewRowAutomationPeer(row);

        Assert.AreEqual("TableViewRow", peer.GetClassName());
    }

    [UITestMethod]
    public void TableViewRowAutomationPeer_Name_ContainsRowLabel()
    {
        var row = new TableViewRow();
        var peer = new TableViewRowAutomationPeer(row);

        // Row.Index returns -1 when not in a list, so name falls back to "Row"
        var name = peer.GetName();

        Assert.IsFalse(string.IsNullOrEmpty(name));
    }

    [UITestMethod]
    public void TableViewRowAutomationPeer_AutomationNameOverride_IsRespected()
    {
        var row = new TableViewRow();
        AutomationProperties.SetName(row, "Custom Row Name");
        var peer = new TableViewRowAutomationPeer(row);

        Assert.AreEqual("Custom Row Name", peer.GetName());
    }

    // ─── TableViewCellAutomationPeer ────────────────────────────────────────

    [UITestMethod]
    public void TableViewCellAutomationPeer_ControlType_IsCustom()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);

        Assert.AreEqual(AutomationControlType.Custom, peer.GetAutomationControlType());
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_ClassName_IsTableViewCell()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);

        Assert.AreEqual("TableViewCell", peer.GetClassName());
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_RowSpan_IsOne()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);
        var gridItemProvider = (IGridItemProvider)peer.GetPattern(PatternInterface.GridItem)!;

        Assert.AreEqual(1, gridItemProvider.RowSpan);
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_ColumnSpan_IsOne()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);
        var gridItemProvider = (IGridItemProvider)peer.GetPattern(PatternInterface.GridItem)!;

        Assert.AreEqual(1, gridItemProvider.ColumnSpan);
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_SupportsGridItemPattern()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);

        var pattern = peer.GetPattern(PatternInterface.GridItem);

        Assert.IsNotNull(pattern);
        Assert.IsInstanceOfType(pattern, typeof(IGridItemProvider));
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_SupportsTableItemPattern()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);

        var pattern = peer.GetPattern(PatternInterface.TableItem);

        Assert.IsNotNull(pattern);
        Assert.IsInstanceOfType(pattern, typeof(ITableItemProvider));
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_SupportsSelectionItemPattern()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);

        var pattern = peer.GetPattern(PatternInterface.SelectionItem);

        Assert.IsNotNull(pattern);
        Assert.IsInstanceOfType(pattern, typeof(ISelectionItemProvider));
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_AutomationNameOverride_IsRespected()
    {
        var cell = new TableViewCell();
        AutomationProperties.SetName(cell, "Custom Cell");
        var peer = new TableViewCellAutomationPeer(cell);

        Assert.AreEqual("Custom Cell", peer.GetName());
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_IsNotSelectedInitially()
    {
        var cell = new TableViewCell();
        var peer = new TableViewCellAutomationPeer(cell);
        var selectionItem = (ISelectionItemProvider)peer.GetPattern(PatternInterface.SelectionItem)!;

        Assert.IsFalse(selectionItem.IsSelected);
    }

    // ─── TableViewColumnHeaderAutomationPeer ────────────────────────────────

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_ControlType_IsHeaderItem()
    {
        var header = new TableViewColumnHeader();
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        Assert.AreEqual(AutomationControlType.HeaderItem, peer.GetAutomationControlType());
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_ClassName_IsTableViewColumnHeader()
    {
        var header = new TableViewColumnHeader();
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        Assert.AreEqual("TableViewColumnHeader", peer.GetClassName());
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_Name_UsesColumnHeader()
    {
        var column = new TableViewTextColumn { Header = "Employee Name" };
        var header = new TableViewColumnHeader { Column = column };
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        Assert.AreEqual("Employee Name", peer.GetName());
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_Name_IncludesSortState_Ascending()
    {
        var column = new TableViewTextColumn { Header = "Name" };
        column.SortDirection = SortDirection.Ascending;
        var header = new TableViewColumnHeader { Column = column };
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        var name = peer.GetName();

        StringAssert.Contains(name, "Name");
        StringAssert.Contains(name, TableViewLocalizedStrings.SortAscending);
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_Name_IncludesSortState_Descending()
    {
        var column = new TableViewTextColumn { Header = "Name" };
        column.SortDirection = SortDirection.Descending;
        var header = new TableViewColumnHeader { Column = column };
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        var name = peer.GetName();

        StringAssert.Contains(name, TableViewLocalizedStrings.SortDescending);
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_Name_IncludesFilterState()
    {
        var column = new TableViewTextColumn { Header = "Name" };
        column.IsFiltered = true;
        var header = new TableViewColumnHeader { Column = column };
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        var name = peer.GetName();

        StringAssert.Contains(name, TableViewLocalizedStrings.Filtered);
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_DoesNotExposeInvokePattern_WhenCannotSort()
    {
        var column = new TableViewTextColumn { Header = "Name", CanSort = false };
        var header = new TableViewColumnHeader { Column = column };
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        var pattern = peer.GetPattern(PatternInterface.Invoke);

        Assert.IsNull(pattern);
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_AutomationNameOverride_IsRespected()
    {
        var header = new TableViewColumnHeader();
        AutomationProperties.SetName(header, "Custom Header");
        var peer = new TableViewColumnHeaderAutomationPeer(header);

        Assert.AreEqual("Custom Header", peer.GetName());
    }

    // ─── TableViewRowHeaderAutomationPeer ───────────────────────────────────

    [UITestMethod]
    public void TableViewRowHeaderAutomationPeer_ControlType_IsHeaderItem()
    {
        var rowHeader = new TableViewRowHeader();
        var peer = new TableViewRowHeaderAutomationPeer(rowHeader);

        Assert.AreEqual(AutomationControlType.HeaderItem, peer.GetAutomationControlType());
    }

    [UITestMethod]
    public void TableViewRowHeaderAutomationPeer_ClassName_IsTableViewRowHeader()
    {
        var rowHeader = new TableViewRowHeader();
        var peer = new TableViewRowHeaderAutomationPeer(rowHeader);

        Assert.AreEqual("TableViewRowHeader", peer.GetClassName());
    }

    [UITestMethod]
    public void TableViewRowHeaderAutomationPeer_AutomationNameOverride_IsRespected()
    {
        var rowHeader = new TableViewRowHeader();
        AutomationProperties.SetName(rowHeader, "Row Header Custom");
        var peer = new TableViewRowHeaderAutomationPeer(rowHeader);

        Assert.AreEqual("Row Header Custom", peer.GetName());
    }

    // ─── Helper Methods ──────────────────────────────────────────────────────

    [UITestMethod]
    public void TableViewRowAutomationPeer_GetColumnHeaderText_ReturnsStringHeader()
    {
        var column = new TableViewTextColumn { Header = "Hello" };

        var result = TableViewRowAutomationPeer.GetColumnHeaderText(column);

        Assert.AreEqual("Hello", result);
    }

    [UITestMethod]
    public void TableViewRowAutomationPeer_GetColumnHeaderText_ReturnsEmptyForNull()
    {
        var column = new TableViewTextColumn();

        var result = TableViewRowAutomationPeer.GetColumnHeaderText(column);

        // Header defaults to null for a freshly created column
        Assert.IsNotNull(result);
    }

    // ─── OnCreateAutomationPeer wiring ──────────────────────────────────────

    [UITestMethod]
    public void TableView_OnCreateAutomationPeer_ReturnsTableViewAutomationPeer()
    {
        var tableView = new TableView();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(tableView);

        Assert.IsInstanceOfType(peer, typeof(TableViewAutomationPeer));
    }

    [UITestMethod]
    public void TableViewRow_OnCreateAutomationPeer_ReturnsTableViewRowAutomationPeer()
    {
        var row = new TableViewRow();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(row);

        Assert.IsInstanceOfType(peer, typeof(TableViewRowAutomationPeer));
    }

    [UITestMethod]
    public void TableViewCell_OnCreateAutomationPeer_ReturnsTableViewCellAutomationPeer()
    {
        var cell = new TableViewCell();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(cell);

        Assert.IsInstanceOfType(peer, typeof(TableViewCellAutomationPeer));
    }

    [UITestMethod]
    public void TableViewColumnHeader_OnCreateAutomationPeer_ReturnsTableViewColumnHeaderAutomationPeer()
    {
        var header = new TableViewColumnHeader();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(header);

        Assert.IsInstanceOfType(peer, typeof(TableViewColumnHeaderAutomationPeer));
    }

    [UITestMethod]
    public void TableViewRowHeader_OnCreateAutomationPeer_ReturnsTableViewRowHeaderAutomationPeer()
    {
        var rowHeader = new TableViewRowHeader();

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(rowHeader);

        Assert.IsInstanceOfType(peer, typeof(TableViewRowHeaderAutomationPeer));
    }

    // ─── Localized automation text ──────────────────────────────────────────
    // Each test swaps the resource value for a sentinel, so it fails on a peer that returns an
    // English literal even when the test runs under en-US, where the literal and the resource agree.

    [UITestMethod]
    public void TableViewAutomationPeer_LocalizedControlType_ComesFromResources()
    {
        AssertUsesResource(v => TableViewLocalizedStrings.TableViewControlType = v, () => TableViewLocalizedStrings.TableViewControlType,
            () => new TableViewAutomationPeer(new TableView()).GetLocalizedControlType());
    }

    [UITestMethod]
    public void TableViewColumnHeaderAutomationPeer_LocalizedControlType_ComesFromResources()
    {
        AssertUsesResource(v => TableViewLocalizedStrings.ColumnHeaderControlType = v, () => TableViewLocalizedStrings.ColumnHeaderControlType,
            () => new TableViewColumnHeaderAutomationPeer(new TableViewColumnHeader()).GetLocalizedControlType());
    }

    [UITestMethod]
    public void TableViewRowHeaderAutomationPeer_LocalizedControlType_ComesFromResources()
    {
        AssertUsesResource(v => TableViewLocalizedStrings.RowHeaderControlType = v, () => TableViewLocalizedStrings.RowHeaderControlType,
            () => new TableViewRowHeaderAutomationPeer(new TableViewRowHeader()).GetLocalizedControlType());
    }

    [UITestMethod]
    public void TableViewCellAutomationPeer_LocalizedControlType_ComesFromResources()
    {
        AssertUsesResource(v => TableViewLocalizedStrings.CellControlType = v, () => TableViewLocalizedStrings.CellControlType,
            () => new TableViewCellAutomationPeer(new TableViewCell()).GetLocalizedControlType());
    }

    [UITestMethod]
    public void TableViewRowAutomationPeer_NameWithoutIndex_ComesFromResources()
    {
        // Row.Index returns -1 when not in a list, so the name is the bare "Row" resource.
        AssertUsesResource(v => TableViewLocalizedStrings.Row = v, () => TableViewLocalizedStrings.Row,
            () => new TableViewRowAutomationPeer(new TableViewRow()).GetName());
    }

    [UITestMethod]
    public void FormatRowNumber_UsesTheRowNumberResource()
    {
        var original = TableViewLocalizedStrings.RowNumber;
        try
        {
            TableViewLocalizedStrings.RowNumber = "R#{0}";
            Assert.AreEqual("R#3", TableViewLocalizedStrings.FormatRowNumber(3));
        }
        finally
        {
            TableViewLocalizedStrings.RowNumber = original;
        }

        StringAssert.Contains(TableViewLocalizedStrings.FormatRowNumber(3), "3");
    }

    [UITestMethod]
    public void AutomationResources_AreNotEmpty()
    {
        Assert.IsFalse(string.IsNullOrEmpty(TableViewLocalizedStrings.TableViewControlType));
        Assert.IsFalse(string.IsNullOrEmpty(TableViewLocalizedStrings.ColumnHeaderControlType));
        Assert.IsFalse(string.IsNullOrEmpty(TableViewLocalizedStrings.RowHeaderControlType));
        Assert.IsFalse(string.IsNullOrEmpty(TableViewLocalizedStrings.CellControlType));
        Assert.IsFalse(string.IsNullOrEmpty(TableViewLocalizedStrings.Row));
        StringAssert.Contains(TableViewLocalizedStrings.RowNumber, "{0}");
    }

    // The indexed names ("Row {n}") are only composed for a row that has an index, i.e. one realized in a
    // loaded TableView. Each test swaps RowNumber for a sentinel and reads the peer, so it fails on a peer
    // that composes the name from an English literal instead of FormatRowNumber.

    [UITestMethod]
    public async Task TableViewRowAutomationPeer_IndexedName_ComesFromResources()
    {
        var row = await RealizeSecondRowAsync();

        AssertUsesRowNumberResource(() => new TableViewRowAutomationPeer(row).GetName(), expected: "R#2");
    }

    [UITestMethod]
    public async Task TableViewRowHeaderAutomationPeer_IndexedName_ComesFromResources()
    {
        var row = await RealizeSecondRowAsync();
        var rowHeader = new TableViewRowHeader { TableViewRow = row };

        AssertUsesRowNumberResource(() => new TableViewRowHeaderAutomationPeer(rowHeader).GetName(), expected: "R#2");
    }

    [UITestMethod]
    public async Task TableViewCellAutomationPeer_IndexedName_ComesFromResources()
    {
        var row = await RealizeSecondRowAsync();
        Assert.IsTrue(row.Cells.Count > 0, "the realized row has no cells");
        var cell = row.Cells[0];

        // The cell's name also carries the column header and the value; only its row part is under test.
        AssertUsesRowNumberResource(() => new TableViewCellAutomationPeer(cell).GetName(), expected: "R#2", whole: false);
    }

    private static void AssertUsesRowNumberResource(Func<string> read, string expected, bool whole = true)
    {
        var original = TableViewLocalizedStrings.RowNumber;
        try
        {
            TableViewLocalizedStrings.RowNumber = "R#{0}";
            var name = read();
            if (whole)
            {
                Assert.AreEqual(expected, name);
            }
            else
            {
                StringAssert.Contains(name, expected);
            }
        }
        finally
        {
            TableViewLocalizedStrings.RowNumber = original;
        }
    }

    private static async Task<TableViewRow> RealizeSecondRowAsync()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(IndexedNameItem.Name)) }
        });
        tableView.ItemsSource = new[]
        {
            new IndexedNameItem { Name = "A" },
            new IndexedNameItem { Name = "B" },
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        var row = tableView.ContainerFromIndex(1) as TableViewRow;
        Assert.IsNotNull(row, "the second row was not realized");
        Assert.AreEqual(1, row.Index, "the realized row reports no index, so no indexed name would be composed");
        return row;
    }

    private sealed class IndexedNameItem
    {
        public string Name { get; set; } = string.Empty;
    }

    private static void AssertUsesResource(Action<string> set, Func<string> get, Func<string> read)
    {
        const string sentinel = "localized-sentinel";
        var original = get();
        try
        {
            set(sentinel);
            Assert.AreEqual(sentinel, read());
        }
        finally
        {
            set(original);
        }
    }
}
