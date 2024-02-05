using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Linq;
using Windows.System;
using Windows.UI.Core;
using SD = CommunityToolkit.WinUI.Collections.SortDirection;

namespace WinUI.TableView;

[TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePointerOver, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StatePressed, GroupName = VisualStates.GroupCommon)]
[TemplateVisualState(Name = VisualStates.StateFocused, GroupName = VisualStates.GroupFocus)]
[TemplateVisualState(Name = VisualStates.StateUnfocused, GroupName = VisualStates.GroupFocus)]
[TemplateVisualState(Name = VisualStates.StateUnsorted, GroupName = VisualStates.GroupSort)]
[TemplateVisualState(Name = VisualStates.StateSortAscending, GroupName = VisualStates.GroupSort)]
[TemplateVisualState(Name = VisualStates.StateSortDescending, GroupName = VisualStates.GroupSort)]
public class TableViewColumnHeader : ContentControl
{
    private bool _canSort;
    private TableViewColumn? _column;
    private TableView? _tableView;
    private Button? _optionsButton;

    public TableViewColumnHeader()
    {
        DefaultStyleKey = typeof(TableViewColumnHeader);
    }

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        if (_canSort && _tableView is not null && _column is TableViewBoundColumn column)
        {
            var isShiftButtonDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) is
                CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
            var path = column.Binding.Path.Path;

            if (!isShiftButtonDown)
            {
                _tableView.CollectionView.SortDescriptions.Clear();

                foreach (var header in _tableView.Columns.Select(x => x.HeaderControl))
                {
                    if (header is not null && header != this)
                    {
                        header.SortDirection = null;
                    }
                }
            }

            if (_tableView.CollectionView.SortDescriptions.FirstOrDefault(x => x.PropertyName == path) is { } description)
            {
                _tableView.CollectionView.SortDescriptions.Remove(description);
            }

            SortDirection = SortDirection == SD.Ascending ? SD.Descending : SD.Ascending;
            _tableView.CollectionView.SortDescriptions.Add(new SortDescription(path, SortDirection.Value));
        }

        base.OnTapped(e);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>();
        _optionsButton = GetTemplateChild("PART_OptionsButton") as Button;
        _column = (TableViewColumn)DataContext;
        _column.HeaderControl = this;
        _canSort = _column is TableViewBoundColumn { CanSort: true, Binding.Path.Path.Length: > 0 };

        if (_optionsButton is not null && _column is TableViewBoundColumn)
        {
            _optionsButton.Visibility = Visibility.Visible;
            _optionsButton.Tapped += OnOptionsButtonTaped;
        }
    }

    private void OnOptionsButtonTaped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private static void OnSortDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewColumnHeader header)
        {
            if (header.SortDirection == SD.Ascending)
            {
                VisualStates.GoToState(header, false, VisualStates.StateSortAscending);
            }
            else if (header.SortDirection == SD.Descending)
            {
                VisualStates.GoToState(header, false, VisualStates.StateSortDescending);
            }
            else
            {
                VisualStates.GoToState(header, false, VisualStates.StateUnsorted);
            }
        }
    }

    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    public SD? SortDirection
    {
        get => (SD?)GetValue(SortDirectionProperty);
        private set => SetValue(SortDirectionProperty, value);
    }

    public static readonly DependencyProperty SortDirectionProperty = DependencyProperty.Register(nameof(SortDirection), typeof(SD?), typeof(TableViewColumnHeader), new PropertyMetadata(default, OnSortDirectionChanged));
    public static readonly DependencyProperty CanResizeProperty = DependencyProperty.Register(nameof(CanResize), typeof(bool), typeof(TableViewColumnHeader), new PropertyMetadata(true));
}
