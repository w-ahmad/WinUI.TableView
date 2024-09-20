
# WinUI.TableView

`WinUI.TableView` is a lightweight and fast data grid control made for WinUI apps. It is easy to use, and capable of handling large number of items with a focus on performance. It's derived from `ListView` so you will experience fluent look and feel in your project. It comes with all the essential features you need, plus extras like an Excel like column filter, options buttons (for columns and the TableView) and easy data export.

[![ci-build](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/ci-build.yml/badge.svg)](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/ci-build.yml)
[![cd-build](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml/badge.svg)](https://github.com/w-ahmad/WinUI.TableView/actions/workflows/cd-build.yml)
[![nuget](https://img.shields.io/nuget/v/WinUI.TableView)](https://www.nuget.org/packages/WinUI.TableView/)
[![nuget](https://img.shields.io/nuget/dt/WinUI.TableView)](https://www.nuget.org/packages/WinUI.TableView/)

## Contributors
<a href="https://github.com/w-ahmad/WinUI.TableView/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=w-ahmad/WinUI.TableView" />
</a>


## Features

- **Auto-generating Columns**: Automatically generate columns based on the data source.
- **Individual cell selection**: You can select any cell for the ease of access and better editing experience.
- **Copy row or cell content**: TableView allows you to copy rows or cells content, with the option to include or exclude column headers.
- **Editing**: Modify cell content directly within the TableView by double tapping on a cell.
- **Sorting**: Offers built in column sorting.
- **Excel-like Column Filter**: TableView allows you to filter data within columns with an excel like flyout to enhance data exploration and analysis.
- **Export functionality**: Built-in export functionality to export data to CSV format. This feature can be enabled by setting the `ShowExportOptions = true`.

## Getting Started

### 1: Install NuGet package to your project

You can install the `WinUI.TableView` NuGet package using the NuGet Package Manager or by running the following command in the Package Manager Console:

```bash
Install-Package WinUI.TableView
```

### 2. Create a New WinUI 3 Project

If you don't already have a WinUI 3 project, create one in Visual Studio.

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
    xmlns:tableView="using:WinUI.TableView">

    <Grid>
        <tableView:TableView x:Name="MyTableView"
                             ItemsSource="{x:Bind ViewModel.Items}"
                             AutoGenerateColumns="True" />
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

## Customization

You can customize the appearance and behavior of the `TableView` by modifying its properties, templates, and styles. For example:

- **Column Customization**: Define custom columns based on data types.
- **Is ReadOnly**: You can make any column or the TableView itself read only.
- **Sorting and Filtering**: Enable sorting and filtering on specific columns or for the all columns.
- **TableView Options**: Turn on or off options flyout for the TableView based on your requirements.

```xml
<tableView:TableView x:Name="MyTableView"
                     ItemsSource="{x:Bind ViewModel.Items}"
                     AutoGenerateColumns="False">
    <tableView:TableView.Columns>
        <tableView:TableViewTextColumn Header="Name" Binding="{Binding Name}" />
        <tableView:TableViewNumberColumn Header="Price" Binding="{Binding Price}" />
        <tableView:TableViewTemplateColumn Header="Quantity">
            <tableView:TableViewTemplateColumn.CellTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Quantity}" />
                </DataTemplate>
            </tableView:TableViewTemplateColumn.CellTemplate>
            <tableView:TableViewTemplateColumn.EditingTemplate>
                <DataTemplate>
                    <NumberBox Value="{Binding Quantity, Mode=TwoWay}" />
                </DataTemplate>
            </tableView:TableViewTemplateColumn.EditingTemplate>
        </tableView:TableViewTemplateColumn>
    </tableView:TableView.Columns>
</tableView:TableView>
```

### Available Column Types
1. TableViewTextColumn
1. TableViewCheckBoxColumn
1. TableViewComboBoxColumn
1. TableViewNumberColumn
1. TableViewToggleSwitchColumn
1. TableViewTemplateColumn

## Contributing

Contributions are welcome from the community! If you find any issues or have suggestions for improvements, please submit them through the GitHub issue tracker or consider making a pull request.

## License

This project is licensed under the [MIT License](https://github.com/w-ahmad/WinUI.TableView?tab=MIT-1-ov-file).
