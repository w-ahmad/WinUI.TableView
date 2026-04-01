using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class ConditionalStylingPage : Page
{
    private SalesViewModel ViewModel => (SalesViewModel)DataContext;
    private CancellationTokenSource? _liveUpdatesCST;

    public ConditionalStylingPage()
    {
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (!(SalesViewModel.SalesList.Count > 0))
        {
            App.Current.MainPage.SetLoading(true);
            await SalesViewModel.InitializeItemsAsync();
            App.Current.MainPage.SetLoading(false);
        }

        ViewModel.SalesData = [.. SalesViewModel.SalesList];
        liveUpdates.IsOn = true;
    }

    private void OnLiveUpdatesToggled(object sender, RoutedEventArgs e)
    {
        if (liveUpdates.IsOn)
        {
            _ = StartLiveUpdatesAsync();
        }
        else
        {
            _liveUpdatesCST?.Cancel();
            _liveUpdatesCST = null;
        }
    }

    private async Task StartLiveUpdatesAsync()
    {
        _liveUpdatesCST = new CancellationTokenSource();

        while (ViewModel.SalesData is not null && !_liveUpdatesCST.IsCancellationRequested)
        {
            await Task.Delay((int)updateInterval.Value, _liveUpdatesCST.Token);

            var region = DataFaker.Region();
            var target = DataFaker.Integer(5_000, 30_000);
            var sales = DataFaker.Integer((int)(target * 0.5), (int)(target * 1.2));
            var growth = DataFaker.Integer(-10, 10);
            var status = sales >= target ? (growth >= 0 ? "Ahead" : "On Track") : (growth < 0 ? "Behind" : "On Track");
            var item = ViewModel.SalesData[Random.Shared.Next(ViewModel.SalesData.Count)];

            item.Region = region;
            item.Target = target;
            item.Sales = sales;
            item.Growth = growth;
            item.Status = status;
        }
    }

    private Predicate<TableViewConditionalCellStyleContext> SalesMetTargetPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Sales) && context.DataItem is SalesExampleModel item && item?.Sales >= item?.Target;

    private Predicate<TableViewConditionalCellStyleContext> SalesBelowTargetPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Sales) && context.DataItem is SalesExampleModel item && item?.Sales < item?.Target;

    private Predicate<TableViewConditionalCellStyleContext> GrowthPositivePredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Growth) && context.DataItem is SalesExampleModel item && item?.Growth > 0;

    private Predicate<TableViewConditionalCellStyleContext> GrowthNegativePredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Growth) && context.DataItem is SalesExampleModel item && item?.Growth < 0;

    private Predicate<TableViewConditionalCellStyleContext> GrowthZeroPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Growth) && context.DataItem is SalesExampleModel item && item?.Growth == 0;

    private Predicate<TableViewConditionalCellStyleContext> LowTargetPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Target) && context.DataItem is SalesExampleModel item && item?.Target < 10_000;

    private Predicate<TableViewConditionalCellStyleContext> MediumTargetPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Target) && context.DataItem is SalesExampleModel item && item?.Target >= 10_000 && item?.Target < 20_000;

    private Predicate<TableViewConditionalCellStyleContext> HighTargetPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Target) && context.DataItem is SalesExampleModel item && item?.Target >= 20_000;

    private Predicate<TableViewConditionalCellStyleContext> OnTrackPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Status) && context.DataItem is SalesExampleModel item && item?.Status is "On Track";

    private Predicate<TableViewConditionalCellStyleContext> AheadPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Status) && context.DataItem is SalesExampleModel item && item?.Status is "Ahead";

    private Predicate<TableViewConditionalCellStyleContext> BehindPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Status) && context.DataItem is SalesExampleModel item && item?.Status is "Behind";

    private Predicate<TableViewConditionalCellStyleContext> RegionEastPredicate =>
       static context => context.Column.Header is nameof(SalesExampleModel.Region) && context.DataItem is SalesExampleModel item && item?.Region is "East";

    private Predicate<TableViewConditionalCellStyleContext> RegionWestPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Region) && context.DataItem is SalesExampleModel item && item?.Region is "West";

    private Predicate<TableViewConditionalCellStyleContext> RegionNorthPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Region) && context.DataItem is SalesExampleModel item && item?.Region is "North";

    private Predicate<TableViewConditionalCellStyleContext> RegionSouthPredicate =>
        static context => context.Column.Header is nameof(SalesExampleModel.Region) && context.DataItem is SalesExampleModel item && item?.Region is "South";
}
