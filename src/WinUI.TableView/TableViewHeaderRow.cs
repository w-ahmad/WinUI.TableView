using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Threading.Tasks;

namespace WinUI.TableView;

public partial class TableViewHeaderRow : Control
{
    private Button? _optionsButton;
    private CheckBox? _selectAllCheckBox;

    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _optionsButton = GetTemplateChild("OptionsButton") as Button;
        _selectAllCheckBox = GetTemplateChild("SelectAllCheckBox") as CheckBox;
        TableView?.RegisterPropertyChangedCallback(ListViewBase.SelectionModeProperty, delegate { OnTableViewSelectionModeChanged(); });
        TableView?.RegisterPropertyChangedCallback(TableView.ItemsSourceProperty, delegate { OnTableViewSelectionChanged(); });

        if (TableView is null)
            return;

        TableView.SelectionChanged += delegate { OnTableViewSelectionChanged(); };
        TableView.Items.VectorChanged += delegate { OnTableViewSelectionChanged(); };

        if (_optionsButton is not null)
        {
            _optionsButton.DataContext = new OptionsFlyoutViewModel(TableView);

            // Hack: this will allow keyboard accelerators to get work
            ShowAndHidOptionsFlyout();
            async void ShowAndHidOptionsFlyout()
            {
                _optionsButton.Flyout.ShowAt(_optionsButton);
                await Task.Delay(5);
                _optionsButton.Flyout.Hide();
            }
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Tapped += OnSelectAllCheckBoxTapped;
        }

        AddHeaders();
        SetExportOptionsVisibility();
        OnTableViewSelectionModeChanged();

        void AddHeaders()
        {
            if (GetTemplateChild("HeadersStackPanel") is StackPanel stackPanel && TableView.Columns?.Count > 0)
            {
                foreach (var column in TableView.Columns)
                {
                    var header = new TableViewColumnHeader { DataContext = column };
                    stackPanel.Children.Add(header);

                    header.SetBinding(ContentControl.ContentProperty,
                                      new Binding { Path = new PropertyPath(nameof(TableViewColumn.Header)) });
                    header.SetBinding(WidthProperty,
                                      new Binding { Path = new PropertyPath(nameof(TableViewColumn.Width)), Mode = BindingMode.TwoWay });
                    header.SetBinding(MinWidthProperty,
                                      new Binding { Path = new PropertyPath(nameof(TableViewColumn.MinWidth)) });
                    header.SetBinding(MaxWidthProperty,
                                      new Binding { Path = new PropertyPath(nameof(TableViewColumn.MaxWidth)) });
                }
            }
        }
    }

    private void SetExportOptionsVisibility()
    {
        var binding = new Binding
        {
            Path = new PropertyPath(nameof(TableView.ShowExportOptions)),
            Source = TableView,
            Converter = new CommunityToolkit.WinUI.Converters.BoolToVisibilityConverter()
        };

        if (GetTemplateChild("ExportAllMenuItem") is MenuFlyoutItem exportAll)
        {
            exportAll.SetBinding(VisibilityProperty, binding);
        }

        if (GetTemplateChild("ExportSelectedMenuItem") is MenuFlyoutItem exportSelected)
        {
            exportSelected.SetBinding(VisibilityProperty, binding);
        }
    }

    private void OnTableViewSelectionChanged()
    {
        if (TableView is not null && _selectAllCheckBox is not null)
        {
            if (TableView.Items.Count == 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = false;
            }
            else if (TableView.SelectedItems.Count == TableView.Items.Count)
            {
                _selectAllCheckBox.IsChecked = true;
                _selectAllCheckBox.IsEnabled = true;
            }
            else if (TableView.SelectedItems.Count > 0)
            {
                _selectAllCheckBox.IsChecked = null;
                _selectAllCheckBox.IsEnabled = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = false;
                _selectAllCheckBox.IsEnabled = true;
            }
        }
    }

    private void OnTableViewSelectionModeChanged()
    {
        if (_optionsButton is not null)
        {
            _optionsButton.Visibility = TableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                TableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_selectAllCheckBox is not null)
        {
            _selectAllCheckBox.Visibility = TableView?.SelectionMode == ListViewSelectionMode.Multiple &&
                TableView?.IsMultiSelectCheckBoxEnabled == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnSelectAllCheckBoxTapped(object sender, RoutedEventArgs e)
    {
        var checkBox = (CheckBox)sender;

        if (checkBox.IsChecked == true)
        {
            TableView?.SelectAllSafe();
        }
        else
        {
            checkBox.IsChecked = false;
            TableView?.DeselectAll();
        }
    }

    public TableView TableView
    {
        get { return (TableView)GetValue(TableViewProperty); }
        set { SetValue(TableViewProperty, value); }
    }

    public static readonly DependencyProperty TableViewProperty = DependencyProperty.Register(nameof(TableView), typeof(TableView), typeof(TableViewHeaderRow), new PropertyMetadata(null));
}
