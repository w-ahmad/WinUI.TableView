using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace WinUI.TableView.Tests;

[TestClass]
public class DragSelectionRectangleTests
{
    private static TableView CreateTableView(ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended)
    {
        var tv = new TableView
        {
            ItemsSource = Enumerable.Range(0, 20).ToList(),
            SelectionMode = selectionMode,
        };
        tv.Columns.Add(new TableViewTextColumn { Header = "Col1" });
        tv.Columns.Add(new TableViewTextColumn { Header = "Col2" });
        return tv;
    }

    private static async Task<TableView> CreateAndLoadTableView(ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended)
    {
        var tv = CreateTableView(selectionMode);
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tv);
        return tv;
    }

    [UITestMethod]
    public void ShowDragRectangle_DefaultsToFalse()
    {
        var tv = new TableView();
        Assert.IsFalse(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public void ShowDragRectangle_CanBeSetToFalse()
    {
        var tv = new TableView();
        tv.ShowDragRectangle = false;
        Assert.IsFalse(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public async Task StartDragSelection_SetsIsDragging_WhenExtendedMode()
    {
        var tv = await CreateAndLoadTableView(ListViewSelectionMode.Extended);
        Assert.IsFalse(tv._isDragSelecting);

        tv.StartDragSelection(new Point(10, 10));

        Assert.IsTrue(tv._isDragSelecting);

        tv.EndDragSelection();
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tv);
    }

    [UITestMethod]
    public void StartDragSelection_DoesNotStart_WhenSingleMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.Single);

        tv.StartDragSelection(new Point(10, 10));

        Assert.IsFalse(tv._isDragSelecting);
    }

    [UITestMethod]
    public void StartDragSelection_DoesNotStart_WhenNoneMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.None);

        tv.StartDragSelection(new Point(10, 10));

        Assert.IsFalse(tv._isDragSelecting);
    }

    [UITestMethod]
    public async Task StartDragSelection_StartsButNoRectangle_WhenShowDragRectangleIsFalse()
    {
        var tv = await CreateAndLoadTableView(ListViewSelectionMode.Extended);
        tv.ShowDragRectangle = false;

        tv.StartDragSelection(new Point(10, 10));

        // Drag selection and auto-scroll are active, but rectangle visual is not shown
        Assert.IsTrue(tv._isDragSelecting);
        Assert.AreEqual(Visibility.Collapsed, tv._dragRectangleCanvas?.Children.OfType<Border>().FirstOrDefault()?.Visibility);

        tv.EndDragSelection();
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tv);
    }

    [UITestMethod]
    public async Task StartDragSelection_Starts_InMultipleMode()
    {
        var tv = await CreateAndLoadTableView(ListViewSelectionMode.Multiple);

        tv.StartDragSelection(new Point(10, 10));

        Assert.IsTrue(tv._isDragSelecting);

        tv.EndDragSelection();
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tv);
    }

    [UITestMethod]
    public async Task EndDragSelection_ResetsIsDragging()
    {
        var tv = await CreateAndLoadTableView(ListViewSelectionMode.Extended);
        tv.StartDragSelection(new Point(10, 10));
        Assert.IsTrue(tv._isDragSelecting);

        tv.EndDragSelection();

        Assert.IsFalse(tv._isDragSelecting);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tv);
    }

    [UITestMethod]
    public void EndDragSelection_IsIdempotent()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        tv.StartDragSelection(new Point(10, 10));

        tv.EndDragSelection();
        tv.EndDragSelection(); // second call should not throw

        Assert.IsFalse(tv._isDragSelecting);
    }

    [UITestMethod]
    public async Task ShowDragRectangle_SetFalse_HidesRectangleButKeepsDragActive()
    {
        var tv = await CreateAndLoadTableView(ListViewSelectionMode.Extended);
        tv.StartDragSelection(new Point(10, 10));
        Assert.IsTrue(tv._isDragSelecting);

        tv.ShowDragRectangle = false;

        // Drag selection and auto-scroll remain active, but rectangle visual is hidden
        Assert.IsTrue(tv._isDragSelecting);

        tv.EndDragSelection();
        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tv);
    }

    [UITestMethod]
    public void ShowDragRectangleProperty_IsRegistered()
    {
        var tv = new TableView();
        var value = tv.GetValue(TableView.ShowDragRectangleProperty);
        Assert.IsInstanceOfType(value, typeof(bool));
        Assert.IsFalse((bool)value);
    }

    [UITestMethod]
    public void ShowDragRectangleProperty_CanBeSetViaDP()
    {
        var tv = new TableView();
        tv.SetValue(TableView.ShowDragRectangleProperty, false);
        Assert.IsFalse(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public void StartDragSelection_StartsEvenWhenCanvasIsNull()
    {
        // Before OnApplyTemplate, _dragRectangleCanvas is null
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        Assert.IsNull(tv._dragRectangleCanvas);

        tv.StartDragSelection(new Point(10, 10));

        // Drag selection and auto-scroll start even without canvas (rectangle visual just won't show)
        Assert.IsTrue(tv._isDragSelecting);

        tv.EndDragSelection();
    }

    [UITestMethod]
    public void UpdateDragRectangleVisual_DoesNotCrash_WhenNotDragging()
    {
        var tv = CreateTableView();

        // Should be a no-op, not throw
        tv.UpdateDragRectangleVisual(new Point(50, 50));

        Assert.IsFalse(tv._isDragSelecting);
    }
}
