using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// A wide table's first layout: VisibleColumns is worked out once and kept until a column changes, a batch
/// of adds works out the frozen columns once (<see cref="TableViewColumnsCollection.DeferColumnUpdates"/>), and the default
/// cell template takes its values from the cell in code instead of seven TemplateBindings per cell.
/// </summary>
[TestClass]
public class CheaperFirstLayoutTests
{
    [UITestMethod]
    public void VisibleColumns_FollowsEveryKindOfChange()
    {
        var collection = new TableViewColumnsCollection(new TableView());
        var a = new TableViewTextColumn { Header = "a" };
        var b = new TableViewTextColumn { Header = "b" };
        var c = new TableViewTextColumn { Header = "c" };
        collection.Add(a);
        collection.Add(b);
        CollectionAssert.AreEqual(new[] { a, b }, collection.VisibleColumns.ToArray(), "two appended");

        collection.Add(c);
        CollectionAssert.AreEqual(new[] { a, b, c }, collection.VisibleColumns.ToArray(), "a third appended to a cached list");

        b.Visibility = Visibility.Collapsed;
        CollectionAssert.AreEqual(new[] { a, c }, collection.VisibleColumns.ToArray(), "hidden");

        b.Visibility = Visibility.Visible;
        a.Order = 5;
        CollectionAssert.AreEqual(new[] { b, c, a }, collection.VisibleColumns.ToArray(), "shown again, and a reordered last");

        var d = new TableViewTextColumn { Header = "d", Order = -1 };
        collection.Add(d);
        CollectionAssert.AreEqual(new[] { d, b, c, a }, collection.VisibleColumns.ToArray(), "appended, but ordered first");

        var hidden = new TableViewTextColumn { Header = "hidden", Visibility = Visibility.Collapsed };
        collection.Add(hidden);
        CollectionAssert.AreEqual(new[] { d, b, c, a }, collection.VisibleColumns.ToArray(), "a hidden column appended");

        collection.Remove(c);
        CollectionAssert.AreEqual(new[] { d, b, a }, collection.VisibleColumns.ToArray(), "removed");

        collection.Insert(0, c);
        CollectionAssert.AreEqual(new[] { d, c, b }, collection.VisibleColumns.Take(3).ToArray(), "inserted at the front among Order 0");

        collection.Clear();
        Assert.AreEqual(0, collection.VisibleColumns.Count, "cleared");
    }

    [UITestMethod]
    public void VisibleColumns_IsOneReadOnlySnapshotUntilAColumnChanges()
    {
        var collection = new TableViewColumnsCollection(new TableView());
        var a = new TableViewTextColumn { Header = "a" };
        collection.Add(a);

        var first = collection.VisibleColumns;
        Assert.AreSame(first, collection.VisibleColumns, "no change: the same snapshot, no new sort");
        Assert.IsTrue(first.IsReadOnly, "a caller cannot edit the shared snapshot");

        collection.Add(new TableViewTextColumn { Header = "b" });
        Assert.AreNotSame(first, collection.VisibleColumns, "a change makes a new snapshot");
        Assert.AreEqual(1, first.Count, "and never edits the one a caller kept");
    }

    [UITestMethod]
    public void DeferColumnUpdates_WorksOutTheFrozenColumnsOnceTheBatchEnds()
    {
        var tableView = new TableView { FrozenColumnCount = 1 };
        var collection = new TableViewColumnsCollection(tableView);
        var first = new TableViewTextColumn { Header = "first" };
        var second = new TableViewTextColumn { Header = "second" };

        using (collection.DeferColumnUpdates())
        {
            using (collection.DeferColumnUpdates())
            {
                collection.Add(first);
            }

            collection.Add(second);
            Assert.IsFalse(first.IsFrozen, "inside the batch the frozen state is not worked out per add");
            Assert.AreEqual(2, collection.VisibleColumns.Count, "but VisibleColumns is always current");
        }

        Assert.IsTrue(first.IsFrozen, "worked out when the outermost batch ends");
        Assert.IsFalse(second.IsFrozen);

        var third = new TableViewTextColumn { Header = "third", Order = -1 };
        collection.Add(third);
        Assert.IsTrue(third.IsFrozen, "outside a batch, every add still works it out at once");
        Assert.IsFalse(first.IsFrozen);
    }

    [UITestMethod]
    public async Task TheDefaultCellTemplate_TakesTheCellsValuesWithoutTemplateBindings()
    {
        var tableView = new TableView { Width = 400, Height = 200, AutoGenerateColumns = false };
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(150),
            Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) },
        });
        tableView.ItemsSource = new[] { new Item { Name = "Alpha" } };
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();

        var cell = tableView.FindDescendants().OfType<TableViewCell>().First();
        var root = cell.FindDescendant<Grid>(g => g.Name == "RootGrid");
        var background = cell.FindDescendant<Border>(b => b.Name == "BackgroundBorder");
        var presenter = cell.FindDescendant<ContentPresenter>(p => p.Name == "Content");
        Assert.IsNotNull(root, "Precondition: the default template names RootGrid");
        Assert.IsNotNull(background);
        Assert.IsNotNull(presenter);
        Assert.AreEqual(cell.VerticalContentAlignment, presenter!.VerticalAlignment, "copied when the template applied");

        var brush = new SolidColorBrush(Colors.Orange);
        var border = new SolidColorBrush(Colors.Blue);
        cell.Background = brush;
        cell.BorderBrush = border;
        cell.BorderThickness = new Thickness(3);
        cell.CornerRadius = new CornerRadius(2);
        cell.Padding = new Thickness(7, 1, 7, 1);
        cell.VerticalContentAlignment = VerticalAlignment.Bottom;
        cell.HorizontalContentAlignment = HorizontalAlignment.Right;

        Assert.AreSame(brush, background!.Background, "Background follows the cell");
        Assert.AreSame(border, root!.BorderBrush, "BorderBrush follows the cell");
        Assert.AreEqual(new Thickness(3), root.BorderThickness);
        Assert.AreEqual(new CornerRadius(2), root.CornerRadius);
        Assert.AreEqual(new Thickness(7, 1, 7, 1), presenter.Margin, "Padding is the presenter's Margin, as the TemplateBinding had it");
        Assert.AreEqual(VerticalAlignment.Bottom, presenter.VerticalAlignment);
        Assert.AreEqual(HorizontalAlignment.Right, presenter.HorizontalAlignment);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
