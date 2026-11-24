using Microsoft.UI;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using WinUI.TableView.SampleApp.Helpers;

namespace WinUI.TableView.SampleApp;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        themeMode.SelectedIndex = ThemeHelper.RootTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            _ => 2,
        };
    }

    private void OnThemeModeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // By index: under Native AOT the selected item may not cast back to ComboBoxItem.
        if (sender is not UIElement senderUiLement || themeMode.SelectedIndex < 0)
        {
            return;
        }

        ThemeHelper.RootTheme = themeMode.SelectedIndex switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        var elementThemeResolved = ThemeHelper.RootTheme == ElementTheme.Default ? ThemeHelper.ActualTheme : ThemeHelper.RootTheme;
        TitleBarHelper.ApplySystemThemeToCaptionButtons(App.Current.MainWindow, elementThemeResolved);

        // announce visual change to automation
        UIHelper.AnnounceActionForAccessibility(
            senderUiLement,
            $"Theme changed to {elementThemeResolved}",
            "ThemeChangedNotificationActivityId");
    }

    /// <summary>
    /// The version of the WinUI.TableView library the app runs on. A release build references the published
    /// package and shows its version; a build from source has no version set and shows its commit instead.
    /// </summary>
    public string Version
    {
        get
        {
            var informational = typeof(TableView).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
            var parts = informational.Split('+');
            var version = parts[0];
            var commit = parts.Length > 1 ? parts[1][..Math.Min(7, parts[1].Length)] : null;

            if (version is "" or "1.0.0" || version.StartsWith("0.0."))
            {
                return commit is null ? "Built from source" : $"Built from source ({commit})";
            }

            return version;
        }
    }
}
