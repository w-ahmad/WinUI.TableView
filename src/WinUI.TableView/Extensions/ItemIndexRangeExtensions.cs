using Microsoft.UI.Xaml.Data;

namespace WinUI.TableView.Extensions;

public static class ItemIndexRangeExtensions
{
    public static bool IsInRange(this ItemIndexRange range, int index)
    {
        return index >= range.FirstIndex && index <= range.LastIndex;
    }
}