﻿using System;
using Windows.ApplicationModel.Resources;

namespace WinUI.TableView;

/// <summary>
/// Provides localized string resources for the TableView.
/// </summary>
internal partial class TableViewLocalizedStrings
{
    private const string WinUI_TableView = "WinUI.TableView";

    private static readonly ResourceLoader _appResourceLoader = new(WinUI_TableView);
    private static readonly ResourceLoader _defaultResourceLoader = new($"{WinUI_TableView}/{WinUI_TableView}");

    static TableViewLocalizedStrings()
    {
        BlankFilterValue = GetValue(nameof(BlankFilterValue));
        Cancel = GetValue(nameof(Cancel));
        ClearFilter = GetValue(nameof(ClearFilter));
        ClearSorting = GetValue(nameof(ClearSorting));
        Copy = GetValue(nameof(Copy));
        CopyCommandDescription = GetValue(nameof(CopyCommandDescription));
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
    }

    private static string GetValue(string name)
    {
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
    }

    public static string BlankFilterValue { get; set; }
    public static string Cancel { get; set; }
    public static string ClearFilter { get; set; }
    public static string ClearSorting { get; set; }
    public static string Copy { get; set; }
    public static string CopyCommandDescription { get; set; }
    public static string CopyWithHeaders { get; set; }
    public static string CopyWithHeadersCommandDescription { get; set; }
    public static string DatePickerPlaceholder { get; set; }
    public static string DeselectAll { get; set; }
    public static string DeselectAllCommandDescription { get; set; }
    public static string ExportAll { get; set; }
    public static string ExportSelected { get; set; }
    public static string Ok { get; set; }
    public static string SearchBoxPlaceholder { get; set; }
    public static string SelectAll { get; set; }
    public static string SelectAllCommandDescription { get; set; }
    public static string SelectAllParenthesized { get; set; }
    public static string SortAscending { get; set; }
    public static string SortDescending { get; set; }
    public static string TimePickerPlaceholder { get; set; }
}
