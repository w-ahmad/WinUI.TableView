using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using WinUI.TableView.SampleApp.Helpers;
using WinUI.TableView.SampleApp.Pages;

namespace WinUI.TableView.SampleApp;

public sealed partial class MainPage : Page
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings = new();
    private bool _canNavigate = true;

    public MainPage()
    {
        InitializeComponent();

        App.Current.MainWindow.Activated += OnMainWindowActivated;
        App.Current.MainWindow.SetTitleBar(AppTitleBar);

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _settings.ColorValuesChanged += delegate { OnSettingsColorValuesChanged(); };
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        OnSettingsColorValuesChanged();

        await ExampleViewModel.InitializeItemsAsync();

        SetLoading(false);

#if DEBUG
        navigationView.SelectedItem = navigationView.MenuItems[2];
#else
        navigationView.SelectedItem = overViewNavItem;
#endif
    }

    internal void SetLoading(bool isLoading)
    {
        loadingIndicator.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnMainWindowActivated(object sender, WindowActivatedEventArgs args)
    {
#if WINDOWS
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            VisualStateManager.GoToState(this, "Deactivated", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "Activated", true);
        }
#endif
    }

    private void OnPaneDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        if (sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
        {
            VisualStateManager.GoToState(this, "Top", true);
        }
        else
        {
            if (args.DisplayMode == NavigationViewDisplayMode.Minimal)
            {
                VisualStateManager.GoToState(this, "Compact", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Default", true);
            }
        }
    }

    // this handles updating the caption button colors correctly when windows system theme is changed
    // while the app is open
    private void OnSettingsColorValuesChanged()
    {
        // This calls comes off-thread, hence we will need to dispatch it to current app's thread
        _dispatcherQueue.TryEnqueue(() => TitleBarHelper.ApplySystemThemeToCaptionButtons(App.Current.MainWindow));
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
                "Filtering" => typeof(FilteringPage),
                "Customize Filter Flyout" => typeof(CustomizeFilterPage),
                "External Filtering" => typeof(ExternalFilteringPage),
                "Editing" => typeof(EditingPage),
                "Sorting" => typeof(SortingPage),
                "Custom Sorting" => typeof(CustomizeSortingPage),
                "Data Export" => typeof(ExportPage),
                "Large Dataset" => typeof(LargeDataPage),
                "Conditional Cell Styling" => typeof(ConditionalStylingPage),
                _ => typeof(BlankPage)
            };

            rootFrame.Navigate(pageType, selectedItem);
        }
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
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

        if (e.Content is Page { DataContext: null } page)
        {
            await Task.Delay(10);
            page.DataContext = new ExampleViewModel();
        }

        _canNavigate = true;
    }
}