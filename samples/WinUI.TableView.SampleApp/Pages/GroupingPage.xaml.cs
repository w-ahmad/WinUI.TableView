using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GroupingPage : Page
{
    private const string None = "(none)";

    private static readonly string[] GroupableProperties =
    [
        None,
        nameof(ExampleModel.Department),
        nameof(ExampleModel.Gender),
        nameof(ExampleModel.IsActive)
    ];

    private bool _isInitialized;

    public GroupingPage()
    {
        InitializeComponent();

        firstLevel.ItemsSource = GroupableProperties;
        secondLevel.ItemsSource = GroupableProperties;
        firstLevel.SelectedItem = nameof(ExampleModel.Department);
        secondLevel.SelectedItem = None;

        _isInitialized = true;

        Loaded += (_, _) => ApplyGrouping();
    }

    private void OnGroupingChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            ApplyGrouping();
        }
    }

    private void OnExpandAllClick(object sender, RoutedEventArgs e)
    {
        tableView.ExpandAllGroups();
        UpdateStats();
    }

    private void OnCollapseAllClick(object sender, RoutedEventArgs e)
    {
        tableView.CollapseAllGroups();
        UpdateStats();
    }

    private void ApplyGrouping()
    {
        var direction = descending.IsOn ? SortDirection.Descending : SortDirection.Ascending;

        tableView.GroupDescriptions.Clear();

        foreach (var comboBox in new[] { firstLevel, secondLevel })
        {
            if (comboBox.SelectedItem is string property && property != None)
            {
                tableView.GroupDescriptions.Add(new GroupDescription(property, direction));
            }
        }

        UpdateStats();
    }

    private void UpdateStats()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            groupCountRun.Text = tableView.Groups.Count.ToString();
            itemCountRun.Text = tableView.Items.Count.ToString();
            realizedRowsRun.Text = CountRealizedRows(tableView).ToString();
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
}
