using CommunityToolkit.Mvvm.ComponentModel;

namespace WinUI.TableView.SampleApp;

public partial class TransactionsViewModel : ObservableObject
{
    public async static Task InitializeItemsAsync()
    {
        await Task.Run(() =>
        {
            var startId = 7_475_327;
            var startDate = new DateTime(2010, 1, 1);

            TransacationsList.Clear();

            for (var i = 0; i < 1_000_000; i++)
            {
                TransacationsList.Add(new TransactionModel
                {
                    Id = startId++,
                    Date = startDate.AddMinutes(i),
                    ClientId = DataFaker.Integer(100, 2000),
                    CardId = DataFaker.Integer(1, 6000),
                    Amount = DataFaker.Decimal(10, 5000) * (DataFaker.Boolean(0.1f) ? -1 : 1),
                    UseChip = DataFaker.Boolean(0.2f) ? "Online Transaction" : "Swipe Transaction",
                    MerchantId = DataFaker.Integer(1000, 99999),
                    MerchantCity = DataFaker.City(),
                    MerchantState = DataFaker.State(),
                    Zip = DataFaker.ZipCode(),
                    Mcc = DataFaker.Integer(1700, 9500)
                });
            }
        });
    }

    [ObservableProperty]
    public partial IList<TransactionModel>? TransacationsData { get; set; }

    public static IList<TransactionModel> TransacationsList { get; set; } = [];
}