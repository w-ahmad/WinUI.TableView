using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

namespace WinUI.TableView.Tests;

[TestClass]
public partial class TableViewBoundColumnTests
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

    [UITestMethod]
    public void TableViewBoundColumn_SetClipboardContent_WritesBackThroughBindingPath()
    {
        var column = new TableViewTextColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Name)) }
        };
        var item = new ColumnTestItem { Name = "before" };

        var success = column.SetClipboardContent(item, "after");

        Assert.IsTrue(success);
        Assert.AreEqual("after", item.Name);
    }

    [UITestMethod]
    public void TableViewBoundColumn_SetClipboardContent_UsesBindingConverterConvertBack()
    {
        var column = new TableViewTextColumn
        {
            Binding = new Binding
            {
                Path = new PropertyPath(nameof(ColumnTestItem.Name)),
                Converter = new ConvertBackSuffixConverter()
            }
        };
        var item = new ColumnTestItem();

        var success = column.SetClipboardContent(item, "value");

        Assert.IsTrue(success);
        Assert.AreEqual("value-converted", item.Name);
    }

    private sealed partial class ConvertBackSuffixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return $"{value}-converted";
        }
    }
}
