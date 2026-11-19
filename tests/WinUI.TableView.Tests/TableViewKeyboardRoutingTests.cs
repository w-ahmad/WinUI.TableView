#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Keys reach the table from inside its rows, after a control in a cell had its chance at them and before
/// the scroll viewer around the rows. Key messages are posted to the test window, so they are routed as real input.
/// </summary>
[TestClass]
public class TableViewKeyboardRoutingTests
{
    private TableView? _tableView;

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_tableView is not null)
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(_tableView);
        }
    }

    [UITestMethod]
    public async Task ArrowKeys_MoveTheCurrentCell()
    {
        var tableView = await CreateTableViewAsync();
        await FocusCellAsync(tableView, new TableViewCellSlot(2, 0));

        await PressAsync(tableView, VirtualKey.Down);
        Assert.AreEqual(new TableViewCellSlot(3, 0), tableView.CurrentCellSlot);

        await PressAsync(tableView, VirtualKey.Right);
        Assert.AreEqual(new TableViewCellSlot(3, 1), tableView.CurrentCellSlot);
    }

    [UITestMethod]
    public async Task PageDownAndEnd_MoveTheCurrentCell()
    {
        var tableView = await CreateTableViewAsync();
        await FocusCellAsync(tableView, new TableViewCellSlot(2, 0));

        await PressAsync(tableView, VirtualKey.PageDown);
        Assert.IsTrue(tableView.CurrentCellSlot?.Row > 5, $"Page Down left the current cell on row {tableView.CurrentCellSlot?.Row}");

        var row = tableView.CurrentCellSlot!.Value.Row;
        await PressAsync(tableView, VirtualKey.End);
        Assert.AreEqual(new TableViewCellSlot(row, 2), tableView.CurrentCellSlot);
    }

    [UITestMethod]
    public async Task ArrowKeys_InAControlInsideACell_StayWithTheControl()
    {
        var tableView = await CreateTableViewAsync();
        tableView.CurrentCellSlot = new TableViewCellSlot(1, 0);
        await SettleAsync(tableView);

        var textBox = tableView.Rows.First(row => row.Index == 1).FindDescendants().OfType<TextBox>().First();
        RequireFocus(textBox.Focus(FocusState.Programmatic));
        textBox.SelectionStart = 3;

        await PressAsync(tableView, VirtualKey.Left);

        Assert.AreEqual(2, textBox.SelectionStart, "the text box should have moved its caret");
        Assert.AreSame(textBox, Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(tableView.XamlRoot));
        Assert.AreEqual(new TableViewCellSlot(1, 0), tableView.CurrentCellSlot);
    }

    private static async Task FocusCellAsync(TableView tableView, TableViewCellSlot slot)
    {
        tableView.CurrentCellSlot = slot;
        await SettleAsync(tableView);
        RequireFocus(tableView.GetCellFromSlot(slot)!.Focus(FocusState.Programmatic));
    }

    private static void RequireFocus(bool focused)
    {
        if (!focused)
        {
            Assert.Inconclusive("The test window cannot take keyboard focus here.");
        }
    }

    private static async Task PressAsync(TableView tableView, VirtualKey key)
    {
        var input = FindInputWindow();
        var scanCode = MapVirtualKey((uint)key, 0);
        var flags = (long)(scanCode << 16) | (1L << 24);

        PostMessage(input, 0x0100, (IntPtr)(int)key, (IntPtr)(1L | flags));
        await Task.Delay(60);
        PostMessage(input, 0x0101, (IntPtr)(int)key, (IntPtr)(0xC0000001L | flags));
        await SettleAsync(tableView);
    }

    private static IntPtr FindInputWindow()
    {
        var window = WinRT.Interop.WindowNative.GetWindowHandle(UnitTestApp.Current.MainWindow);
        var children = new List<IntPtr>();

        EnumChildWindows(window, (child, _) =>
        {
            var name = new StringBuilder(256);
            GetClassName(child, name, name.Capacity);

            if (name.ToString().Contains("InputSite"))
            {
                children.Add(child);
            }

            return true;
        }, IntPtr.Zero);

        if (children.Count is 0)
        {
            Assert.Inconclusive("The test window has no input site to post keys to.");
        }

        return children[^1];
    }

    private static async Task SettleAsync(TableView tableView)
    {
        await Task.Delay(120);
        await WaitForLayoutAsync(tableView);
    }

    private async Task<TableView> CreateTableViewAsync()
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = 400,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended,
            ItemsSource = new ObservableCollection<HostItem>(Enumerable.Range(0, 500).Select(i => new HostItem { Id = i, Name = $"Item {i}" }))
        };

        foreach (var property in new[] { nameof(HostItem.Name), nameof(HostItem.Category) })
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = property,
                Width = new GridLength(150),
                Binding = new Binding { Path = new PropertyPath(property) }
            });
        }

        tableView.Columns.Add(new TableViewTemplateColumn
        {
            Header = "Box",
            Width = new GridLength(150),
            CellTemplate = (DataTemplate)XamlReader.Load(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBox Text='abcdef' /></DataTemplate>")
        });

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);
}
#endif
