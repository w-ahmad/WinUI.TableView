using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewBeginEditTests
{
    [UITestMethod]
    public async Task BeginEditAsync_MakesTheCellCurrentAndBeginsEditingIt()
    {
        var tableView = await CreateTableViewAsync();
        var slot = new TableViewCellSlot(1, 1);

        var began = await tableView.BeginEditAsync(slot);

        Assert.IsTrue(began);
        Assert.AreEqual(slot, tableView.CurrentCellSlot);
        Assert.IsTrue(tableView.IsEditing);
        Assert.IsInstanceOfType<TextBox>(tableView.GetCellFromSlot(slot)!.Content);
    }

    [UITestMethod]
    public async Task BeginEditAsync_LeavesFocusInTheEditingElement()
    {
        var tableView = await CreateTableViewAsync();
        var slot = new TableViewCellSlot(1, 1);

        await tableView.BeginEditAsync(slot);
        await Task.Delay(200); // Let the editing element load and take focus.

        var editor = (TextBox)tableView.GetCellFromSlot(slot)!.Content;
        var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as DependencyObject;

        Assert.IsTrue(IsSelfOrDescendant(focused, editor), $"Focus is on {focused?.GetType().Name ?? "nothing"}, not the editor.");
    }

    [UITestMethod]
    public async Task BeginEditAsync_OnTheCurrentCell_BeginsEditingIt()
    {
        var tableView = await CreateTableViewAsync();
        var slot = new TableViewCellSlot(0, 1);
        tableView.CurrentCellSlot = slot;
        await Task.Delay(100);

        var began = await tableView.BeginEditAsync(slot);

        Assert.IsTrue(began);
        Assert.IsTrue(tableView.IsEditing);
        Assert.AreEqual(slot, tableView.CurrentCellSlot);
    }

    [UITestMethod]
    public async Task BeginEditAsync_CommitsTheEditInProgressOnAnotherCell()
    {
        var tableView = await CreateTableViewAsync();
        var ended = new System.Collections.Generic.List<TableViewEditAction>();
        tableView.CellEditEnded += (_, e) => ended.Add(e.EditAction);

        Assert.IsTrue(await tableView.BeginEditAsync(new TableViewCellSlot(0, 0)));
        Assert.IsTrue(await tableView.BeginEditAsync(new TableViewCellSlot(1, 1)));

        CollectionAssert.AreEqual(new[] { TableViewEditAction.Commit }, ended);
        Assert.AreEqual(new TableViewCellSlot(1, 1), tableView.CurrentCellSlot);
        Assert.IsTrue(tableView.IsEditing);
    }

    [UITestMethod]
    public async Task BeginEditAsync_OnTheCellAlreadyBeingEdited_ReturnsFalseAndKeepsEditing()
    {
        var tableView = await CreateTableViewAsync();
        var slot = new TableViewCellSlot(0, 0);
        Assert.IsTrue(await tableView.BeginEditAsync(slot));

        Assert.IsFalse(await tableView.BeginEditAsync(slot));
        Assert.IsTrue(tableView.IsEditing);
    }

    [UITestMethod]
    public async Task BeginEditAsync_HonoursACancelledBeginningEdit()
    {
        var tableView = await CreateTableViewAsync();
        tableView.BeginningEdit += (_, e) => e.Cancel = true;

        var began = await tableView.BeginEditAsync(new TableViewCellSlot(1, 0));

        Assert.IsFalse(began);
        Assert.IsFalse(tableView.IsEditing);
    }

    [UITestMethod]
    public async Task BeginEditAsync_DoesNotEditAReadOnlyColumn()
    {
        var tableView = await CreateTableViewAsync();
        tableView.Columns[1].IsReadOnly = true;

        var began = await tableView.BeginEditAsync(new TableViewCellSlot(0, 1));

        Assert.IsFalse(began);
        Assert.IsFalse(tableView.IsEditing);
    }

    [UITestMethod]
    public async Task BeginEditAsync_DoesNotEditAReadOnlyTable()
    {
        var tableView = await CreateTableViewAsync();
        tableView.IsReadOnly = true;

        Assert.IsFalse(await tableView.BeginEditAsync(new TableViewCellSlot(0, 0)));
        Assert.IsFalse(tableView.IsEditing);
    }

    [UITestMethod]
    public async Task BeginEditAsync_RejectsASlotOutsideTheTable()
    {
        var tableView = await CreateTableViewAsync();

        Assert.IsFalse(await tableView.BeginEditAsync(new TableViewCellSlot(5, 0)));
        Assert.IsFalse(tableView.IsEditing);
    }

    private static bool IsSelfOrDescendant(DependencyObject? element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor)) return true;
        }

        return false;
    }

    private static async Task<TableView> CreateTableViewAsync()
    {
        var tableView = new TableView
        {
            SelectionMode = ListViewSelectionMode.Extended,
            SelectionUnit = TableViewSelectionUnit.Cell,
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(EditItem.Name)), Mode = BindingMode.TwoWay }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Note",
            Binding = new Binding { Path = new PropertyPath(nameof(EditItem.Note)), Mode = BindingMode.TwoWay }
        });
        tableView.ItemsSource = new[]
        {
            new EditItem { Name = "A", Note = "first" },
            new EditItem { Name = "B", Note = "second" }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    public sealed class EditItem
    {
        public string Name { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}
