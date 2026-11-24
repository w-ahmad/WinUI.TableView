using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests that cell and row header content is sized by ordinary layout. Both used to overwrite their content's
/// MaxWidth and MaxHeight to work around microsoft-ui-xaml#9860, an ItemsStackPanel bug the row host does not have.
/// </summary>
[TestClass]
public class TableViewCellContentLayoutTests
{
    private const string LongText = "A value far too long to fit in a narrow column";

    [UITestMethod]
    public async Task LongText_IsTrimmedInsideItsCell()
    {
        var tableView = await CreateTableViewAsync(new GridLength(80), CreateStyle(
            new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis)));

        foreach (var cell in RealizedRows(tableView).Select(row => row.Cells[0]))
        {
            var text = (TextBlock)cell.Content;
            var textWidth = text.ActualWidth;
#if !WINDOWS
            // Uno reports the width of the slot, margins included, as the actual width.
            textWidth -= text.Margin.Left + text.Margin.Right;
#endif
            var bounds = text.TransformToVisual(cell).TransformBounds(new Rect(0, 0, textWidth, text.ActualHeight));

#if WINDOWS
            // Uno's Skia text engine stopped reporting IsTextTrimmed in 6.7 — a bare 80px TextBlock with
            // CharacterEllipsis and long text reports false too — so the flag is only asserted on Windows.
            Assert.IsTrue(text.IsTextTrimmed, "the text should have been measured at the cell's width and trimmed");
#endif
            Assert.IsTrue(text.ActualWidth <= cell.ActualWidth + 0.5d,
                $"the text should have been measured at the cell's width, not its own ({text.ActualWidth}px in a {cell.ActualWidth}px cell)");
            Assert.IsTrue(bounds.Left >= 0d && bounds.Right <= cell.ActualWidth + 0.5d,
                $"the text spans {bounds.Left}..{bounds.Right} in a {cell.ActualWidth}px wide cell");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

#if WINDOWS
    // Uno limits what a text block reports measuring to the space it was given, so text never shows as overflowing.
    [UITestMethod]
    public async Task OverflowingText_DoesNotDrawOverNeighbouringCells()
    {
        // Text that is neither trimmed nor wrapped, in a column too narrow and a row too short for it. Every other item
        // is empty, so the cell below an overflowing one and the cell to its right should both stay blank.
        var tableView = await CreateTableViewAsync(new GridLength(80), CreateStyle(
            new Setter(TextBlock.FontSizeProperty, 60d)), tableView => tableView.RowHeight = 40d,
            index => index % 2 == 0 ? LongText : string.Empty);

        var rows = RealizedRows(tableView);
        var overflowing = rows[0].Cells[0];
        var text = (TextBlock)overflowing.Content;
        Assert.IsTrue(text.ActualWidth > overflowing.ActualWidth && text.ActualHeight > overflowing.ActualHeight, "the text should overflow its cell");

        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(tableView);
        var pixels = (await bitmap.GetPixelsAsync()).ToArray();

        // The overflowing cell itself has text in it, which shows the pixels being read are the right ones.
        Assert.IsFalse(IsBlank(tableView, bitmap, pixels, overflowing), "the overflowing cell should show its text");
        Assert.IsTrue(IsBlank(tableView, bitmap, pixels, rows[0].Cells[1]), "the text was drawn over the cell to its right");
        Assert.IsTrue(IsBlank(tableView, bitmap, pixels, rows[1].Cells[0]), "the text was drawn over the cell below it");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }
#endif

    [UITestMethod]
    public async Task ContentMaxSize_FromElementStyle_IsKept_ThroughLayoutAndAResizePreview()
    {
        var tableView = await CreateTableViewAsync(new GridLength(200), CreateStyle(
            new Setter(FrameworkElement.MaxWidthProperty, 50d),
            new Setter(FrameworkElement.MaxHeightProperty, 18d)));

        var cells = RealizedRows(tableView).Select(row => row.Cells[0]).ToList();
        Assert.IsTrue(cells.Count > 0);

        AssertMaxSizeKept(cells, "after layout");

        foreach (var cell in cells)
        {
            cell.BeginResizePreview(4_000d);
        }

        await WaitForLayoutAsync(tableView);
        AssertMaxSizeKept(cells, "during a resize preview");

        foreach (var cell in cells)
        {
            cell.EndResizePreview();
        }

        await WaitForLayoutAsync(tableView);
        AssertMaxSizeKept(cells, "after a resize preview");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        static void AssertMaxSizeKept(List<TableViewCell> cells, string when)
        {
            foreach (var text in cells.Select(cell => (TextBlock)cell.Content))
            {
                Assert.AreEqual(50d, text.MaxWidth, $"the style's MaxWidth should be kept {when}");
                Assert.AreEqual(18d, text.MaxHeight, $"the style's MaxHeight should be kept {when}");
            }
        }
    }

    [UITestMethod]
    public async Task ResizePreview_LeavesNoMaxSizeOnTheContent()
    {
        var tableView = await CreateTableViewAsync(new GridLength(200));
        var cells = RealizedRows(tableView).Select(row => row.Cells[0]).ToList();

        foreach (var cell in cells)
        {
            cell.BeginResizePreview(4_000d);
            cell.EndResizePreview();
        }

        await WaitForLayoutAsync(tableView);

        foreach (var text in cells.Select(cell => (TextBlock)cell.Content))
        {
            Assert.AreEqual(double.PositiveInfinity, text.MaxWidth);
            Assert.AreEqual(double.PositiveInfinity, text.MaxHeight);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AutoWidthColumn_FitsItsLongestCell()
    {
        var tableView = await CreateTableViewAsync(GridLength.Auto);
        tableView.ColumnAutoWidthMode = TableViewColumnAutoWidthMode.Cells;
        await WaitForLayoutAsync(tableView);

        var cell = RealizedRows(tableView)[0].Cells[0];
        var text = (TextBlock)cell.Content;

        var natural = new TextBlock { Text = LongText, FontFamily = text.FontFamily, FontSize = text.FontSize };
        natural.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var needed = natural.DesiredSize.Width + text.Margin.Left + text.Margin.Right;
        Assert.IsTrue(tableView.Columns[0].ActualWidth >= needed - 1d,
            $"the column is {tableView.Columns[0].ActualWidth}px wide but its text needs {needed}px");
        Assert.IsFalse(text.IsTextTrimmed);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task FixedWidthColumn_DoesNotMeasureItsCellsUnconstrained()
    {
        var tableView = await CreateTableViewAsync(new GridLength(80));
        await ScrollToAsync(tableView, 200d);

        var column = tableView.Columns[0];
        var headerWidth = column.HeaderControl!.CachedDesiredWidth ?? 0d;

        // A fixed width never uses the width the cells want, so only the header should have reported one.
        Assert.AreEqual(headerWidth, column.DesiredWidth, 0.5, "cells of a fixed-width column were measured at their natural width");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task AutoWidthColumn_GrowsWhenAnItemsTextGrowsInPlace()
    {
        var items = new ObservableCollection<NotifyingItem>(Enumerable.Range(0, 20).Select(i => new NotifyingItem { Name = "Short" }));
        var tableView = new TableView { Width = 600, Height = 300, AutoGenerateColumns = false, ColumnAutoWidthMode = TableViewColumnAutoWidthMode.Cells };
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = GridLength.Auto,
            Binding = new Binding { Path = new PropertyPath(nameof(NotifyingItem.Name)) }
        });
        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var before = tableView.Columns[0].ActualWidth;
        items[3].Name = LongText;

        // The column re-sizes after its header row's debounce, which a busy test run can stretch.
        for (var attempt = 0; attempt < 30 && tableView.Columns[0].ActualWidth <= before + 50; attempt++)
        {
            await Task.Delay(100);
            await WaitForLayoutAsync(tableView);
        }

        var cell = RealizedRows(tableView).First(x => x.Index == 3).Cells[0];
        Assert.IsTrue(tableView.Columns[0].ActualWidth > before + 50, $"the column stayed {tableView.Columns[0].ActualWidth}px wide");
        Assert.IsFalse(((TextBlock)cell.Content).IsTextTrimmed, "the longer text should fit the widened column");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RowHeaderContent_KeepsItsOwnMaxWidth()
    {
        var tableView = await CreateTableViewAsync(new GridLength(200), configure: tableView =>
        {
            tableView.RowHeaderTemplate = (DataTemplate)XamlReader.Load(
                """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <TextBlock MaxWidth="30" MaxHeight="18" Text="A row header too long for 30px" />
                </DataTemplate>
                """);
        });

        foreach (var row in RealizedRows(tableView))
        {
            var header = row.FindDescendants().OfType<TableViewRowHeader>().First();
            var text = (TextBlock)TableViewRowHostTestHelpers.GetTemplateRoot(header)!;

            Assert.AreEqual(30d, text.MaxWidth, $"row {row.Index}'s header content should keep its MaxWidth");
            Assert.AreEqual(18d, text.MaxHeight, $"row {row.Index}'s header content should keep its MaxHeight");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    /// <summary>
    /// Returns whether every rendered pixel inside a cell, a few pixels in from its grid lines, is the same.
    /// </summary>
    private static bool IsBlank(TableView tableView, RenderTargetBitmap bitmap, byte[] pixels, TableViewCell cell)
    {
        const double inset = 4d;

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

    private static Style CreateStyle(params Setter[] setters)
    {
        var style = new Style(typeof(TextBlock));

        foreach (var setter in setters)
        {
            style.Setters.Add(setter);
        }

        return style;
    }

    private static List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0).OrderBy(row => row.Index)];
    }

    private static async Task<TableView> CreateTableViewAsync(GridLength nameWidth,
                                                            Style? nameStyle = null,
                                                            System.Action<TableView>? configure = null,
                                                            System.Func<int, string>? name = null)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = 300,
            AutoGenerateColumns = false
        };

        var nameColumn = new TableViewTextColumn
        {
            Header = "Name",
            Width = nameWidth,
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        };

        if (nameStyle is not null)
        {
            nameColumn.ElementStyle = nameStyle;
        }

        tableView.Columns.Add(nameColumn);
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(120),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Category)) }
        });

        configure?.Invoke(tableView);

        tableView.ItemsSource = new ObservableCollection<HostItem>(Enumerable.Range(0, 20).Select(i => new HostItem
        {
            Id = i,
            Name = name?.Invoke(i) ?? LongText,
            Category = string.Empty
        }));

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }
}

/// <summary>
/// An item that reports changes to its name. Public, because the binding engine observes only public types.
/// </summary>
public sealed class NotifyingItem : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    public string Name
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new(nameof(Name)));
        }
    } = string.Empty;
}
