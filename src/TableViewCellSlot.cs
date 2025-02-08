namespace WinUI.TableView;

/// <summary>
/// Represents a slot of a TableView cell, identified by its row and column indices.
/// </summary>
public readonly record struct TableViewCellSlot(int Row, int Column);
