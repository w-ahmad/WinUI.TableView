namespace WinUI.TableView;

/// <summary>
/// Defines a contract for connecting generated TableView helpers to named TableView instances.
/// </summary>
public interface ITableViewConnector
{
    /// <summary>
    /// Connects a generated provider to the specified <see cref="TableView"/> instance.
    /// </summary>
    /// <param name="tableView">The target <see cref="TableView"/> to connect.</param>
    void ConnectTableView(TableView tableView);

    /// <summary>
    /// Connects generated providers to their corresponding TableView controls.
    /// </summary>
    void ConnectTableViews();
}
