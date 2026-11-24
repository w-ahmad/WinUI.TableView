using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using WinUI.TableView.Selection;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewSelectionModelTests
{
    [TestMethod]
    public void NewModel_IsEmpty()
    {
        var model = new TableViewSelectionModel();

        Assert.AreEqual(0, model.Count);
        Assert.AreEqual(0, model.RangeCount);
        Assert.AreEqual(-1, model.FirstIndex);
        Assert.AreEqual(-1, model.LastIndex);
        Assert.IsFalse(model.Contains(0));
    }

    [TestMethod]
    public void Select_SingleIndex_IsSelected()
    {
        var model = new TableViewSelectionModel();

        Assert.IsTrue(model.Select(5, 5));

        Assert.AreEqual(1, model.Count);
        Assert.IsTrue(model.Contains(5));
        Assert.IsFalse(model.Contains(4));
        Assert.IsFalse(model.Contains(6));
    }

    [TestMethod]
    public void Select_SameRangeTwice_ReportsNoChangeTheSecondTime()
    {
        var model = new TableViewSelectionModel();

        Assert.IsTrue(model.Select(2, 8));
        Assert.IsFalse(model.Select(2, 8));
        Assert.IsFalse(model.Select(3, 7));

        Assert.AreEqual(7, model.Count);
        Assert.AreEqual(1, model.RangeCount);
    }

    [TestMethod]
    public void Select_AdjacentRanges_AreMerged()
    {
        var model = new TableViewSelectionModel();

        model.Select(0, 4);
        model.Select(5, 9);

        Assert.AreEqual(1, model.RangeCount);
        Assert.AreEqual(10, model.Count);
        Assert.AreEqual(0, model.FirstIndex);
        Assert.AreEqual(9, model.LastIndex);
    }

    [TestMethod]
    public void Select_OverlappingRanges_AreMerged()
    {
        var model = new TableViewSelectionModel();

        model.Select(0, 6);
        model.Select(4, 10);

        Assert.AreEqual(1, model.RangeCount);
        Assert.AreEqual(11, model.Count);
    }

    [TestMethod]
    public void Select_DisjointRanges_StayApart()
    {
        var model = new TableViewSelectionModel();

        model.Select(0, 2);
        model.Select(10, 12);

        Assert.AreEqual(2, model.RangeCount);
        Assert.AreEqual(6, model.Count);
        Assert.IsFalse(model.Contains(5));
    }

    [TestMethod]
    public void Select_BridgingRange_MergesEverything()
    {
        var model = new TableViewSelectionModel();

        model.Select(0, 2);
        model.Select(10, 12);
        model.Select(20, 22);

        Assert.AreEqual(3, model.RangeCount);

        model.Select(1, 21);

        Assert.AreEqual(1, model.RangeCount);
        Assert.AreEqual(23, model.Count);
    }

    [TestMethod]
    public void Deselect_Middle_SplitsTheRange()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 9);

        Assert.IsTrue(model.Deselect(4, 5));

        Assert.AreEqual(2, model.RangeCount);
        Assert.AreEqual(8, model.Count);
        Assert.IsTrue(model.Contains(3));
        Assert.IsFalse(model.Contains(4));
        Assert.IsFalse(model.Contains(5));
        Assert.IsTrue(model.Contains(6));
    }

    [TestMethod]
    public void Deselect_LeadingAndTrailingEdges_TrimTheRange()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 9);

        model.Deselect(0, 1);
        model.Deselect(8, 9);

        Assert.AreEqual(1, model.RangeCount);
        Assert.AreEqual(2, model.FirstIndex);
        Assert.AreEqual(7, model.LastIndex);
        Assert.AreEqual(6, model.Count);
    }

    [TestMethod]
    public void Deselect_SpanningSeveralRanges_RemovesThemAll()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 2);
        model.Select(10, 12);
        model.Select(20, 22);

        Assert.IsTrue(model.Deselect(1, 21));

        Assert.AreEqual(2, model.RangeCount);
        Assert.AreEqual(2, model.Count);
        Assert.IsTrue(model.Contains(0));
        Assert.IsTrue(model.Contains(22));
    }

    [TestMethod]
    public void Deselect_UnselectedRange_ReportsNoChange()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 2);

        Assert.IsFalse(model.Deselect(10, 20));
        Assert.AreEqual(3, model.Count);
    }

    [TestMethod]
    public void SelectAll_OverAMillionRows_IsASingleRange()
    {
        var model = new TableViewSelectionModel();

        Assert.IsTrue(model.SelectAll(1_000_000));

        Assert.AreEqual(1_000_000, model.Count);
        Assert.AreEqual(1, model.RangeCount);
        Assert.IsTrue(model.Contains(0));
        Assert.IsTrue(model.Contains(999_999));
        Assert.IsFalse(model.Contains(1_000_000));
        Assert.IsFalse(model.SelectAll(1_000_000));
    }

    [TestMethod]
    public void SelectOnly_ReplacesTheWholeSelection()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 100);

        Assert.IsTrue(model.SelectOnly(7));

        Assert.AreEqual(1, model.Count);
        Assert.AreEqual(7, model.FirstIndex);
        Assert.IsFalse(model.SelectOnly(7));
    }

    [TestMethod]
    public void Clear_EmptiesTheSelection()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 10);

        Assert.IsTrue(model.Clear());
        Assert.AreEqual(0, model.Count);
        Assert.IsFalse(model.Clear());
    }

    [TestMethod]
    public void IndexAt_And_PositionOf_RoundTrip()
    {
        var model = new TableViewSelectionModel();
        model.Select(3, 5);
        model.Select(10, 11);

        Assert.AreEqual(5, model.Count);
        Assert.AreEqual(3, model.IndexAt(0));
        Assert.AreEqual(4, model.IndexAt(1));
        Assert.AreEqual(5, model.IndexAt(2));
        Assert.AreEqual(10, model.IndexAt(3));
        Assert.AreEqual(11, model.IndexAt(4));
        Assert.AreEqual(-1, model.IndexAt(5));
        Assert.AreEqual(-1, model.IndexAt(-1));

        Assert.AreEqual(0, model.PositionOf(3));
        Assert.AreEqual(3, model.PositionOf(10));
        Assert.AreEqual(4, model.PositionOf(11));
        Assert.AreEqual(-1, model.PositionOf(6));
        Assert.AreEqual(-1, model.PositionOf(99));
    }

    [TestMethod]
    public void GetRanges_ReturnsAscendingItemIndexRanges()
    {
        var model = new TableViewSelectionModel();
        model.Select(10, 12);
        model.Select(0, 1);

        var ranges = model.GetRanges().ToList();

        Assert.AreEqual(2, ranges.Count);
        Assert.AreEqual(0, ranges[0].FirstIndex);
        Assert.AreEqual(2u, ranges[0].Length);
        Assert.AreEqual(10, ranges[1].FirstIndex);
        Assert.AreEqual(3u, ranges[1].Length);
    }

    [TestMethod]
    public void GetIndexes_EnumeratesAscending()
    {
        var model = new TableViewSelectionModel();
        model.Select(5, 6);
        model.Select(1, 2);

        CollectionAssert.AreEqual(new[] { 1, 2, 5, 6 }, model.GetIndexes().ToArray());
    }

    [TestMethod]
    public void OnItemsInserted_BeforeSelection_ShiftsItDown()
    {
        var model = new TableViewSelectionModel();
        model.Select(5, 7);

        model.OnItemsInserted(0, 2);

        Assert.AreEqual(7, model.FirstIndex);
        Assert.AreEqual(9, model.LastIndex);
        Assert.AreEqual(3, model.Count);
    }

    [TestMethod]
    public void OnItemsInserted_AfterSelection_LeavesItAlone()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 2);

        model.OnItemsInserted(10, 5);

        Assert.AreEqual(0, model.FirstIndex);
        Assert.AreEqual(2, model.LastIndex);
    }

    [TestMethod]
    public void OnItemsInserted_InsideSelection_SplitsAndLeavesNewItemsUnselected()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 5);

        model.OnItemsInserted(3, 2);

        Assert.AreEqual(2, model.RangeCount);
        Assert.AreEqual(6, model.Count);
        Assert.IsTrue(model.Contains(2));
        Assert.IsFalse(model.Contains(3));
        Assert.IsFalse(model.Contains(4));
        Assert.IsTrue(model.Contains(5));
        Assert.IsTrue(model.Contains(7));
    }

    [TestMethod]
    public void OnItemsRemoved_DropsRemovedItemsAndShiftsTheRest()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 2);
        model.Select(10, 12);

        Assert.IsFalse(model.OnItemsRemoved(5, 2));

        Assert.AreEqual(6, model.Count);
        Assert.IsTrue(model.Contains(0));
        Assert.IsTrue(model.Contains(8));
        Assert.IsTrue(model.Contains(10));
        Assert.IsFalse(model.Contains(12));
    }

    [TestMethod]
    public void OnItemsRemoved_OfSelectedItems_ReportsAChange()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 9);

        Assert.IsTrue(model.OnItemsRemoved(4, 2));

        Assert.AreEqual(8, model.Count);
        Assert.AreEqual(1, model.RangeCount, "the two halves become adjacent and should merge");
        Assert.AreEqual(0, model.FirstIndex);
        Assert.AreEqual(7, model.LastIndex);
    }

    [TestMethod]
    public void TrimTo_DropsSelectionPastTheEnd()
    {
        var model = new TableViewSelectionModel();
        model.Select(0, 20);

        Assert.IsTrue(model.TrimTo(10));

        Assert.AreEqual(10, model.Count);
        Assert.AreEqual(9, model.LastIndex);
        Assert.IsFalse(model.TrimTo(10));
        Assert.IsTrue(model.TrimTo(0));
        Assert.AreEqual(0, model.Count);
    }

    [TestMethod]
    public void Select_InvalidRange_IsIgnored()
    {
        var model = new TableViewSelectionModel();

        Assert.IsFalse(model.Select(5, 4));
        Assert.IsFalse(model.Select(-1, 3));
        Assert.AreEqual(0, model.Count);
    }
}
