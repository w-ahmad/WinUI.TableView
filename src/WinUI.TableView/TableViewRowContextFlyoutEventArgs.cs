using Microsoft.UI.Xaml.Controls.Primitives;
using System.ComponentModel;

namespace WinUI.TableView;

public partial class TableViewRowContextFlyoutEventArgs : HandledEventArgs
{
    public TableViewRowContextFlyoutEventArgs(int index, TableViewRow row, object item, FlyoutBase flyoutBase)
    {
        Index = index;
        Row = row;
        Item = item;
        Flyout = flyoutBase;
    }

    public int Index { get; }
    public TableViewRow Row { get; }
    public object Item { get; }
    public FlyoutBase Flyout { get; }
}
