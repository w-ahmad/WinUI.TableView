using Microsoft.UI.Xaml;

namespace WinUI.TableView.SampleApp;

public static class ExampleModelColumnsHelper
{
    public static void OnAutoGeneratingColumns(object sender, TableViewAutoGeneratingColumnEventArgs e)
    {
        var viewModel = (ExampleViewModel)((TableView)sender).DataContext;
        var boundColumn = (TableViewBoundColumn)e.Column;

        switch (e.PropertyName)
        {
            case nameof(ExampleModel.Id):
                e.Column.Width = new GridLength(60);
                break;
            case nameof(ExampleModel.FirstName):
                e.Column.Width = new GridLength(110);
                break;
            case nameof(ExampleModel.LastName):
                e.Column.Width = new GridLength(110);
                break;
            case nameof(ExampleModel.Email):
                e.Column.Width = new GridLength(270);
                break;
            case nameof(ExampleModel.Gender):
                e.Column = new TableViewComboBoxColumn
                {
                    Binding = boundColumn.Binding,
                    Header = boundColumn.Header,
                    Width = new GridLength(120),
                    ItemsSource = viewModel.Genders
                };
                break;
            case nameof(ExampleModel.Dob):
                e.Column.Width = new GridLength(110);
                break;
            case nameof(ExampleModel.ActiveAt):
                e.Column.Width = new GridLength(110);
                break;
            case nameof(ExampleModel.IsActive):
                e.Column.Width = new GridLength(100);
                break;
            case nameof(ExampleModel.Department):
                e.Column = new TableViewComboBoxColumn
                {
                    Binding = boundColumn.Binding,
                    Header = boundColumn.Header,
                    Width = new GridLength(200),
                    ItemsSource = viewModel.Departments
                };
                break;
            case nameof(ExampleModel.Designation):
                e.Column = new TableViewComboBoxColumn
                {
                    Binding = boundColumn.Binding,
                    Header = boundColumn.Header,
                    Width = new GridLength(200),
                    ItemsSource = viewModel.Designations
                };
                break;
            case nameof(ExampleModel.Address):
                e.Column.Width = new GridLength(200);
                break;
            default:
                break;
        }
    }
}
