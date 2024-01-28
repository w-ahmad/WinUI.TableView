// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace CommunityToolkit.WinUI.Collections;

/// <summary>
/// Sort description
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SortDescription"/> class.
/// </remarks>
/// <param name="propertyName">Name of property to sort on</param>
/// <param name="direction">Direction of sort</param>
/// <param name="comparer">Comparer to use. If null, will use default comparer</param>
public class SortDescription(string propertyName, SortDirection direction, IComparer? comparer = null)
{
    /// <summary>
    /// Gets the name of property to sort on
    /// </summary>
    public string PropertyName { get; } = propertyName;

    /// <summary>
    /// Gets the direction of sort
    /// </summary>
    public SortDirection Direction { get; } = direction;

    /// <summary>
    /// Gets the comparer
    /// </summary>
    public IComparer Comparer { get; } = comparer ?? ObjectComparer.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="SortDescription"/> class that describes
    /// a sort on the object itself
    /// </summary>
    /// <param name="direction">Direction of sort</param>
    /// <param name="comparer">Comparer to use. If null, will use default comparer</param>
    public SortDescription(SortDirection direction, IComparer? comparer = null)
        : this(null!, direction, comparer!)
    {
    }
}

file class ObjectComparer : IComparer
{
    public static readonly IComparer Instance = new ObjectComparer();

    private ObjectComparer()
    {
    }

    public int Compare(object? x, object? y)
    {
        var cx = x as IComparable;
        var cy = y as IComparable;

        return cx == cy ? 0 : cx == null ? -1 : cy == null ? +1 : cx.CompareTo(cy);
    }
}