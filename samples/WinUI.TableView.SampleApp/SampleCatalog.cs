using WinUI.TableView.SampleApp.Pages;

namespace WinUI.TableView.SampleApp;

public sealed class SampleInfo(string title, string glyph, Type pageType, string description)
{
    public string Title { get; } = title;

    public string Glyph { get; } = glyph;

    public Type PageType { get; } = pageType;

    public string Description { get; } = description;

    /// <summary>
    /// The library version the sample was added in, as "major.minor", e.g. "1.6".
    /// </summary>
    public string? AddedIn { get; set; }

    /// <summary>
    /// Whether the sample is shown as new: from when it is added until a release after the one it was added in.
    /// </summary>
    public bool IsNew => Version.TryParse(AddedIn, out var addedIn) && addedIn >= ReleaseInfo.LatestRelease;

    /// <summary>
    /// The folder of the page's source files, relative to the sample app project.
    /// </summary>
    public string Folder { get; set; } = "Pages";

    public SampleGroup Group { get; internal set; } = null!;
}

public sealed class SampleGroup(string title, string glyph, string description, params SampleInfo[] samples)
{
    public string Title { get; } = title;

    public string Glyph { get; } = glyph;

    public string Description { get; } = description;

    public List<SampleInfo> Samples { get; } = [.. samples];
}

/// <summary>
/// Every sample page, grouped the way the navigation menu, the home page and search show them.
/// </summary>
public static class SampleCatalog
{
    private const string ShowcasesFolder = "Pages/Showcases";
    private const string CustomizationFolder = "Pages/Customization";

    public static SampleGroup Showcases { get; } = new("Showcases", "",
        "Complete, real-world screens built on TableView, each with its full source.",
        new("Sales Pipeline", "", typeof(SalesPipelinePage),
            "A CRM deal ledger with KPI cards, stage pills, probability bars, grouping and density switching.") { AddedIn = "1.6", Folder = ShowcasesFolder },
        new("Market Watchlist", "", typeof(MarketWatchlistPage),
            "A live stock watchlist with ticking prices, sparklines, range bars and favorite toggles in row headers.") { AddedIn = "1.6", Folder = ShowcasesFolder },
        new("Issue Tracker", "", typeof(IssueTrackerPage),
            "An editable sprint backlog grouped by status, with custom group headers, label chips and row details.") { AddedIn = "1.6", Folder = ShowcasesFolder },
        new("Inbox", "", typeof(InboxPage),
            "A mail client: unread styling, flags, a filter bar and a reading pane driven by the selected row.") { AddedIn = "1.6", Folder = ShowcasesFolder });

    public static SampleGroup Fundamentals { get; } = new("Fundamentals", "",
        "The basics: binding data, column types, selection, editing and the clipboard.",
        new("Overview", "", typeof(OverviewPage),
            "The default TableView: auto-generated columns, sorting, filtering and editing out of the box."),
        new("Column Types", "", typeof(ColumnTypesPage),
            "Every built-in column type side by side: text, number, check box, combo box, date, time, toggle switch, hyperlink and template.") { AddedIn = "1.6" },
        new("Selection", "", typeof(SelectionPage),
            "Row, cell and mixed selection units with single, multiple and extended modes."),
        new("Editing", "", typeof(EditingPage),
            "In-place cell editing with type-specific editors and read-only columns."),
        new("Clipboard Actions", "", typeof(ClipboardActionsPage),
            "Copy and paste rows and cell ranges, with or without headers."),
        new("Context Flyouts", "", typeof(ContextFlyoutsPage),
            "Row and cell context menus that know which row and cell were clicked."),
        new("Data Export", "", typeof(ExportPage),
            "Built-in CSV export and a custom Excel export of all or selected content."));

    public static SampleGroup DataOperations { get; } = new("Sorting, Filtering & Grouping", "",
        "Shape what the table shows: sort, filter, group and page through data.",
        new("Sorting", "", typeof(SortingPage),
            "Single and multi-column sorting from the column headers."),
        new("Custom Sorting", "", typeof(CustomizeSortingPage),
            "Take over the Sorting event to sort on the server or with your own comparer."),
        new("Filtering", "", typeof(FilteringPage),
            "Excel-like filter flyouts on every column header."),
        new("Customize Filter Flyout", "", typeof(CustomizeFilterPage),
            "Replace the items a filter flyout offers with your own filter handler."),
        new("External Filtering", "", typeof(ExternalFilteringPage),
            "Filter from a search box outside the table with FilterDescriptions."),
        new("Grouping", "", typeof(GroupingPage),
            "Group by one or more columns with collapsible, sticky group headers."),
        new("Pagination", "", typeof(PaginationPage),
            "Page through a large collection with a pager control."));

