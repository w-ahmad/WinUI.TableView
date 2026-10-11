using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class ItemIndexRangeExtensionsTests
{
    private static TableView TableView => new() { ItemsSource = Enumerable.Range(0, 10).ToList() };

    [TestMethod]
    public void IsInRange_InsideRange_ReturnsTrue()
    {
        var range = new ItemIndexRange(2, 5);
        Assert.IsTrue(range.IsInRange(3));
    }

    [TestMethod]
    public void IsInRange_AtBoundaries_ReturnsTrue()
    {
        var range = new ItemIndexRange(2, 5);
        Assert.IsTrue(range.IsInRange(2));
        Assert.IsTrue(range.IsInRange(5));
    }

    [TestMethod]
    public void IsInRange_OutsideRange_ReturnsFalse()
    {
        var range = new ItemIndexRange(2, 5);
        Assert.IsFalse(range.IsInRange(1));
        Assert.IsFalse(range.IsInRange(7));
    }

    [UITestMethod]
    public void IsValid_ValidRange_ReturnsTrue()
    {
        var range = new ItemIndexRange(0, 4);
        Assert.IsTrue(range.IsValid(TableView));
    }

    [UITestMethod]
    public void IsValid_FirstIndexNegative_ReturnsFalse()
    {
        var range = new ItemIndexRange(-1, 4);
        Assert.IsFalse(range.IsValid(TableView));
    }

    [UITestMethod]
    public void IsValid_LastIndexTooLarge_ReturnsFalse()
    {
        var range = new ItemIndexRange(8, 4);
        Assert.IsFalse(range.IsValid(TableView));
    }
}
