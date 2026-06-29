# Binding data

`TableView` accepts any `IEnumerable` as its items source. For the best experience, use `ObservableCollection<T>` so that item additions and removals are reflected automatically, and implement `INotifyPropertyChanged` on your model so that cell values update when the underlying data changes.

## When to use it

Use data binding when you have a collection of objects whose properties map to table columns. This is the standard pattern for all TableView usage.

## Basic example

```xml
<tv:TableView ItemsSource="{x:Bind Products}" />
```

```csharp
public sealed partial class MainWindow : Window
{
    public ObservableCollection<Product> Products { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        Products.Add(new Product { Name = "Widget", Price = 9.99, InStock = true });
        Products.Add(new Product { Name = "Gadget", Price = 24.50, InStock = false });
    }
}
```

## Model example

For editable cells, implement `INotifyPropertyChanged` so that changes committed in the editing control propagate back to the UI:

```csharp
public class Product : INotifyPropertyChanged
{
    private string? _name;
    private double _price;
    private bool _inStock;

    public string? Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public double Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(nameof(Price)); }
    }

    public bool InStock
    {
        get => _inStock;
        set { _inStock = value; OnPropertyChanged(nameof(InStock)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

## Common options

| Property | Type | Default | Description |
|---|---|---|---|
| `ItemsSource` | `object` | `null` | The data collection to display. |
| `AutoGenerateColumns` | `bool` | `true` | When `true`, columns are generated automatically from the public properties of the item type. |

## Using CollectionView

`TableView` wraps your source in an internal `AdvancedCollectionView`. You can access it through `TableView.CollectionView` to apply programmatic sort and filter descriptions.

```csharp
// Sort by price descending
tableView.SortDescriptions.Add(new SortDescription("Price", SortDirection.Descending));
```

## Live shaping

The internal collection view supports live shaping. When you update a property on an item, sorting and filtering re-evaluate automatically:

```csharp
tableView.AllowLiveShaping = true;
```

Live shaping has a performance cost on large collections. Disable it if you do not need items to re-sort or re-filter when individual properties change.

## Notes and limitations

- Setting `ItemsSource` to `null` clears the table.
- If you replace the entire collection (by assigning a new list to `ItemsSource`) any applied sort or filter descriptions are preserved on the internal view and applied to the new source.
- The internal `CollectionView` is read-only; do not cast and mutate it directly. Use `TableView.SortDescriptions` and `TableView.FilterDescriptions` instead.

## Related articles

- [AutoGenerateColumns and defining columns](defining-columns.md)
- [Sorting](sorting.md)
- [Filtering](filtering.md)
