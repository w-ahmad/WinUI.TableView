using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        var selectedTheme = ((ComboBoxItem)themeMode.SelectedItem)?.Tag?.ToString();
        string color;
        if (selectedTheme != null)
        {
            ThemeHelper.RootTheme = ThemeHelper.GetElementTheme(selectedTheme);
            if (selectedTheme == "Dark")
            {
                TitleBarHelper.SetCaptionButtonColors(App.Current.MainWindow, Colors.White);
                color = selectedTheme;
            }
            else if (selectedTheme == "Light")
            {
                TitleBarHelper.SetCaptionButtonColors(App.Current.MainWindow, Colors.Black);
                color = selectedTheme;
            }
            else
            {
                color = TitleBarHelper.ApplySystemThemeToCaptionButtons(App.Current.MainWindow) == Colors.White ? "Dark" : "Light";
            }
            // announce visual change to automation
            UIHelper.AnnounceActionForAccessibility(
                (UIElement)sender,
                $"Theme changed to {color}",
                "ThemeChangedNotificationActivityId");
        }
    }

    public string Version
    {
        get
        {
            var version = typeof(SettingsPage).Assembly.GetName().Version!;
            return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
        }
    }
}
