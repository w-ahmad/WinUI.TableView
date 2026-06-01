using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GroupingPage : Page, INotifyPropertyChanged
{
    private string? _groupByPath = "Department";
    private SortDirection _groupSortDirection = SortDirection.Ascending;

    public GroupingPage()
    {
        InitializeComponent();
    }

    public string? GroupByPath
    {
        get => _groupByPath;
        set { _groupByPath = value; OnPropertyChanged(); }
    }

    public SortDirection GroupSortDirection
    {
        get => _groupSortDirection;
        set { _groupSortDirection = value; OnPropertyChanged(); }
    }

    private void OnGroupBySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var path = item.Tag?.ToString();
            GroupByPath = string.IsNullOrEmpty(path) ? null : path;
        }
    }

    private void OnSortDirectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            GroupSortDirection = comboBox.SelectedIndex == 0
                ? SortDirection.Ascending
                : SortDirection.Descending;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
