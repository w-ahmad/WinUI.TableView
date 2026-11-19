#if !WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// A cell whose content is left out of layout while it is out of view still follows the table's row heights.
/// </summary>
[TestClass]
public class TableViewDeferredCellTests
{
    [UITestMethod]
    public async Task DeferredCell_KeepsFollowingTheRowMinHeight()
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
            ItemsSource = new ObservableCollection<HostItem>(Enumerable.Range(0, 20).Select(i => new HostItem { Id = i, Name = $"Item {i}" }))
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(150),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        try
        {
            var row = (TableViewRow)tableView.ContainerFromIndex(0)!;
            var cell = row.Cells[0];
            var height = row.ActualHeight;
            var line = height - tableView.RowMinHeight; // the row is a grid line taller than its cells

            cell.UpdateRenderCulling(isInView: false, isNearView: false);
            await WaitForLayoutAsync(tableView);
            Assert.AreEqual(height, row.ActualHeight, 0.5, "a row keeps its height while a cell's content is out of layout");

            tableView.RowMinHeight = height + 24 - line;
            await WaitForLayoutAsync(tableView);
            Assert.AreEqual(height + 24, row.ActualHeight, 0.5, "a deferred cell follows a taller row height");

            tableView.RowMinHeight = 20;
            await WaitForLayoutAsync(tableView);
            Assert.IsTrue(row.ActualHeight < height + 24, $"the row stayed {row.ActualHeight} high after the row height was lowered");

            cell.UpdateRenderCulling(isInView: true, isNearView: true);
            tableView.RowMinHeight = height + 10 - line;
            await WaitForLayoutAsync(tableView);
            Assert.AreEqual(height + 10, row.ActualHeight, 0.5, "a cell back in view follows the row height");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }
}
#endif
