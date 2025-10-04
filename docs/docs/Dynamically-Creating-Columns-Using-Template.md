> [🔙 **Back to *Adding data to your TableView***](docs\Data-To-TableView.md)

# Dynamically creating columns using a DataTemplate

This is a more advanced method for adding data to your TableView, but it allows you to **create custom columns and assign data to them on-the-fly**.
It’s your go-to approach for building features such as task trackers, project dashboards, or any app that needs **customizable columns** . For example, letting users add fields like *Due Date*, *Description*, *Status*, *Difficulty*, etc.

|     | This method lets you...                     |
| --- | ------------------------------------------- |
| ✅  | Display a TableView with custom data        |
| ✅  | Edit and access the TableView’s data        |
| ✅  | Create new rows dynamically                 |
| ✅  | Create new custom columns dynamically       |
| ✅  | Assign new data values to those new columns |

> [!NOTE]
> For more advanced columns like `TableViewComboBoxColumn`, you'll need to set up binding and use a `DataTemplate`.

## 1. Get your data ready

We’ll start by defining a flexible data model that can store values for any number of columns.
Instead of having fixed properties like `Title`, `Description`, or `Status`, we’ll use a **dictionary-backed indexer** so we can easily add or remove columns without changing the model.

Here’s what that class looks like:
```csharp
public class DynamicRow : INotifyPropertyChanged
{
    private readonly Dictionary<string, object?> _values = new();

    public object? this[string key]
    {
        get => _values.TryGetValue(key, out var v) ? v : null;
        set
        {
            _values[key] = value;
            OnPropertyChanged($"Item[{key}]");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

### How it works

This class represents **one row** in your `TableView`.
Each row contains a collection of values - one for each column - stored in a dictionary:
- The **key** (the string index like `"Title"` or `"Status"`) represents the column name.
- The **value** is the actual cell value in that column.

For example, imagine this TableView:
| Title       | Description                                |
| ----------- | ------------------------------------------ |
| Fix bug #43 | App crashes when collapsing NavigationView |
| Review PR   | Review the pull request by Georgios1999    |

Internally, each row is stored as something like this:
```csharp
var row1 = new DynamicRow();
row1["Title"] = "Fix bug #43";
row1["Description"] = "App crashes when collapsing NavigationView";

