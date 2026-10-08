using System;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Lets the host application supply the TableView's user-facing strings from its own resources,
/// in place of the ones this library ships.
/// </summary>
/// <remarks>
/// <para>The library ships its strings in <c>WinUI.TableView.resw</c> for a fixed set of languages. An
/// application that supports a language the library does not, or that wants its own wording (for
/// example the UI Automation control types and row names a screen reader announces), sets
/// <see cref="StringResolver"/>. It is asked for a key on every read, so an application that changes
/// language while running is followed.</para>
/// <para>The keys are the resource names in <c>WinUI.TableView.resw</c> (see <see cref="Keys"/>). The
/// resolver returns <see langword="null"/> or an empty string to keep the library's own value. A
/// resolver that throws is treated as returning <see langword="null"/>: these strings are read inside
/// UI Automation calls, which must not fail. <c>RowNumber</c> is a composite format with one
/// placeholder, <c>{0}</c>, for the one-based row number; a value that is not a valid format falls
/// back to the library's own.</para>
/// </remarks>
public static class TableViewLocalization
{
    /// <summary>
    /// Gets or sets the function the TableView asks for each user-facing string, by resource key.
    /// <see langword="null"/> (the default) uses the library's own resources only.
    /// </summary>
    public static Func<string, string?>? StringResolver { get; set; }

    /// <summary>
    /// Gets every key the TableView asks <see cref="StringResolver"/> for.
    /// </summary>
    /// <remarks>
    /// Listed here by name rather than read off <see cref="TableViewLocalizedStrings"/>, so asking for the keys
    /// does not load the library's resources (which needs a running app).
    /// </remarks>
    public static IReadOnlyList<string> Keys { get; } = Array.AsReadOnly(new[]
    {
        nameof(TableViewLocalizedStrings.BlankFilterValue),
        nameof(TableViewLocalizedStrings.Cancel),
        nameof(TableViewLocalizedStrings.ClearFilter),
        nameof(TableViewLocalizedStrings.ClearSorting),
        nameof(TableViewLocalizedStrings.Copy),
        nameof(TableViewLocalizedStrings.CopyCommandDescription),
        nameof(TableViewLocalizedStrings.Paste),
        nameof(TableViewLocalizedStrings.PasteCommandDescription),
        nameof(TableViewLocalizedStrings.CopyWithHeaders),
        nameof(TableViewLocalizedStrings.CopyWithHeadersCommandDescription),
        nameof(TableViewLocalizedStrings.DatePickerPlaceholder),
        nameof(TableViewLocalizedStrings.DeselectAll),
        nameof(TableViewLocalizedStrings.DeselectAllCommandDescription),
        nameof(TableViewLocalizedStrings.ExportAll),
        nameof(TableViewLocalizedStrings.ExportSelected),
        nameof(TableViewLocalizedStrings.Ok),
        nameof(TableViewLocalizedStrings.SearchBoxPlaceholder),
        nameof(TableViewLocalizedStrings.SelectAll),
        nameof(TableViewLocalizedStrings.SelectAllCommandDescription),
        nameof(TableViewLocalizedStrings.SelectAllParenthesized),
        nameof(TableViewLocalizedStrings.SortAscending),
        nameof(TableViewLocalizedStrings.SortDescending),
        nameof(TableViewLocalizedStrings.TimePickerPlaceholder),
        nameof(TableViewLocalizedStrings.Filtered),
        nameof(TableViewLocalizedStrings.Group),
        nameof(TableViewLocalizedStrings.Ungroup),
        nameof(TableViewLocalizedStrings.UngroupAll),
        nameof(TableViewLocalizedStrings.SortGroupsByCount),
        nameof(TableViewLocalizedStrings.SortGroupsByValue),
        nameof(TableViewLocalizedStrings.TableViewControlType),
        nameof(TableViewLocalizedStrings.ColumnHeaderControlType),
        nameof(TableViewLocalizedStrings.RowHeaderControlType),
        nameof(TableViewLocalizedStrings.CellControlType),
        nameof(TableViewLocalizedStrings.RowNumber),
        nameof(TableViewLocalizedStrings.Row),
    });

    /// <summary>
    /// The host's value for <paramref name="key"/>, or <see langword="null"/> when there is no resolver,
    /// it supplies nothing, or it throws.
    /// </summary>
    internal static string? TryResolve(string key)
    {
        var resolver = StringResolver;
        if (resolver is null)
        {
            return null;
        }

        try
        {
            return resolver(key);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
