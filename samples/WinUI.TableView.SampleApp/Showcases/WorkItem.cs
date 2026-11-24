using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections;
using System.Collections.ObjectModel;
using Windows.UI;

namespace WinUI.TableView.SampleApp.Showcases;

#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class WorkItem : ObservableObject
{
    public string Key { get; set; } = string.Empty;

    public string Type { get; set; } = WorkItemData.Types[0];

    public string Reporter { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<WorkItemLabel> Labels { get; set; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Status { get; set; } = WorkItemData.Statuses[0];

    [ObservableProperty]
    public partial string Priority { get; set; } = WorkItemData.Priorities[2];

    [ObservableProperty]
    public partial string Assignee { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Points { get; set; }

    [ObservableProperty]
    public partial DateOnly Due { get; set; }
}

public sealed class WorkItemLabel(string name)
{
    public string Name { get; } = name;
}

/// <summary>
/// Colors and glyphs for the issue tracker's cell and group header templates.
/// </summary>
public static class WorkItemVisuals
{
    public static string TypeGlyph(string type) => type switch
    {
        "Bug" => "",
        "Feature" => "",
        "Docs" => "",
        _ => ""
    };

    public static Brush TypeBrush(string type) => Palette.Solid(type switch
    {
        "Bug" => Palette.Red,
        "Feature" => Palette.Green,
        "Docs" => Palette.Purple,
        _ => Palette.Blue
    });

    public static Color StatusColor(string? status) => status switch
    {
        "In progress" => Palette.Blue,
        "In review" => Palette.Purple,
        "Done" => Palette.Green,
        _ => Palette.Gray
    };

    public static Brush StatusBrush(string status) => Palette.Solid(StatusColor(status));

    public static Brush StatusTint(string status) => Palette.Tint(StatusColor(status));

    public static string PriorityGlyph(string priority) => priority switch
    {
        "Critical" => "",
        "High" => "",
        "Low" => "",
        _ => ""
    };

    public static Brush PriorityBrush(string priority) => Palette.Solid(priority switch
    {
        "Critical" => Palette.Red,
        "High" => Palette.Orange,
        "Low" => Palette.Gray,
        _ => Palette.Amber
    });

    public static Brush LabelTint(string label) => Palette.Tint(Palette.Avatar(label.Length + label.FirstOrDefault()).Color, 52);

    public static Brush AvatarBrush(string name) => Palette.Avatar(name.Length * 7 + name.FirstOrDefault());

    public static Visibility AssignedVisibility(string assignee) => Format.CollapsedIf(string.IsNullOrEmpty(assignee));

    public static Visibility UnassignedVisibility(string assignee) => Format.VisibleIf(string.IsNullOrEmpty(assignee));

    // Group headers hand the key over as an object, whichever column the table is grouped by.
    public static Brush GroupBrush(object? key) => key is string text && WorkItemData.Statuses.Contains(text)
        ? Palette.Solid(StatusColor(text))
        : Palette.Avatar($"{key}".Length);

    public static string GroupLabel(object? key) => string.IsNullOrEmpty($"{key}") ? "Unassigned" : $"{key}";
}

public static class WorkItemData
{
    public static List<string> Types { get; } = ["Task", "Bug", "Feature", "Docs"];

    public static List<string> Statuses { get; } = ["Backlog", "In progress", "In review", "Done"];

    public static List<string> Priorities { get; } = ["Critical", "High", "Medium", "Low"];

    public static List<string> Assignees { get; } =
        ["Aisha Khan", "Diego Morales", "Emma Walsh", "Jun Bae", "Kwame Mensah", "Priya Raman", "Sofia Rossi"];

    private static readonly string[] _labels =
        ["grouping", "sorting", "filtering", "editing", "a11y", "performance", "styling", "uno", "aot", "clipboard", "export"];

    private static readonly (string Type, string Title)[] _titles =
    [
        ("Bug", "Sticky group header overlaps the first row after a collapse"),
        ("Bug", "Frozen column loses its shadow when the theme changes"),
        ("Feature", "Add a summary row with per-column aggregates"),
        ("Task", "Raise the row recycling pool limit for tall rows"),
        ("Bug", "Date editor commits on Escape in the Japanese calendar"),
        ("Feature", "Drag a column header to the group panel"),
        ("Docs", "Document the lightweight styling resource keys"),
        ("Task", "Measure scroll frame times with one million rows"),
        ("Bug", "Paste ignores the last column of a ragged selection"),
        ("Feature", "Persist column order, widths and sorting"),
        ("Docs", "Write the migration guide from DataGrid"),
        ("Task", "Add UI tests for keyboard range selection"),
        ("Bug", "Filter flyout search box loses focus while typing"),
        ("Feature", "Let a template column take part in filtering"),
        ("Task", "Trim reflection from the export path"),
        ("Bug", "Row details stay expanded after the row is recycled"),
        ("Feature", "Inline validation errors for edited cells"),
        ("Docs", "Add a styling cookbook to the docs site"),
        ("Task", "Profile the WebAssembly build after the Uno upgrade"),
        ("Bug", "Narrator reads the wrong header for reordered columns"),
        ("Feature", "Multi-level group headers with indentation"),
        ("Task", "Audit template parts for Native AOT wrappers"),
        ("Bug", "Auto-width ignores the sort icon in narrow headers"),
        ("Feature", "Copy cells as an HTML table"),
    ];

    /// <summary>
    /// Orders status groups the way work flows, not alphabetically.
    /// </summary>
    public static IComparer StatusOrder { get; } = Comparer<object?>.Create(
        (x, y) => Statuses.IndexOf($"{x}").CompareTo(Statuses.IndexOf($"{y}")));

    public static IComparer PriorityOrder { get; } = Comparer<object?>.Create(
        (x, y) => Priorities.IndexOf($"{x}").CompareTo(Priorities.IndexOf($"{y}")));

    public static ObservableCollection<WorkItem> Create()
    {
        var random = new Random(482);
        var items = new ObservableCollection<WorkItem>();

        for (var i = 0; i < 96; i++)
        {
            var (type, title) = _titles[i % _titles.Length];
            var status = Statuses[random.NextDouble() switch { < 0.34 => 0, < 0.6 => 1, < 0.76 => 2, _ => 3 }];

            items.Add(new WorkItem
            {
                Key = $"TV-{482 - i}",
                Type = type,
                Title = title,
                Status = status,
                Priority = Priorities[random.NextDouble() switch { < 0.08 => 0, < 0.33 => 1, < 0.78 => 2, _ => 3 }],
                Assignee = status == "Backlog" && random.NextDouble() < 0.4 ? string.Empty : Assignees[random.Next(Assignees.Count)],
                Reporter = Assignees[random.Next(Assignees.Count)],
                Points = new[] { 1, 2, 3, 5, 8, 13 }[random.Next(6)],
                Due = new DateOnly(2026, 11, 19).AddDays(random.Next(-6, 45)),
                Labels = [.. _labels.OrderBy(_ => random.Next()).Take(random.Next(1, 3)).Select(label => new WorkItemLabel(label))],
                Description = $"{title}. Reproduced on the latest build; see the linked discussion for the steps, the expected result and what was tried so far."
            });
        }

        return items;
    }
}
