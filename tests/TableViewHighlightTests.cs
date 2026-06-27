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
    public void TableView_Constructor_InitializesColumnHighlights()
    {
        var tableView = new TableView();

        Assert.IsNotNull(tableView.ColumnHighlights);
        Assert.AreEqual(0, tableView.ColumnHighlights.Count);
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
    public void HighlightColumn_AddsHighlight()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var column = new TableViewTextColumn();
        tableView.Columns.Add(column);

        var background = new SolidColorBrush(Colors.Gold);
        var foreground = new SolidColorBrush(Colors.Black);

        tableView.HighlightColumn(0, background, foreground);

        var highlight = tableView.GetColumnHighlight(0);

        Assert.IsNotNull(highlight);
        Assert.AreEqual(0, highlight.Index);
        Assert.AreSame(background, highlight.Background);
        Assert.AreSame(foreground, highlight.Foreground);
    }

    [UITestMethod]
    public void HighlightColumn_UpdatesExistingHighlight()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var column = new TableViewTextColumn();
        tableView.Columns.Add(column);

        var newBackground = new SolidColorBrush(Colors.SeaGreen);

        tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold), new SolidColorBrush(Colors.Black));
        tableView.HighlightColumn(0, newBackground);

        Assert.AreEqual(1, tableView.ColumnHighlights.Count);

        var highlight = tableView.GetColumnHighlight(0);

        Assert.IsNotNull(highlight);
        Assert.AreSame(newBackground, highlight.Background);
        Assert.IsNull(highlight.Foreground);
    }

    [UITestMethod]
    public void HighlightColumn_ByColumnObject_AddsHighlight()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var first = new TableViewTextColumn();
        var second = new TableViewTextColumn();
        tableView.Columns.Add(first);
        tableView.Columns.Add(second);

        var background = new SolidColorBrush(Colors.Gold);

        tableView.HighlightColumn(second, background);

        var highlight = tableView.GetColumnHighlight(1);

        Assert.IsNotNull(highlight);
        Assert.AreSame(background, highlight.Background);
    }

    [UITestMethod]
    public void HighlightColumn_ThrowsOnInvalidIndex()
    {
        var tableView = new TableView { AutoGenerateColumns = false };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold)));
    }

    [UITestMethod]
    public void GetColumnHighlight_ReturnsNullForUnhighlightedColumn()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var column = new TableViewTextColumn();
        tableView.Columns.Add(column);

        tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold));

        Assert.IsNull(tableView.GetColumnHighlight(1));
        Assert.IsNull(tableView.GetColumnHighlight(-1));
    }

    [UITestMethod]
    public void GetColumnHighlight_LastMatchingHighlightWins()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var first = new TableViewColumnHighlight { Index = 0, Background = new SolidColorBrush(Colors.Gold) };
        var second = new TableViewColumnHighlight { Index = 0, Background = new SolidColorBrush(Colors.SeaGreen) };

        tableView.ColumnHighlights.Add(first);
        tableView.ColumnHighlights.Add(second);

        Assert.AreSame(second, tableView.GetColumnHighlight(0));
    }

    [UITestMethod]
    public void ClearColumnHighlight_RemovesHighlightsOfColumn()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var first = new TableViewTextColumn();
        var second = new TableViewTextColumn();
        tableView.Columns.Add(first);
        tableView.Columns.Add(second);

        tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold));
        tableView.HighlightColumn(1, new SolidColorBrush(Colors.SeaGreen));
        tableView.ClearColumnHighlight(first);

        Assert.IsNull(tableView.GetColumnHighlight(0));
        Assert.IsNotNull(tableView.GetColumnHighlight(1));
        Assert.AreEqual(1, tableView.ColumnHighlights.Count);
    }

    [UITestMethod]
    public void ClearColumnHighlights_RemovesAllHighlights()
    {
        var tableView = new TableView { AutoGenerateColumns = false };
        var first = new TableViewTextColumn();
        var second = new TableViewTextColumn();
        tableView.Columns.Add(first);
        tableView.Columns.Add(second);

        tableView.HighlightColumn(0, new SolidColorBrush(Colors.Gold));
        tableView.HighlightColumn(1, new SolidColorBrush(Colors.SeaGreen));
        tableView.ClearColumnHighlights();

        Assert.AreEqual(0, tableView.ColumnHighlights.Count);
    }
}
