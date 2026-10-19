using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests what a cell draws behind its content: the selection tint, the current cell's border, its own background and
/// the pointer-over highlight, and that the whole cell can be hit, whichever way the template builds them.
/// </summary>
[TestClass]
public class TableViewCellVisualsTests
{
    private const double ViewportHeight = 400d;

    [UITestMethod]
    public async Task SelectedCell_ShowsASelectionTint_AndOthersDoNot()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(2, 0), new TableViewCellSlot(3, 1)));
        await WaitForLayoutAsync(tableView);

        foreach (var cell in RealizedRows(tableView).SelectMany(row => row.Cells))
        {
            Assert.AreEqual(cell.IsSelected, ShowsFill(Layer(cell, "SelectionBorder")?.Background), $"cell {cell.Slot}");
        }

        Assert.IsTrue(GetCell(tableView, 2, 0).IsSelected && GetCell(tableView, 3, 1).IsSelected && !GetCell(tableView, 4, 0).IsSelected);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task DeselectedCell_ShowsNoSelectionTint()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var slot = new TableViewCellSlot(2, 0);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(slot, slot));
        await WaitForLayoutAsync(tableView);
        Assert.IsTrue(ShowsFill(Layer(GetCell(tableView, 2, 0), "SelectionBorder")?.Background));

        tableView.DeselectAll();
        await WaitForLayoutAsync(tableView);

        Assert.IsFalse(GetCell(tableView, 2, 0).IsSelected);
        Assert.IsFalse(ShowsFill(Layer(GetCell(tableView, 2, 0), "SelectionBorder")?.Background));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CurrentCell_ShowsABorder_AndOthersDoNot()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        tableView.CurrentCellSlot = new TableViewCellSlot(1, 1);
        await WaitForLayoutAsync(tableView);

        foreach (var cell in RealizedRows(tableView).SelectMany(row => row.Cells))
        {
            var layer = Layer(cell, "SelectionBorder");

            Assert.AreEqual(cell.IsCurrent, ShowsFill(layer?.BorderBrush), $"cell {cell.Slot}");

            if (cell.IsCurrent)
            {
                Assert.AreEqual(2d, layer!.BorderThickness.Left, "the current cell's border is two pixels thick");
                Assert.AreEqual(2d, layer.BorderThickness.Top);
                Assert.AreEqual(4d, layer.CornerRadius.TopLeft, "and rounded");
            }
        }

        tableView.CurrentCellSlot = new TableViewCellSlot(2, 0);
        await WaitForLayoutAsync(tableView);

        Assert.IsFalse(ShowsFill(Layer(GetCell(tableView, 1, 1), "SelectionBorder")?.BorderBrush), "the old current cell keeps its border");
        Assert.IsTrue(ShowsFill(Layer(GetCell(tableView, 2, 0), "SelectionBorder")?.BorderBrush));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ACellThatIsSelectedAndCurrent_ShowsBoth()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var slot = new TableViewCellSlot(1, 1);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(slot, slot));
        tableView.CurrentCellSlot = slot;
        await WaitForLayoutAsync(tableView);

        var layer = Layer(GetCell(tableView, 1, 1), "SelectionBorder");
        Assert.IsTrue(ShowsFill(layer?.Background) && ShowsFill(layer?.BorderBrush));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectionTint_AndCurrentBorder_FollowTheTheme()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var slot = new TableViewCellSlot(1, 0);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(slot, slot));
        tableView.CurrentCellSlot = slot;
        tableView.RequestedTheme = ElementTheme.Light;
        await WaitForLayoutAsync(tableView);

        var layer = Layer(GetCell(tableView, 1, 0), "SelectionBorder");
        var lightTint = ((SolidColorBrush)layer!.Background).Color;
        var lightBorder = ((SolidColorBrush)layer.BorderBrush).Color;

        tableView.RequestedTheme = ElementTheme.Dark;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);

        layer = Layer(GetCell(tableView, 1, 0), "SelectionBorder");
        Assert.AreNotEqual(lightTint, ((SolidColorBrush)layer!.Background).Color, "the selection tint ignored the theme change");
        Assert.AreNotEqual(lightBorder, ((SolidColorBrush)layer.BorderBrush).Color, "the current cell's border ignored the theme change");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CellBackground_IsShown_AndHiddenAgain()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var cell = GetCell(tableView, 1, 0);
        Assert.IsFalse(ShowsFill(Layer(cell, "BackgroundBorder")?.Background), "a cell has no background by default");

        cell.Background = new SolidColorBrush(Colors.Red);
        await WaitForLayoutAsync(tableView);

        var layer = Layer(cell, "BackgroundBorder");
        Assert.IsTrue(ShowsFill(layer?.Background), "the cell's background is not shown");
        Assert.AreEqual(Colors.Red, ((SolidColorBrush)layer!.Background).Color);
        Assert.AreEqual(4d, layer.CornerRadius.TopLeft);

        cell.Background = new SolidColorBrush(Colors.Blue) { Opacity = 0 };
        await WaitForLayoutAsync(tableView);
        Assert.IsFalse(ShowsFill(Layer(cell, "BackgroundBorder")?.Background));

        cell.Background = new SolidColorBrush(Colors.Green);
        await WaitForLayoutAsync(tableView);
        Assert.AreEqual(Colors.Green, ((SolidColorBrush)Layer(cell, "BackgroundBorder")!.Background).Color);

        cell.Background = null;
        await WaitForLayoutAsync(tableView);
        Assert.IsFalse(ShowsFill(Layer(cell, "BackgroundBorder")?.Background));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ConditionalCellStyle_Background_IsShown_AndFollowsTheItem()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.ConditionalCellStyles.Add(new TableViewConditionalCellStyle
        {
            Predicate = context => context.DataItem is HostItem { Id: var id } && id % 2 == 0,
            Style = (Style)XamlReader.Load("""
                <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:local="using:WinUI.TableView" TargetType="local:TableViewCell">
                  <Setter Property="Background" Value="Red" />
                </Style>
                """)
        });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        AssertBackgroundsFollowTheItems(tableView);

        // The rows are reused for other items, whose cells must not keep what the last item's had.
        await ScrollToAsync(tableView, 20_001d);
        AssertBackgroundsFollowTheItems(tableView);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        static void AssertBackgroundsFollowTheItems(TableView tableView)
        {
            var cells = RealizedRows(tableView).SelectMany(row => row.Cells).ToList();
            Assert.IsTrue(cells.Count > 0);

            foreach (var cell in cells)
            {
                var expectRed = ((HostItem)cell.Row!.Content!).Id % 2 == 0;
                Assert.AreEqual(expectRed, ShowsFill(Layer(cell, "BackgroundBorder")?.Background), $"cell {cell.Slot} (item {((HostItem)cell.Row.Content!).Id})");
            }
        }
    }

    [UITestMethod]
    public async Task PointerOver_HighlightsTheCell_AndNormalClearsIt()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var cell = GetCell(tableView, 1, 0);
        var root = Layer(cell, "RootBorder");
        Assert.IsNotNull(root);
        Assert.IsFalse(ShowsFill(root.Background));

        VisualStateManager.GoToState(cell, VisualStates.StatePointerOver, false);
        Assert.IsTrue(ShowsFill(root.Background), "the pointer-over highlight is not shown");

        VisualStateManager.GoToState(cell, VisualStates.StateNormal, false);
        Assert.IsFalse(ShowsFill(root.Background));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task TheWholeCell_CanBeHit_EvenWhereItShowsNothing()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50, nameOf: id => id == 1 ? string.Empty : $"Item {id}");
        var cell = GetCell(tableView, 1, 0);
        Assert.AreEqual(string.Empty, ((TextBlock)cell.Content).Text);

        // Only an element with a background can be hit where it draws nothing, so one has to span the cell: without it,
        // clicking the blank part of a cell would fall through to whatever is behind.
        var surface = cell.FindDescendants().OfType<FrameworkElement>().FirstOrDefault(x => x is Panel { Background: not null } or Border { Background: not null }
                                                                                            && x.ActualWidth >= cell.ActualWidth - 2d && x.ActualHeight >= cell.ActualHeight - 2d);
        Assert.IsNotNull(surface, "nothing in the cell can be hit across its whole area");

