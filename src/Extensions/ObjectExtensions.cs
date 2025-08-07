using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for object types.
/// </summary>
internal static partial class ObjectExtensions
{
    // Regex to split property paths into property names and indexers (for cases like e.g. "[2].Foo[0].Bar", where Foo might be a Property that returns an array)
    [GeneratedRegex(@"([^.[]+)|(\[[^\]]+\])", RegexOptions.Compiled)]
    private static partial Regex PropertyPathRegex();

    /// <summary>
    /// Creates and returns a compiled lambda expression for accessing the property path on instances, with runtime type checking and casting support.
    /// </summary>
    /// <param name="dataItem">The data item instance to use for runtime type evaluation.</param>
    /// <param name="bindingPath">The binding path to access, e.g. "[0].SubPropertyArray[0].SubSubProperty".</param>
    /// <returns>A compiled function that takes an instance and returns the property value, or null if the property path is invalid.</returns>
    internal static Func<object, object?>? GetFuncCompiledPropertyPath(this object dataItem, string bindingPath)
    {
        try
        {
            // Build the property access expression chain with runtime type checking
            var parameterObj = Expression.Parameter(typeof(object), "obj");
            var propertyAccess = BuildPropertyPathExpressionTree(parameterObj, bindingPath, dataItem);

            // var strCode = propertyAccess.ToString();  // Uncomment to debug the expression tree

            // Compile the lambda expression
            var lambda = Expression.Lambda<Func<object, object?>>(Expression.Convert(propertyAccess, typeof(object)), parameterObj);
            var _compiledPropertyPath = lambda.Compile();
            return _compiledPropertyPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds an expression tree for accessing a property path on the given instance expression, with runtime type checking and casting support.
    /// </summary>
    /// <param name="parameterObj">The expression representing the instance parameter for which the binding path will be evaluated.</param>
    /// <param name="bindingPath">The binding path to access.</param>
    /// <param name="dataItem">The actual data item to use for runtime type evaluation, to help with any needed subclass type conversions.</param>
    /// <returns>An expression that accesses the property value specified by the binding path for the provided dataItem instance.</returns>
    private static Expression BuildPropertyPathExpressionTree(ParameterExpression parameterObj, string bindingPath, object dataItem)
    {
        Expression current = parameterObj;

        // The function uses a generic object input parameter to allow for any type of data item,
        // but we need to ensure that the runtime type matches the data item type that is inputted as example to be able to find members
        if (current.Type != dataItem.GetType())
            current = Expression.Convert(current, dataItem.GetType());

        var matches = PropertyPathRegex().Matches(bindingPath);

        foreach (Match match in matches)
        {
            string part = match.Value;

            // Indexer
            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                string stringIndex = part[1..^1];
                object index = int.TryParse(stringIndex, out int intIndex) ? intIndex : stringIndex;

                if (current.Type.IsArray)
                {
                    current = Expression.ArrayIndex(current, Expression.Constant(index));
                }
                else
                {
                    // Try to find an indexer property, with the type of by indexAnyType.
                    var indexerProperty = current.Type.GetProperty("Item", [index.GetType()]) 
                        ?? throw new ArgumentException($"Type '{current.Type.Name}' does not support integer indexing");
                    current = Expression.Property(current, indexerProperty, Expression.Constant(index));
                }
            }
            // Simple property access
            else
            {   
                var propertyInfo = current.Type.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new ArgumentException($"Property '{part}' not found on type '{current.Type.Name}'");
                current = Expression.Property(current, propertyInfo);
            }

            // Compile a lambda of the partial expression thus far (cast to object), to see if we need to add a cast
            var lambdaTemp = Expression.Lambda<Func<object, object?>>(EnsureObjectCompatibleResult(current), parameterObj);
            var funcCurrent = lambdaTemp.Compile();
            // Evaluate this compiled function, to see if the result type is more specific than the current expression type. If so, cast to it
            var result = funcCurrent(dataItem);
            var runtimeType = result?.GetType() ?? current.Type;
            if (current.Type != runtimeType && current.Type.IsAssignableFrom(runtimeType))
                current = Expression.Convert(current, runtimeType);
        }

        return EnsureObjectCompatibleResult(current);
    }

    private static Expression EnsureObjectCompatibleResult(Expression expression) => 
        typeof(object).IsAssignableFrom(expression.Type) && !expression.Type.IsValueType
            ? expression
            : Expression.Convert(expression, typeof(object));


    /// <summary>
    /// Determines the type of items contained within the specified <see cref="IEnumerable"/>.
    /// </summary>
    /// <remarks>This method attempts to determine the item type of the provided <see cref="IEnumerable"/>
    /// using the following strategies: <list type="bullet"> <item> If the <paramref name="list"/> is a generic
    /// enumerable, the generic type is returned. </item> <item> If the item type implements <see
    /// cref="ICustomTypeProvider"/>, the method may attempt to retrieve a custom type from the list's items. </item>
    /// <item> If the item type cannot be determined directly, the method inspects the first item in the list to infer
    /// its type. </item> </list> If the list is empty or the item type is <see cref="object"/>, the method may return
    /// <see langword="null"/>.</remarks>
    /// <param name="list">The <see cref="IEnumerable"/> instance to analyze.</param>
    /// <returns>The <see cref="Type"/> of the items in the <paramref name="list"/>, or <see langword="null"/> if the type cannot
    /// be determined. If the list is empty or contains items of type <see cref="object"/>, additional heuristics may be
    /// applied to infer a more specific type.</returns>
    internal static Type? GetItemType(this IEnumerable list)
    {
        var listType = list.GetType();
        Type? itemType = null;
        var isICustomTypeProvider = false;

        // If it's a generic enumerable, get the generic type.

        // Unfortunately, if data source is fed from a bare IEnumerable, TypeHelper will report an element type of object,
        // which is not particularly interesting.  It is dealt with it further on.
        itemType = listType.GetEnumerableItemType();

        if (itemType != null)
        {
            isICustomTypeProvider = typeof(ICustomTypeProvider).IsAssignableFrom(itemType);
        }

        // Bare IEnumerables mean that result type will be object.  In that case, try to get something more interesting.
        // Or, if the itemType implements ICustomTypeProvider, try to retrieve the custom type from one of the object instances.
        if (itemType == null || itemType == typeof(object) || isICustomTypeProvider)
        {
            // No type was located yet. Does the list have anything in it?
            Type? firstItemType = null;
            var en = list.GetEnumerator();
            if (en.MoveNext() && en.Current != null)
            {
                firstItemType = en.Current.GetCustomOrCLRType();
            }
            else
            {
                firstItemType = list
                    .Cast<object>() // cast to convert IEnumerable to IEnumerable<object>
                    .Select(x => x.GetType()) // get the type
                    .FirstOrDefault(); // get only the first thing to come out of the sequence, or null if empty
            }

            if (firstItemType != typeof(object))
            {
                return firstItemType;
            }
        }

        // Couldn't get the CustomType because there were no items.
        if (isICustomTypeProvider)
        {
            return null;
        }

        return itemType;
    }

    /// <summary>
    /// Returns instance.GetCustomType() if the instance implements ICustomTypeProvider; otherwise,
    /// returns instance.GetType().
    /// </summary>
    /// <param name="instance">Object to return the type of</param>
    /// <returns>Type of the instance</returns>
    internal static Type? GetCustomOrCLRType(this object? instance)
    {
        if (instance is ICustomTypeProvider customTypeProvider)
        {
            return customTypeProvider.GetCustomType() ?? instance.GetType();
        }

        return instance?.GetType();
    }
}
