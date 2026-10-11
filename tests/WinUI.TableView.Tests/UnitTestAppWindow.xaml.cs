using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WinUI.TableView.Tests;

public sealed partial class UnitTestAppWindow : Window
{
    public UnitTestAppWindow()
    {
        InitializeComponent();
    }

    internal async Task LoadTestContentAsync(FrameworkElement content)
    {
        var taskCompletionSource = new TaskCompletionSource<object?>();

        content.Loaded += OnLoaded;
        Content = content;

        await taskCompletionSource.Task;

        async void OnLoaded(object sender, RoutedEventArgs args)
        {
            content.Loaded -= OnLoaded;

            // Wait for first Render pass
            await ExecuteAfterCompositionRenderingAsync();

            taskCompletionSource.SetResult(null);
        }
    }

    private static async Task ExecuteAfterCompositionRenderingAsync()
    {
        var taskCompletionSource = new TaskCompletionSource<object?>();

        void Callback(object? sender, object args)
        {
            CompositionTarget.Rendering -= Callback;
            taskCompletionSource.SetResult(null);
        }

        CompositionTarget.Rendering += Callback;

        await taskCompletionSource.Task;
    }

    public async Task UnloadTestContentAsync(FrameworkElement element)
    {
        var taskCompletionSource = new TaskCompletionSource<object?>();

        element.Unloaded += OnUnloaded;

        Content = null;

        await taskCompletionSource.Task;
        Assert.IsFalse(element.IsLoaded);

        void OnUnloaded(object sender, RoutedEventArgs args)
        {
            element.Unloaded -= OnUnloaded;
            taskCompletionSource.SetResult(null);
        }
    }
}
