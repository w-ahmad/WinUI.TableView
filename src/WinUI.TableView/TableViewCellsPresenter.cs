using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;

namespace WinUI.TableView;

public class TableViewCellsPresenter : StackPanel
{
    public TableViewCellsPresenter()
    {
        Orientation = Orientation.Horizontal;
    }

    public IList<TableViewCell> Cells => Children.OfType<TableViewCell>().ToList().AsReadOnly();
}