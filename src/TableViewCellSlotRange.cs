namespace WinUI.TableView;

/// <summary>
/// Represents a coordinate-based range of cell slots in a TableView.
/// </summary>
public class TableViewCellSlotRange
{
    /// <summary>
    /// Gets the starting row index of the range.
    /// </summary>
    public int FirstRow { get; }

    /// <summary>
    /// Gets the starting column index of the range.
    /// </summary>
    public int FirstColumn { get; }

    /// <summary>
    /// Gets the first cell slot in the range.
    /// </summary>
    public TableViewCellSlot FirstSlot => new(FirstRow, FirstColumn);

    /// <summary>
    /// Gets the number of rows spanned by this range.
    /// </summary>
    public int Rows { get; }

    /// <summary>
    /// Gets the number of columns spanned by this range.
    /// </summary>
    public int Columns { get; }

    /// <summary>
    /// Gets the total number of cell slots in the range.
    /// </summary>
    public int Length => Rows * Columns;

    /// <summary>
    /// Gets the last row index included in the range.
    /// </summary>
    public int LastRow => FirstRow + Rows - 1;

    /// <summary>
    /// Gets the last column index included in the range.
    /// </summary>
    public int LastColumn => FirstColumn + Columns - 1;

    /// <summary>
    /// Gets the last cell slot in the range.
    /// </summary>
    public TableViewCellSlot LastSlot => new(LastRow, LastColumn);

    /// <summary>
    /// Initializes a new instance of the TableViewCellSlotRange class using starting indices and dimensions.
    /// </summary>
    public TableViewCellSlotRange(int firstRowIndex, int firstColumnIndex, int rowCount, int columnCount)
    {
        if (firstRowIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(firstRowIndex), "Index cannot be negative.");

        if (firstColumnIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(firstColumnIndex), "Index cannot be negative.");

        if (rowCount < 1)
            throw new ArgumentOutOfRangeException(nameof(rowCount), "Count must be at least 1.");
        if (columnCount < 1)
            throw new ArgumentOutOfRangeException(nameof(columnCount), "Count must be at least 1.");

        FirstRow = firstRowIndex;
        FirstColumn = firstColumnIndex;
        Rows = rowCount;
        Columns = columnCount;
    }

    /// <summary>
    /// Helper factory to initialize using start and end coordinates directly.
    /// </summary>
    public static TableViewCellSlotRange FromCoordinates(int startRow, int startCol, int endRow, int endCol)
    {
        var rowCount = Math.Abs(endRow - startRow) + 1;
        var colCount = Math.Abs(endCol - startCol) + 1;

        return new TableViewCellSlotRange(startRow, startCol, rowCount, colCount);
    }

    /// <summary>
    /// Helper factory to initialize using two TableViewCellSlot instances.
    /// </summary>
    public static TableViewCellSlotRange FromSlots(TableViewCellSlot firstSlot, TableViewCellSlot? lastSlot = default)
    {
        lastSlot ??= firstSlot;
        return FromCoordinates(firstSlot.Row, firstSlot.Column, lastSlot.Value.Row, lastSlot.Value.Column);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is TableViewCellSlotRange other)
        {
            return FirstRow == other.FirstRow &&
                   FirstColumn == other.FirstColumn &&
                   Rows == other.Rows &&
                   Columns == other.Columns;
        }
        return false;
    }

    /// <inheritdoc />
    public static bool operator ==(TableViewCellSlotRange? left, TableViewCellSlotRange? right)
    {
        return Equals(left, right);
    }

    /// <inheritdoc />
    public static bool operator !=(TableViewCellSlotRange? left, TableViewCellSlotRange? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(FirstRow, FirstColumn, Rows, Columns);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[{FirstSlot}]..[{LastSlot}] ({Length})";
    }
}