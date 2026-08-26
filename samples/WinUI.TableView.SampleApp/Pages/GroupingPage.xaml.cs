using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#if WINDOWS
using System.Collections.Specialized;
#endif

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GroupingPage : Page
{
    private static readonly string[] Departments = ["Sales", "Engineering", "Support", "Marketing"];

    private int _nextId = 1;
    private bool _sortGroupsByCount;

    public GroupingPage()
    {
        InitializeComponent();

#if WINDOWS
        // Groups are always created fresh (Group(), or the "Group by last name" button below) with
        // SortMode defaulting back to Key - without this, toggling "Sort Groups by Count" before any
        // column is grouped, or grouping a second level after the toggle, would silently stay key-ordered.
        if (tableView.GroupDescriptions is INotifyCollectionChanged groupDescriptions)
        {
            groupDescriptions.CollectionChanged += (_, e) =>
            {
                // This fires synchronously from inside Group()'s own Add() call, itself still inside
                // Group()'s own DeferRefresh() scope - calling RefreshGrouping() (which ignores any
                // in-progress defer) from here directly would rebuild grouping reentrantly, mid-rebuild.
                // Deferring to the next dispatcher tick lets Group() fully unwind first.
                if (e.NewItems is { Count: > 0 })
                {
                    DispatcherQueue.TryEnqueue(ApplyGroupSortMode);
                }
            };
        }
#endif
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        ReseedData(viewModel);
    }

    private void OnResetClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        ReseedData(viewModel);
    }

    /// <summary>
    /// Seeds a small, deliberately grouping-friendly dataset - a handful of items per department,
    /// so it's easy to see items land in (or shift between) the right group after each operation.
    /// </summary>
    private void ReseedData(ExampleViewModel viewModel)
    {
        var items = new List<ExampleModel>();

        for (var i = 0; i < 100; i++)
        {
            items.Add(new ExampleModel
            {
                Id = i + 1,
                FirstName = DataFaker.FirstName(),
                LastName = DataFaker.LastName(),
                Department = Departments[i % Departments.Length],
                Gender = DataFaker.Gender(),
            });
        }

        _nextId = items.Count + 1;
        viewModel.Items = new(items);
        viewModel.SelectedItem = null;
    }

    private ExampleModel CreateNewItem()
    {
        var department = newItemDepartment.SelectedItem as string ?? Departments[0];

        return new ExampleModel
        {
            Id = _nextId++,
            FirstName = DataFaker.FirstName(),
            LastName = DataFaker.LastName(),
            Department = department,
            Gender = DataFaker.Gender(),
        };
    }

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items.Add(CreateNewItem());
    }

    private void OnInsertAtTopClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items.Insert(0, CreateNewItem());
    }

    private void OnInsertBeforeSelectedClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        var index = viewModel.SelectedItem is { } selected ? viewModel.Items.IndexOf(selected) : -1;

        viewModel.Items.Insert(Math.Max(0, index), CreateNewItem());
    }

    private void OnMoveUpClicked(object sender, RoutedEventArgs e) => Move(-1);

    private void OnMoveDownClicked(object sender, RoutedEventArgs e) => Move(1);

    /// <summary>
    /// Moves the selected item by <paramref name="delta"/> positions in the source collection - exercises
    /// ObservableCollection.Move, which raises NotifyCollectionChangedAction.Move.
    /// </summary>
    private void Move(int delta)
    {
        if (DataContext is not ExampleViewModel viewModel || viewModel.SelectedItem is not { } selected)
        {
            return;
        }

        var oldIndex = viewModel.Items.IndexOf(selected);
        var newIndex = oldIndex + delta;

        if (oldIndex < 0 || newIndex < 0 || newIndex >= viewModel.Items.Count)
        {
            return;
        }

        viewModel.Items.Move(oldIndex, newIndex);
    }

    private void OnDeleteSelectedClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ExampleViewModel viewModel || viewModel.SelectedItem is not { } selected)
        {
            return;
        }

        viewModel.Items.Remove(selected);
    }

    private void OnToggleLastNameGroupClicked(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        var existingGroupDescription = tableView.GroupDescriptions
    .OfType<GroupDescription>()
    .FirstOrDefault(gd => gd.PropertyName is nameof(ExampleModel.LastName));

        if (existingGroupDescription is not null)
        {
            tableView.GroupDescriptions.Remove(existingGroupDescription);
        }
        else
        {
            tableView.GroupDescriptions.Add(new GroupDescription(nameof(ExampleModel.LastName)));
        } 
#endif
    }

    /// <summary>
    /// Records the desired mode and applies it to every currently-grouped level - and, via the
    /// CollectionChanged subscription in the constructor, to any level grouped afterward too.
    /// </summary>
    private void OnSortGroupsByCountToggled(object sender, RoutedEventArgs e)
    {
        _sortGroupsByCount = sortGroupsByCount.IsOn;
#if WINDOWS
        ApplyGroupSortMode();
#endif
    }

#if WINDOWS
    private void ApplyGroupSortMode()
    {
        var mode = _sortGroupsByCount ? GroupSortMode.Count : GroupSortMode.Key;
        var changed = false;

        foreach (var groupDescription in tableView.GroupDescriptions)
        {
            if (groupDescription.SortMode == mode) continue;

            groupDescription.SortMode = mode;
            changed = true;
        }

        if (changed)
        {
            tableView.RefreshGrouping();
        }
    }
#endif
}
