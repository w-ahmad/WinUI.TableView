using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewHyperlinkColumnTests
{
    [UITestMethod]
    public async Task TableViewHyperlinkColumn_GeneratesBoundHyperlink()
    {
        var item = new ColumnTestItem
        {
            Link = new Uri("https://example.com/docs"),
            Name = "Docs"
        };

        var column = new TableViewHyperlinkColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Link)) },
            ContentBinding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Name)) }
        };

        var element = (HyperlinkButton)column.GenerateElement(new TableViewCell(), item);

        try
        {
            element.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(element);
            Assert.AreEqual(item.Link, element.NavigateUri);
            Assert.AreEqual("Docs", element.Content);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(element);
        }
    }

    [UITestMethod]
    public async Task TableViewHyperlinkColumn_FallsBackToBindingForContent()
    {
        var item = new ColumnTestItem
        {
            Link = new Uri("https://example.com/fallback")
        };

        var column = new TableViewHyperlinkColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.Link)) }
        };

        var element = (HyperlinkButton)column.GenerateElement(new TableViewCell(), item);

        try
        {
            element.DataContext = item;
            await UnitTestApp.Current.MainWindow.LoadTestContentAsync(element);
            Assert.AreEqual(item.Link, element.NavigateUri);
            Assert.AreEqual(item.Link, element.Content);
            Assert.AreEqual(HorizontalAlignment.Stretch, element.HorizontalAlignment);
            Assert.AreEqual(HorizontalAlignment.Left, element.HorizontalContentAlignment);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(element);
        }
    }
}
