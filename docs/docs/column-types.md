# Column types

WinUI.TableView provides a set of built-in column types to handle the most common data types. All column types inherit from `TableViewColumn`. Columns that bind to a data property inherit from `TableViewBoundColumn`, which adds the `Binding`, `ElementStyle`, and `EditingElementStyle` properties.

## TableViewTextColumn

Displays a `TextBlock` and edits with a `TextBox`.

```xml
<tv:TableViewTextColumn Header="Name" Binding="{Binding Name}" />
```

| Property | Description |
|---|---|
| `Binding` | Two-way binding to the data property |
| `ElementStyle` | Style for the display `TextBlock` |
| `EditingElementStyle` | Style for the editing `TextBox` |

## TableViewNumberColumn

Displays a right-aligned `TextBlock` and edits with a `NumberBox`.

```xml
<tv:TableViewNumberColumn Header="Price" Binding="{Binding Price}" />
```

| Property | Description |
|---|---|
| `Binding` | Two-way binding to a numeric property |
| `ElementStyle` | Style for the display `TextBlock` |
| `EditingElementStyle` | Style for the editing `NumberBox` |

## TableViewCheckBoxColumn

Displays a `CheckBox`. Uses a single element for both display and editing (no separate edit mode toggle). The checkbox is read-only when the column or `TableView` is read-only.

```xml
<tv:TableViewCheckBoxColumn Header="In Stock" Binding="{Binding InStock}" />
```

| Property | Description |
|---|---|
| `Binding` | Two-way binding to a `bool` property |
| `ElementStyle` | Style for the `CheckBox` |

## TableViewToggleSwitchColumn

Displays a `ToggleSwitch`. Like `TableViewCheckBoxColumn`, uses a single element for display and editing.

```xml
<tv:TableViewToggleSwitchColumn Header="Active" Binding="{Binding IsActive}" />
```

| Property | Description |
|---|---|
| `Binding` | Two-way binding to a `bool` property |
| `OnContent` | Content shown on the switch when it is on |
| `OffContent` | Content shown on the switch when it is off |
| `ElementStyle` | Style for the `ToggleSwitch` |

```xml
<tv:TableViewToggleSwitchColumn Header="Active"
                                Binding="{Binding IsActive}"
                                OnContent="Yes"
                                OffContent="No" />
```

## TableViewComboBoxColumn

Displays a `TextBlock` and edits with a `ComboBox`.

```xml
<tv:TableViewComboBoxColumn Header="Category"
                            Binding="{Binding Category}"
                            ItemsSource="{x:Bind Categories}" />
```

| Property | Description |
|---|---|
| `Binding` | Two-way binding to the selected value/item |
| `ItemsSource` | The list of items to show in the dropdown |
| `DisplayMemberPath` | Property path used to display the item text |
| `SelectedValuePath` | Property path for the selected value |
| `IsEditable` | Whether the ComboBox allows free-form text input |
| `TextBinding` | Optional separate binding for the ComboBox text when `IsEditable` is true |
| `SelectedValueBinding` | Optional binding for the selected value when using `SelectedValuePath` |
| `ElementStyle` | Style for the display `TextBlock` |
| `EditingElementStyle` | Style for the editing `ComboBox` |

### Example with a complex object list

```csharp
public record Category(int Id, string Name);

// In your ViewModel:
public IList<Category> Categories { get; } = new List<Category>
{
    new(1, "Electronics"),
    new(2, "Clothing"),
    new(3, "Food"),
};
```

```xml
<tv:TableViewComboBoxColumn Header="Category"
                            Binding="{Binding CategoryId}"
                            ItemsSource="{x:Bind ViewModel.Categories}"
                            DisplayMemberPath="Name"
                            SelectedValuePath="Id" />
```

## TableViewDateColumn

Displays a formatted date string and edits with a custom `TableViewDatePicker`. Supports `DateOnly`, `DateTime`, and `DateTimeOffset` property types.

```xml
<tv:TableViewDateColumn Header="Order Date" Binding="{Binding OrderDate}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `Binding` | `Binding` | — | Two-way binding to the date property |
| `DateFormat` | `string` | `"shortdate"` | Display format string (WinRT format) |
| `MinDate` | `DateTimeOffset` | `DateTimeOffset.MinValue` | Minimum selectable date |
| `MaxDate` | `DateTimeOffset` | `DateTimeOffset.MaxValue` | Maximum selectable date |
| `IsTodayHighlighted` | `bool` | `true` | Highlights today in the calendar |
| `IsOutOfScopeEnabled` | `bool` | `true` | Enables out-of-scope dates |
| `IsGroupLabelVisible` | `bool` | `true` | Shows the month/year label |
| `FirstDayOfWeek` | `DayOfWeek` | `Sunday` | First day of the week in the calendar |
| `PlaceHolderText` | `string?` | localized | Placeholder when no date is selected |
| `ElementStyle` | `Style` | — | Style for the display `TextBlock` |
| `EditingElementStyle` | `Style` | — | Style for the `TableViewDatePicker` |

## TableViewTimeColumn

Displays a formatted time string and edits with a custom `TableViewTimePicker`. Supports `TimeOnly`, `TimeSpan`, `DateTime`, and `DateTimeOffset` property types.

```xml
<tv:TableViewTimeColumn Header="Meeting Time" Binding="{Binding MeetingTime}" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| `Binding` | `Binding` | — | Two-way binding to the time property |
| `ClockIdentifier` | `string` | system clock | `"12HourClock"` or `"24HourClock"` |
| `MinuteIncrement` | `int` | `1` | Minute step in the time picker |
| `PlaceholderText` | `string?` | localized | Placeholder when no time is selected |
| `ElementStyle` | `Style` | — | Style for the display `TextBlock` |
| `EditingElementStyle` | `Style` | — | Style for the `TableViewTimePicker` |

