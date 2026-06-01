using Uno.UI.Hosting;
using WinUI.TableView.SampleApp;

App.InitializeLogging();

var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
