using CommunityToolkit.Mvvm.ComponentModel;
namespace WinUI.TableView.SampleApp;

/// <summary>
/// Represents a single transaction record from the CSV sample data.
/// </summary>
public partial class TransactionModel : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial DateTime Date { get; set; }

    [ObservableProperty]
    public partial int ClientId { get; set; }

    [ObservableProperty]
    public partial int CardId { get; set; }

    [ObservableProperty]
    public partial decimal Amount { get; set; }

    [ObservableProperty]
    public partial string? UseChip { get; set; }

    [ObservableProperty]
    public partial int MerchantId { get; set; }

    [ObservableProperty]
    public partial string? MerchantCity { get; set; }

    [ObservableProperty]
    public partial string? MerchantState { get; set; }

    [ObservableProperty]
    public partial string Zip { get; set; }

    [ObservableProperty]
    public partial int Mcc { get; set; }

    [ObservableProperty]
    public partial string? Errors { get; set; }
}

