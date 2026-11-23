using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Diagnostics;
using System.Linq;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ColumnVirtualizationPage : Page
{
    private static readonly int[] Counts = [10, 100, 500, 1_000];
    private static readonly string[] Paths =
    [
        nameof(ExampleModel.FirstName),
        nameof(ExampleModel.LastName),
        nameof(ExampleModel.Email),
        nameof(ExampleModel.Department),
        nameof(ExampleModel.Designation),
        nameof(ExampleModel.Address),
    ];

    public ColumnVirtualizationPage()
    {
        InitializeComponent();

        columnCounts.ItemsSource = Counts.Select(x => x.ToString("N0")).ToArray();
        columnCounts.SelectedIndex = 1;
    }

    private void OnColumnCountChanged(object sender, SelectionChangedEventArgs e)
    {
        if (columnCounts.SelectedIndex < 0)
        {
            return;
        }

        var count = Counts[columnCounts.SelectedIndex];

        // A new table each time: the columns are added before it is shown.
        var tableView = new TableView
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            ItemsSource = ExampleViewModel.ItemsList
        };

        addTimeRun.Text = layoutTimeRun.Text = "-";

        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < count; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Field {i}",
                Width = new GridLength(110),
                Binding = new Binding { Path = new PropertyPath(Paths[i % Paths.Length]) }
            });
        }

        addTimeRun.Text = $"{stopwatch.ElapsedMilliseconds:N0} ms ({count:N0} columns)";
        tableHost.Child = tableView;
        tableView.LayoutUpdated += OnFirstLayout;

        void OnFirstLayout(object? sender, object args)
        {
            tableView.LayoutUpdated -= OnFirstLayout;
            layoutTimeRun.Text = $"{stopwatch.ElapsedMilliseconds:N0} ms";
        }
    }
}
