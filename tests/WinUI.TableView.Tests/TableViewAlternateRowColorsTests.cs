using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers <see cref="TableView.AlternateRowBackground"/> / <see cref="TableView.AlternateRowForeground"/>
/// staying in step with each realized row's index while rows are recycled, removed, or inserted.
/// </summary>
[TestClass]
public class TableViewAlternateRowColorsTests
{
    private const int ItemCount = 300;

    private readonly SolidColorBrush _alternateBackground = new(Colors.Red);
    private readonly SolidColorBrush _alternateForeground = new(Colors.Blue);

    [UITestMethod]
    public async Task AlternateColors_AreApplied_ToInitiallyRealizedRows()
    {
        var (tableView, _) = await CreateTableViewAsync();

        await SettleAsync(tableView);

        AssertAlternation(tableView);
    }

    [UITestMethod]
    public async Task AlternateColors_StayCorrect_WhenScrollingBackAndForth()
    {
        var (tableView, _) = await CreateTableViewAsync();

        foreach (var index in new[] { 250, 5, 181, 20, 299, 0, 120, 61, 200, 3 })
        {
            await tableView.ScrollRowIntoView(index);
            await SettleAsync(tableView);

            AssertAlternation(tableView);
        }
    }

    [UITestMethod]
    public async Task AlternateColors_StayCorrect_WhenTheCollectionIsReset()
    {
        var (tableView, items) = await CreateTableViewAsync();

        await tableView.ScrollRowIntoView(100);
        await SettleAsync(tableView);

        items.Clear();
        for (var i = 0; i < ItemCount - 1; i++)
        {
            items.Add(new AlternateRowTestItem { Id = i });
        }

        await SettleAsync(tableView);
        AssertAlternation(tableView);

        await tableView.ScrollRowIntoView(150);
        await SettleAsync(tableView);
        AssertAlternation(tableView);
    }

    [UITestMethod]
    public async Task AlternateColors_StayCorrect_WhenAnItemAboveIsRemovedOrInserted()
    {
        var (tableView, items) = await CreateTableViewAsync();
        await SettleAsync(tableView);

        items.RemoveAt(0);
        await SettleAsync(tableView);
        AssertAlternation(tableView);

        items.RemoveAt(5);
        await SettleAsync(tableView);
        AssertAlternation(tableView);

        items.Insert(2, new AlternateRowTestItem { Id = -1 });
        await SettleAsync(tableView);
        AssertAlternation(tableView);
    }

    [UITestMethod]
    public async Task AlternateColors_AreRemoved_WhenAlternateBrushesAreCleared()
    {
        var (tableView, _) = await CreateTableViewAsync();
        await SettleAsync(tableView);

        tableView.AlternateRowBackground = null!;
        tableView.AlternateRowForeground = null!;
        await SettleAsync(tableView);

        foreach (var row in tableView.Rows.Where(r => r.RowPresenter is not null))
        {
            Assert.AreNotSame(_alternateBackground, row.RowPresenter!.Background);
            Assert.AreNotSame(_alternateForeground, row.RowPresenter.Foreground);
        }
    }

    private void AssertAlternation(TableView tableView)
    {
        var rows = tableView.Rows.Where(r => r.RowPresenter is not null && r.Index >= 0).ToList();

        Assert.IsTrue(rows.Count > 2, "Expected several realized rows.");

        foreach (var row in rows)
        {
            var isAlternate = row.Index % 2 == 1;

            Assert.AreEqual(isAlternate, ReferenceEquals(_alternateBackground, row.RowPresenter!.Background), $"Background of row {row.Index}.");
            Assert.AreEqual(isAlternate, ReferenceEquals(_alternateForeground, row.RowPresenter.Foreground), $"Foreground of row {row.Index}.");
        }
    }

    // Rows are realized and prepared over several dispatcher turns (including idle-time cache
    // realization), so wait until the set of realized rows stops changing and the queue is drained.
    private static async Task SettleAsync(TableView tableView)
    {
        var previous = 0;
        var stableSamples = 0;

        for (var i = 0; i < 100 && stableSamples < 3; i++)
        {
            await Task.Delay(50);
            await DrainDispatcherQueueAsync();

            var snapshot = tableView.Rows.Aggregate(tableView.Rows.Count, (hash, row) => HashCode.Combine(hash, row.Index));

            stableSamples = snapshot == previous ? stableSamples + 1 : 0;
            previous = snapshot;
        }

        Assert.IsTrue(stableSamples >= 3, "The realized rows never stabilized.");
    }

    private static Task DrainDispatcherQueueAsync()
    {
        var completion = new TaskCompletionSource<object?>();

        DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Low, () => completion.SetResult(null));

        return completion.Task;
    }

    private async Task<(TableView TableView, ObservableCollection<AlternateRowTestItem> Items)> CreateTableViewAsync()
    {
        var items = new ObservableCollection<AlternateRowTestItem>(
            Enumerable.Range(0, ItemCount).Select(i => new AlternateRowTestItem { Id = i }));

        var tableView = new TableView
        {
            AlternateRowBackground = _alternateBackground,
            AlternateRowForeground = _alternateForeground,
            Height = 400,
            ItemsSource = items
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Id",
            Binding = new Binding { Path = new PropertyPath(nameof(AlternateRowTestItem.Id)) }
        });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return (tableView, items);
    }

    private sealed class AlternateRowTestItem
    {
        public int Id { get; set; }
    }
}
