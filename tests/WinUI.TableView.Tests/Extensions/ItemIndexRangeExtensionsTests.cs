using Microsoft.UI.Xaml.Data;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class ItemIndexRangeExtensionsTests
{
    [Fact]
    public void IsInRange_WithIndexWithinRange_ReturnsTrue()
    {
        // Arrange
        var range = new ItemIndexRange(5, 10u); // Range from 5 to 14 (inclusive)

        // Act & Assert
        Assert.True(range.IsInRange(5));  // First index
        Assert.True(range.IsInRange(10)); // Middle index
        Assert.True(range.IsInRange(14)); // Last index (5 + 10 - 1)
        Assert.True(range.IsInRange(7));  // Random index within range
    }

    [Fact]
    public void IsInRange_WithIndexOutsideRange_ReturnsFalse()
    {
        // Arrange
        var range = new ItemIndexRange(5, 10u); // Range from 5 to 14 (inclusive)

        // Act & Assert
        Assert.False(range.IsInRange(4));  // Before range
        Assert.False(range.IsInRange(15)); // After range
        Assert.False(range.IsInRange(0));  // Way before range
        Assert.False(range.IsInRange(100)); // Way after range
    }

    [Fact]
    public void IsInRange_WithSingleItemRange_ReturnsCorrectResult()
    {
        // Arrange
        var range = new ItemIndexRange(7, 1u); // Single item at index 7

        // Act & Assert
        Assert.True(range.IsInRange(7));   // Exact match
        Assert.False(range.IsInRange(6));  // Before
        Assert.False(range.IsInRange(8));  // After
    }

    [Fact]
    public void IsInRange_WithZeroLengthRange_ReturnsFalse()
    {
        // Arrange
        var range = new ItemIndexRange(5, 0u); // Zero length range

        // Act & Assert
        Assert.False(range.IsInRange(5));  // Even the start index should return false
        Assert.False(range.IsInRange(4));  // Before
        Assert.False(range.IsInRange(6));  // After
    }

    [Fact]
    public void IsInRange_WithNegativeIndex_ReturnsFalse()
    {
        // Arrange
        var range = new ItemIndexRange(0, 10u); // Range from 0 to 9

        // Act & Assert
        Assert.False(range.IsInRange(-1));
        Assert.False(range.IsInRange(-5));
        Assert.True(range.IsInRange(0));   // Boundary check
    }

    [Fact]
    public void IsValid_WithValidRange_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItems(20); // 20 items
        var range = new ItemIndexRange(0, 10u);  // Range from 0 to 9
        var range2 = new ItemIndexRange(5, 15u); // Range from 5 to 19
        var range3 = new ItemIndexRange(19, 1u); // Single item at last index

        // Act & Assert
        Assert.True(range.IsValid(tableView));
        Assert.True(range2.IsValid(tableView));
        Assert.True(range3.IsValid(tableView));
    }

    [Fact]
    public void IsValid_WithInvalidRange_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItems(10); // 10 items (0-9)
        var rangeStartingBeforeZero = new ItemIndexRange(-1, 5u);  // Invalid start
        var rangeEndingAfterCount = new ItemIndexRange(5, 10u);    // Range from 5 to 14, but only 0-9 exist
        var rangeStartingAfterCount = new ItemIndexRange(15, 5u);  // Start beyond available items

        // Act & Assert
        Assert.False(rangeStartingBeforeZero.IsValid(tableView));
        Assert.False(rangeEndingAfterCount.IsValid(tableView));
        Assert.False(rangeStartingAfterCount.IsValid(tableView));
    }

    [Fact]
    public void IsValid_WithEmptyTableView_ReturnsFalse()
    {
        // Arrange
        var emptyTableView = CreateMockTableViewWithItems(0); // No items
        var range = new ItemIndexRange(0, 1u);

        // Act & Assert
        Assert.False(range.IsValid(emptyTableView));
    }

    [Fact]
    public void IsValid_WithZeroLengthRange_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItems(10);
        var range = new ItemIndexRange(5, 0u); // Zero length

        // Act & Assert
        Assert.False(range.IsValid(tableView));
    }

    [Fact]
    public void IsValid_WithNullTableView_ReturnsFalse()
    {
        // Arrange
        var range = new ItemIndexRange(0, 5u);

        // Act & Assert
        Assert.False(range.IsValid(null!));
    }

    [Fact]
    public void IsValid_WithBoundaryConditions_ReturnsCorrectResult()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItems(10); // Items 0-9
        var exactFitRange = new ItemIndexRange(0, 10u);    // Exactly all items
        var lastItemRange = new ItemIndexRange(9, 1u);     // Last item only

        // Act & Assert
        Assert.True(exactFitRange.IsValid(tableView));
        Assert.True(lastItemRange.IsValid(tableView));
    }

    [Theory]
    [InlineData(0, 1u, 0, true)]    // First item
    [InlineData(4, 1u, 4, true)]    // Middle item
    [InlineData(9, 1u, 9, true)]    // Last item
    [InlineData(0, 5u, 2, true)]    // Range including index
    [InlineData(3, 4u, 5, true)]    // Range including index
    [InlineData(5, 3u, 4, false)]   // Index before range
    [InlineData(5, 3u, 8, false)]   // Index after range
    public void IsInRange_WithVariousRangesAndIndices_ReturnsExpectedResult(
        int firstIndex, uint length, int testIndex, bool expected)
    {
        // Arrange
        var range = new ItemIndexRange(firstIndex, length);

        // Act
        var result = range.IsInRange(testIndex);

        // Assert
        Assert.Equal(expected, result);
    }

    // Helper method to create a mock TableView with specified number of items
    private static TableView CreateMockTableViewWithItems(int itemCount)
    {
        var tableView = new TableView();
        
        // Create a mock items collection
        var items = new List<object>();
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new { Index = i, Name = $"Item {i}" });
        }
        
        // Set the items source
        tableView.ItemsSource = items;
        
        return tableView;
    }
}