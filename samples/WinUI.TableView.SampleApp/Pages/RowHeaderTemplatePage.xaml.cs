using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class RowHeaderTemplatePage : Page
{
    public RowHeaderTemplatePage()
    {
        InitializeComponent();
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        viewModel.Items = [.. ExampleViewModel.ItemsList.Take(40)];
    }

    private const string AvatarXaml = """
        <tv:TableView.RowHeaderTemplate>
            <DataTemplate x:DataType="local:ExampleModel">
                <Grid MinWidth="48" Padding="4">
                    <Border Width="28" Height="28" CornerRadius="14"
                            HorizontalAlignment="Center"
                            Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                        <Image Source="{x:Bind Avatar}" Stretch="UniformToFill" />
                    </Border>
                </Grid>
            </DataTemplate>
        </tv:TableView.RowHeaderTemplate>
        """;

    private const string StatusXaml = """
        <tv:TableView.RowHeaderTemplate>
            <DataTemplate x:DataType="local:ExampleModel">
                <Grid MinWidth="32">
                    <Ellipse Width="10" Height="10"
                             HorizontalAlignment="Center" VerticalAlignment="Center"
                             StrokeThickness="1.5"
                             Stroke="{ThemeResource ControlStrongStrokeColorDefaultBrush}" />
                    <Ellipse Width="10" Height="10"
                             HorizontalAlignment="Center" VerticalAlignment="Center"
                             Fill="{ThemeResource SystemFillColorSuccessBrush}"
                             Visibility="{x:Bind IsActive, Mode=OneWay}" />
                </Grid>
            </DataTemplate>
        </tv:TableView.RowHeaderTemplate>
        """;

    private void OnRowHeaderContentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tableView is null || presenter is null) return;

        tableView.ShowRowNumbers = rowHeaderContent.SelectedIndex is 1;
        tableView.RowHeaderTemplate = rowHeaderContent.SelectedIndex switch
        {
            2 => (DataTemplate)Resources["AvatarHeaderTemplate"],
            3 => (DataTemplate)Resources["StatusHeaderTemplate"],
            _ => null
        };

        presenter.Xaml = rowHeaderContent.SelectedIndex switch
        {
            1 => """<tv:TableView ItemsSource="{Binding Items}" ShowRowNumbers="True" />""",
            2 => AvatarXaml,
            3 => StatusXaml,
            _ => """<tv:TableView ItemsSource="{Binding Items}" />"""
        };
    }
}
