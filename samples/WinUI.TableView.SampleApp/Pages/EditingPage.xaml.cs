using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUI.TableView.SampleApp.Pages;

public sealed partial class EditingPage : Page
{
    private readonly List<string> _edits = [];

    public EditingPage()
    {
        InitializeComponent();
    }

    private void OnCellEditEnding(object? sender, TableViewCellEditEndingEventArgs e)
    {
        var isInvalid = validateEmail.IsOn
            && e.EditAction is TableViewEditAction.Commit
            && e.Column.Header is "Email"
            && e.EditingElement is TextBox textBox
            && !textBox.Text.Contains('@');

        e.Cancel = isInvalid;
        validation.Message = "An email address needs an @. Fix it, or press Escape to keep the old value.";
        validation.IsOpen = isInvalid;
    }

    private void OnCellEditEnded(object? sender, TableViewCellEditEndedEventArgs e)
    {
        _edits.Insert(0, $"{e.EditAction}: {e.Column.Header} of #{(e.DataItem as ExampleModel)?.Id}");

        if (_edits.Count > 8)
        {
            _edits.RemoveAt(_edits.Count - 1);
        }

        editLog.Text = string.Join(Environment.NewLine, _edits);
    }
}
