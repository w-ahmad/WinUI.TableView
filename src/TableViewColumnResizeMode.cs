namespace WinUI.TableView;

/// <summary>
/// Specifies how a column behaves while the user drags to resize it.
/// </summary>
public enum TableViewColumnResizeMode
{
    /// <summary>
    /// Cells appear to resize live via a composition-only preview (Clip/RenderTransform) — no real
    /// layout runs until the drag ends, keeping the drag smooth regardless of row count. The real
    /// width is committed once, when the pointer is released.
    /// </summary>
    Preview,

    /// <summary>
    /// The column's real width updates on every pointer-move frame, so every visible row's cell goes
    /// through a real layout pass during the drag. Simpler and fully "real", at the cost of frame
    /// rate on grids with many visible rows. This is the default.
    /// </summary>
    Live
}
