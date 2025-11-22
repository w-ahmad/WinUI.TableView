#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation.Collections;

namespace WinUI.TableView.Collections;

/// <summary>
/// Provides a strongly-typed wrapper around <see cref="DependencyObjectCollection"/>
/// for collections containing <typeparamref name="T"/> items.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
[ContentProperty(Name = nameof(Items))]
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class DependencyObjectCollection<T> : DependencyObjectCollection, IList<T> where T : DependencyObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyObjectCollection{T}"/> class.
    /// </summary>
    public DependencyObjectCollection()
    {
        VectorChanged += OnVectorChanged;
    }

    /// <summary>
    /// Handles changes to the underlying vector of <see cref="DependencyObject"/> items.
    /// </summary>
    private void OnVectorChanged(IObservableVector<DependencyObject> sender, IVectorChangedEventArgs args)
    {
        if (args.CollectionChange is CollectionChange.ItemInserted)
        {
            if (this[(int)args.Index] is not T)
            {
                throw new InvalidOperationException($"Only items of type {typeof(T).FullName} can be added to this collection.");
            }
        }
    }

    /// <summary>
    /// Gets an <see cref="IList{T}"/> view over this collection.
    /// This provides strongly-typed access to the underlying items.
    /// </summary>
    public IList<T> Items => this;

    T IList<T>.this[int index]
    {
        get => (T)base[index];
        set => base[index] = value;
    }

    int ICollection<T>.Count => Count;

    bool ICollection<T>.IsReadOnly => IsReadOnly;

    void ICollection<T>.Add(T item)
    {
        Add(item);
    }

    void ICollection<T>.Clear()
    {
        Clear();
    }

    bool ICollection<T>.Contains(T item)
    {
        return Contains(item);
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(array as DependencyObject[], arrayIndex);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        foreach (var item in this)
        {
            yield return (T)item;
        }
    }

    int IList<T>.IndexOf(T item)
    {
        return IndexOf(item);
    }

    void IList<T>.Insert(int index, T item)
    {
        Insert(index, item);
    }

    bool ICollection<T>.Remove(T item)
    {
        var index = IndexOf(item);

        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    void IList<T>.RemoveAt(int index)
    {
        RemoveAt(index);
    }
}
#endif