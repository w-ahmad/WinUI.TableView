using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ContextFlyoutsPage : Page
{
    public ObservableCollection<ExampleModel> Items => (ObservableCollection<ExampleModel>)tableView.ItemsSource!;

    public ContextFlyoutsPage()
    {
        InitializeComponent();
    }

    private void OnCellContextFlyoutOpening(object sender, TableViewCellContextFlyoutEventArgs e)
    {
        if (e.Flyout is MenuFlyout flyout)
        {
            foreach (var item in flyout.Items)
            {
                item.Tag = e.Cell;
            }
        }
    }

    private void OnCellMenuItemClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var menuItem = sender as MenuFlyoutItem;

        if ((menuItem == copyCell || menuItem == copyCellWithHeader) && menuItem.Tag is TableViewCell cell)
        {
            var content = tableView.GetCellsContent([cell.Slot], menuItem == copyCellWithHeader);

            CopyToClipboard(content);
        }
        else if (menuItem == deleteRow2)
        {
            tableView.CollectionView.Remove(deleteRow2.DataContext);
        }
    }

    private void OnRowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
    {
        moveRowUp.IsEnabled = tableView.Items.IndexOf(e.Item) > 0;
        moveRowDown.IsEnabled = tableView.Items.IndexOf(e.Item) < tableView.Items.Count - 2;
    }

    private void OnRowMenuItemClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var menuItem = sender as MenuFlyoutItem;

        if (menuItem == copyRow || menuItem == copyRowWithHeader)
        {
            var index = tableView.CollectionView.IndexOf(moveRowUp.DataContext);
            var content = tableView.GetRowsContent([index], menuItem == copyRowWithHeader);

            CopyToClipboard(content);
        }
        else if (menuItem == moveRowUp || menuItem == moveRowDown)
        {
            var oldIndex = tableView.CollectionView.IndexOf(moveRowUp.DataContext);
            var newIndex = oldIndex + (menuItem == moveRowUp ? -1 : +1);

            tableView.CollectionView.RemoveAt(oldIndex);
            tableView.CollectionView.Insert(newIndex, moveRowUp.DataContext);
        }
        else if (menuItem == deleteRow)
        {
            tableView.CollectionView.Remove(deleteRow.DataContext);
        }
    }

    private static void CopyToClipboard(string content)
    {
        var package = new DataPackage();

        package.SetText(content);
        Clipboard.SetContent(package);
    }
}
