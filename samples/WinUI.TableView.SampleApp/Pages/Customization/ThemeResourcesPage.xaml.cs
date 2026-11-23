using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ThemeResourcesPage : Page
{
    private const string Namespaces = """xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" """;

    // Each set is what goes inside <tv:TableView.Resources>; the page loads the chosen one with XamlReader.
    private static readonly Dictionary<string, string> _presets = new()
    {
        ["Accent"] = """
            <!--  Header row in the accent color  -->
            <StaticResource x:Key="TableViewHeaderRowBackground" ResourceKey="AccentFillColorDefaultBrush" />
            <StaticResource x:Key="TableViewHeaderForeground" ResourceKey="TextOnAccentFillColorPrimaryBrush" />

            <!--  Hovered and selected rows  -->
            <SolidColorBrush x:Key="TableViewRowBackgroundPointerOver" Color="{ThemeResource SystemAccentColor}" Opacity="0.12" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelected" Color="{ThemeResource SystemAccentColor}" Opacity="0.28" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelectedPointerOver" Color="{ThemeResource SystemAccentColor}" Opacity="0.36" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelectedPressed" Color="{ThemeResource SystemAccentColor}" Opacity="0.22" />
            """,
        ["Ledger"] = """
            <!--  Square corners and a green header, like a spreadsheet  -->
            <CornerRadius x:Key="TableViewRowCornerRadius">0</CornerRadius>
            <CornerRadius x:Key="TableViewRowCellSelectionBorderCornerRadius">0</CornerRadius>
            <SolidColorBrush x:Key="TableViewHeaderRowBackground" Color="#107C41" />
            <SolidColorBrush x:Key="TableViewHeaderForeground" Color="White" />

            <!--  Stronger grid lines  -->
            <StaticResource x:Key="TableViewHorizontalGridLineStroke" ResourceKey="ControlStrongStrokeColorDefaultBrush" />
            <StaticResource x:Key="TableViewVerticalGridLineStroke" ResourceKey="ControlStrongStrokeColorDefaultBrush" />

            <!--  Green selection  -->
            <SolidColorBrush x:Key="TableViewCellSelectionBrush" Color="#21A366" />
            <SolidColorBrush x:Key="TableViewRowSelectionIndicatorBrush" Color="#21A366" />
            <SolidColorBrush x:Key="TableViewRowBackgroundPointerOver" Color="#1A21A366" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelected" Color="#3321A366" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelectedPointerOver" Color="#4021A366" />
            """,
        ["Soft"] = """
            <!--  No lines, rounder rows  -->
            <SolidColorBrush x:Key="TableViewHorizontalGridLineStroke" Color="Transparent" />
            <SolidColorBrush x:Key="TableViewVerticalGridLineStroke" Color="Transparent" />
            <SolidColorBrush x:Key="TableViewHeaderRowBackground" Color="Transparent" />
            <CornerRadius x:Key="TableViewRowCornerRadius">10</CornerRadius>
            <CornerRadius x:Key="TableViewRowCellSelectionBorderCornerRadius">8</CornerRadius>

            <!--  Purple hover, selection and indicator  -->
            <SolidColorBrush x:Key="TableViewHeaderForeground" Color="#A98BD9" />
            <SolidColorBrush x:Key="TableViewCellSelectionBrush" Color="#8764B8" />
            <SolidColorBrush x:Key="TableViewRowSelectionIndicatorBrush" Color="#A98BD9" />
            <SolidColorBrush x:Key="TableViewRowBackgroundPointerOver" Color="#1F8764B8" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelected" Color="#408764B8" />
            <SolidColorBrush x:Key="TableViewRowBackgroundSelectedPointerOver" Color="#528764B8" />
            """
    };

    public ThemeResourcesPage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ExampleViewModel viewModel)
        {
            viewModel.Items = [.. ExampleViewModel.ItemsList.Take(100)];
            ApplyPreset();
        }
    }

    private void OnPresetChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyPreset();
    }

    /// <summary>
    /// Templates read theme resources when they are applied, so a new resource set needs a new table.
    /// </summary>
    private void ApplyPreset()
    {
        if (host is null || DataContext is not ExampleViewModel viewModel) return;

        var tableView = new TableView
        {
            DataContext = viewModel,
            SelectionMode = ListViewSelectionMode.Extended,
            SelectionUnit = TableViewSelectionUnit.CellOrRow
        };

        if (_presets.TryGetValue(preset.SelectedItem as string ?? string.Empty, out var xaml))
        {
            var resources = (ResourceDictionary)XamlReader.Load($"<ResourceDictionary {Namespaces}>{xaml}</ResourceDictionary>");

            tableView.Resources.MergedDictionaries.Add(resources);
            presenter.Xaml = $$"""
                <tv:TableView ItemsSource="{Binding Items}">
                    <tv:TableView.Resources>
                        {{xaml.ReplaceLineEndings("\n        ")}}
                    </tv:TableView.Resources>
                </tv:TableView>
                """;
        }
        else
        {
            presenter.Xaml = """<tv:TableView ItemsSource="{Binding Items}" />""";
        }

        tableView.AutoGeneratingColumn += ExampleModelColumnsHelper.OnAutoGeneratingColumns;
        tableView.ItemsSource = viewModel.Items;

        host.Content = tableView;
    }
}
