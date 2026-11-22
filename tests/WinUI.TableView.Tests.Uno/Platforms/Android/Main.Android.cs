using Android.App;
using Android.Runtime;
using Uno.UI.Hosting;

namespace WinUI.TableView.Tests.Droid;

[Application(
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting")]
public class MainApplication : global::Uno.UI.Runtime.Android.NativeApplication
{
    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }
    protected override UnoPlatformHost CreateHost() =>
        UnoPlatformHostBuilder.Create().App(() => new UnitTestApp()).UseAndroid().Build();

}
