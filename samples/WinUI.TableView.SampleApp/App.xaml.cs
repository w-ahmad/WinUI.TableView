using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace WinUI.TableView.SampleApp;

public partial class App : Application
{
    private readonly Lazy<MainWindow> _mainWindow = new(() => new MainWindow());

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

#if DEBUG && WINDOWS
        DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        DebugSettings.XamlResourceReferenceFailed += DebugSettings_XamlResourceReferenceFailed;
        UnhandledException += App_UnhandledException;
#endif
    }

#if DEBUG && WINDOWS
    private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
    {
        Debug.WriteLine(e.Message);
    }

    private void DebugSettings_XamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs args)
    {
        Debug.WriteLine(args.Message);
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        if (Debugger.IsAttached) Debugger.Break();
    }
#endif

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            DebugSettings.EnableFrameRateCounter = true;
        }
#if !WINDOWS
        MainWindow.UseStudio();
        MainWindow.SetWindowIcon();
#endif
#endif
        MainWindow.Activate();
    }

    public static void InitializeLogging()
    {

#if DEBUG
        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif !WINDOWS
            builder.AddConsole();
#else
            builder.AddDebug();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });


#if !WINDOWS
        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }

    public MainWindow MainWindow => _mainWindow.Value;
    public static new App Current => (App)Application.Current;
}
