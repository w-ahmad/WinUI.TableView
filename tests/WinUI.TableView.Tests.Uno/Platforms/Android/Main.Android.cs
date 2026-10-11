using Android.App;
using Android.Runtime;

namespace WinUI.TableView.Tests.Droid;

[Application(
    HardwareAccelerated = true,
    Theme = "@style/Theme.App.Starting")]
public class MainApplication : Microsoft.UI.Xaml.NativeApplication
{
    public MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new UnitTestApp(), javaReference, transfer)
    {
    }
}
