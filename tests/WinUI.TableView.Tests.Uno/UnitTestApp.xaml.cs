using Microsoft.UI.Xaml;

namespace WinUI.TableView.Tests;

public partial class UnitTestApp : Application
{
    private UnitTestAppWindow? _mainWindow;

    internal static new UnitTestApp Current => (UnitTestApp)Application.Current;

    internal UnitTestAppWindow MainWindow
    {
        get
        {
            _mainWindow ??= new UnitTestAppWindow();
            return _mainWindow;
        }
    }

    public UnitTestApp()
    {
        UnhandledException += (_, args) =>
            UnoTestRunner.WriteMessage($"Uno test host unhandled exception: {args.Exception}");
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var exitAfterTests = Environment.GetCommandLineArgs()
            .Contains("--exit-after-tests", StringComparer.Ordinal);
        MainWindow.Activate();
        await UnoTestRunner.RunAsync(MainWindow, exitAfterTests);
    }
}
