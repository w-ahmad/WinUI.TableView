using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class VirtualizationPage : Page
{
    private static readonly int[] Counts = [1_000, 10_000, 100_000, 1_000_000];
    private static readonly string[] Categories = ["Alpha", "Beta", "Gamma", "Delta", "Epsilon"];

    public VirtualizationPage()
    {
        InitializeComponent();

        itemCounts.ItemsSource = Counts.Select(x => x.ToString("N0")).ToArray();
        itemCounts.SelectedIndex = 1;
    }

    private void OnItemCountChanged(object sender, SelectionChangedEventArgs e)
    {
        if (itemCounts.SelectedIndex < 0)
        {
            return;
        }

        var count = Counts[itemCounts.SelectedIndex];
        var stopwatch = Stopwatch.StartNew();

        tableView.ItemsSource = CreateItems(count);

        stopwatch.Stop();
        Report($"built and bound {count:N0} items in {stopwatch.ElapsedMilliseconds} ms");

        jumpTarget.Maximum = count - 1;
    }

    private void OnSelectAllClick(object sender, RoutedEventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();

        tableView.SelectRange(new ItemIndexRange(0, (uint)tableView.Items.Count));

        stopwatch.Stop();
        Report($"selected {tableView.Items.Count:N0} rows in {stopwatch.ElapsedMilliseconds} ms");
    }

    private void OnClearSelectionClick(object sender, RoutedEventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();

        tableView.DeselectAll();

        stopwatch.Stop();
        Report($"cleared the selection in {stopwatch.ElapsedMilliseconds} ms");
    }

    private async void OnJumpClick(object sender, RoutedEventArgs e)
    {
        var target = (int)jumpTarget.Value;
        var stopwatch = Stopwatch.StartNew();

        await tableView.ScrollRowIntoView(target);

        stopwatch.Stop();
        Report($"scrolled to row {target:N0} in {stopwatch.ElapsedMilliseconds} ms");
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCounters();
    }

    private void Report(string message)
    {
        timingRun.Text = message;
        UpdateCounters();
    }

    private void UpdateCounters()
    {
        // Deferred so the counters reflect the state after the layout pass the operation triggered.
        DispatcherQueue.TryEnqueue(() =>
        {
            itemCountRun.Text = tableView.Items.Count.ToString("N0");
            realizedRowsRun.Text = CountRealizedRows(tableView).ToString();
            selectedCountRun.Text = tableView.SelectedItems.Count.ToString("N0");
            selectedRangesRun.Text = tableView.SelectedRanges.Count.ToString();
        });
    }


    /// <summary>
    /// Counts the realized row elements by walking the visual tree, which is what the counters are about: how
    /// many row elements actually exist for the current item count.
    /// </summary>
    private static int CountRealizedRows(DependencyObject root)
    {
        var count = 0;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);

            if (child is TableViewRow { IsLoaded: true })
            {
                count++;
            }

            count += CountRealizedRows(child);
        }

        return count;
    }

    private static List<VirtualizationItem> CreateItems(int count)
    {
        var items = new List<VirtualizationItem>(count);

        for (var i = 0; i < count; i++)
        {
            items.Add(new VirtualizationItem
            {
                Id = i,
                Name = $"Row {i:N0}",
                Category = Categories[i % Categories.Length],
                City = DataFaker.City(),
                Amount = (i % 997) * 1.5m
            });
        }

        return items;
    }
}

public sealed class VirtualizationItem
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Category { get; set; }

    public string? City { get; set; }

    public decimal Amount { get; set; }
}
