using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinUI.TableView.Layout;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewRowHeightsTests
{
    [TestMethod]
    public void UniformRows_OffsetsAndIndexesAreArithmetic()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 1_000_000 };

        Assert.AreEqual(0, heights.RecordedCount);
        Assert.AreEqual(40_000_000d, heights.TotalHeight);
        Assert.AreEqual(0d, heights.GetOffset(0));
        Assert.AreEqual(400d, heights.GetOffset(10));
        Assert.AreEqual(40_000_000d, heights.GetOffset(1_000_000));

        Assert.AreEqual(0, heights.GetIndexAt(0d));
        Assert.AreEqual(0, heights.GetIndexAt(39.9d));
        Assert.AreEqual(1, heights.GetIndexAt(40d));
        Assert.AreEqual(250_000, heights.GetIndexAt(10_000_000d));
        Assert.AreEqual(999_999, heights.GetIndexAt(40_000_000d));
        Assert.AreEqual(999_999, heights.GetIndexAt(99_999_999d));
    }

    [TestMethod]
    public void SetHeight_MatchingTheDefault_IsNotRecorded()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };

        Assert.IsFalse(heights.SetHeight(3, 40d));
        Assert.AreEqual(0, heights.RecordedCount);
        Assert.AreEqual(400d, heights.TotalHeight);
    }

    [TestMethod]
    public void SetHeight_DifferingFromTheDefault_ChangesTheExtent()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };

        Assert.IsTrue(heights.SetHeight(3, 100d));

        Assert.AreEqual(1, heights.RecordedCount);
        Assert.AreEqual(100d, heights.GetHeight(3));
        Assert.AreEqual(40d, heights.GetHeight(2));
        Assert.AreEqual(460d, heights.TotalHeight);

        Assert.AreEqual(120d, heights.GetOffset(3), "rows 0..2 are still the default height");
        Assert.AreEqual(220d, heights.GetOffset(4), "row 3 contributes its recorded height");
        Assert.AreEqual(260d, heights.GetOffset(5));

        Assert.IsFalse(heights.SetHeight(3, 100d), "no change means no report");
    }

    [TestMethod]
    public void SetHeight_BackToTheDefault_DropsTheRecord()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };
        heights.SetHeight(3, 100d);

        Assert.IsTrue(heights.SetHeight(3, 40d));

        Assert.AreEqual(0, heights.RecordedCount);
        Assert.AreEqual(400d, heights.TotalHeight);
    }

    [TestMethod]
    public void GetIndexAt_WithRecordedHeights_FindsTheContainingRow()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };
        heights.SetHeight(2, 200d);
        heights.SetHeight(7, 80d);

        // rows: 0,1 = 40 | 2 = 200 | 3..6 = 40 | 7 = 80 | 8,9 = 40
        Assert.AreEqual(0d, heights.GetOffset(0));
        Assert.AreEqual(80d, heights.GetOffset(2));
        Assert.AreEqual(280d, heights.GetOffset(3));
        Assert.AreEqual(440d, heights.GetOffset(7));
        Assert.AreEqual(520d, heights.GetOffset(8));
        Assert.AreEqual(600d, heights.TotalHeight);

        Assert.AreEqual(0, heights.GetIndexAt(0d));
        Assert.AreEqual(1, heights.GetIndexAt(40d));
        Assert.AreEqual(2, heights.GetIndexAt(80d));
        Assert.AreEqual(2, heights.GetIndexAt(279d));
        Assert.AreEqual(3, heights.GetIndexAt(280d));
        Assert.AreEqual(6, heights.GetIndexAt(439d));
        Assert.AreEqual(7, heights.GetIndexAt(440d));
        Assert.AreEqual(7, heights.GetIndexAt(519d));
        Assert.AreEqual(8, heights.GetIndexAt(520d));
        Assert.AreEqual(9, heights.GetIndexAt(599d));
    }

    [TestMethod]
    public void OffsetAndIndex_RoundTripForEveryRow()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 32d, Count = 50 };
        heights.SetHeight(0, 10d);
        heights.SetHeight(17, 90d);
        heights.SetHeight(18, 15d);
        heights.SetHeight(49, 120d);

        for (var index = 0; index < heights.Count; index++)
        {
            var offset = heights.GetOffset(index);

            Assert.AreEqual(index, heights.GetIndexAt(offset), $"row {index} start");
            Assert.AreEqual(index, heights.GetIndexAt(offset + (heights.GetHeight(index) / 2d)), $"row {index} middle");
        }
    }

    [TestMethod]
    public void ChangingDefaultHeight_KeepsRecordedHeightsAbsolute()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 4 };
        heights.SetHeight(1, 100d);

        Assert.AreEqual(220d, heights.TotalHeight);

        heights.DefaultHeight = 50d;

        Assert.AreEqual(100d, heights.GetHeight(1));
        Assert.AreEqual(250d, heights.TotalHeight);
    }

    [TestMethod]
    public void OnRowsInserted_ShiftsRecordedHeights()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 5 };
        heights.SetHeight(3, 100d);

        heights.OnRowsInserted(1, 2);

        Assert.AreEqual(7, heights.Count);
        Assert.AreEqual(100d, heights.GetHeight(5));
        Assert.AreEqual(40d, heights.GetHeight(3));
        Assert.AreEqual(340d, heights.TotalHeight);
    }

    [TestMethod]
    public void OnRowsInserted_AfterTheRecordedRow_LeavesItAlone()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 5 };
        heights.SetHeight(1, 100d);

        heights.OnRowsInserted(4, 1);

        Assert.AreEqual(100d, heights.GetHeight(1));
        Assert.AreEqual(6, heights.Count);
    }

    [TestMethod]
    public void OnRowsRemoved_DropsAndShiftsRecordedHeights()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };
        heights.SetHeight(2, 100d);
        heights.SetHeight(8, 200d);

        heights.OnRowsRemoved(1, 3);

        Assert.AreEqual(7, heights.Count);
        Assert.AreEqual(40d, heights.GetHeight(2), "the recorded row 2 was removed");
        Assert.AreEqual(200d, heights.GetHeight(5), "row 8 shifted down by 3");
        Assert.AreEqual((7 * 40d) + 160d, heights.TotalHeight);
    }

    [TestMethod]
    public void Reset_ForgetsEveryRecordedHeight()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 10 };
        heights.SetHeight(2, 100d);

        heights.Reset(3);

        Assert.AreEqual(3, heights.Count);
        Assert.AreEqual(0, heights.RecordedCount);
        Assert.AreEqual(120d, heights.TotalHeight);
    }

    [TestMethod]
    public void EmptyStore_IsWellBehaved()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 0 };

        Assert.AreEqual(0d, heights.TotalHeight);
        Assert.AreEqual(0d, heights.GetOffset(0));
        Assert.AreEqual(0d, heights.GetOffset(5));
        Assert.AreEqual(0, heights.GetIndexAt(0d));
        Assert.AreEqual(0, heights.GetIndexAt(1000d));
    }

    [TestMethod]
    public void SetHeight_IgnoresNonsenseValues()
    {
        var heights = new TableViewRowHeights { DefaultHeight = 40d, Count = 5 };

        Assert.IsFalse(heights.SetHeight(-1, 100d));
        Assert.IsFalse(heights.SetHeight(1, double.NaN));
        Assert.IsFalse(heights.SetHeight(1, double.PositiveInfinity));
        Assert.IsFalse(heights.SetHeight(1, -5d));
        Assert.AreEqual(0, heights.RecordedCount);
    }
}
