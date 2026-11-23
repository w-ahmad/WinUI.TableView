#if !WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Column headers scrolled out of view sideways are left out of rendering, like the cells under them.
/// </summary>
[TestClass]
public class TableViewHeaderCullingTests
{
    [UITestMethod]
    public async Task HeadersOutOfView_AreHidden_AndShownWhenScrolledIntoView()
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = 300,
            AutoGenerateColumns = false,
            FrozenColumnCount = 1,
            ItemsSource = new ObservableCollection<HostItem>(Enumerable.Range(0, 20).Select(i => new HostItem { Id = i, Name = $"Item {i}" }))
        };

        for (var i = 0; i < 20; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(150),
                Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
            });
        }

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        try
        {
            if (ElementVisuals.AreKnownNotComposed)
            {
                Assert.Inconclusive("This renderer does not compose element visuals.");
            }

            var headers = tableView.FindDescendants().OfType<TableViewColumnHeader>().ToDictionary(x => x.Column!);
            bool IsShown(int column) => ElementVisuals.Get(headers[(TableViewColumn)tableView.Columns[column]])!.Opacity > 0f;

            Assert.IsTrue(IsShown(0), "the frozen header is shown");
            Assert.IsTrue(IsShown(1), "the first scrollable header is in view");
            Assert.IsFalse(IsShown(19), "the last header is far out of view");

            tableView.ScrollTo(150d * 16, 0d);
            await WaitForLayoutAsync(tableView);
            await Task.Delay(100);
            await WaitForLayoutAsync(tableView);

            Assert.IsTrue(IsShown(0), "the frozen header stays shown");
            Assert.IsFalse(IsShown(1), "the first scrollable header has scrolled out of view");
            Assert.IsTrue(IsShown(19), "the last header has scrolled into view");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }
}
#endif