var row2 = new DynamicRow();
row2["Title"] = "Review PR";
row2["Description"] = "Review the pull request by Georgios1999";
```

Each row is flexible — if you later add a new column (for example, “Status”), you can simply assign:
```csharp
row1["Status"] = "In Progress";
row2["Status"] = "Pending Review";
```

No need to change your class or rebuild — the rows adapt automatically.

## 2. Adding dynamic columns

Once you have your data model (`DynamicRow`), you can add columns dynamically to the TableView in your page’s code-behind.

Here’s a simple example that adds a **text column** at runtime:
```csharp
private void AddNewColumn()
{
    // These two are optional, but we're using them now to give a unique name to each column
    int n = MyTableView.Columns.Count + 1;
    string headerText = $"Column {n}";

    string fieldKey = $"Column{n}";

    var col = new TableViewTextColumn
    {
        Header = headerText, // The column's header (its name). We have set it to headerText for this example, but you can have it be anything.
        Binding = new Binding
        {
            Path = new PropertyPath($"[{fieldKey}]"),
            Mode = BindingMode.TwoWay
        }
    };

    MyTableView.Columns.Add(col); // Adds the new column.

    // Initialize existing rows so they display a default value. This is part of the example and not needed in your code.
    foreach (var row in Rows) // for each row...
        row[fieldKey] = $"Value {n}"; // set the value
}
```

### What’s happening here:
- We create a new **TableViewTextColumn** dynamically.
- The `Binding` path uses `"[ColumnX]"` syntax to connect to our `DynamicRow`’s dictionary.
- The column is added to the TableView, and any existing rows will immediately show their values.

## 3. About more advanced columns

For basic columns, such as for text or numbers, the setup we already have is fully sufficient. You don’t need to change the dictionary type.

The `Dictionary<string, object?>` we use in `DynamicRow` can hold any kind of value:
- text (`string`)
- numbers (`int`, `double`, `decimal`)
- dates (`DateTime`)
- checkboxes (`bool`)
- or even custom objects

That means this single model supports all standard TableView column types (`TableViewTextColumn`, `TableViewNumberColumn`, `TableViewCheckBoxColumn`, `TableViewDateColumn`, etc.) and it changes automatically based on what type of value you use.

For example:
```csharp
var row = new DynamicRow();
row["Title"] = "Fix crash on startup";       // Text column
row["Progress"] = 0.75;                      // Number column
row["IsComplete"] = true;                    // Checkbox column
row["DueDate"] = DateTime.Today.AddDays(3);  // Date column
```

> [!NOTE]
> You should always keep the dictionary key as `string`.
> The key represents the **column name**, and TableView bindings reference it using syntax like `Path=[ColumnName]`.

## Adding advanced columns like `TableViewComboBoxColumn`
Advanced columns like `TableViewComboBoxColumn` need extra setup. `ComboBox` specifically, has its own items (options), default option and values. We need extra work to set these up. In this example, we're going to set up a `TableViewComboBoxColumn`.

### Getting everything ready:
#### 1. Get your data ready
You can use the exact same setup from before for this.

#### 2. Set up a helper function to escape XAML characters
We'll be writing directly into XAML using C# to add the `DataTemplate`. However, the strings need to be interpreted in a special way that doesn't involve using the actual characters. Copy and paste this helper function into your code so you can use it when needed.
```cs
private static string XmlEscape(string s)
{
    if (s is null) return "";
    return s.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
}
```

#### 3. Declare your class
Declare your data class in your page's class like so:
```cs
...
public sealed partial class MainPage : Page
{
    public ObservableCollection<DynamicRow> Rows { get; } = new ObservableCollection<DynamicRow>();
...
```
By doing this, you can use it to edit, reference or add columns.

#### 4. Set your TableView's `ItemsSource` to your data
This sets the items source for your TableView to your rows, so when a new item is added to `Rows`, it also gets added and shown to your TableView.
```cs
public MainPage()
{
    this.InitializeComponent();

    // Attach our rows as the ItemsSource for the TableView.
    MyTableView.ItemsSource = Rows;
}
``` 

### Adding the columns
#### 5. Create a function for adding new columns
Let's create a function that adds a new column when called. We'll use it for this example and you can change it later as you see fit. Some things remain the same from our previous function for the basic columns.
```cs
public void AddNewColumn()
{
    // These two are optional, but we're using them now to give a unique name to each column
    int n = MyTableView.Columns.Count + 1;
    string headerText = $"Column {n}";

    string fieldKey = $"Column{n}";
    ...
```
Now, inside this function, we'll write the code that adds the new column to your TableView.

#### 6. Prepare your ComboBox's options
A ComboBox column needs a list of selectable items. In this example, we’ll create a simple list of difficulty levels that each task can have. We'll set these options up by adding them to their own list, like so:
```cs
private void AddNewColumn()
{
    // These two are optional, but we're using them now to give a unique name to each column
    int n = MyTableView.Columns.Count + 1;
    string headerText = $"Column {n}";

    string fieldKey = $"Column{n}";

    // The set of options for this new ComboBox column.
    // In a real app this could come from user settings or input.
    var options = new List<string> { "Easy", "Medium", "Hard" };
```

#### 7. Create the items for the ComboBox
Now that we have a list with the items to add, we'll create the items as `ComboBoxItems` so we can add them to the `TableViewComboBoxColumn`.
```cs
    foreach (var opt in options)
    {
        sbItems.Append("<ComboBoxItem Content='");
        sbItems.Append(XmlEscape(opt)); // escape special chars for valid XAML. This uses the XmlEscape helped we made earlier.
        sbItems.Append("'/>");
    }
```

#### 8. Create the `ComboBox` using XAML (in C#)
This builds the `DataTemplate` XAML string for the `ComboBox` in our columns. Specifically, it:

- Defines a ComboBox with fixed width.
- SelectedValuePath='Content' makes SelectedValue return the item text.
- SelectedValue is two-way bound to the row indexer (row[fieldKey]).

This is how we connect each cell to its row’s dictionary and create the `DataTemplate` for the `ComboBox` inside the `TableViewComboBoxColumn`

```cs
    string xaml =
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
        $"<ComboBox Width='160' SelectedValuePath='Content' SelectedValue='{{Binding Path=[{fieldKey}], Mode=TwoWay}}'>" +
        sbItems.ToString() +
        "</ComboBox></DataTemplate>";
```

and then take that string, which is the XAML, and actually load it using `XamlReader`:

```cs
DataTemplate template = (DataTemplate)XamlReader.Load(xaml);
```

#### 9. Wrap the `DataTemplate` inside a TableView column definition.
Now, we put that `DataTemplate` in a `TableViewTemplateColumn`, which means we are essentially creating our own custom column, which in this case is a `ComboBoxColumn` and we add it to the TableView.
```cs
    var col = new TableViewTemplateColumn
    {
        Header = headerText,   // column header text
        CellTemplate = template
    };

    // Add the new column to the TableView.
    MyTableView.Columns.Add(col);
```

#### 10. Set the default option
This is optional, but we can set the default option for our column like this:
```cs
    var first = options.FirstOrDefault();
    if (first != null)
    {
        foreach (var row in Columns)
        {
            if (row[fieldKey] == null)
                row[fieldKey] = first;
        }
    }
```

#### And this is it
We have created our own `TableViewComboBoxColumn`. Now, whenever `AddNewColumn()` is called, a new ComboBox column is added with its own header and options.

This is the full function:
```cs
private void AddNewColumn()
{
    int n = _columnCounter++;
    string fieldKey = $"Column{n}";      // internal key used in bindings
    string headerText = $"Column {n}";   // user-facing header text

    // The set of options for this new ComboBox column.
    // In a real app this could come from user settings or input.
    var options = new List<string> { "Easy", "Medium", "Hard" };

    // Build ComboBoxItem elements in XAML string form.
    // We inline them here so no external resource dictionaries are required.
    var sbItems = new StringBuilder();
    foreach (var opt in options)
    {
        sbItems.Append("<ComboBoxItem Content='");
        sbItems.Append(XmlEscape(opt)); // escape special chars for valid XAML
        sbItems.Append("'/>");
    }

    // Build the DataTemplate XAML string:
    // - Defines a ComboBox with fixed width.
    // - SelectedValuePath='Content' makes SelectedValue return the item text.
    // - SelectedValue is two-way bound to the row indexer (row[fieldKey]).
    //   This is how we connect each cell to its row’s dictionary.
    string xaml =
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
        $"<ComboBox Width='160' SelectedValuePath='Content' SelectedValue='{{Binding Path=[{fieldKey}], Mode=TwoWay}}'>" +
        sbItems.ToString() +
        "</ComboBox></DataTemplate>";

    // Parse the XAML string into a real DataTemplate using XamlReader.
    // XamlReader.Load takes a string and constructs the UI tree.
    DataTemplate template = (DataTemplate)XamlReader.Load(xaml);

    // Wrap the DataTemplate inside a TableView column definition.
    var col = new TableViewTemplateColumn
    {
        Header = headerText,   // column header text
        CellTemplate = template
    };

    // Add the new column to the TableView.
    MyTableView.Columns.Add(col);

    // Initialize existing rows with the first option so the new column
    // doesn’t appear empty (optional, purely cosmetic).
    var first = options.FirstOrDefault();
    if (first != null)
    {
        foreach (var row in Columns)
        {
            if (row[fieldKey] == null)
                row[fieldKey] = first;
        }
    }
}
```