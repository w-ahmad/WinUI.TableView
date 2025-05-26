#if !WINDOWS
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WinUI.TableView.Extensions;

namespace WinUI.TableView;

/// <summary>
/// Partial class for TableView that contains Uno stuff.
/// </summary>
partial class TableView
{
    private const BindingFlags BindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
    private PropertyInfo? _disableRaiseSelectionChangedPropertyInfo;
    private MethodInfo? _invokeSelectionChangedMethodInfo;

    private void SetDisableRaiseSelectionChanged(bool value)
    {
        _disableRaiseSelectionChangedPropertyInfo ??= typeof(Selector).GetProperty("DisableRaiseSelectionChanged", BindingAttr);
        _disableRaiseSelectionChangedPropertyInfo?.SetValue(this, value);
    }

    private void InvokeSelectionChanged(object[] removedItems, object[] addedItems)
    {
        _invokeSelectionChangedMethodInfo ??= typeof(Selector).GetMethod("InvokeSelectionChanged", BindingAttr);
        _invokeSelectionChangedMethodInfo?.Invoke(this, [removedItems, addedItems]);
    }

    private new void DeselectRange(ItemIndexRange itemIndexRange)
    {
        var removedItems = new List<object>();

        SetDisableRaiseSelectionChanged(true);
        {
            if (!itemIndexRange.IsValid(this))
            {
                throw new IndexOutOfRangeException("The given item index range bounds are not valid.");
            }

            for (var index = itemIndexRange.FirstIndex; index <= itemIndexRange.LastIndex; index++)
            {
                var item = Items[index];
                if (SelectedItems.Contains(item))
                {
                    removedItems.Add(item);
                    SelectedItems.Remove(item);
                }
            }

            AdjustSelectedRanges();
        }
        SetDisableRaiseSelectionChanged(false);

        InvokeSelectionChanged([.. removedItems], []);
    }

    private void AdjustSelectedRanges()
    {
        SelectedRanges.Clear();

        if (SelectedItems.Count == 0) return;

        var selectedIndexes = SelectedItems.Select(Items.IndexOf).Order();
        var start = selectedIndexes.First();
        var prev = start;

        foreach (var index in selectedIndexes)
        {
            if (index != prev + 1)
            {
                var length = (uint)(prev - start + 1);
                SelectedRanges.Add(new ItemIndexRange(start, length));
                start = index;
            }
            prev = index;
        }

        var finalLength = (uint)(prev - start + 1);
        SelectedRanges.Add(new ItemIndexRange(start, finalLength));
    }

    private new void SelectRange(ItemIndexRange itemIndexRange)
    {
        var addedItems = new List<object>();

        SetDisableRaiseSelectionChanged(true);
        {
            if (!itemIndexRange.IsValid(this))
            {
                throw new IndexOutOfRangeException("The given item index range bounds are not valid.");
            }

            for (var index = itemIndexRange.FirstIndex; index <= itemIndexRange.LastIndex; index++)
            {
                var item = Items[index];
                if (!SelectedItems.Contains(item))
                {
                    addedItems.Add(item);
                    SelectedItems.Add(item);
                }
            }

            AdjustSelectedRanges();
        }
        SetDisableRaiseSelectionChanged(false);

        InvokeSelectionChanged([], [.. addedItems]);
    }

    private new IList<ItemIndexRange> SelectedRanges { get; } = [];
}
#endif