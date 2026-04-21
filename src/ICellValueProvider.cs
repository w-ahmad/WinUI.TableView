namespace WinUI.TableView;

/// <summary>
/// Provides value resolution for generated TableView sort, display, and clipboard paths.
/// </summary>
public interface ICellValueProvider
{
    /// <summary>
    /// Tries to resolve a cell display value for the specified <see cref="TableViewBoundColumn.Binding"/>.
    /// </summary>
    /// <param name="path">The member path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful.</param>
    /// <returns><see langword="true"/> when a value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetBindingValue(string? path, object? item, out object? value);

    /// <summary>
    /// Tries to set a cell value for the specified <see cref="TableViewBoundColumn.Binding"/>.
    /// </summary>
    /// <param name="path">The member path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">The value to set.</param>
    /// <returns><see langword="true"/> when a value was set; otherwise <see langword="false"/>.</returns>
    bool TrySetBindingValue(string? path, object? item, object? value);

    /// <summary>
    /// Tries to resolve a sort value for the specified <see cref="TableViewColumn.SortMemberPath"/>.
    /// </summary>
    /// <param name="path">The member path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful.</param>
    /// <returns><see langword="true"/> when a value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetSortMemberValue(string? path, object? item, out object? value);

    /// <summary>
    /// Tries to resolve clipboard content for the specified <see cref="TableViewColumn.SortMemberPath"/>.
    /// </summary>
    /// <param name="path">The member path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful.</param>
    /// <returns><see langword="true"/> when a value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetClipboardContentBindingValue(string? path, object? item, out object? value);

    /// <summary>
    /// Tries to resolve a content value for the specified <see cref="TableViewHyperlinkColumn.ContentBinding"/>.
    /// </summary>
    /// <param name="path">The content binding path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful.</param>
    /// <returns><see langword="true"/> when a value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetContentBindingValue(string? path, object? item, out object? value);

    /// <summary>
    /// Tries to resolve a display member value for the combo box column when the column's <see cref="TableViewComboBoxColumn.DisplayMemberPath"/> is used to specified.
    /// </summary>
    /// <param name="path">The member path to resolve.</param>
    /// <param name="item">The row item instance.</param>
    /// <param name="value">When this method returns, contains the resolved value if successful.</param>
    /// <returns><see langword="true"/> when a value was resolved; otherwise <see langword="false"/>.</returns>
    bool TryGetDisplayMemberValue(string? path, object? item, out object? value);
}

