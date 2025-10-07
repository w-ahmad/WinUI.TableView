using CommunityToolkit.WinUI;
using Windows.Foundation;

namespace WinUI.TableView.Primitives;

/// <inheritdoc/>
public partial class ListViewItemPresenter : Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter
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
