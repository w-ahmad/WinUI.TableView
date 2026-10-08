using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Globalization;

namespace WinUI.TableView;

/// <summary>
/// Provides localized string resources for the TableView.
/// </summary>
internal partial class TableViewLocalizedStrings
{
    private const string WinUI_TableView = "WinUI.TableView";
#if WINDOWS
    private static readonly ResourceManager _resourceManager = new(); 
#else
    private static readonly ResourceLoader _appResourceLoader = new(WinUI_TableView);
    private static readonly ResourceLoader _defaultResourceLoader = new($"{WinUI_TableView}/{WinUI_TableView}");
#endif

    static TableViewLocalizedStrings()
    {
        BlankFilterValue = GetValue(nameof(BlankFilterValue));
        Cancel = GetValue(nameof(Cancel));
        ClearFilter = GetValue(nameof(ClearFilter));
        ClearSorting = GetValue(nameof(ClearSorting));
        Copy = GetValue(nameof(Copy));
        CopyCommandDescription = GetValue(nameof(CopyCommandDescription));
        Paste = GetValue(nameof(Paste));
        PasteCommandDescription = GetValue(nameof(PasteCommandDescription));
        CopyWithHeaders = GetValue(nameof(CopyWithHeaders));
        CopyWithHeadersCommandDescription = GetValue(nameof(CopyWithHeadersCommandDescription));
        DatePickerPlaceholder = GetValue(nameof(DatePickerPlaceholder));
        DeselectAll = GetValue(nameof(DeselectAll));
        DeselectAllCommandDescription = GetValue(nameof(DeselectAllCommandDescription));
        ExportAll = GetValue(nameof(ExportAll));
        ExportSelected = GetValue(nameof(ExportSelected));
        Ok = GetValue(nameof(Ok));
        SearchBoxPlaceholder = GetValue(nameof(SearchBoxPlaceholder));
        SelectAll = GetValue(nameof(SelectAll));
        SelectAllCommandDescription = GetValue(nameof(SelectAllCommandDescription));
        SelectAllParenthesized = GetValue(nameof(SelectAllParenthesized));
        SortAscending = GetValue(nameof(SortAscending));
        SortDescending = GetValue(nameof(SortDescending));
        TimePickerPlaceholder = GetValue(nameof(TimePickerPlaceholder));
        Filtered = GetValue(nameof(Filtered));
        Group = GetValue(nameof(Group));
        Ungroup = GetValue(nameof(Ungroup));
        UngroupAll = GetValue(nameof(UngroupAll));
        SortGroupsByCount = GetValue(nameof(SortGroupsByCount));
        SortGroupsByValue = GetValue(nameof(SortGroupsByValue));
        TableViewControlType = GetValue(nameof(TableViewControlType));
        ColumnHeaderControlType = GetValue(nameof(ColumnHeaderControlType));
        RowHeaderControlType = GetValue(nameof(RowHeaderControlType));
        CellControlType = GetValue(nameof(CellControlType));
        RowNumber = GetValue(nameof(RowNumber));
        _resourceRowNumber = GetValue(nameof(RowNumber));
        Row = GetValue(nameof(Row));
    }

    private static string GetValue(string name)
    {
#if WINDOWS
        var value = _resourceManager.MainResourceMap.TryGetValue($"{WinUI_TableView}/{name}");
        value ??= _resourceManager.MainResourceMap.GetValue($"{WinUI_TableView}/{WinUI_TableView}/{name}");

        return value.ValueAsString; 
#else
        if (_appResourceLoader.GetString(name) is { Length: > 0 } appValue)
        {
            return appValue;
        }
        else if (_defaultResourceLoader.GetString(name) is { Length: > 0 } defaultValue)
        {
            return defaultValue;
        }
        else
        {
            throw new InvalidOperationException("Should not happen.");
        }
#endif
    }

