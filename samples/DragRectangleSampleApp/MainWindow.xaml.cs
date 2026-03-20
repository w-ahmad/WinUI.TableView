using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using WinUI.TableView;

namespace DragRectangleSampleApp;

/// <summary>
/// Main window demonstrating the TableView drag rectangle feature.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "TableView Drag Rectangle Sample";

        // Configuration set in code-behind to avoid XAML parsing issues
        SampleTableView.SelectionUnit = TableViewSelectionUnit.Cell;
        SampleTableView.ShowDragRectangle = true;
        DragRectangleToggle.IsOn = true;

        SampleTableView.Columns.Add(new TableViewTextColumn { Header = "Name", Binding = new Binding { Path = new PropertyPath("Name") }, Width = new GridLength(150) });
        SampleTableView.Columns.Add(new TableViewTextColumn { Header = "Department", Binding = new Binding { Path = new PropertyPath("Department") }, Width = new GridLength(150) });
        SampleTableView.Columns.Add(new TableViewTextColumn { Header = "Title", Binding = new Binding { Path = new PropertyPath("Title") }, Width = new GridLength(200) });
        SampleTableView.Columns.Add(new TableViewTextColumn { Header = "Location", Binding = new Binding { Path = new PropertyPath("Location") }, Width = new GridLength(150) });

        SampleTableView.ItemsSource = new ObservableCollection<Employee>
        {
            new() { Name = "Alice Johnson", Department = "Engineering", Title = "Senior Developer", Location = "Seattle" },
            new() { Name = "Bob Smith", Department = "Marketing", Title = "Marketing Manager", Location = "New York" },
            new() { Name = "Carol White", Department = "Engineering", Title = "Tech Lead", Location = "Seattle" },
            new() { Name = "David Brown", Department = "Sales", Title = "Sales Rep", Location = "Chicago" },
            new() { Name = "Eva Davis", Department = "Engineering", Title = "Junior Developer", Location = "Austin" },
            new() { Name = "Frank Miller", Department = "HR", Title = "HR Director", Location = "New York" },
            new() { Name = "Grace Wilson", Department = "Engineering", Title = "DevOps Engineer", Location = "Seattle" },
            new() { Name = "Henry Taylor", Department = "Finance", Title = "Financial Analyst", Location = "Chicago" },
            new() { Name = "Iris Anderson", Department = "Engineering", Title = "QA Engineer", Location = "Austin" },
            new() { Name = "Jack Thomas", Department = "Marketing", Title = "Content Writer", Location = "Remote" },
        };
    }

    private void DragRectangleToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SampleTableView.ShowDragRectangle = DragRectangleToggle.IsOn;
    }

    private void SelectionUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SampleTableView is null) return;

        SampleTableView.SelectionUnit = SelectionUnitCombo.SelectedIndex switch
        {
            0 => TableViewSelectionUnit.CellOrRow,
            1 => TableViewSelectionUnit.Cell,
            2 => TableViewSelectionUnit.Row,
            _ => TableViewSelectionUnit.CellOrRow
        };
    }
}

/// <summary>
/// Sample employee data model.
/// </summary>
[WinRT.GeneratedBindableCustomProperty]
public partial class Employee
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the department.</summary>
    public string Department { get; set; } = string.Empty;
    /// <summary>Gets or sets the title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Gets or sets the location.</summary>
    public string Location { get; set; } = string.Empty;
}
