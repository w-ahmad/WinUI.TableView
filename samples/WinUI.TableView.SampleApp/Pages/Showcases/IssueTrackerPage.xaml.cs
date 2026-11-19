using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class IssueTrackerPage : Page
{
    private string _searchText = string.Empty;
    private int _nextKey = 483;

    public IssueTrackerPage()
    {
        InitializeComponent();

        tableView.FilterDescriptions.Add(new FilterDescription(nameof(WorkItem.Title), IsMatch));
        ApplyGrouping();
        UpdateSummary();
    }

    public ObservableCollection<WorkItem> Items { get; } = WorkItemData.Create();

    private bool IsMatch(object? item)
    {
        return item is WorkItem issue
            && (_searchText.Length == 0
                || issue.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || issue.Key.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || issue.Assignee.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                || issue.Labels.Any(label => label.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
    }

    private void ApplyGrouping()
    {
        tableView.UngroupAll();

        // Status and priority groups follow their own order rather than the alphabet.
        GroupDescription? description = groupBy.SelectedIndex switch
        {
            0 => new(nameof(WorkItem.Status), comparer: WorkItemData.StatusOrder, valueDelegate: item => (item as WorkItem)?.Status),
            1 => new(nameof(WorkItem.Assignee), valueDelegate: item => (item as WorkItem)?.Assignee),
            2 => new(nameof(WorkItem.Priority), comparer: WorkItemData.PriorityOrder, valueDelegate: item => (item as WorkItem)?.Priority),
            _ => null
        };

        if (description is not null)
        {
            tableView.GroupDescriptions.Add(description);
        }
    }

    private void OnGroupByChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is not null)
        {
            ApplyGrouping();
        }
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _searchText = sender.Text.Trim();
        tableView.RefreshFilter();
    }

    private void OnNewIssueClicked(object sender, RoutedEventArgs e)
    {
        var issue = new WorkItem
        {
            Key = $"TV-{_nextKey++}",
            Title = "New issue",
            Reporter = WorkItemData.Assignees[0],
            Description = "Describe the issue here.",
            Points = 1,
            Due = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
        };

        Items.Insert(0, issue);
        tableView.SelectedItem = issue;
        tableView.ScrollIntoView(issue);
        UpdateSummary();
    }

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        foreach (var issue in tableView.SelectedItems.OfType<WorkItem>().ToList())
        {
            Items.Remove(issue);
        }

        UpdateSummary();
    }

    private void OnMarkDoneClicked(object sender, RoutedEventArgs e)
    {
        foreach (var issue in tableView.SelectedItems.OfType<WorkItem>().ToList())
        {
            issue.Status = "Done";
        }

        UpdateSummary();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        deleteButton.IsEnabled = tableView.SelectedItems.Count > 0;
    }

    private void OnCellEditEnded(object? sender, TableViewCellEditEndedEventArgs e)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var open = Items.Where(issue => issue.Status != "Done").ToList();

        summary.Text = $"{Items.Count} issues · {open.Count} open · {open.Sum(issue => issue.Points)} points left";
    }
}
