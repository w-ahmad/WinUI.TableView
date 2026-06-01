using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Helpers;

internal class TitleBarHelper
{
    public static Color ApplySystemThemeToCaptionButtons(Window window)
    {
        if (App.Current.MainWindow.Content is FrameworkElement element)
        {
            var color = element.ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
            SetCaptionButtonColors(window, color);
            return color;
        }

        return default;
    }

    public static void SetCaptionButtonColors(Window window, Color color)
    {
        var res = Application.Current.Resources;
        res["WindowCaptionForeground"] = color;
        window.AppWindow.TitleBar.ButtonForegroundColor = color;
    }
}