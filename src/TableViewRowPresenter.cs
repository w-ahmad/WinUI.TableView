using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace WinUI.TableView;

/// <summary>
/// Represents the visual elements of a TableViewRow
/// </summary>
public partial class TableViewRowPresenter : ListViewItemPresenter
{
    private TableViewCellsPresenter? _cellsPresenter;

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        _cellsPresenter ??= this.FindDescendant<TableViewCellsPresenter>();
        _cellsPresenter?.Arrange(new Rect(0, 0, _cellsPresenter.ActualWidth, finalSize.Height));

        return finalSize;
    }
}
