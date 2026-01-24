using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI.TableView.Tests;
/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class UnitTestApp : Application
{
    private UnitTestAppWindow? _window;

    internal static new UnitTestApp Current => (UnitTestApp)Application.Current;

    internal UnitTestAppWindow MainWindow
    {
        get
        {
            _window ??= new UnitTestAppWindow();
            return _window;
        }
    }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public UnitTestApp()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        UnitTestClient.CreateDefaultUI();

        MainWindow.Activate();

        UITestMethodAttribute.DispatcherQueue = MainWindow.DispatcherQueue;

        UnitTestClient.Run(Environment.CommandLine);
    }
}
