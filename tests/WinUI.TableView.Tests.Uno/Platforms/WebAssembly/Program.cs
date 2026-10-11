using Uno.UI.Hosting;
using WinUI.TableView.Tests;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new UnitTestApp())
    .UseWebAssembly()
    .Build();

await host.RunAsync();
