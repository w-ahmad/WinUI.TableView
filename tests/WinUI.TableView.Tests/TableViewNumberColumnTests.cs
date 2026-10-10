using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewNumberColumnTests
{
    [UITestMethod]
    public void TableViewNumberColumn_GeneratesNumericElements()
    {
        var column = new TableViewNumberColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Amount)) }
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), null);
        var editingElement = (NumberBox)column.GenerateEditingElement(new TableViewCell(), null);
        editingElement.Value = 42.5;

        Assert.AreEqual(TextAlignment.Right, element.TextAlignment);
        Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);
        Assert.AreEqual(42.5, column.PrepareCellForEdit(new TableViewCell { Content = editingElement }, new RoutedEventArgs()));
    }

    [UITestMethod]
    public async Task TableViewNumberColumn_GeneratesBoundEditingElement()
    {
        var item = new ColumnTestItem { Amount = 11.25 };
        var column = new TableViewNumberColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Amount)) }
        };

        var editingElement = (NumberBox)column.GenerateEditingElement(new TableViewCell(), item);

        try
        {
            editingElement.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(editingElement);
            Assert.AreEqual(11.25, editingElement.Value);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(editingElement);
        }
    }
}
