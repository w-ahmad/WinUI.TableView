using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewRowDetailsLayoutTests
{
    // A details template whose content (a horizontally scrolling code line) is far wider than the table.
    private const string WideDetailsTemplate =
        """
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            <StackPanel Padding="16,8,16,14" Spacing="6">
                <TextBlock Text="{Binding Name}" />
                <Border Padding="10,8" Background="Gray" CornerRadius="4">
                    <ScrollViewer HorizontalScrollBarVisibility="Auto" HorizontalScrollMode="Enabled" VerticalScrollMode="Disabled">
                        <TextBlock FontFamily="Consolas" Text="{Binding Code}" />
                    </ScrollViewer>
                </Border>
            </StackPanel>
        </DataTemplate>
        """;

    [UITestMethod]
    public async Task ExpandingWideDetails_WithUnboundedHeight_DoesNotCycleLayout()
    {
        var (host, tableView) = await CreateUnboundedHostAsync();
        try
        {
            var row = GetRow(tableView, 0);

            row.RowPresenter!.ShowDetailPane(visible: true);
            host.UpdateLayout(); // throws "Layout cycle detected" if layout never settles
            await Task.Delay(300);
            host.UpdateLayout();

            Assert.IsTrue(row.RowPresenter.IsDetailsPanelVisible, "The details panel is open");
            Assert.IsTrue(row.RowPresenter.GetDetailsContentHeight() > 0, "The details panel has a height");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(host);
        }
    }

    [UITestMethod]
    public async Task WideDetails_ScrollInsideTheDetailsInsteadOfWideningTheRow()
    {
        var (host, tableView) = await CreateUnboundedHostAsync();
        try
        {
            var row = GetRow(tableView, 0);
            var rowWidth = row.ActualWidth;

            row.RowPresenter!.ShowDetailPane(visible: true);
            host.UpdateLayout();
            await Task.Delay(300);
            host.UpdateLayout();

            var codeViewer = row.FindDescendants().OfType<ScrollViewer>().First();

            Assert.AreEqual(rowWidth, row.ActualWidth, 1, "Opening the details does not widen the row");
            AssertHeadersAlignWithCells(tableView, row);
            Assert.IsTrue(codeViewer.ViewportWidth < tableView.ActualWidth, "The code line's viewer fits within the table");
            Assert.IsTrue(codeViewer.ScrollableWidth > 0, "The long code line scrolls horizontally inside the details");

            row.RowPresenter.ShowDetailPane(visible: false);
            host.UpdateLayout();
            await Task.Delay(300);
            host.UpdateLayout();

            Assert.IsFalse(row.RowPresenter.IsDetailsPanelVisible, "The details panel is closed");
            Assert.AreEqual(0, row.RowPresenter.GetDetailsContentHeight(), "A closed details panel has no height");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(host);
        }
    }

    [UITestMethod]
    public Task ScrollingTheTableHorizontally_MovesWideDetailsWithTheCells()
    {
        return AssertHorizontalScrollAsync(areRowDetailsFrozen: false);
    }

    [UITestMethod]
    public Task ScrollingTheTableHorizontally_KeepsFrozenWideDetailsInPlace()
    {
        return AssertHorizontalScrollAsync(areRowDetailsFrozen: true);
    }

    private static async Task AssertHorizontalScrollAsync(bool areRowDetailsFrozen)
    {
        var (host, tableView) = await CreateUnboundedHostAsync(tableView =>
        {
            // Fixed columns wider than the table, so the table itself scrolls horizontally.
            tableView.Width = 400;
            tableView.Columns[0].Width = new GridLength(300);
            tableView.AreRowDetailsFrozen = areRowDetailsFrozen;
        });
        try
        {
            var row = GetRow(tableView, 0);
            row.RowPresenter!.ShowDetailPane(visible: true);
            host.UpdateLayout();
            await Task.Delay(300);

            var details = GetDetailsPanel(row);
            var cell = row.Cells[0];
            var detailsLeft = details.TransformToVisual(row).TransformPoint(default).X;
            var cellLeft = cell.TransformToVisual(row).TransformPoint(default).X;

            tableView.SetValue(TableView.HorizontalOffsetProperty, 100d);
            host.UpdateLayout();
            await Task.Delay(300);
            host.UpdateLayout();

            Assert.AreEqual(100d, tableView.HorizontalOffset, 0.5, "Precondition: the table scrolled horizontally");
            Assert.AreEqual(cellLeft - 100, cell.TransformToVisual(row).TransformPoint(default).X, 1, "The cells scroll with the table");
            Assert.AreEqual(areRowDetailsFrozen ? detailsLeft : detailsLeft - 100,
                            details.TransformToVisual(row).TransformPoint(default).X, 1,
                            areRowDetailsFrozen ? "Frozen details stay put" : "Details scroll with the cells");
            AssertHeadersAlignWithCells(tableView, row);
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(host);
        }
    }

#if WINDOWS // The selection background and indicator belong to the Windows ListViewItemPresenter template.
    [UITestMethod]
    public async Task SelectingARowWithWideDetails_KeepsTheSelectionVisualsOnTheCells()
    {
        var (host, tableView) = await CreateUnboundedHostAsync(tableView => tableView.SelectionMode = ListViewSelectionMode.Single);
        try
        {
            var row = GetRow(tableView, 0);
            row.RowPresenter!.ShowDetailPane(visible: true);
            host.UpdateLayout();
            await Task.Delay(300);

            tableView.SelectedIndex = 0;
            host.UpdateLayout();
            await Task.Delay(300);
            host.UpdateLayout();

            var detailsHeight = row.RowPresenter.GetDetailsContentHeight();
            var cellsHeight = row.ActualHeight - detailsHeight;
            Assert.IsTrue(detailsHeight > 0, "Precondition: the details panel is open");
            Assert.IsTrue(row.IsSelected, "Precondition: the row is selected");

            var background = row.FindDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "SelectionBackground");
            Assert.IsNotNull(background, "Precondition: the selection background exists");
            Assert.IsTrue(background!.Margin.Bottom >= detailsHeight, "The selection background leaves the details uncovered");

            var indicator = row.FindDescendants().OfType<Border>().FirstOrDefault(b => b.Width == 3);
            Assert.IsNotNull(indicator, "Precondition: the selection indicator exists");
            var indicatorTop = indicator!.TransformToVisual(row).TransformPoint(default).Y;
            var indicatorCenter = indicatorTop + (indicator.ActualHeight / 2);
            Assert.IsTrue(indicatorCenter > 0 && indicatorCenter < cellsHeight, "The selection indicator sits beside the cells, not the details");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(host);
        }
    }

