# Customizing TableView
TableView's extensive built-in customization features allow it to fit in any app.

#### 👉 Use the [Sample App](https://github.com/w-ahmad/WinUI.TableView.SampleApp.git) to easily try out and change customization options.

> [!NOTE]
> If you have any questions or want to suggest docs for features not included in the app, please create an [Issue](https://github.com/w-ahmad/WinUI.TableView/issues). For issues related to documentation, you may want to mention @Georgios1999

## Editing properties in C#
To customize your TableView using C#, first give it a name:
```xml
<tv:TableView x:Name="TViev">
```
then you can edit its properties in code like this:
```csharp
TView.Opacity = 1;
```

### Useful Properties
- `TableView.Columns` allows you to edit columns by using `.Add()` to add a column, `.Remove()` to remove one, `.Clear()` to clear all and more
- `TableView.ScrollIntoView(`any column item`)` Scrolls to the chosen item
- `TableView.SelectAll()` Selects all the items