## TableViewHyperlinkColumn

Displays a `HyperlinkButton`. The `Binding` property sets the navigation URI.

```xml
<tv:TableViewHyperlinkColumn Header="Website" Binding="{Binding WebsiteUrl}" />
```

| Property | Description |
|---|---|
| `Binding` | Binding for the `NavigateUri`; also used for content if `ContentBinding` is not set |
| `ContentBinding` | Optional separate binding for the visible link text |
| `ElementStyle` | Style for the `HyperlinkButton` |
| `EditingElementStyle` | Style for the editing `TextBox` |

```xml
<!-- Show a friendly label instead of the raw URL -->
<tv:TableViewHyperlinkColumn Header="Website"
                             Binding="{Binding WebsiteUrl}"
                             ContentBinding="{Binding WebsiteLabel}" />
```

## TableViewTemplateColumn

The most flexible column type. Uses `DataTemplate`s for both display and editing. By default, `CanSort` and `CanFilter` are `false` because there is no bound property path to sort or filter on.

```xml
<tv:TableViewTemplateColumn Header="Rating">
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <RatingControl Value="{Binding Rating}" IsReadOnly="True" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
    <tv:TableViewTemplateColumn.EditingTemplate>
        <DataTemplate>
            <RatingControl Value="{Binding Rating, Mode=TwoWay}" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.EditingTemplate>
</tv:TableViewTemplateColumn>
```

| Property | Description |
|---|---|
| `CellTemplate` | `DataTemplate` for the display element (the content property) |
| `CellTemplateSelector` | `DataTemplateSelector` to choose the display template per item |
| `EditingTemplate` | `DataTemplate` for the editing element |
| `EditingTemplateSelector` | `DataTemplateSelector` to choose the editing template per item |

If `EditingTemplate` is not set, the display `CellTemplate` is reused during editing.

### Using OperationContentBinding for sort/filter

To enable sorting or filtering on a `TableViewTemplateColumn`, set `OperationContentBinding` to point to the underlying property:

```xml
<tv:TableViewTemplateColumn Header="Rating" CanSort="True" CanFilter="True">
    <tv:TableViewTemplateColumn.OperationContentBinding>
        <Binding Path="Rating" />
    </tv:TableViewTemplateColumn.OperationContentBinding>
    <tv:TableViewTemplateColumn.CellTemplate>
        <DataTemplate>
            <RatingControl Value="{Binding Rating}" IsReadOnly="True" />
        </DataTemplate>
    </tv:TableViewTemplateColumn.CellTemplate>
</tv:TableViewTemplateColumn>
```

## ElementStyle and EditingElementStyle

`TableViewBoundColumn` exposes two style properties for the generated display and editing elements:

```xml
<tv:TableViewTextColumn Header="Name" Binding="{Binding Name}">
    <tv:TableViewTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="FontWeight" Value="Bold" />
        </Style>
    </tv:TableViewTextColumn.ElementStyle>
    <tv:TableViewTextColumn.EditingElementStyle>
        <Style TargetType="TextBox">
            <Setter Property="Background" Value="LightYellow" />
        </Style>
    </tv:TableViewTextColumn.EditingElementStyle>
</tv:TableViewTextColumn>
```

## Notes and limitations

- `TableViewCheckBoxColumn` and `TableViewToggleSwitchColumn` use a single element (`UseSingleElement = true`). The same element is used in both read and edit mode; clicking the control directly commits the change.
- `TableViewTemplateColumn` does not support sorting or filtering by default. Set `OperationContentBinding` to enable these features.
- `TableViewDateColumn` and `TableViewTimeColumn` automatically detect the property type (`DateOnly`, `DateTime`, `DateTimeOffset`, `TimeOnly`, `TimeSpan`) and configure the picker accordingly.

## Related articles

- [Defining columns](defining-columns.md)
- [Column sizing](column-sizing.md)
- [Editing](editing.md)
- [Styling rows, cells, and headers](styling.md)