#endif

    [UITestMethod]
    public Task RowCells_LineUpWithColumnHeaders()
    {
        return AssertCellsLineUpWithHeadersAsync(ListViewSelectionMode.Single, TableViewRowDetailsVisibilityMode.Collapsed);
    }

    [UITestMethod]
    public Task RowCells_LineUpWithColumnHeaders_InMultipleSelectionMode()
    {
        return AssertCellsLineUpWithHeadersAsync(ListViewSelectionMode.Multiple, TableViewRowDetailsVisibilityMode.Collapsed);
    }

    [UITestMethod]
    public Task RowCells_LineUpWithColumnHeaders_WithExpandableDetails()
    {
        return AssertCellsLineUpWithHeadersAsync(ListViewSelectionMode.Single, TableViewRowDetailsVisibilityMode.VisibleWhenExpanded);
    }

    private static async Task AssertCellsLineUpWithHeadersAsync(ListViewSelectionMode selectionMode, TableViewRowDetailsVisibilityMode detailsMode)
    {
        var tableView = CreateTableView(detailsMode);
        tableView.Width = 500;
        tableView.Height = 300;
        tableView.SelectionMode = selectionMode;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            await Task.Delay(300);
            AssertHeadersAlignWithCells(tableView, GetRow(tableView, 1));
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    private static void AssertHeadersAlignWithCells(TableView tableView, TableViewRow row)
    {
        foreach (var column in tableView.Columns)
        {
            var header = column.HeaderControl;
            var cell = row.Cells.First(c => c.Column == column);
            Assert.IsNotNull(header, "Precondition: the column header is realized");

            var headerLeft = header!.TransformToVisual(null).TransformPoint(default).X;
            var cellLeft = cell.TransformToVisual(null).TransformPoint(default).X;
            Assert.AreEqual(headerLeft, cellLeft, 1, $"The '{column.Header}' cell lines up with its header");
        }
    }

    private static Panel GetDetailsPanel(TableViewRow row)
    {
        var details = row.FindDescendants().OfType<Panel>().FirstOrDefault(p => p.Name == "DetailsPanel");
        Assert.IsNotNull(details, "Precondition: the details panel is in the row template");
        return details!;
    }

    private static TableViewRow GetRow(TableView tableView, int index)
    {
        var row = tableView.FindDescendants().OfType<TableViewRow>().FirstOrDefault(r => r.Index == index);
        Assert.IsNotNull(row, $"Precondition: row {index} is realized");
        return row!;
    }

    // The TableView sits in a vertical StackPanel inside a ScrollViewer, so it is measured with unbounded height.
    private static async Task<(FrameworkElement Host, TableView TableView)> CreateUnboundedHostAsync(Action<TableView>? configure = null)
    {
        var tableView = CreateTableView(TableViewRowDetailsVisibilityMode.VisibleWhenExpanded);
        configure?.Invoke(tableView);

        var host = new ScrollViewer
        {
            HorizontalScrollMode = ScrollMode.Disabled,
            Content = new StackPanel
            {
                Padding = new Thickness(24),
                Spacing = 14,
                Children =
                {
                    new TextBlock { Text = "Steps" },
                    tableView
                }
            }
        };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(host);
        await Task.Delay(300);

        return (host, tableView);
    }

    private static TableView CreateTableView(TableViewRowDetailsVisibilityMode detailsMode)
    {
        var tableView = new TableView
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = ListViewSelectionMode.None,
            RowDetailsVisibilityMode = detailsMode,
            RowDetailsTemplate = (DataTemplate)XamlReader.Load(WideDetailsTemplate)
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(1, GridUnitType.Star),
            Binding = new Binding { Path = new PropertyPath(nameof(CodeItem.Name)) }
        });
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Other",
            Width = new GridLength(150),
            Binding = new Binding { Path = new PropertyPath(nameof(CodeItem.Name)) }
        });

        var longLine = "var joined = string.Join(\", \", new[] { "
                       + string.Join(", ", Enumerable.Range(0, 40).Select(i => $"\"value{i}\""))
                       + " });";

        tableView.ItemsSource = Enumerable.Range(0, 3)
            .Select(i => new CodeItem
            {
                Name = $"Step {i}",
                Code = $"// step {i}\nvar items = Load();\n{longLine}\nreturn joined;"
            })
            .ToArray();

        return tableView;
    }

    private sealed class CodeItem
    {
        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }
}
