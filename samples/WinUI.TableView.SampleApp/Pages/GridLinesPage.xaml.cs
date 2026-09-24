using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class GridLinesPage : Page
{
    public GridLinesPage()
    {
        InitializeComponent();
    }

    private void OnBorderThicknessChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        tableView.BorderThickness = new Thickness(args.NewValue);
    }

    private void OnCornerRadiusChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        tableView.CornerRadius = new CornerRadius(e.NewValue);
    }

}
