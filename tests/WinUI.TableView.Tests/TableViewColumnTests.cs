using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewColumnTests
{
    [UITestMethod]
    public void TableViewColumn_Constructor_InitializesConditionalCellStyles()
    {
        var column = new ColumnTestColumn();

        Assert.IsNotNull(column.ConditionalCellStyles);
        Assert.AreEqual(0, column.ConditionalCellStyles.Count);
    }

    [UITestMethod]
    public void TableViewColumn_ContentAccessors_UseConfiguredBindings()
    {
        var column = new ColumnTestColumn
        {
            OperationContentBinding = new Binding
            {
                Path = new PropertyPath($"{nameof(ColumnTestItem.Nested)}.{nameof(ColumnTestNestedItem.Value)}"),
                Converter = new PrefixConverter(),
                ConverterParameter = "display:"
            },
            ClipboardContentBinding = new Binding
            {
                Path = new PropertyPath(nameof(ColumnTestItem.ClipboardText)),
                Converter = new PrefixConverter(),
                ConverterParameter = "clipboard:"
            }
        };

        var item = new ColumnTestItem
        {
            Nested = new ColumnTestNestedItem { Value = "row value" },
            ClipboardText = "copy value"
        };

        Assert.AreEqual("display:row value", column.GetCellContent(item));
        Assert.AreEqual("clipboard:copy value", column.GetClipboardContent(item));
        Assert.IsNull(column.GetCellContent(null));
        Assert.IsNull(column.GetClipboardContent(null));
    }

    [UITestMethod]
    public void TableViewColumn_ClipboardContentBinding_FallsBackToOperationContentBinding()
    {
        var operationBinding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Name)) };
        var column = new ColumnTestColumn
        {
            OperationContentBinding = operationBinding
        };

        var item = new ColumnTestItem { Name = "fallback" };

        Assert.AreSame(operationBinding, column.ClipboardContentBinding);
        Assert.AreEqual("fallback", column.GetClipboardContent(item));
    }

    [UITestMethod]
    public void TableViewColumn_GetCellContent_ReturnsInput_WhenNoBindingIsConfigured()
    {
        var column = new ColumnTestColumn();
        var item = new ColumnTestItem { Name = "raw" };

        Assert.AreSame(item, column.GetCellContent(item));
        Assert.AreSame(item, column.GetClipboardContent(item));
    }

    [UITestMethod]
    public void TableViewColumn_EnsureHeaderStyle_UsesTableViewStyleAndColumnOverride()
    {
        var tableViewStyle = new Style { TargetType = typeof(TableViewColumnHeader) };
        var columnStyle = new Style { TargetType = typeof(TableViewColumnHeader) };
        var tableView = new TableView { ColumnHeaderStyle = tableViewStyle };
        var headerControl = new TableViewColumnHeader();
        var column = new ColumnTestColumn();

        column.SetOwningTableView(tableView);
        column.HeaderControl = headerControl;
        column.EnsureHeaderStyle();

        Assert.AreSame(tableViewStyle, headerControl.Style);

        column.HeaderStyle = columnStyle;

        Assert.AreSame(columnStyle, headerControl.Style);
    }

    [UITestMethod]
    public void TableViewColumn_DefaultVirtualMembers_AreNoOps()
    {
        var column = new ColumnTestColumn();
        var cell = new TableViewCell();

        column.RefreshElement(cell, null);
        column.UpdateElementState(cell, null);
        column.EndCellEditing(cell, null, TableViewEditAction.Cancel, null);

        Assert.IsNull(column.PrepareCellForEdit(cell, new RoutedEventArgs()));
    }
}
