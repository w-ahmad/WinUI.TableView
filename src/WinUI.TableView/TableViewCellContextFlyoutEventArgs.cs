using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;

namespace WinUI.TableView;

public partial class TableViewCellContextFlyoutEventArgs : HandledEventArgs
{
    public TableViewCellContextFlyoutEventArgs(TableViewCellSlot slot, TableViewCell cell, object item, FlyoutBase flyout)
    {
        Slot = slot;
        Cell = cell;
        Item = item;
        Flyout = flyout;
    }

    public TableViewCellSlot Slot { get; }
    public TableViewCell Cell { get; }
    public object Item { get; }
    public FlyoutBase Flyout { get; }
}
