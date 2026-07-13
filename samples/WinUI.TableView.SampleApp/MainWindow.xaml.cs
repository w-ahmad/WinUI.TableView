using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinUI.TableView.SampleApp.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI.TableView.SampleApp;

public sealed partial class MainWindow : Window
{
    private bool _canNavigate = true;

    public MainWindow()
    {
        InitializeComponent();
#if WINDOWS
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);  
#endif
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/TableView.ico");
    }

    private async void RootGridLoaded(object sender, RoutedEventArgs e)
    {
        await ExampleViewModel.InitializeItemsAsync();

        SetLoading(false);
        navigationView.SelectedItem = overViewNavItem;
    }

    internal void SetLoading(bool isLoading)
    {
        loadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navigationView.IsPaneOpen = !navigationView.IsPaneOpen;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (!_canNavigate)
        {
            return;
        }

        if (args.SelectedItem is NavigationViewItem { Content: string } selectedItem)
        {
            var pageType = selectedItem.Content.ToString() switch
            {
                "Settings" => typeof(SettingsPage),
                "Overview" => typeof(OverviewPage),
                "Grid Lines" => typeof(GridLinesPage),
                "Selection" => typeof(SelectionPage),
                "Corner Button" => typeof(CornerButtonPage),
                "Alternate Row Color" => typeof(AlternateRowColorPage),
                "Context Flyouts" => typeof(ContextFlyoutsPage),
                "Row Reorder" => typeof(ReorderRowsPage),
                "Pagination" => typeof(PaginationPage),
                "Incremental Loading" => typeof(IncrementalLoadingPage),
                "Filtering" => typeof(FilteringPage),
                "Customize Filter Flyout" => typeof(CustomizeFilterPage),
                "External Filtering" => typeof(ExternalFilteringPage),
                "Clipboard Actions" => typeof(ClipboardActionsPage),
                "Editing" => typeof(EditingPage),
                "Sorting" => typeof(SortingPage),
                "Custom Sorting" => typeof(CustomizeSortingPage),
                "Data Export" => typeof(ExportPage),
                "Large Dataset" => typeof(LargeDataPage),
                "Conditional Cell Styling" => typeof(ConditionalStylingPage),
                "Column Sizing" => typeof(ColumnSizingPage),
                _ => typeof(BlankPage)
            };

            rootFrame.Navigate(pageType, selectedItem);
        }
    }

    private void OnBackRequested(TitleBar sender, object args)
    {
        if (rootFrame.CanGoBack)
        {
            rootFrame.GoBack();
        }
    }

    private async void OnRootFrameNavigated(object sender, NavigationEventArgs e)
    {
        _canNavigate = false;

        if (e.NavigationMode == NavigationMode.Back && e.Parameter is NavigationViewItem navItem && !navItem.Equals(navigationView.SelectedItem))
        {
            navigationView.SelectedItem = navItem;
        }

        if (e.Content is Page page)
        {
            page.NavigationCacheMode = NavigationCacheMode.Disabled;

            if (page.DataContext is null)
            {
                await Task.Delay(10);
                page.DataContext = new ExampleViewModel();
            }
        }

        _canNavigate = true;
    }

}
