# Getting Started with Uno Platform

This guide walks you through setting up and using `WinUI.TableView` in your Uno Platform application. Follow the steps below to quickly integrate a powerful and flexible data grid into your project.

### 1. Create a new Uno project

If you don't already have an Uno project, create one in Visual Studio.

### 2. Install the NuGet package
Install [WinUI.TableView](https://www.nuget.org/packages/WinUI.TableView) NuGet package to your app with your preferred method. Here is the one using NuGet Package Manager:

```bash
Install-Package WinUI.TableView
```
### 3. Add `TableView` to your XAML

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

### 4. Bind data to `TableView`

Create a simple model class with properties to represent in `TableView` cells:

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

### 5. Run your application

Build and run your application. You should see `WinUI.TableView` populated with the rows and cells from your `Items` collection. Here is the result running on the Desktop platform.

![Desktop](https://github.com/user-attachments/assets/9b338487-702c-4812-a8ec-29d49e54549c)

## Uno Platform notes

`WinUI.TableView` targets all Uno Platform heads. The core feature set works on all supported targets, but there are some behavioral differences to be aware of.

### WebAssembly (WASM)

- **Binding**: `{Binding}` (runtime binding) and `{x:Bind}` (compiled binding) are both supported. Prefer `{x:Bind}` where possible for better performance and Native AOT compatibility.
- **Keyboard navigation**: Most keyboard shortcuts work, but browser focus management may intercept some key events (for example, **Tab** may navigate out of the page in some configurations). Test keyboard behavior in your target browser.
- **Accessibility**: The WASM target exposes accessibility information via ARIA attributes managed by Uno Platform. The full UIA automation tree available on Windows is not present on WASM.
- **Performance**: Rendering performance for large datasets in WASM depends on the browser and Skia rendering mode used. For very large tables, limit the visible row count and prefer explicit columns over `AutoGenerateColumns`.

### Desktop (Skia/GTK, Skia/WPF, Skia/X11)

- The control behaves similarly to the WinUI target. Keyboard navigation and mouse interaction are fully functional.
- Focus management follows the native platform conventions for the Skia host in use.
- Theming follows the Uno fluent theme. If your app uses a custom WinUI theme, verify it renders correctly on Skia targets.

### Android and iOS

- Touch/pointer interactions are supported. Tap to select, double-tap to edit.
- Keyboard editing works when a hardware keyboard or on-screen keyboard is active.
- Some `TableViewDateColumn` and `TableViewTimeColumn` picker controls may have platform-specific rendering differences. Verify on device or emulator.
- For best performance on mobile, keep the column count and row count reasonable. Prefer explicit columns (`AutoGenerateColumns="False"`).

### General binding notes

- On non-Windows targets, `{Binding}` path resolution may behave slightly differently for complex nested paths. Prefer `{x:Bind}` with an explicit `x:DataType` for compiled, type-safe bindings.
- When using `SortMemberPath` or `OperationContentBinding`, the string-based property access relies on reflection. This works on all targets, but on WASM/AOT consider using `{x:Bind}`-based template columns instead.

### Known limitations on Uno targets

- `AutoGenerateColumns="True"` uses reflection to discover model properties. This works on all Uno targets but is slower than explicit column definitions on first load.
- The filter flyout UI renders using the same XAML as the Windows target. On small-screen targets (phone), the flyout may extend beyond the visible area. Consider disabling the filter flyout (`CanFilterColumns="False"`) on those form factors.
- Row details expand/collapse animation may not be as smooth on Skia targets compared to Windows.

## Related articles

- [Installation and quick start](getting-started.md)
- [Overview and concepts](overview.md)
- [Performance guidance](performance.md)
- [Accessibility](accessibility.md)
- [Native AOT compatibility](aot-compatibility.md)
