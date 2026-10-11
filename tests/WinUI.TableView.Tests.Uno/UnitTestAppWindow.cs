using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

internal sealed class UnitTestAppWindow : Window
{
    public UnitTestAppWindow()
    {
        Content = new Grid();
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
            await ExecuteAfterCompositionRenderingAsync();
            taskCompletionSource.SetResult(null);
        }
    }

    internal async Task UnloadTestContentAsync(FrameworkElement element)
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
}
