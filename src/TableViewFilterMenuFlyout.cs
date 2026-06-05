using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Represents the filter menu flyout for a TableViewColumnHeader.
/// </summary>
public partial class TableViewFilterMenuFlyout : MenuFlyout
{
    private TableViewFilterItemsControl? _filterItemsControl;
    private readonly StandardUICommand _okCommand = new() { Label = TableViewLocalizedStrings.Ok };
    private readonly StandardUICommand _cancelCommand = new() { Label = TableViewLocalizedStrings.Cancel };

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewFilterMenuFlyout"/> class.
    /// </summary>
    public TableViewFilterMenuFlyout()
    {
        Opening += OnOpening;
        Closing += OnClosing;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="TableViewFilterMenuFlyout"/> class.
    /// </summary>
    ~TableViewFilterMenuFlyout()
    {
        Opening -= OnOpening;
        Closing -= OnClosing;
    }

    /// <inheritdoc/>
    protected override Control CreatePresenter()
    {
        var presenter = base.CreatePresenter();
        presenter.Loaded += OnPresenterLoaded;

        return presenter;
    }

    /// <summary>
    /// Handles the Opening event of the flyout, initializing the filter items control.
    /// </summary>
    private void OnOpening(object? sender, object e)
    {
        _filterItemsControl?.Initialize();
    }

    /// <summary>
    /// Handles the Closing event of the flyout, clearing the search box in the filter items control.
    /// </summary>
    private void OnClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        _filterItemsControl?.ClearSearchBox();
    }

    /// <summary>
    /// Handles the Loaded event of the MenuFlyoutPresenter.
    /// </summary>
    private void OnPresenterLoaded(object sender, RoutedEventArgs e)
    {
        var presenter = (MenuFlyoutPresenter)sender;
        var okButton = presenter.FindDescendant<Button>(b => b.Name is "OkButton");
        var cancelButton = presenter.FindDescendant<Button>(b => b.Name is "CancelButton");

        _filterItemsControl = presenter.FindDescendant<TableViewFilterItemsControl>();
        _filterItemsControl?.TableView = TableView;
        _filterItemsControl?.ColumnHeader = ColumnHeader;
        _okCommand.ExecuteRequested += delegate { ExecuteOkCommand(); };
        _cancelCommand.ExecuteRequested += delegate { Hide(); };

        okButton?.Command = _okCommand;
        cancelButton?.Command = _cancelCommand;

        presenter.Loaded -= OnPresenterLoaded;
        _filterItemsControl?.Initialize();
    }

    /// <summary>
    /// Executes the OK command, applying the filter and hiding the flyout.
    /// </summary>
    private void ExecuteOkCommand()
    {
        Hide();
        ColumnHeader?.ApplyFilter();
    }

    /// <summary>
    /// Gets or sets the TableView associated with this filter menu flyout.
    /// </summary>
    public TableView? TableView { get; set; }

    /// <summary>
    /// Gets or sets the TableViewColumnHeader associated with this filter menu flyout.
    /// </summary>
    public TableViewColumnHeader? ColumnHeader { get; set; }
}
