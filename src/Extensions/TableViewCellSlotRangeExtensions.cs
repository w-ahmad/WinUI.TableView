namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the TableViewCellSlotRange type.
/// </summary>
internal static class TableViewCellSlotRangeExtensions
{
    /// <summary>
    /// Determines whether a specified cell slot is within the range.
    /// </summary>
    /// <param name="range">The TableViewCellSlotRange to check.</param>
    /// <param name="slot">The cell slot to check.</param>
    /// <returns>True if the slot is within the range; otherwise, false.</returns>
    public static bool IsInRange(this TableViewCellSlotRange? range, TableViewCellSlot slot)
    {
        if (range is null || range.Length <= 0) return false;

        var minRow = Math.Min(range.FirstRow, range.LastRow);
        var maxRow = Math.Max(range.FirstRow, range.LastRow);
        var minColumn = Math.Min(range.FirstColumn, range.LastColumn);
        var maxColumn = Math.Max(range.FirstColumn, range.LastColumn);

        return slot.Row >= minRow && slot.Row <= maxRow
            && slot.Column >= minColumn && slot.Column <= maxColumn;
    }

    /// <summary>
    /// Determines whether a specified row index is within the range.
    /// </summary>
    /// <param name="range">The TableViewCellSlotRange to check.</param>
    /// <param name="row">The row index to check.</param>
    /// <returns>True if the row index is within the range; otherwise, false.</returns>
    public static bool IsRowInRange(this TableViewCellSlotRange? range, int row)
    {
        return range?.Length > 0
            && row >= Math.Min(range.FirstRow, range.LastRow)
            && row <= Math.Max(range.FirstRow, range.LastRow);
    }

    /// <summary>
    /// Determines whether a specified column index is within the range.
    /// </summary>
    /// <param name="range">The TableViewCellSlotRange to check.</param>
    /// <param name="column">The column index to check.</param>
    /// <returns>True if the column index is within the range; otherwise, false.</returns>
    public static bool IsColumnInRange(this TableViewCellSlotRange? range, int column)
    {
        return range?.Length > 0
            && column >= Math.Min(range.FirstColumn, range.LastColumn)
            && column <= Math.Max(range.FirstColumn, range.LastColumn);
    }

    /// <summary>
    /// Determines whether the given cell slot range is valid within the TableView.
    /// </summary>
    /// <param name="range">The TableViewCellSlotRange to check.</param>
    /// <param name="tableView">The TableView to check against.</param>
    /// <returns>True if the cell slot range of TableView is valid; otherwise, false.</returns>
    public static bool IsValid(this TableViewCellSlotRange range, TableView tableView)
    {
        return range.FirstSlot.IsValid(tableView) && range.LastSlot.IsValid(tableView);
    }

    /// <summary>
    /// Returns all cell slots contained within this range, enumerated row by row.
    /// </summary>
    public static IEnumerable<TableViewCellSlot> GetSlots(this TableViewCellSlotRange range)
    {
        for (var row = range.FirstRow; row <= range.LastRow; row++)
        {
            for (var col = range.FirstColumn; col <= range.LastColumn; col++)
            {
                yield return new TableViewCellSlot(row, col);
            }
        }
    }

    /// <summary>
    /// Determines whether a specific cell slot falls within this range.
    /// </summary>
    public static bool Contains(this TableViewCellSlotRange range, int rowIndex, int columnIndex)
    {
        return rowIndex >= range.FirstRow && rowIndex <= range.LastRow &&
               columnIndex >= range.FirstColumn && columnIndex <= range.LastColumn;
    }

    /// <summary>
    /// Determines whether another TableViewCellSlotRange is completely contained within this range.
    /// </summary>
    public static bool Contains(this TableViewCellSlotRange? range, TableViewCellSlotRange? other)
    {
        if (range == null || other == null) return false;

        return range.Contains(other.FirstRow, other.FirstColumn) &&
            range.Contains(other.LastRow, other.LastColumn);
    }

    /// <summary>
    /// Determines whether another range intersects with this range.
    /// </summary>
    public static bool IntersectsWith(this TableViewCellSlotRange range, TableViewCellSlotRange other)
    {
        if (other == null) return false;

        return range.FirstRow <= other.LastRow && range.LastRow >= other.FirstRow &&
               range.FirstColumn <= other.LastColumn && range.LastColumn >= other.FirstColumn;
    }

    /// <summary>
    /// Subtracts another range from this range and returns the resulting ranges.
    /// </summary>
    /// <param name="range">The range to subtract from.</param>
    /// <param name="other">The range to subtract.</param>
    /// <returns>An enumerable of resulting ranges after subtraction.</returns>
    public static IEnumerable<TableViewCellSlotRange> Subtract(this TableViewCellSlotRange range, TableViewCellSlotRange other)
    {
        // No overlap.
        if (!range.IntersectsWith(other))
        {
            yield return range;
            yield break;
        }

        // Intersection rectangle.
        var top = Math.Max(range.FirstRow, other.FirstRow);
        var left = Math.Max(range.FirstColumn, other.FirstColumn);
        var bottom = Math.Min(range.LastRow, other.LastRow);
        var right = Math.Min(range.LastColumn, other.LastColumn);

        // Top strip.
        if (range.FirstRow < top)
        {
            yield return new TableViewCellSlotRange(
                range.FirstRow,
                range.FirstColumn,
                top - range.FirstRow,
                range.Columns);
        }

        // Bottom strip.
        if (bottom < range.LastRow)
        {
            yield return new TableViewCellSlotRange(
                bottom + 1,
                range.FirstColumn,
                range.LastRow - bottom,
                range.Columns);
        }

        // Left strip.
        if (range.FirstColumn < left)
        {
            yield return new TableViewCellSlotRange(
                top,
                range.FirstColumn,
                bottom - top + 1,
                left - range.FirstColumn);
        }

        // Right strip.
        if (right < range.LastColumn)
        {
            yield return new TableViewCellSlotRange(
                top,
                right + 1,
                bottom - top + 1,
                range.LastColumn - right);
        }
    }

    /// <summary>
    /// Subtracts every range in <paramref name="others"/> from <paramref name="range"/> and returns what is left.
    /// </summary>
    /// <param name="range">The range to subtract from.</param>
    /// <param name="others">The ranges to subtract.</param>
    /// <returns>The disjoint ranges covering the part of <paramref name="range"/> none of the others cover.</returns>
    public static List<TableViewCellSlotRange> SubtractAll(this TableViewCellSlotRange range,
                                                          IEnumerable<TableViewCellSlotRange> others)
    {
        List<TableViewCellSlotRange> remainder = [range];
        List<TableViewCellSlotRange> buffer = [];

        foreach (var other in others)
        {
            if (remainder.Count is 0)
            {
                break;
            }

            buffer.Clear();

            foreach (var part in remainder)
            {
                buffer.AddRange(part.Subtract(other));
            }

            (remainder, buffer) = (buffer, remainder);
        }

        return remainder;
    }

    /// <summary>
    /// Merges two TableViewCellSlotRanges into a single range that encompasses both.
    /// </summary>
    public static TableViewCellSlotRange Merge(this TableViewCellSlotRange range, TableViewCellSlotRange other)
    {
        var firstRow = Math.Min(range.FirstRow, other.FirstRow);
        var firstColumn = Math.Min(range.FirstColumn, other.FirstColumn);
        var lastRow = Math.Max(range.LastRow, other.LastRow);
        var lastColumn = Math.Max(range.LastColumn, other.LastColumn);

        return TableViewCellSlotRange.FromCoordinates(
            firstRow,
            firstColumn,
            lastRow,
            lastColumn);
    }
}