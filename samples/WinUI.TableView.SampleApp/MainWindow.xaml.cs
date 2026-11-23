using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinUI.TableView.SampleApp.Helpers;
using WinUI.TableView.SampleApp.Pages;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI.TableView.SampleApp;

public sealed partial class MainWindow : Window
{
    private const string HomeTag = "Home";
    private readonly Dictionary<object, NavigationViewItem> _navItems = [];
    private NavigationViewItem? _homeNavItem;
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
        BuildNavigation();
        ThemeHelper.Initialize();

        await ExampleViewModel.InitializeItemsAsync();

        SetLoading(false);
        navigationView.SelectedItem = _homeNavItem;
    }

    private void BuildNavigation()
    {
        _homeNavItem = CreateNavItem("Home", "", HomeTag);
        navigationView.MenuItems.Add(_homeNavItem);

        foreach (var group in SampleCatalog.Groups)
        {
            var groupItem = CreateNavItem(group.Title, group.Glyph, group);
#if AOT_PROFILE_GEN
            groupItem.IsExpanded = true;
#endif

            foreach (var sample in group.Samples)
            {
                var sampleItem = new NavigationViewItem { Content = sample.Title, Tag = sample };
                _navItems[sample] = sampleItem;
                groupItem.MenuItems.Add(sampleItem);
            }

            _navItems[group] = groupItem;
            navigationView.MenuItems.Add(groupItem);
        }
    }

    private static NavigationViewItem CreateNavItem(string title, string glyph, object tag)
    {
        return new NavigationViewItem
        {
            Content = title,
            Tag = tag,
            Icon = new FontIcon { Glyph = glyph }
        };
    }

    /// <summary>
    /// Opens a sample or a group's page by selecting its item in the navigation menu.
    /// </summary>
    internal void NavigateTo(object? sampleOrGroup)
    {
        if (sampleOrGroup is null || !_navItems.TryGetValue(sampleOrGroup, out var navItem)) return;

        if (sampleOrGroup is SampleInfo sample && _navItems.TryGetValue(sample.Group, out var groupItem))
        {
            groupItem.IsExpanded = true;
        }

        navigationView.SelectedItem = navItem;
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

        var pageType = args.IsSettingsSelected ? typeof(SettingsPage) : (args.SelectedItem as NavigationViewItem)?.Tag switch
        {
            SampleInfo sample => sample.PageType,
            SampleGroup => typeof(SectionPage),
            HomeTag => typeof(HomePage),
            _ => null
        };

        if (pageType is not null)
        {
            rootFrame.Navigate(pageType, args.SelectedItem);
        }
    }

    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
        {
            sender.ItemsSource = SampleCatalog.Search(sender.Text);
        }
    }

    private void OnSearchQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var sample = args.ChosenSuggestion as SampleInfo ?? SampleCatalog.Search(args.QueryText).FirstOrDefault();

        if (sample is not null)
        {
            sender.Text = string.Empty;
            sender.ItemsSource = null;
            NavigateTo(sample);
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
