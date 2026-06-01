using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class CustomizeFilterPage : Page
{
    public CustomizeFilterPage()
    {
        InitializeComponent();

    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is not ExampleViewModel viewModel) return;

        tableView.FilterHandler = new FilterHandler(tableView, viewModel);
        viewModel.Items = new(ExampleViewModel.ItemsList.Take(20));
    }
}

public class FilterHandler : ColumnFilterHandler
{
    private readonly TableView _tableView;
    private readonly ExampleViewModel _viewModel;

    public FilterHandler(TableView tableView, ExampleViewModel viewModel) : base(tableView)
    {
        _tableView = tableView;
        _viewModel = viewModel;
    }

    public override IList<TableViewFilterItem> GetFilterItems(TableViewColumn column, string? searchText)
    {
        var existingItems = SelectedValues.TryGetValue(column, out var selectedValues) ? selectedValues : [];
        bool isSelected(object value) => !existingItems.Any() || existingItems.Contains(value);
        var items = GetItems(column);

        return [.. items.Select(x => GetPropertyValue(x, column))
                    .Where(x => string.IsNullOrEmpty(searchText) || x?.ToString()?.Contains(searchText, StringComparison.OrdinalIgnoreCase) is true)
                    .Distinct()
                    .Order()
                    .Select(x => x ?? "(Blank)")
                    .Select(x => new TableViewFilterItem(!string.IsNullOrEmpty(searchText) || isSelected(x), x))];
    }

    public override void ApplyFilter(TableViewColumn column)
    {
        _tableView.DeselectAll();
        _viewModel.Items = new(GetItems().Take(20));

        if (!column.IsFiltered)
        {
            column.IsFiltered = true;
            _tableView.FilterDescriptions.Add(new FilterDescription(
                GetPropertyName(column),
                (o) => Filter(column, o)));
        }
    }

    public override void ClearFilter(TableViewColumn? column)
    {
        if (column is not null)
        {
            var fd = _tableView.FilterDescriptions.First(x => x.PropertyName == GetPropertyName(column));
            _tableView.FilterDescriptions.Remove(fd);
        }

        base.ClearFilter(column);

        _viewModel.Items = new(GetItems().Take(20));
    }

    public override bool Filter(TableViewColumn column, object? item)
    {
        if (column.Header?.ToString() is "Full Name" && item is ExampleModel model)
        {
            var value = $"{model.FirstName} {model.LastName}";
            return CompareValue(SelectedValues[column], value);
        }

        return base.Filter(column, item);
    }

    private IEnumerable<ExampleModel> GetItems(TableViewColumn? excludeColumns = default)
    {
        return ExampleViewModel.ItemsList.Where(x
            => SelectedValues.All(e =>
            {
                if (e.Key == excludeColumns) return true;

                var value = GetPropertyValue(x, e.Key);
                return CompareValue(e.Value, value);
            }));
    }

    private static bool CompareValue(ICollection<object?> selectedValue, object? value)
    {
        value = string.IsNullOrWhiteSpace(value?.ToString()) ? "(Blank)" : value;
        return selectedValue.Contains(value);
    }

    private static string? GetPropertyName(TableViewColumn column)
    {
        return (column?.Header?.ToString()) switch
        {
            "Id" => nameof(ExampleModel.Id),
            "First Name" => nameof(ExampleModel.FirstName),
            "Last Name" => nameof(ExampleModel.LastName),
            "Full Name" => "FullName",
            "Email" => nameof(ExampleModel.Email),
            "Gender" => nameof(ExampleModel.Gender),
            "Dob" => nameof(ExampleModel.Dob),
            _ => null,
        };
    }

    private static object? GetPropertyValue(ExampleModel item, TableViewColumn column)
    {
        return (column?.Header?.ToString()) switch
        {
            "Id" => item.Id,
            "First Name" => item.FirstName,
            "Last Name" => item.LastName,
            "Full Name" => $"{item.FirstName} {item.LastName}",
            "Email" => item.Email,
            "Gender" => item.Gender,
            "Dob" => item.Dob,
            _ => null,
        };
    }
}
