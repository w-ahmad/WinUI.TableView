using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GroupHeaderTemplatesPage : Page
{
    public GroupHeaderTemplatesPage()
    {
        InitializeComponent();
        ApplyGrouping();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            viewModel.Items = [.. ExampleViewModel.ItemsList.Take(300)];
        }
    }

    private void ApplyGrouping()
    {
        tableView.UngroupAll();
        tableView.GroupDescriptions.Add(new GroupDescription(groupBy.SelectedItem as string));
    }

    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is not null)
        {
            ApplyGrouping();
        }
    }

    private void OnGroupStyleChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is null) return;

        switch (groupStyle.SelectedIndex)
        {
            case 0:
                tableView.DefaultGroupStyle = (GroupStyle)Resources["BadgeGroupStyle"];
                break;
            case 1:
                tableView.DefaultGroupStyle = (GroupStyle)Resources["BannerGroupStyle"];
                break;
            default:
                tableView.ClearValue(TableView.DefaultGroupStyleProperty);
                break;
        }
    }
}

public static class GroupVisuals
{
    public static string Label(object? key) => string.IsNullOrEmpty($"{key}") ? "(none)" : $"{key}";

    public static string UpperLabel(object? key) => Label(key).ToUpperInvariant();

    public static string People(int count) => count == 1 ? "1 person" : $"{count} people";
}
