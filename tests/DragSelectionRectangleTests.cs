using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
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

    [UITestMethod]
    public void ShowDragRectangle_DefaultsToTrue()
    {
        var tv = new TableView();
        Assert.IsTrue(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public void ShowDragRectangle_CanBeSetToFalse()
    {
        var tv = new TableView();
        tv.ShowDragRectangle = false;
        Assert.IsFalse(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public void StartDragRectangle_SetsIsDragging_WhenExtendedMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        Assert.IsFalse(tv._isDragging);

        tv.StartDragRectangle(new Point(10, 10));

        Assert.IsTrue(tv._isDragging);
    }

    [UITestMethod]
    public void StartDragRectangle_DoesNotStart_WhenSingleMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.Single);

        tv.StartDragRectangle(new Point(10, 10));

        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void StartDragRectangle_DoesNotStart_WhenNoneMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.None);

        tv.StartDragRectangle(new Point(10, 10));

        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void StartDragRectangle_DoesNotStart_WhenShowDragRectangleIsFalse()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        tv.ShowDragRectangle = false;

        tv.StartDragRectangle(new Point(10, 10));

        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void StartDragRectangle_Starts_InMultipleMode()
    {
        var tv = CreateTableView(ListViewSelectionMode.Multiple);

        tv.StartDragRectangle(new Point(10, 10));

        Assert.IsTrue(tv._isDragging);
    }

    [UITestMethod]
    public void EndDragRectangle_ResetsIsDragging()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        tv.StartDragRectangle(new Point(10, 10));
        Assert.IsTrue(tv._isDragging);

        tv.EndDragRectangle();

        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void EndDragRectangle_IsIdempotent()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        tv.StartDragRectangle(new Point(10, 10));

        tv.EndDragRectangle();
        tv.EndDragRectangle(); // second call should not throw

        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void ShowDragRectangle_SetFalse_EndsDragIfActive()
    {
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        tv.StartDragRectangle(new Point(10, 10));
        Assert.IsTrue(tv._isDragging);

        tv.ShowDragRectangle = false;

        Assert.IsFalse(tv._isDragging);
    }

    [TestMethod]
    public void TableViewCellSlot_Equality_SameValues_AreEqual()
    {
        var slot1 = new TableViewCellSlot(3, 5);
        var slot2 = new TableViewCellSlot(3, 5);
        Assert.AreEqual(slot1, slot2);
        Assert.IsTrue(slot1 == slot2);
    }

    [TestMethod]
    public void TableViewCellSlot_Equality_DifferentValues_AreNotEqual()
    {
        var slot1 = new TableViewCellSlot(3, 5);
        var slot2 = new TableViewCellSlot(4, 5);
        Assert.AreNotEqual(slot1, slot2);
        Assert.IsTrue(slot1 != slot2);

        var slot3 = new TableViewCellSlot(3, 6);
        Assert.AreNotEqual(slot1, slot3);
    }

    [TestMethod]
    public void TableViewCellSlot_HashCode_SameValues_SameHash()
    {
        var slot1 = new TableViewCellSlot(3, 5);
        var slot2 = new TableViewCellSlot(3, 5);
        Assert.AreEqual(slot1.GetHashCode(), slot2.GetHashCode());
    }

    [UITestMethod]
    public void ShowDragRectangleProperty_IsRegistered()
    {
        var tv = new TableView();
        var value = tv.GetValue(TableView.ShowDragRectangleProperty);
        Assert.IsInstanceOfType(value, typeof(bool));
        Assert.IsTrue((bool)value);
    }

    [UITestMethod]
    public void ShowDragRectangleProperty_CanBeSetViaDP()
    {
        var tv = new TableView();
        tv.SetValue(TableView.ShowDragRectangleProperty, false);
        Assert.IsFalse(tv.ShowDragRectangle);
    }

    [UITestMethod]
    public void StartDragRectangle_DoesNotStart_WhenCanvasIsNull()
    {
        // Before OnApplyTemplate, _dragRectangleCanvas is null
        var tv = CreateTableView(ListViewSelectionMode.Extended);
        Assert.IsNull(tv._dragRectangleCanvas);

        tv.StartDragRectangle(new Point(10, 10));

        // Should not crash, and should not set _isDragging since canvas is null
        Assert.IsFalse(tv._isDragging);
    }

    [UITestMethod]
    public void UpdateDragRectangleVisual_DoesNotCrash_WhenNotDragging()
    {
        var tv = CreateTableView();

        // Should be a no-op, not throw
        tv.UpdateDragRectangleVisual(new Point(50, 50));

        Assert.IsFalse(tv._isDragging);
    }
}
