using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewHighlightTests
{
    [UITestMethod]
    public void TableView_Constructor_InitializesRowHighlights()
    {
        var tableView = new TableView();

        Assert.IsNotNull(tableView.RowHighlights);
        Assert.AreEqual(0, tableView.RowHighlights.Count);
    }

    [UITestMethod]
    public void HighlightRow_AddsHighlight()
    {
        var tableView = new TableView();
        var background = new SolidColorBrush(Colors.Gold);
        var foreground = new SolidColorBrush(Colors.Black);

        tableView.HighlightRow(2, background, foreground);

        var highlight = tableView.GetRowHighlight(2);

        Assert.IsNotNull(highlight);
        Assert.AreEqual(2, highlight.Index);
        Assert.AreSame(background, highlight.Background);
        Assert.AreSame(foreground, highlight.Foreground);
    }

    [UITestMethod]
    public void HighlightRow_UpdatesExistingHighlight()
    {
        var tableView = new TableView();
        var newBackground = new SolidColorBrush(Colors.SeaGreen);

        tableView.HighlightRow(2, new SolidColorBrush(Colors.Gold), new SolidColorBrush(Colors.Black));
        tableView.HighlightRow(2, newBackground);

        Assert.AreEqual(1, tableView.RowHighlights.Count);

        var highlight = tableView.GetRowHighlight(2);

        Assert.IsNotNull(highlight);
        Assert.AreSame(newBackground, highlight.Background);
        Assert.IsNull(highlight.Foreground);
    }

    [UITestMethod]
    public void HighlightRow_ThrowsOnNegativeIndex()
    {
        var tableView = new TableView();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tableView.HighlightRow(-1, new SolidColorBrush(Colors.Gold)));
    }

    [UITestMethod]
    public void GetRowHighlight_ReturnsNullForUnhighlightedRow()
    {
        var tableView = new TableView();

        tableView.HighlightRow(2, new SolidColorBrush(Colors.Gold));

        Assert.IsNull(tableView.GetRowHighlight(1));
        Assert.IsNull(tableView.GetRowHighlight(-1));
    }

    [UITestMethod]
    public void GetRowHighlight_LastMatchingHighlightWins()
    {
        var tableView = new TableView();
        var first = new TableViewRowHighlight { Index = 2, Background = new SolidColorBrush(Colors.Gold) };
        var second = new TableViewRowHighlight { Index = 2, Background = new SolidColorBrush(Colors.SeaGreen) };

        tableView.RowHighlights.Add(first);
        tableView.RowHighlights.Add(second);

        Assert.AreSame(second, tableView.GetRowHighlight(2));
    }

    [UITestMethod]
    public void ClearRowHighlight_RemovesHighlightsOfRow()
    {
        var tableView = new TableView();

        tableView.HighlightRow(2, new SolidColorBrush(Colors.Gold));
        tableView.HighlightRow(5, new SolidColorBrush(Colors.SeaGreen));
        tableView.ClearRowHighlight(2);

        Assert.IsNull(tableView.GetRowHighlight(2));
        Assert.IsNotNull(tableView.GetRowHighlight(5));
        Assert.AreEqual(1, tableView.RowHighlights.Count);
    }

    [UITestMethod]
    public void ClearRowHighlights_RemovesAllHighlights()
    {
        var tableView = new TableView();

        tableView.HighlightRow(2, new SolidColorBrush(Colors.Gold));
        tableView.HighlightRow(5, new SolidColorBrush(Colors.SeaGreen));
        tableView.ClearRowHighlights();

        Assert.AreEqual(0, tableView.RowHighlights.Count);
    }

    [UITestMethod]
    public void HighlightColumn_SetsBrushesOnColumn()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var column = new TableViewTextColumn();
        tableView.Columns.Add(column);

        var background = new SolidColorBrush(Colors.Gold);
        var foreground = new SolidColorBrush(Colors.Black);

        tableView.HighlightColumn(0, background, foreground);

        Assert.AreSame(background, column.HighlightBackground);
        Assert.AreSame(foreground, column.HighlightForeground);
    }

    [UITestMethod]
    public void HighlightColumn_ThrowsOnInvalidIndex()
    {
        var tableView = new TableView { AutoGenerateColumns = false };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold)));
    }

    [UITestMethod]
    public void ClearColumnHighlight_RemovesBrushesFromColumn()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var column = new TableViewTextColumn();
        tableView.Columns.Add(column);

        tableView.HighlightColumn(column, new SolidColorBrush(Colors.Gold), new SolidColorBrush(Colors.Black));
        tableView.ClearColumnHighlight(column);

        Assert.IsNull(column.HighlightBackground);
        Assert.IsNull(column.HighlightForeground);
    }

    [UITestMethod]
    public void ClearColumnHighlights_RemovesBrushesFromAllColumns()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var first = new TableViewTextColumn();
        var second = new TableViewTextColumn();
        tableView.Columns.Add(first);
        tableView.Columns.Add(second);

        tableView.HighlightColumn(first, new SolidColorBrush(Colors.Gold));
        tableView.HighlightColumn(second, new SolidColorBrush(Colors.SeaGreen));
        tableView.ClearColumnHighlights();

        Assert.IsNull(first.HighlightBackground);
        Assert.IsNull(second.HighlightBackground);
    }
}