#if WINDOWS
        var bounds = cell.TransformToVisual(null).TransformBounds(new Rect(0, 0, cell.ActualWidth, cell.ActualHeight));
        var point = new Point(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
        var hit = VisualTreeHelper.FindElementsInHostCoordinates(point, tableView).ToList();

        Assert.IsTrue(hit.Any(element => ReferenceEquals(element, cell) || element.FindAscendants().Any(x => ReferenceEquals(x, cell))),
            "a click on the empty part of a cell does not reach the cell");
#endif

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RecycledCells_ShowNothingTheLastItemHad()
    {
        var tableView = await CreateTableViewAsync(itemCount: 2_000);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(new TableViewCellSlot(0, 0), new TableViewCellSlot(3, 1)));
        tableView.CurrentCellSlot = new TableViewCellSlot(1, 1);
        await WaitForLayoutAsync(tableView);
        AssertLayersMatchTheCells(tableView);

        // The first rows go back to the pool and are shown again for far away items, then for the first ones again.
        await ScrollToAsync(tableView, 20_000d);
        AssertLayersMatchTheCells(tableView);

        await ScrollToAsync(tableView, 0d);
        AssertLayersMatchTheCells(tableView);
        Assert.IsTrue(GetCell(tableView, 1, 1).IsCurrent && GetCell(tableView, 1, 1).IsSelected);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        static void AssertLayersMatchTheCells(TableView tableView)
        {
            foreach (var cell in RealizedRows(tableView).SelectMany(row => row.Cells))
            {
                var layer = Layer(cell, "SelectionBorder");

                Assert.AreEqual(cell.IsSelected, ShowsFill(layer?.Background), $"cell {cell.Slot}'s selection tint");
                Assert.AreEqual(cell.IsCurrent, ShowsFill(layer?.BorderBrush), $"cell {cell.Slot}'s current border");
            }
        }
    }

    [UITestMethod]
    public async Task ATemplateWithTheLayersOfItsOwn_KeepsDrawingThemThroughItsStates()
    {
        var tableView = CreateTableView(itemCount: 50);
        tableView.CellStyle = (Style)XamlReader.Load("""
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                   xmlns:local="using:WinUI.TableView" TargetType="local:TableViewCell">
              <Setter Property="Template">
                <Setter.Value>
                  <ControlTemplate TargetType="local:TableViewCell">
                    <Grid>
                      <VisualStateManager.VisualStateGroups>
                        <VisualStateGroup x:Name="SelectionStates">
                          <VisualState x:Name="Selected">
                            <VisualState.Setters>
                              <Setter Target="SelectionBorder.Background" Value="Blue" />
                            </VisualState.Setters>
                          </VisualState>
                          <VisualState x:Name="Unselected" />
                        </VisualStateGroup>
                        <VisualStateGroup x:Name="CurrentStates">
                          <VisualState x:Name="Regular" />
                          <VisualState x:Name="Current">
                            <VisualState.Setters>
                              <Setter Target="SelectionBorder.BorderBrush" Value="Green" />
                            </VisualState.Setters>
                          </VisualState>
                        </VisualStateGroup>
                      </VisualStateManager.VisualStateGroups>
                      <Border x:Name="BackgroundBorder" Background="{TemplateBinding Background}" />
                      <Border x:Name="SelectionBorder" BorderThickness="3" />
                      <Border x:Name="RootBorder">
                        <ContentPresenter x:Name="Content" />
                      </Border>
                    </Grid>
                  </ControlTemplate>
                </Setter.Value>
              </Setter>
            </Style>
            """);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var slot = new TableViewCellSlot(1, 0);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(slot, slot));
        tableView.CurrentCellSlot = slot;
        await WaitForLayoutAsync(tableView);

        var cell = GetCell(tableView, 1, 0);
        var layer = Layer(cell, "SelectionBorder");
        Assert.AreEqual(Colors.Blue, ((SolidColorBrush)layer!.Background).Color, "the template's own selected state is not applied");
        Assert.AreEqual(Colors.Green, ((SolidColorBrush)layer.BorderBrush).Color, "the template's own current state is not applied");
        Assert.AreEqual(3d, layer.BorderThickness.Left);
        Assert.AreEqual(1, cell.FindDescendants().OfType<Border>().Count(x => x.Name is "SelectionBorder"), "the cell added a layer of its own to a template that has one");

        cell.Background = new SolidColorBrush(Colors.Red);
        await WaitForLayoutAsync(tableView);
        Assert.AreEqual(Colors.Red, ((SolidColorBrush)Layer(cell, "BackgroundBorder")!.Background).Color);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ATemplateWithoutLayers_ShowsNoneAndStillWorks()
    {
        var tableView = CreateTableView(itemCount: 50);
        tableView.CellStyle = (Style)XamlReader.Load("""
            <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                   xmlns:local="using:WinUI.TableView" TargetType="local:TableViewCell">
              <Setter Property="Template">
                <Setter.Value>
                  <ControlTemplate TargetType="local:TableViewCell">
                    <ContentPresenter x:Name="Content" />
                  </ControlTemplate>
                </Setter.Value>
              </Setter>
            </Style>
            """);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var slot = new TableViewCellSlot(1, 0);
        tableView.SelectCellRange(TableViewCellSlotRange.FromSlots(slot, slot));
        tableView.CurrentCellSlot = slot;
        GetCell(tableView, 1, 0).Background = new SolidColorBrush(Colors.Red);
        await WaitForLayoutAsync(tableView);

        var cell = GetCell(tableView, 1, 0);
        Assert.IsTrue(cell.IsSelected && cell.IsCurrent);
        Assert.AreEqual(0, cell.FindDescendants().OfType<Border>().Count(), "a template without layers was given some");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AutoWidthColumn_AddsTheCellsChrome_ToItsContent()
    {
        var tableView = CreateTableView(itemCount: 50, width: GridLength.Auto, nameOf: id => $"A long value so that the cells, not the header, decide the width {id}");
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        tableView.ColumnAutoWidthMode = TableViewColumnAutoWidthMode.Cells;
        await WaitForLayoutAsync(tableView);

        var cell = GetCell(tableView, 1, 0);
        var column = tableView.Columns[0];
        var cellContentWidth = ((FrameworkElement)cell.Content).DesiredSize.Width;

        // The selection border's thickness on both sides and the vertical grid line, around the content and its
        // margin. How much of the margin the content's desired size reports moved between Uno releases, so the
        // chrome is bounded rather than exact: at least the layers' widths, at most them plus the margin.
        var chrome = column.ActualWidth - cellContentWidth;
        Assert.IsTrue(column.ActualWidth >= cellContentWidth, $"the column ({column.ActualWidth}px) is narrower than its content ({cellContentWidth}px)");
        Assert.IsTrue(chrome >= MinimumChrome - 0.5d && chrome <= ExpectedChrome + 0.5d, $"the column is {column.ActualWidth}px wide for {cellContentWidth}px of content");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    // What the cell added around its content before its layers became lazy, the same on every platform.
    private const double ExpectedChrome = 13d;

    // The selection border's two sides and the vertical grid line, with nothing of the content's margin.
    private const double MinimumChrome = 5d;

    private static Border? Layer(TableViewCell cell, string name)
    {
        return cell.FindDescendants().OfType<Border>().FirstOrDefault(x => x.Name == name);
    }

    private static bool ShowsFill(Brush? brush)
    {
        return brush switch
        {
            null => false,
            SolidColorBrush solid => solid.Opacity > 0d && solid.Color.A > 0,
            _ => true
        };
    }

    [UITestMethod]
    public async Task EveryRealizedCell_InheritsItsRowsItem()
    {
        var tableView = CreateTableView(itemCount: 500);

        // Enough columns that most are out of view sideways, as the sample pages have.
        for (var extra = 0; extra < 8; extra++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Extra {extra}",
                Width = new GridLength(150),
                Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
            });
        }

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        var scrollViewer = GetScrollViewer(tableView);

        // Recycle rows a few times, in both directions, and move the columns in view sideways.
        foreach (var (vertical, horizontal) in new[] { (3000d, 0d), (9000d, 600d), (1500d, 0d) })
        {
            scrollViewer.ChangeView(horizontal, vertical, null, disableAnimation: true);
            await WaitForLayoutAsync(tableView);
            await Task.Delay(100);
            await WaitForLayoutAsync(tableView);

            foreach (var row in RealizedRows(tableView))
            {
                foreach (var cell in row.Cells)
                {
                    Assert.AreSame(row.Content, cell.DataContext,
                        $"cell {row.Index},{cell.Slot.Column} at {vertical}/{horizontal} inherits {cell.DataContext?.GetType().Name} instead of its row's item");
                }
            }
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CellContextFlyout_LeavesTheCellsTextVisible()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        tableView.ForceRowOrCellSelectionOnContextRequested = true;
        tableView.CellContextFlyout = new MenuFlyout { Items = { new MenuFlyoutItem { Text = "Copy" } } };
        // The sample's pattern: the opening handler tags the items with the clicked slot. Tagging the cell element
        // itself must stay out of the samples: on Uno it moves the cell's DataContext inheritance to the menu item.
        tableView.CellContextFlyoutOpening += (_, e) =>
        {
            foreach (var item in ((MenuFlyout)e.Flyout!).Items)
            {
                item.Tag = e.Slot;
            }
        };
        await WaitForLayoutAsync(tableView);

        // What the cell does when it is right-clicked.
        var cell = GetCell(tableView, 4, 0);
        var expectedContext = cell.DataContext;
        tableView.MakeSelection(cell.Slot, false);
        var shown = tableView.ShowCellContext(cell, new Point(10, 10));
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(shown, "the context flyout did not open");

        var text = cell.FindDescendants().OfType<TextBlock>().FirstOrDefault();

        Assert.IsNotNull(text, "the cell lost its text element");
        Assert.AreSame(expectedContext, cell.DataContext,
            $"the cell inherits {cell.DataContext?.GetType().Name} instead of its row's item after the flyout opened");
        Assert.AreEqual("Item 4", text.Text);
        Assert.AreEqual(Visibility.Visible, text.Visibility);
        Assert.IsTrue(text.ActualWidth > 0, $"the text has no width ({text.ActualWidth})");

        tableView.CellContextFlyout.Hide();
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);

#if !__WASM__
        // The cell interior should still render its text once the flyout is gone.
        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
        await bitmap.RenderAsync(tableView);
        var pixels = (await bitmap.GetPixelsAsync()).ToArray();
        Assert.IsFalse(IsUniform(tableView, bitmap, pixels, cell), "the right-clicked cell rendered without its text");
#endif

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    /// <summary>
    /// Returns whether every rendered pixel inside a cell, a few pixels in from its edges, is the same: a cell showing
    /// text is not uniform.
    /// </summary>
    private static bool IsUniform(TableView tableView, Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap bitmap, byte[] pixels, TableViewCell cell)
    {
        const double inset = 5d;

        var scale = bitmap.PixelWidth / tableView.ActualWidth;
        var bounds = cell.TransformToVisual(tableView).TransformBounds(new Rect(0, 0, cell.ActualWidth, cell.ActualHeight));
        var left = (int)((bounds.Left + inset) * scale);
        var right = (int)((bounds.Right - inset) * scale);
        var top = (int)((bounds.Top + inset) * scale);
        var bottom = (int)((bounds.Bottom - inset) * scale);
        var first = PixelAt(left, top);

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                if (PixelAt(x, y) != first)
                {
                    return false;
                }
            }
        }

        return true;

        uint PixelAt(int x, int y) => System.BitConverter.ToUInt32(pixels, ((y * bitmap.PixelWidth) + x) * 4);
    }

    private static TableViewCell GetCell(TableView tableView, int row, int column)
    {
        return RealizedRows(tableView).First(x => x.Index == row).Cells[column];
    }

    private static System.Collections.Generic.List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0)];
    }

    private static async Task<TableView> CreateTableViewAsync(int itemCount, System.Func<int, string>? nameOf = null)
    {
        var tableView = CreateTableView(itemCount, nameOf: nameOf);
        tableView.SelectionUnit = TableViewSelectionUnit.CellOrRow;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private static TableView CreateTableView(int itemCount, GridLength? width = null, System.Func<int, string>? nameOf = null)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = ViewportHeight,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended,
            SelectionUnit = TableViewSelectionUnit.CellOrRow
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = width ?? new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Category)) }
        });

        tableView.ItemsSource = new ObservableCollection<HostItem>(
            Enumerable.Range(0, itemCount).Select(i => new HostItem { Id = i, Name = nameOf?.Invoke(i) ?? $"Item {i}", Category = "A" }));

        return tableView;
    }
}
