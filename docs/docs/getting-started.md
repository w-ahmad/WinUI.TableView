## Getting Started

### 1. Create a New WinUI3 or Uno Project

If you don't already have a WinUI 3 project or Uno prject, create one in Visual Studio.

### 2: Install NuGet Package
Inatall `WinUI.TableView` NuGet package to your app with your preferred method. Here is the one using NuGet Package Manager:

```bash
Install-Package WinUI.TableView
```
### 3. Add `WinUI.TableView` to Your XAML

In your `MainWindow.xaml`, add the `WinUI.TableView` control:

```xaml
<Window
    x:Class="App1.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:App1"
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    xmlns:tv="using:WinUI.TableView">

    <Grid>
        <tv:TableView x:Name="MyTableView"
            ItemsSource="{x:Bind ViewModel.Items}" />
    </Grid>
</Window>
```

### 4. Bind Data to `TableView`

In your `MainPage.xaml.cs`, set up the data context and bind data to the `TableView`:

```csharp
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; } = new MainViewModel();

    public MainWindow()
    {
        this.InitializeComponent();
    }
}
```

Create a simple `MainViewModel` with a collection of items to display:

```csharp
public class MainViewModel
{
    public ObservableCollection<Item> Items { get; set; }

    public MainViewModel()
    {
        Items = new ObservableCollection<Item>
        {
            new Item { Name = "Item 1", Price = 10.0, Quantity = 1 },
            new Item { Name = "Item 2", Price = 15.0, Quantity = 2 },
            new Item { Name = "Item 3", Price = 20.0, Quantity = 3 },
            // Add more items here
        };
    }
}

public class Item : INotifyPropertyChanged
{
    private string _name;
    private double _price;
    private int _quantity;

    public string Name
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

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

### 5. Run Your Application

Build and run your application. You should see the `TableView` populated with the rows and cells from your `ViewModel`.
