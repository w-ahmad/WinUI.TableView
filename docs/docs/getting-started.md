# Getting Started

This guide will walk you through the process of setting up and using WinUI.TableView in your WinUI 3 application. Follow the steps below to quickly integrate a powerful and flexible DataGrid into your project.

### 1. Create a New WinUI3

If you don't already have a WinUI 3 project, create one in Visual Studio.

### 2. Install NuGet Package
Install [WinUI.TableView](https://www.nuget.org/packages/WinUI.TableView) NuGet package to your app with your preferred method. Here is the one using NuGet Package Manager:

```bash
Install-Package WinUI.TableView
```
### 3. Add `WinUI.TableView` to Your XAML

> [!TIP]
> Make sure to read the [docs](https://github.com/w-ahmad/WinUI.TableView/tree/main/docs) to learn more about TableView.

In your `MainWindow.xaml`, add the `WinUI.TableView` control:

```xml
<Window
    x:Class="App1.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:tv="using:WinUI.TableView"
    mc:Ignorable="d"
    Title="App1">

    <Window.SystemBackdrop>
        <MicaBackdrop />
    </Window.SystemBackdrop>

    <Grid>
        <tv:TableView ItemsSource="{x:Bind Items}" />
    </Grid>
</Window>
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

In your `MainWindow.xaml.cs`, set up the data context and bind data to the `TableView`:

```csharp
public sealed partial class MainWindow : Window
{
    public IList Items { get; }

    public MainWindow()
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


![image](https://github.com/user-attachments/assets/e00bffc7-19e0-40bd-bbda-07198d6bc60a)