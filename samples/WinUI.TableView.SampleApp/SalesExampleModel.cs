using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace WinUI.TableView.SampleApp;

public partial class SalesExampleModel : ObservableObject
{
    [ObservableProperty]
    public partial string? Employee { get; set; }

    [ObservableProperty]
    public partial string? Region { get; set; }

    [ObservableProperty]
    public partial int Sales { get; set; }

    [ObservableProperty]
    public partial int Target { get; set; }

    [ObservableProperty]
    public partial int Growth { get; set; }

    [ObservableProperty]
    public partial string? Status { get; set; }
}

