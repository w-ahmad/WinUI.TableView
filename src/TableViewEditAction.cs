namespace WinUI.TableView;

/// <summary>
/// Specifies the action to take when editing a cell in TableView.
/// </summary>
public enum TableViewEditAction
{
    /// <summary>
    /// Cancel editing and revert to the original value.
    /// </summary>
    Cancel,

    /// <summary>
    /// Commit the edit and save the new value.
    /// </summary>
    Commit
}