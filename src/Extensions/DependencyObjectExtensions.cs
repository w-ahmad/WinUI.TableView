using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for traversing the visual tree of <see cref="DependencyObject"/>.
/// </summary>
internal static class DependencyObjectExtensions
{
    /// <summary>
    /// Finds the first descendant of the specified type <typeparamref name="T"/> that matches the optional predicate.
    /// </summary>
    /// <typeparam name="T">The type of descendant to find.</typeparam>
    /// <param name="element">The root element to search from.</param>
    /// <param name="predicate">An optional predicate to filter descendants.</param>
    /// <returns>The first matching descendant, or <c>null</c> if none found.</returns>
    internal static T? FindDescendant<T>(this DependencyObject element, Func<T, bool>? predicate = default) where T : DependencyObject
    {
        foreach (var descendant in element.FindDescendants())
        {
            if (descendant is T tDescendant && (predicate?.Invoke(tDescendant) ?? true))
            {
                return tDescendant;
            }
        }

        return default;
    }

    /// <summary>
    /// Enumerates all descendants of the specified <see cref="DependencyObject"/> in the visual tree.
    /// </summary>
    /// <param name="element">The root element to enumerate from.</param>
    /// <returns>An enumerable of all descendant <see cref="DependencyObject"/>s.</returns>
    internal static IEnumerable<DependencyObject> FindDescendants(this DependencyObject element)
    {
        var childrenCount = VisualTreeHelper.GetChildrenCount(element);

        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);

            yield return child;

            foreach (var childOfChild in FindDescendants(child))
            {
                yield return childOfChild;
            }
        }
    }

    /// <summary>
    /// Finds the first ascendant of the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of ascendant to find.</typeparam>
    /// <param name="element">The element to start searching from.</param>
    /// <returns>The first matching ascendant, or <c>null</c> if none found.</returns>
    internal static T? FindAscendant<T>(this DependencyObject element) where T : DependencyObject
    {
        foreach (var ascendant in element.FindAscendants())
        {
            if (ascendant is T tAscendant)
            {
                return tAscendant;
            }
        }

        return default;
    }

    /// <summary>
    /// Enumerates all ascendants of the specified <see cref="DependencyObject"/> in the visual tree.
    /// </summary>
    /// <param name="element">The element to start enumerating from.</param>
    /// <returns>An enumerable of all ascendant <see cref="DependencyObject"/>s.</returns>
    internal static IEnumerable<DependencyObject> FindAscendants(this DependencyObject element)
    {
        while (true)
        {
            var parent = VisualTreeHelper.GetParent(element);

            if (parent is null)
            {
                yield break;
            }

            yield return parent;

            element = parent;
        }
    }
}
