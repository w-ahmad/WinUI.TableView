## Getting Started

### 1. Create a New Uno Project

If you don't already have a Uno project, create one in Visual Studio.

### 2: Install NuGet Package
Install [WinUI.TableView](https://www.nuget.org/packages/WinUI.TableView) NuGet package to your app with your preferred method. Here is the one using NuGet Package Manager:

```bash
Install-Package WinUI.TableView
```
### 3. Add `WinUI.TableView` to Your XAML

In your `MainPage.xaml`, add the `WinUI.TableView` control:

```xml
<Page x:Class="UnoApp1.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:tv="using:WinUI.TableView"      
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
    <Grid Margin="40">
        <tv:TableView AutoGenerateColumns="False"
                      ItemsSource="{x:Bind Items}">
            <tv:TableView.Columns>
                <tv:TableViewTextColumn Header="Name" Width="250" Binding="{Binding Name}" />
                <tv:TableViewNumberColumn Header="Price" Width="100" Binding="{Binding Price}" />
                <tv:TableViewNumberColumn Header="Quantity" Width="100" Binding="{Binding Quantity}" />
                <tv:TableViewDateColumn Header="Purchase Date" Width="140" Binding="{Binding PurchaseDate}" />
            </tv:TableView.Columns>
        </tv:TableView>
    </Grid>
</Page>
```

### 4. Bind Data to `TableView`

Create a simple Model class with properties to represent in TableView cells:

```csharp
public class Item : INotifyPropertyChanged
{
    private string? _name;
    private double _price;
    private int _quantity;
    private DateOnly _purchaseDate;

    public string? Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }
    public double Price
    {
        get => _price;
        set
        {
            _price = value;
            OnPropertyChanged(nameof(Price));
        }
    }
    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            OnPropertyChanged(nameof(Quantity));
        }
    }
    public DateOnly PurchaseDate
    {
        get => _purchaseDate;
        set
        {
            _purchaseDate = value;
            OnPropertyChanged(nameof(PurchaseDate));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

In your `MainPage.xaml.cs`, set up the data context and bind data to the `TableView`:

```csharp
public sealed partial class MainPage : Page
{
    public IList<Item> Items { get; }

    public MainPage()
    {
        this.InitializeComponent();

        Items = Enumerable.Range(1, 20).Select(i => GetRandomItem(i)).ToList();
    }

    private static Item GetRandomItem(int i)
    {
        return new Item
        {
            Name = $"Random Item {i}",
            Price = Math.Round(Random.Shared.NextDouble() * 100, 2),
            Quantity = Random.Shared.Next(1, 100),
            PurchaseDate = DateOnly.FromDateTime(DateTime.Today.AddDays(Random.Shared.Next(-90, 90)))
        };
    }
}
```

### 5. Run Your Application

Build and run your application. You should see the `WinUI.TableView` populated with the rows and cells from your `Items` collection. Here is the result by running the app on Desktop platform.

![Desktop](https://github.com/user-attachments/assets/9b338487-702c-4812-a8ec-29d49e54549c)
