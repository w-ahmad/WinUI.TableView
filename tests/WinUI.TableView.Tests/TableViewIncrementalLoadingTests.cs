using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Threading.Tasks;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// More items are asked for while the rows shown do not fill the viewport, and not beyond that.
/// </summary>
[TestClass]
public class TableViewIncrementalLoadingTests
{
    private TableView? _tableView;

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_tableView is not null)
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(_tableView);
        }
    }

    [UITestMethod]
    public async Task SourceWithAFewItems_ThatFitTheViewport_IsAskedForMore()
    {
        var source = new CountingIncrementalSource();

        for (var i = 0; i < 4; i++)
        {
            source.Add(new HostItem { Id = i, Name = $"Item {i}" });
        }

        await CreateTableViewAsync(source);
        await WaitForLoadingToSettleAsync(source);

        Assert.IsTrue(source.LoadCount > 0, "Four rows leave the viewport unfilled, so more must be asked for");
        Assert.IsTrue(source.Count < CountingIncrementalSource.MaxItems, $"The whole source was pulled in: {source.Count}");
    }

    [UITestMethod]
    public async Task CollapsedGroups_ThatFillTheViewport_StopTheLoading()
    {
        var source = new CountingIncrementalSource();
        var tableView = await CreateTableViewAsync(source, table =>
        {
            table.DefaultGroupState = TableViewGroupState.Collapsed;
            table.GroupDescriptions.Add(new GroupDescription(nameof(HostItem.Name)));
        });

        await WaitForLoadingToSettleAsync(source);

        Assert.IsTrue(source.LoadCount > 0, "The first page must be asked for");
        Assert.AreEqual(0, tableView.Items.Count, "Precondition: every group is collapsed");
        Assert.IsTrue(source.Count < CountingIncrementalSource.MaxItems,
            $"Group headers fill the viewport, yet the whole source was pulled in: {source.Count}");
    }

    private static async Task WaitForLoadingToSettleAsync(CountingIncrementalSource source)
    {
        var quiet = 0;

        for (var i = 0; i < 400 && quiet < 15; i++)
        {
            var count = source.LoadCount;
            await Task.Delay(40);
            quiet = source.LoadCount == count ? quiet + 1 : 0;
        }
    }

    private async Task<TableView> CreateTableViewAsync(CountingIncrementalSource source, System.Action<TableView>? configure = null)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 400,
            RowHeight = 40,
            AutoGenerateColumns = false,
            ItemsSource = source
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        configure?.Invoke(tableView);

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }
}
