using CommunityToolkit.WinUI;
using Windows.Foundation;

namespace WinUI.TableView.Primitives;

/// <inheritdoc/>
public partial class ListViewItemPresenter : Microsoft.UI.Xaml.Controls.Primitives.ListViewItemPresenter
{
    private TableViewRowPresenter? _rowPresenter;

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        finalSize = base.ArrangeOverride(finalSize);

        _rowPresenter ??= this.FindDescendant<TableViewRowPresenter>();
        _rowPresenter?.Arrange(new Rect(0, 0, _rowPresenter.ActualWidth, finalSize.Height));

        return finalSize;
    }
}