    public static SampleGroup ColumnsAndRows { get; } = new("Columns & Rows", "",
        "Size, freeze, reorder and extend columns and rows.",
        new("Column Sizing", "", typeof(ColumnSizingPage),
            "Fixed, auto and resizable column widths, plus auto-width modes."),
        new("Star Columns", "", typeof(StarColumnsPage),
            "Proportional star-sized columns that share the available width."),
        new("Frozen Columns", "", typeof(FrozenColumnsPage),
            "Keep leading columns in view while the rest scroll horizontally.") { AddedIn = "1.6" },
        new("Column Reorder", "", typeof(ReorderColumnsPage),
            "Drag column headers to reorder, with events to veto a move."),
        new("Row Reorder", "", typeof(ReorderRowsPage),
            "Drag rows to reorder the items of the bound collection."),
        new("Headers Visibility", "", typeof(HeadersVisibilityPage),
            "Show or hide the column headers and the row headers."),
        new("Corner Button", "", typeof(CornerButtonPage),
            "Choose what the top-left corner button does: select all, options, or nothing."),
        new("Row Header Template", "", typeof(RowHeaderTemplatePage),
            "Put row numbers, avatars or any content in the row headers."),
        new("Row Details", "", typeof(RowDetailsPage),
            "Expand a row to show a details section under it."));

    public static SampleGroup Customization { get; } = new("Styling & Customization", "",
        "Make TableView yours: styles, templates and theme resources for every part.",
        new("Header Styling", "", typeof(HeaderStylingPage),
            "Restyle column headers for the whole table or a single column, and put any content in a header.") { AddedIn = "1.6", Folder = CustomizationFolder },
        new("Cell Styling", "", typeof(CellStylingPage),
            "Cell, element and editing-element styles at the table and the column level.") { AddedIn = "1.6", Folder = CustomizationFolder },
        new("Template Columns", "", typeof(TemplateColumnsPage),
            "Cell and editing templates: ratings, progress, avatars and tags inside cells.") { AddedIn = "1.6", Folder = CustomizationFolder },
        new("Group Header Templates", "", typeof(GroupHeaderTemplatesPage),
            "Replace the group header content and restyle its container with GroupStyle.") { AddedIn = "1.6", Folder = CustomizationFolder },
        new("Theme Resources", "", typeof(ThemeResourcesPage),
            "Lightweight styling: recolor rows, selection, headers and grid lines by overriding resources.") { AddedIn = "1.6", Folder = CustomizationFolder },
        new("Conditional Cell Styling", "", typeof(ConditionalStylingPage),
            "Style cells by their values with predicates that re-evaluate as data changes."),
        new("Alternate Row Color", "", typeof(AlternateRowColorPage),
            "Alternating row background and foreground colors."),
        new("Grid Lines", "", typeof(GridLinesPage),
            "Grid line visibility, thickness and color for rows and headers."),
        new("Compact Sizing", "", typeof(CompactSizingPage),
            "Fluent compact density with a unified font family and size."));

    public static SampleGroup Performance { get; } = new("Performance", "",
        "Virtualization and on-demand loading for very large data.",
        new("Virtualization", "", typeof(VirtualizationPage),
            "A million rows: scroll, jump and select with only the visible rows realized."),
        new("Incremental Loading", "", typeof(IncrementalLoadingPage),
            "Fetch more items as the user scrolls toward the end."));

    public static IReadOnlyList<SampleGroup> Groups { get; } =
        [Showcases, Fundamentals, DataOperations, ColumnsAndRows, Customization, Performance];

    public static IEnumerable<SampleInfo> Samples => Groups.SelectMany(group => group.Samples);

    static SampleCatalog()
    {
#if !WINDOWS
        // Uno resolves the library's theme resources app-wide, so a table's own Resources can't override them there.
        Customization.Samples.RemoveAll(sample => sample.PageType == typeof(ThemeResourcesPage));
#endif

        foreach (var group in Groups)
        {
            foreach (var sample in group.Samples)
            {
                sample.Group = group;
            }
        }
    }

    public static SampleInfo? Find(Type pageType)
    {
        return Samples.FirstOrDefault(sample => sample.PageType == pageType);
    }

    public static List<SampleInfo> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return [.. Samples
            .Where(sample => terms.All(term => sample.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                                            || sample.Description.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(sample => sample.Title.Contains(terms[0], StringComparison.OrdinalIgnoreCase))];
    }
}
