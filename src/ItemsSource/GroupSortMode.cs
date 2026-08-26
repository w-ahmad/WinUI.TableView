namespace WinUI.TableView;

/// <summary>
/// Specifies what a <see cref="GroupDescription"/> orders its groups by.
/// </summary>
public enum GroupSortMode
{
    /// <summary>
    /// Groups are ordered by their key value (default).
    /// </summary>
    Key = 0,

    /// <summary>
    /// Groups are ordered by the number of items they contain.
    /// </summary>
    Count = 1
}
