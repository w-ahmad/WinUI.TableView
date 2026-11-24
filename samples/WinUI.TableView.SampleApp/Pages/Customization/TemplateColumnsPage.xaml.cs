using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinUI.TableView.SampleApp.Showcases;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class TemplateColumnsPage : Page
{
    public TemplateColumnsPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            viewModel.Items = [.. ExampleViewModel.ItemsList.Take(200)];
        }
    }
}

/// <summary>
/// What the cell templates draw for a value; they call these through x:Bind.
/// </summary>
public static class EmployeeVisuals
{
    private static readonly Color _active = Color.FromArgb(255, 16, 137, 62);
    private static readonly Color _away = Color.FromArgb(255, 122, 132, 144);

    public static List<string?> Departments { get; } =
        [.. ExampleViewModel.ItemsList.Select(item => item.Department).Distinct().Order()];

    public static Brush DepartmentBrush(string? department)
    {
        var color = Palette.Avatar(Departments.IndexOf(department)).Color;

        return Palette.Tint(color, 56);
    }

    public static string StatusText(bool isActive) => isActive ? "Active" : "Away";

    public static Brush StatusBrush(bool isActive) => Palette.Solid(isActive ? _active : _away);

    public static Brush StatusTint(bool isActive) => Palette.Tint(isActive ? _active : _away);

    public static string Birthday(DateOnly dob) => dob.ToString("MMMM d");

    public static string Age(DateOnly dob)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dob.Year - (today < dob.AddYears(today.Year - dob.Year) ? 1 : 0);

        return $"turns {age + 1}";
    }
}
