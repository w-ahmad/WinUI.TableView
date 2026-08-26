# Native AOT compatibility

`TableView` relies on runtime bindings and dynamic value resolution for features like sorting, filtering, editing, clipboard, and export. These mechanisms are not fully compatible with IL trimming and Native AOT by default. This page explains what you need to do to make your app work correctly under `<PublishAot>true</PublishAot>`.

> **See also:** [Native AOT deployment overview (.NET)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) · [C#/WinRT overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/)

## When this applies

You need to follow this guidance if **any** of the following are true for your project:

- `<PublishAot>true</PublishAot>` is set in your `.csproj`
- `<PublishTrimmed>true</PublishTrimmed>` is set in your `.csproj`
- You see AOT/trimming warnings related to `WinRT` or binding at compile time

## The rule

> Any model class whose **properties are accessed via traditional `{Binding}` expressions** must be marked with `[WinRT.GeneratedBindableCustomProperty]` and declared as `partial`.

Traditional binding (`{Binding PropertyName}`) resolves property values at runtime using the WinRT binding infrastructure. Under Native AOT, that infrastructure requires the `[WinRT.GeneratedBindableCustomProperty]` attribute to generate the necessary binding metadata at compile time. This attribute is part of [C#/WinRT](https://github.com/microsoft/CsWinRT).

## What needs the attribute

### Bound columns (attribute required)

Columns that use a `Binding` property — all built-in bound column types — resolve values at runtime through the WinRT binding system:

```xml
<tv:TableViewTextColumn    Header="Name"   Binding="{Binding Name}" />
<tv:TableViewNumberColumn  Header="Price"  Binding="{Binding Price}" />
<tv:TableViewCheckBoxColumn Header="Active" Binding="{Binding IsActive}" />
<tv:TableViewDateColumn    Header="Due"    Binding="{Binding DueDate}" />
<tv:TableViewTimeColumn    Header="Time"   Binding="{Binding ActiveAt}" />
<tv:TableViewComboBoxColumn Header="Role"  Binding="{Binding Role}" />
<tv:TableViewHyperlinkColumn Header="Url"  Binding="{Binding Url}" />
```

Any model class whose properties are accessed by these columns **must** have the attribute.

### Template columns with `{Binding}` (attribute required)

If your `CellTemplate` or `EditingTemplate` uses `{Binding}` to access model properties, the attribute is still required:

```xml
<tv:TableViewTemplateColumn Header="Status">
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <!-- {Binding} requires the attribute on the model -->
            <TextBlock Text="{Binding StatusLabel}" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
</tv:TableViewTemplateColumn>
```

### Template columns with `{x:Bind}` (attribute NOT required)

When a `TableViewTemplateColumn` uses `{x:Bind}` exclusively in its templates, the binding is compiled at build time and does **not** require the attribute on the model:

```xml
<tv:TableViewTemplateColumn Header="Status">
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate x:DataType="local:Product">
            <!-- x:Bind generates compile-time code — no attribute needed -->
            <TextBlock Text="{x:Bind StatusLabel}" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
</tv:TableViewTemplateColumn>
```

## How to apply the attribute

Add `[WinRT.GeneratedBindableCustomProperty]` to your model class and mark it as `partial`:

```csharp
using WinRT;

[GeneratedBindableCustomProperty]
public partial class Product
{
    public string? Name { get; set; }
    public double Price { get; set; }
    public bool InStock { get; set; }
}
```

> **Important:** The class **must** be `partial`. Without `partial`, the source generator cannot emit the required binding implementation and the build will fail.

## Using CommunityToolkit.Mvvm

If your models use [`ObservableObject`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observableobject) from [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/), combine both attributes. Use `partial` properties with [`[ObservableProperty]`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/observableproperty) as normal:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using WinRT;

[GeneratedBindableCustomProperty]
public partial class Product : ObservableObject
{
    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial double Price { get; set; }

    [ObservableProperty]
    public partial bool InStock { get; set; }
}
```

## Nested models

If a column binds to a nested property (e.g. `Binding="{Binding Address.City}"`), the **nested type** must also have the attribute:

```csharp
[GeneratedBindableCustomProperty]
public partial class Order
{
    [ObservableProperty]
    public partial string? OrderNumber { get; set; }

    [ObservableProperty]
    public partial Address? ShippingAddress { get; set; }
}

[GeneratedBindableCustomProperty]  // Required because columns bind into Address properties
public partial class Address
{
    [ObservableProperty]
    public partial string? City { get; set; }

    [ObservableProperty]
    public partial string? Country { get; set; }
}
```

## Project configuration

Enable AOT and set the CsWinRT warning level to catch any remaining issues at build time:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <CsWinRTAotWarningLevel>2</CsWinRTAotWarningLevel>
</PropertyGroup>

<!-- Trim only for Release builds -->
<PropertyGroup>
  <PublishTrimmed Condition="'$(Configuration)' != 'Debug'">true</PublishTrimmed>
</PropertyGroup>
```

`CsWinRTAotWarningLevel=2` upgrades [CsWinRT](https://github.com/microsoft/CsWinRT) AOT hints to errors so missing attributes are caught during the build rather than at runtime.

## Notes and limitations

- Only WinUI (Windows App SDK) targets support Native AOT publishing. Uno Platform targets have their own AOT characteristics — consult the [Uno Platform documentation](https://platform.uno/docs/articles/uno-development/aot-compilation.html) for those targets.
- `AutoGenerateColumns="True"` uses reflection to discover properties. This works with the attribute in place, but for maximum AOT compatibility prefer explicit columns (`AutoGenerateColumns="False"`).
- `SortMemberPath` uses reflection-based property access when set. Ensure the model has the attribute when using `SortMemberPath`.
- [Grouping](grouping.md) is built on the same `SortDescription`-derived reflection path as sorting - grouping by a bound column's property (rather than its rendered cell content) needs the same model attribute.

## Related articles

- [Binding data](binding-data.md)
- [Defining columns](defining-columns.md)
- [Column types](column-types.md)
- [Grouping](grouping.md)
- [Performance guidance](performance.md)
- [.NET Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [C#/WinRT overview](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [CsWinRT on GitHub](https://github.com/microsoft/CsWinRT)
