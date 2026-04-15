using Microsoft.UI.Xaml;
using Windows.Storage;

namespace WinUI.TableView.SampleApp.Helpers;

/// <summary>
/// Class providing functionality around switching and restoring theme settings
/// </summary>
internal static class ThemeHelper
{
    private const string SelectedAppThemeKey = "SelectedAppTheme";

    /// <summary>
    /// Gets the current actual theme of the app based on the requested theme of the
    /// root element, or if that value is Default, the requested theme of the Application.
    /// </summary>
    public static ElementTheme ActualTheme
    {
        get
        {
            if (App.Current.MainWindow.Content is FrameworkElement rootElement)
            {
                if (rootElement.RequestedTheme != ElementTheme.Default)
                {
                    return rootElement.RequestedTheme;
                }
            }

            return App.Current.RequestedTheme.ToElementTheme();
        }
    }

    /// <summary>
    /// Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
    /// </summary>
    public static ElementTheme RootTheme
    {
        get => App.Current.MainWindow.Content is FrameworkElement rootElement ? rootElement.RequestedTheme : ElementTheme.Default;
        set
        {
            if (App.Current.MainWindow.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = value;
            }

            if (NativeHelper.IsAppPackaged)
            {
                ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
            }
        }
    }

    public static void Initialize()
    {
        if (NativeHelper.IsAppPackaged)
        {
            var savedTheme = ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey]?.ToString();

            if (savedTheme != null)
            {
                RootTheme = GetElementTheme(savedTheme);
            }
        }
    }

    public static bool IsDarkTheme()
    {
        return RootTheme == ElementTheme.Default ? App.Current.RequestedTheme == ApplicationTheme.Dark : RootTheme == ElementTheme.Dark;
    }

    internal static ElementTheme ToElementTheme(this ApplicationTheme applicationTheme)
    {
        return applicationTheme switch
        {
            ApplicationTheme.Light => ElementTheme.Light,
            ApplicationTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    public static ElementTheme GetElementTheme(string applicationTheme)
    {
        return applicationTheme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }
}