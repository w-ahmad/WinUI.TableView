using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class HighlightsPage : Page
{
    private bool _initialHighlightsApplied;

    public HighlightsPage()
    {
        InitializeComponent();

        tableView.Columns.CollectionChanged += delegate { DispatcherQueue.TryEnqueue(RefreshColumnList); };
    }

    private void RefreshColumnList()
    {
        var selectedIndex = columnComboBox.SelectedIndex;

        columnComboBox.Items.Clear();

        for (var i = 0; i < tableView.Columns.Count; i++)
        {
            columnComboBox.Items.Add(tableView.Columns[i].Header?.ToString() ?? $"Column {i}");
        }

        if (columnComboBox.Items.Count > 0)
        {
            columnComboBox.SelectedIndex = selectedIndex >= 0 && selectedIndex < columnComboBox.Items.Count ? selectedIndex : 0;
        }

        ApplyInitialHighlights();
    }

    private void ApplyInitialHighlights()
    {
        if (_initialHighlightsApplied || tableView.Columns.Count < 2) return;

        _initialHighlightsApplied = true;

        tableView.HighlightRow(2,
            new SolidColorBrush(rowBackgroundPicker.SelectedColor),
            new SolidColorBrush(rowForegroundPicker.SelectedColor));

        tableView.HighlightColumn(1,
            new SolidColorBrush(columnBackgroundPicker.SelectedColor),
            new SolidColorBrush(columnForegroundPicker.SelectedColor));
    }

    private void OnHighlightRow(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(rowIndexBox.Value)) return;

        tableView.HighlightRow((int)rowIndexBox.Value,
            new SolidColorBrush(rowBackgroundPicker.SelectedColor),
            new SolidColorBrush(rowForegroundPicker.SelectedColor));
    }

    private void OnClearRowHighlight(object sender, RoutedEventArgs e)
    {
        if (double.IsNaN(rowIndexBox.Value)) return;

        tableView.ClearRowHighlight((int)rowIndexBox.Value);
    }

    private void OnHighlightColumn(object sender, RoutedEventArgs e)
    {
        if (columnComboBox.SelectedIndex < 0) return;

        tableView.HighlightColumn(columnComboBox.SelectedIndex,
            new SolidColorBrush(columnBackgroundPicker.SelectedColor),
            new SolidColorBrush(columnForegroundPicker.SelectedColor));
    }

    private void OnClearColumnHighlight(object sender, RoutedEventArgs e)
    {
        if (columnComboBox.SelectedIndex < 0) return;

        tableView.ClearColumnHighlight(columnComboBox.SelectedIndex);
    }

    private void OnClearAllHighlights(object sender, RoutedEventArgs e)
    {
        tableView.ClearRowHighlights();
        tableView.ClearColumnHighlights();
    }

    private void OnMergeToggled(object sender, RoutedEventArgs e)
    {
        tableView.MergeOverlappingHighlights = mergeToggle.IsOn;
        priorityComboBox.IsEnabled = !mergeToggle.IsOn;
    }

    private void OnPriorityChanged(object sender, SelectionChangedEventArgs e)
    {
        tableView.OverlappingHighlightPriority = priorityComboBox.SelectedIndex == 1
            ? TableViewHighlightPriority.Column
            : TableViewHighlightPriority.Row;
    }
}
