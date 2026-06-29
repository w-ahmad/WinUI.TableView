# Styling rows, cells, and headers

`TableView` provides several ways to customize the appearance of rows, cells, and column headers. You can apply global styles, per-column styles, grid line settings, and alternate row colors.

## When to use it

Use styling to match your application's design language, improve readability with alternating row colors, or add visual structure with grid lines.

## Cell style

Apply a style to all cells in the table:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.CellStyle>
        <Style TargetType="tv:TableViewCell">
            <Setter Property="Padding" Value="8,4" />
            <Setter Property="FontSize" Value="13" />
        </Style>
    </tv:TableView.CellStyle>
</tv:TableView>
```

Apply a style to all cells in a specific column:

```xml
<tv:TableViewTextColumn Header="Name" Binding="{Binding Name}">
    <tv:TableViewTextColumn.CellStyle>
        <Style TargetType="tv:TableViewCell">
            <Setter Property="Background" Value="#F0F4FF" />
        </Style>
    </tv:TableViewTextColumn.CellStyle>
</tv:TableViewTextColumn>
```

## Column header style

Apply a style to all column headers:

```xml
<tv:TableView ItemsSource="{x:Bind Products}">
    <tv:TableView.ColumnHeaderStyle>
        <Style TargetType="tv:TableViewColumnHeader">
            <Setter Property="Background" Value="{ThemeResource AccentFillColorDefaultBrush}" />
            <Setter Property="Foreground" Value="White" />
            <Setter Property="FontWeight" Value="SemiBold" />
        </Style>
    </tv:TableView.ColumnHeaderStyle>
</tv:TableView>
```

Apply a style to a specific column's header:

```xml
<tv:TableViewTextColumn Header="Price" Binding="{Binding Price}">
    <tv:TableViewTextColumn.HeaderStyle>
        <Style TargetType="tv:TableViewColumnHeader">
            <Setter Property="HorizontalContentAlignment" Value="Right" />
        </Style>
    </tv:TableViewTextColumn.HeaderStyle>
</tv:TableViewTextColumn>
```

## Element style and editing element style

For bound column types, use [`ElementStyle`](xref:WinUI.TableView.TableViewBoundColumn.ElementStyle) to style the display element and [`EditingElementStyle`](xref:WinUI.TableView.TableViewBoundColumn.EditingElementStyle) to style the editing element:

```xml
<tv:TableViewTextColumn Header="Notes" Binding="{Binding Notes}">
    <tv:TableViewTextColumn.ElementStyle>
        <Style TargetType="TextBlock">
            <Setter Property="Foreground" Value="{ThemeResource TextFillColorSecondaryBrush}" />
            <Setter Property="FontStyle" Value="Italic" />
        </Style>
    </tv:TableViewTextColumn.ElementStyle>
    <tv:TableViewTextColumn.EditingElementStyle>
        <Style TargetType="TextBox">
            <Setter Property="MaxLength" Value="200" />
        </Style>
    </tv:TableViewTextColumn.EditingElementStyle>
</tv:TableViewTextColumn>
```

## Alternating row colors

Set background and/or foreground for even-indexed rows:

```xml
<tv:TableView AlternateRowBackground="#F5F5F5"
              AlternateRowForeground="{ThemeResource TextFillColorPrimaryBrush}" />
```

The alternate style is applied to even rows (0-based index: rows 0, 2, 4, …).

## Grid lines

Control the visibility and appearance of grid lines:

```xml
<tv:TableView GridLinesVisibility="All"
              HorizontalGridLinesStrokeThickness="1"
              VerticalGridLinesStrokeThickness="1"
              HorizontalGridLinesStroke="#E0E0E0"
              VerticalGridLinesStroke="#E0E0E0" />
```

| Property | Type | Default | Description |
|---|---|---|---|
| [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | [`TableViewGridLinesVisibility`](xref:WinUI.TableView.TableViewGridLinesVisibility) | `All` | Which grid lines to show for data rows |
| [`HeaderGridLinesVisibility`](xref:WinUI.TableView.TableView.HeaderGridLinesVisibility) | [`TableViewGridLinesVisibility`](xref:WinUI.TableView.TableViewGridLinesVisibility) | `All` | Which grid lines to show in the header row |
| [`HorizontalGridLinesStrokeThickness`](xref:WinUI.TableView.TableView.HorizontalGridLinesStrokeThickness) | `double` | `1` | Thickness of horizontal lines |
| [`VerticalGridLinesStrokeThickness`](xref:WinUI.TableView.TableView.VerticalGridLinesStrokeThickness) | `double` | `1` | Thickness of vertical lines |
| [`HorizontalGridLinesStroke`](xref:WinUI.TableView.TableView.HorizontalGridLinesStroke) | `Brush` | theme default | Color of horizontal lines |
| [`VerticalGridLinesStroke`](xref:WinUI.TableView.TableView.VerticalGridLinesStroke) | `Brush` | theme default | Color of vertical lines |

[`TableViewGridLinesVisibility`](xref:WinUI.TableView.TableViewGridLinesVisibility) values:

| Value | Description |
|---|---|
| `All` | Both horizontal and vertical grid lines |
| `Horizontal` | Only horizontal lines |
| `Vertical` | Only vertical lines |
| `None` | No grid lines |

### Hide all grid lines

```xml
<tv:TableView GridLinesVisibility="None"
              HeaderGridLinesVisibility="None" />
```

## Summary of styling properties

| Property | Target | Level |
|---|---|---|
| [`CellStyle`](xref:WinUI.TableView.TableView.CellStyle) | [`TableViewCell`](xref:WinUI.TableView.TableViewCell) | Table or column |
| [`ColumnHeaderStyle`](xref:WinUI.TableView.TableView.ColumnHeaderStyle) | [`TableViewColumnHeader`](xref:WinUI.TableView.TableViewColumnHeader) | Table |
| `TableViewColumn.HeaderStyle` | [`TableViewColumnHeader`](xref:WinUI.TableView.TableViewColumnHeader) | Column |
| `TableViewColumn.CellStyle` | [`TableViewCell`](xref:WinUI.TableView.TableViewCell) | Column |
| `TableViewBoundColumn.ElementStyle` | Display element (e.g. `TextBlock`) | Column |
| `TableViewBoundColumn.EditingElementStyle` | Editing element (e.g. `TextBox`) | Column |
| [`AlternateRowBackground`](xref:WinUI.TableView.TableView.AlternateRowBackground) | Row background | Table |
| [`AlternateRowForeground`](xref:WinUI.TableView.TableView.AlternateRowForeground) | Row foreground | Table |
| [`GridLinesVisibility`](xref:WinUI.TableView.TableView.GridLinesVisibility) | Grid lines | Table |
| [`HeaderGridLinesVisibility`](xref:WinUI.TableView.TableView.HeaderGridLinesVisibility) | Header grid lines | Table |

## Related articles

- [Conditional cell styling](conditional-styling.md)
- [Column types](column-types.md)
- [Row details](row-details.md)
