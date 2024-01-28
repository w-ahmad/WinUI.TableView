using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI3.TableView;

public class TableViewHeaderRow : ItemsControl
{
    private TableView? _tableView;

    public TableViewHeaderRow()
    {
        DefaultStyleKey = typeof(TableViewHeaderRow);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tableView = this.FindAscendant<TableView>()!;

        if (GetTemplateChild("SelectAllButton") is Button button)
        {
            button.Click += OnSelectAllButtonClick;
        }
    }

    private void OnSelectAllButtonClick(object sender, RoutedEventArgs e)
    {
        _tableView?.SelectAllSafe();
    }
}
