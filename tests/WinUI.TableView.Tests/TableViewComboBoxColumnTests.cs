using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewComboBoxColumnTests
{
    [UITestMethod]
    public async Task TableViewComboBoxColumn_GeneratesDisplayAndEditingElements()
    {
        var options = new List<ColumnTestOptionItem>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };

        var item = new ColumnTestItem
        {
            SelectedOption = options[1],
            SelectedOptionId = options[1].Id,
            SelectedOptionText = options[1].Name
        };

        var column = new TableViewComboBoxColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.SelectedOption)) },
            DisplayMemberPath = nameof(ColumnTestOptionItem.Name),
            SelectedValuePath = nameof(ColumnTestOptionItem.Id),
            ItemsSource = options,
            IsEditable = true,
            TextBinding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.SelectedOptionText)), Mode = BindingMode.OneWay },
            SelectedValueBinding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.SelectedOptionId)), Mode = BindingMode.OneWay }
        };

        Assert.AreEqual(BindingMode.TwoWay, column.TextBinding!.Mode);
        Assert.AreEqual(BindingMode.TwoWay, column.SelectedValueBinding!.Mode);

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), item);
        var editingElement = (ComboBox)column.GenerateEditingElement(new TableViewCell(), item);

        Assert.IsNotNull(element.GetBindingExpression(FrameworkElement.DataContextProperty));
        Assert.IsNotNull(element.GetBindingExpression(TextBlock.TextProperty));
        Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);

        try
        {
            editingElement.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(editingElement);
            Assert.AreSame(options, editingElement.ItemsSource);
            Assert.AreEqual(nameof(ColumnTestOptionItem.Name), editingElement.DisplayMemberPath);
            Assert.AreEqual(nameof(ColumnTestOptionItem.Id), editingElement.SelectedValuePath);
            Assert.IsTrue(editingElement.IsEditable);
            Assert.AreSame(options[1], editingElement.SelectedItem);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(editingElement);
        }

        var cell = new TableViewCell { Content = editingElement };
        Assert.AreSame(options[1], column.PrepareCellForEdit(cell, new RoutedEventArgs()));
    }

    [UITestMethod]
    public async Task TableViewComboBoxColumn_GeneratesDisplayElementWithoutDisplayMemberPath()
    {
        var item = new ColumnTestItem { SelectedOptionText = "Direct text" };
        var column = new TableViewComboBoxColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.SelectedOptionText)) }
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), item);

        try
        {
            element.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(element);
            Assert.AreEqual("Direct text", element.Text);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(element);
        }
    }

    [UITestMethod]
    public void TableViewComboBoxColumn_GenerateEditingElement_DoesNotBindOptionalProperties_WhenUnset()
    {
        var column = new TableViewComboBoxColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.SelectedOptionText)) }
        };

        var editingElement = (ComboBox)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());

        Assert.IsNull(editingElement.GetBindingExpression(ComboBox.TextProperty));
        Assert.IsNull(editingElement.GetBindingExpression(Selector.SelectedValueProperty));
        Assert.AreEqual(HorizontalAlignment.Stretch, editingElement.HorizontalAlignment);
    }
}
