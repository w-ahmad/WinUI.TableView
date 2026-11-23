using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinUI.TableView.Extensions;
using WinUI.TableView.Selection;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewCellSlotCollectionTests
{
    [TestMethod]
    public void ASelectionLargerThanAListCanCount_DoesNotOverflow()
    {
        var range = new TableViewCellSlotRange(0, 0, 1_000_000, 3_000);
        var slots = new TableViewCellSlotCollection([range]);

        Assert.AreEqual(int.MaxValue, range.Length);
        Assert.AreEqual(int.MaxValue, slots.Count);
        Assert.AreEqual(new TableViewCellSlot(0, 0), slots[0]);
        Assert.AreEqual(new TableViewCellSlot(1, 2), slots[3_002]);
        Assert.AreEqual(3_002, slots.IndexOf(new TableViewCellSlot(1, 2)));
        Assert.AreEqual(-1, slots.IndexOf(new TableViewCellSlot(999_999, 2_999)));
        Assert.IsTrue(slots.Contains(new TableViewCellSlot(999_999, 2_999)));
        Assert.IsTrue(range.IsInRange(new TableViewCellSlot(999_999, 2_999)));
    }
}
