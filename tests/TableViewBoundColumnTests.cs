using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewBoundColumnTests
{
    [UITestMethod]
    public void TableViewBoundColumn_BindingAndOperationBinding_UseExpectedDefaults()
    {
        var binding = new Binding
        {
            Path = new PropertyPath(nameof(ColumnTestItem.Name)),
            Mode = BindingMode.OneWay,
            UpdateSourceTrigger = UpdateSourceTrigger.Default
        };

        var column = new TableViewTextColumn
        {
            Binding = binding
        };

        Assert.AreEqual(BindingMode.TwoWay, column.Binding.Mode);
        Assert.AreEqual(UpdateSourceTrigger.Explicit, column.Binding.UpdateSourceTrigger);
        Assert.AreSame(column.Binding, column.OperationContentBinding);
    }

    [UITestMethod]
    public void TableViewBoundColumn_ClipboardContentBinding_FallsBackToBinding()
    {
        var column = new TableViewTextColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Name)) }
        };

        Assert.AreSame(column.Binding, column.ClipboardContentBinding);
        Assert.AreEqual(nameof(ColumnTestItem.Name), column.ClipboardContentBinding!.Path!.Path);
    }
}
