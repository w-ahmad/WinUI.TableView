using Uno.UI.Hosting;
using UIKit;
using WinUI.TableView.Tests;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new UnitTestApp())
    .UseAppleUIKit()
    .Build();

host.Run();
