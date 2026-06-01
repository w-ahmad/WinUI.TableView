using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
#if !WINDOWS
using Uno.Resizetizer;
#endif

namespace WinUI.TableView.SampleApp;

public partial class App : Application
{
    private readonly Lazy<Window> _mainWindow = new(() => new Window());
    private readonly Lazy<MainPage> _mainPage = new(() => new MainPage());

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if WINDOWS
        MainWindow.ExtendsContentIntoTitleBar = true;
        MainWindow.SystemBackdrop = new MicaBackdrop();
#endif
        MainWindow.Content = MainPage;
#if DEBUG && !WINDOWS
        MainWindow.UseStudio();
        MainWindow.SetWindowIcon();
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
            // Note: DebugSettings.EnableFrameRateCounter requires an Application instance
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


    public MainPage MainPage => _mainPage.Value;
    public Window MainWindow => _mainWindow.Value;
    public static new App Current => (App)Application.Current;
}
