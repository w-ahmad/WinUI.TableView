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
#if WINDOWS
    private int _navigationGcGeneration;
#endif

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
                "Virtualization" => typeof(VirtualizationPage),
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
                "Grouping" => typeof(GroupingPage),
                "Data Export" => typeof(ExportPage),
                "Large Dataset" => typeof(LargeDataPage),
                "Conditional Cell Styling" => typeof(ConditionalStylingPage),
                "Column Sizing" => typeof(ColumnSizingPage),
                "Headers Visibility" => typeof(HeadersVisibilityPage),
                "Row Header Template" => typeof(RowHeaderTemplatePage),
                "Row Details" => typeof(RowDetailsPage),
                "Compact Sizing" => typeof(CompactSizingPage),
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

#if WINDOWS
        // The page left behind is thousands of XAML objects, and WinUI reacts to that memory pressure by
        // inducing a gen2 collection on its own schedule, whose reference-tracker phase suspends the process
        // for hundreds of milliseconds - measured landing in the middle of the first scroll on the new page.
        // Collecting once the new page has settled takes the same pause while nothing is moving. Another
        // navigation within the delay supersedes this one.
        var generation = ++_navigationGcGeneration;
        await Task.Delay(500);

        if (generation == _navigationGcGeneration)
        {
            GC.Collect();
        }
#endif
    }

}