    public static string BlankFilterValue { get => Resolve(nameof(BlankFilterValue), field); set; }
    public static string Cancel { get => Resolve(nameof(Cancel), field); set; }
    public static string ClearFilter { get => Resolve(nameof(ClearFilter), field); set; }
    public static string ClearSorting { get => Resolve(nameof(ClearSorting), field); set; }
    public static string Copy { get => Resolve(nameof(Copy), field); set; }
    public static string CopyCommandDescription { get => Resolve(nameof(CopyCommandDescription), field); set; }
    public static string Paste { get => Resolve(nameof(Paste), field); set; }
    public static string PasteCommandDescription { get => Resolve(nameof(PasteCommandDescription), field); set; }
    public static string CopyWithHeaders { get => Resolve(nameof(CopyWithHeaders), field); set; }
    public static string CopyWithHeadersCommandDescription { get => Resolve(nameof(CopyWithHeadersCommandDescription), field); set; }
    public static string DatePickerPlaceholder { get => Resolve(nameof(DatePickerPlaceholder), field); set; }
    public static string DeselectAll { get => Resolve(nameof(DeselectAll), field); set; }
    public static string DeselectAllCommandDescription { get => Resolve(nameof(DeselectAllCommandDescription), field); set; }
    public static string ExportAll { get => Resolve(nameof(ExportAll), field); set; }
    public static string ExportSelected { get => Resolve(nameof(ExportSelected), field); set; }
    public static string Ok { get => Resolve(nameof(Ok), field); set; }
    public static string SearchBoxPlaceholder { get => Resolve(nameof(SearchBoxPlaceholder), field); set; }
    public static string SelectAll { get => Resolve(nameof(SelectAll), field); set; }
    public static string SelectAllCommandDescription { get => Resolve(nameof(SelectAllCommandDescription), field); set; }
    public static string SelectAllParenthesized { get => Resolve(nameof(SelectAllParenthesized), field); set; }
    public static string SortAscending { get => Resolve(nameof(SortAscending), field); set; }
    public static string SortDescending { get => Resolve(nameof(SortDescending), field); set; }
    public static string TimePickerPlaceholder { get => Resolve(nameof(TimePickerPlaceholder), field); set; }
    public static string Filtered { get => Resolve(nameof(Filtered), field); set; }
    public static string Group { get => Resolve(nameof(Group), field); set; }
    public static string Ungroup { get => Resolve(nameof(Ungroup), field); set; }
    public static string UngroupAll { get => Resolve(nameof(UngroupAll), field); set; }
    public static string SortGroupsByCount { get => Resolve(nameof(SortGroupsByCount), field); set; }
    public static string SortGroupsByValue { get => Resolve(nameof(SortGroupsByValue), field); set; }
    public static string TableViewControlType { get => Resolve(nameof(TableViewControlType), field); set; }
    public static string ColumnHeaderControlType { get => Resolve(nameof(ColumnHeaderControlType), field); set; }
    public static string RowHeaderControlType { get => Resolve(nameof(RowHeaderControlType), field); set; }
    public static string CellControlType { get => Resolve(nameof(CellControlType), field); set; }
    public static string RowNumber { get => Resolve(nameof(RowNumber), field); set; }
    public static string Row { get => Resolve(nameof(Row), field); set; }

    /// <summary>
    /// Formats the UI Automation name of a row from its one-based number, e.g. "Row 3".
    /// </summary>
    public static string FormatRowNumber(int oneBasedRowNumber)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, RowNumber, oneBasedRowNumber);
        }
        catch (FormatException)
        {
            // A host-supplied format that is not a valid composite format must not take down a UI
            // Automation call; fall back to this library's own resource.
            return string.Format(CultureInfo.CurrentCulture, _resourceRowNumber, oneBasedRowNumber);
        }
    }

    private static string _resourceRowNumber = "Row {0}";

    /// <summary>
    /// The host application's string for <paramref name="key"/> when it supplies one through
    /// <see cref="TableViewLocalization.StringResolver"/>; otherwise <paramref name="resourceValue"/>,
    /// this library's own resource. Read on every access, so a host that changes language at run time
    /// is followed without restarting.
    /// </summary>
    private static string Resolve(string key, string resourceValue)
    {
        return TableViewLocalization.TryResolve(key) is { Length: > 0 } hostValue ? hostValue : resourceValue;
    }
}
