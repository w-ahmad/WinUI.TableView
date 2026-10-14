using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

// All UI tests share a single window and its content.
[assembly: DoNotParallelize]

namespace WinUI.TableView.Tests;

[TestClass]
public sealed class UITestLifecycleTests
{
    private Grid? _content;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        AssertDispatcherAccess();
        await Task.Yield();
        AssertDispatcherAccess();

        _content = new Grid { Width = 100, Height = 100 };
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(_content);
    }

    [UITestMethod]
    public async Task AsyncTestLifecycle_UsesTheWindowDispatcherAndRendersContent()
    {
        AssertDispatcherAccess();
        await Task.Yield();
        AssertDispatcherAccess();

        Assert.IsNotNull(_content);
        Assert.IsTrue(_content.IsLoaded);
        Assert.IsNotNull(_content.XamlRoot);
        Assert.AreEqual(100d, _content.ActualWidth);
        Assert.AreEqual(100d, _content.ActualHeight);
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        AssertDispatcherAccess();
        await Task.Yield();
        AssertDispatcherAccess();

        if (_content?.IsLoaded == true)
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(_content);
            Assert.IsFalse(_content.IsLoaded);
        }
    }

    private static void AssertDispatcherAccess()
    {
        Assert.IsTrue(UnitTestApp.Current.MainWindow.DispatcherQueue.HasThreadAccess,
            "Test initialization, async continuations and cleanup must use the WinUI dispatcher.");
    }
}
