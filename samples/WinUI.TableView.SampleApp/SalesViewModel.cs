using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace WinUI.TableView.SampleApp;

public partial class SalesViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            SalesList.Clear();

            for (var i = 0; i < 20; i++)
            {
                var target = DataFaker.Integer(5_000, 30_000);
                var sales = DataFaker.Integer((int)(target * 0.5), (int)(target * 1.2));
                var growth = DataFaker.Integer(-10, 10);
                var status = sales >= target ? (growth >= 0 ? "Ahead" : "On Track") : (growth < 0 ? "Behind" : "On Track");

                SalesList.Add(new SalesExampleModel
                {
                    Employee = DataFaker.FullName(),
                    Region = DataFaker.Region(),
                    Target = target,
                    Sales = sales,
                    Growth = growth,
                    Status = status
                });
            }
        });
    }

    [ObservableProperty]
    public partial IList<SalesExampleModel>? SalesData { get; set; }

    public static IList<SalesExampleModel> SalesList { get; set; } = [];
}