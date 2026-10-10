using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewTextColumnTests
{
    [UITestMethod]
    public async Task TableViewTextColumn_GeneratesBoundTextElements()
    {
        var item = new ColumnTestItem { Name = "Alice" };
        var column = new TableViewTextColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Name)) }
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), item);
        var editingElement = (TextBox)column.GenerateEditingElement(new TableViewCell(), item);

        try
        {
            element.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(element);
            Assert.AreEqual("Alice", element.Text);
            Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(element);
        }

        try
        {
            editingElement.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(editingElement);
            Assert.AreEqual("Alice", editingElement.Text);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(editingElement);
        }

        var cell = new TableViewCell { Content = editingElement };
        Assert.AreEqual("Alice", column.PrepareCellForEdit(cell, new RoutedEventArgs()));
    }

    [UITestMethod]
    public void TableViewTextColumn_PrepareCellForEdit_ReturnsNull_WhenContentIsNotTextBox()
    {
        var column = new TableViewTextColumn();
        var cell = new TableViewCell { Content = new TextBlock { Text = "not editing" } };

        Assert.IsNull(column.PrepareCellForEdit(cell, new RoutedEventArgs()));
    }
}
