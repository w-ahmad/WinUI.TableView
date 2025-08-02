using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class TableViewCellSlotExtensionsTests
{
    [Fact]
    public void IsValidRow_WithValidRow_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3); // 5 rows, 3 columns
        var slot = new TableViewCellSlot(2, 1); // Valid row

        // Act
        var result = slot.IsValidRow(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidRow_WithFirstRow_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(0, 1); // First row

        // Act
        var result = slot.IsValidRow(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidRow_WithLastRow_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(4, 1); // Last row (index 4 for 5 items)

        // Act
        var result = slot.IsValidRow(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidRow_WithNegativeRow_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(-1, 1); // Invalid negative row

        // Act
        var result = slot.IsValidRow(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRow_WithRowBeyondCount_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(5, 1); // Row index 5 when only 0-4 exist

        // Act
        var result = slot.IsValidRow(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRow_WithNullTableView_ReturnsFalse()
    {
        // Arrange
        var slot = new TableViewCellSlot(0, 0);

        // Act
        var result = slot.IsValidRow(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidColumn_WithValidColumn_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3); // 5 rows, 3 columns
        var slot = new TableViewCellSlot(1, 2); // Valid column

        // Act
        var result = slot.IsValidColumn(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidColumn_WithFirstColumn_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(1, 0); // First column

        // Act
        var result = slot.IsValidColumn(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidColumn_WithLastColumn_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(1, 2); // Last column (index 2 for 3 columns)

        // Act
        var result = slot.IsValidColumn(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidColumn_WithNegativeColumn_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(1, -1); // Invalid negative column

        // Act
        var result = slot.IsValidColumn(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidColumn_WithColumnBeyondCount_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(1, 3); // Column index 3 when only 0-2 exist

        // Act
        var result = slot.IsValidColumn(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidColumn_WithNullTableView_ReturnsFalse()
    {
        // Arrange
        var slot = new TableViewCellSlot(0, 0);

        // Act
        var result = slot.IsValidColumn(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithValidSlot_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(2, 1); // Valid row and column

        // Act
        var result = slot.IsValid(tableView);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithValidBoundarySlot_ReturnsTrue()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot1 = new TableViewCellSlot(0, 0); // Top-left corner
        var slot2 = new TableViewCellSlot(4, 2); // Bottom-right corner

        // Act & Assert
        Assert.True(slot1.IsValid(tableView));
        Assert.True(slot2.IsValid(tableView));
    }

    [Fact]
    public void IsValid_WithInvalidRow_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(5, 1); // Invalid row, valid column

        // Act
        var result = slot.IsValid(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithInvalidColumn_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(2, 3); // Valid row, invalid column

        // Act
        var result = slot.IsValid(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithBothInvalid_ReturnsFalse()
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3);
        var slot = new TableViewCellSlot(5, 3); // Both invalid

        // Act
        var result = slot.IsValid(tableView);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNullTableView_ReturnsFalse()
    {
        // Arrange
        var slot = new TableViewCellSlot(0, 0);

        // Act
        var result = slot.IsValid(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithEmptyTableView_ReturnsFalse()
    {
        // Arrange
        var emptyTableView = CreateMockTableViewWithItemsAndColumns(0, 0);
        var slot = new TableViewCellSlot(0, 0);

        // Act
        var result = slot.IsValid(emptyTableView);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0, 0, true)]   // Top-left
    [InlineData(2, 1, true)]   // Middle
    [InlineData(4, 2, true)]   // Bottom-right
    [InlineData(-1, 0, false)] // Invalid row
    [InlineData(0, -1, false)] // Invalid column
    [InlineData(5, 2, false)]  // Row out of bounds
    [InlineData(2, 3, false)]  // Column out of bounds
    [InlineData(-1, -1, false)] // Both invalid
    public void IsValid_WithVariousSlots_ReturnsExpectedResult(int row, int column, bool expected)
    {
        // Arrange
        var tableView = CreateMockTableViewWithItemsAndColumns(5, 3); // 5 rows, 3 columns
        var slot = new TableViewCellSlot(row, column);

        // Act
        var result = slot.IsValid(tableView);

        // Assert
        Assert.Equal(expected, result);
    }

    // Helper method to create a mock TableView with specified items and columns
    private static TableView CreateMockTableViewWithItemsAndColumns(int itemCount, int columnCount)
    {
        var tableView = new TableView();
        
        // Create mock items
        var items = new List<object>();
        for (int i = 0; i < itemCount; i++)
        {
            items.Add(new { Index = i, Name = $"Item {i}" });
        }
        tableView.ItemsSource = items;

        // Create mock columns - need to simulate visible columns
        // Since we can't easily mock the internal structure, we'll assume the extension
        // method works correctly with the Columns.VisibleColumns.Count property
        // This is more of an integration test approach
        for (int i = 0; i < columnCount; i++)
        {
            var column = new TableViewTextColumn
            {
                Header = $"Column {i}",
                Binding = $"Property{i}",
                Visibility = Microsoft.UI.Xaml.Visibility.Visible
            };
            tableView.Columns.Add(column);
        }

        return tableView;
    }
}