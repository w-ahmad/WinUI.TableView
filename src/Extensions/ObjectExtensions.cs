using System;
using System.Collections;
using System.Collections.Generic;
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

            // Compile the lambda expression
            var lambda = Expression.Lambda<Func<object, object?>>(Expression.Convert(propertyAccess, typeof(object)), parameterObj);
            
            // To debug, set a breakpoint below and inspect the "DebugView" property on "propertyAccess"
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
        {
            var type = dataItem.GetType();
            if (current.Type != type && !type.IsValueType)
                current = Expression.Convert(current, dataItem.GetType());
        }

        var matches = PropertyPathRegex().Matches(bindingPath);

        foreach (Match match in matches)
        {
            string part = match.Value;
            Expression nextPropertyAccess;

            // Indexer
            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                object[] indices = GetIndices(part[1..^1]);

                if (current.Type.IsArray)
                {
                    // Arrays only support integer indexing
                    if (!indices.All(idx => idx is int))
                        throw new ArgumentException($"Arrays only support integer indexing, not the provided indexer [{part[1..^1]}]");

                    nextPropertyAccess = AddArrayAccessWithBoundsCheck(current, [.. indices.Select(index => (int)index)]);
                }
                else
                {
                    nextPropertyAccess = AddIndexerAccessWithSafetyChecks(current, indices);
                }
            }
            // Simple property access
            else
            {
                var propertyInfo = current.Type.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new ArgumentException($"Property '{part}' not found on type '{current.Type.Name}'");
                nextPropertyAccess = Expression.Property(current, propertyInfo);
            }

            // Add null check: if current is null, return null; otherwise, evaluate the property access
            var notNullCheck = Expression.NotEqual(current, Expression.Constant(null));
            current = Expression.Condition(
                notNullCheck,
                nextPropertyAccess,
                Expression.Constant(null, nextPropertyAccess.Type)
            );

            // Compile a lambda of the partial expression thus far (cast to object), to see if we need to add a cast
            var lambdaTemp = Expression.Lambda<Func<object, object?>>(EnsureObjectCompatibleResult(current), parameterObj);
            var funcCurrent = lambdaTemp.Compile();
            // Evaluate this compiled function, to see if the result type is more specific than the current expression type. If so, cast to it
            var result = funcCurrent(dataItem);
            var runtimeType = result?.GetType() ?? current.Type;
            if (current.Type != runtimeType
                && current.Type.IsAssignableFrom(runtimeType)
                && !runtimeType.IsValueType // Avoid conversion for value types; the result could be null, causing System.NullReferenceException at runtime
               )
                current = Expression.Convert(current, runtimeType);
        }

        return EnsureObjectCompatibleResult(current);
    }

    /// <summary>
    /// Returns the indices from a (possible multi-dimensional) indexer that may be a mixture of integers and strings.
    /// </summary>
    private static object[] GetIndices(string stringIndexer)
    {
        // Split by comma and parse each indexer, removing whitespace
        var indexerParts = stringIndexer.Split(',')
                                        .Select(s => s.Trim())
                                        .ToArray();

        // Parse each part into appropriate type (int or string)
        return [.. indexerParts.Select(indexPart =>
            int.TryParse(indexPart, out int intIndex) ? (object)intIndex : indexPart
        )];
    }

    /// <summary>
    /// Adds array access to the current expression, given the inputted indices, and adds bounds checking; 
    /// the code for this expression will return null if the indices are out of bounds.
    /// </summary>
    private static BlockExpression AddArrayAccessWithBoundsCheck(Expression current, int[] indices)
    {
        if (!current.Type.IsArray)
            throw new ArgumentException("Current expression must be an array.");

        // Since array/indexer handling does various checks, it is more performant and readable to store the current expression in a variable
        // which we will use as input to the block with array/indexer handling code.
        var parameterArray = Expression.Parameter(current.Type, "array");
        var assignArray = Expression.Assign(parameterArray, current);

        var rank = current.Type.GetArrayRank();
        if (indices.Length != rank)
            throw new ArgumentException($"Array of rank {rank} requires {rank} indices, but {indices.Length} were provided.");

        // Bounds check for each dimension
        Expression boundsCheck = null!;
        var getLengthMethod = typeof(Array).GetMethod("GetLength")!;
        var indexConstants = new Expression[rank];
        for (int dimension = 0; dimension < rank; dimension++)
        {
            var index = indices[dimension];
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(indices), $"Index for dimension {dimension} cannot be negative: {index}");

            var indexConst = Expression.Constant(index);
            indexConstants[dimension] = indexConst;

            // GetLength method call for the current dimension
            var lengthProp = Expression.Call(parameterArray, getLengthMethod, Expression.Constant(dimension));

            var dimCheck = Expression.LessThan(indexConst, lengthProp);
            boundsCheck = boundsCheck == null ? dimCheck : Expression.AndAlso(boundsCheck, dimCheck);
        }

        // If bounds check is not satisfied, return null; otherwise, access the array element
        var expressionBlock = Expression.Condition( 
            boundsCheck,
            Expression.Convert(Expression.ArrayAccess(parameterArray, indexConstants), typeof(object)),
            Expression.Constant(null, typeof(object))
        );

        // Add the block
        return Expression.Block(
            [parameterArray],
            assignArray,
            expressionBlock
            );

    }

    private static Expression EnsureObjectCompatibleResult(Expression expression) => 
        typeof(object).IsAssignableFrom(expression.Type) && !expression.Type.IsValueType
            ? expression
            : Expression.Convert(expression, typeof(object));

    /// <summary>
    /// Adds safe indexer access to the current expression, with appropriate bounds/key checking for various collection types.
    /// Returns null if the indexer access would fail (out of bounds, missing key, etc.).
    /// </summary>
    private static Expression AddIndexerAccessWithSafetyChecks(Expression current, object[] indices)
    {
        var currentType = current.Type;
        var parameter = Expression.Parameter(currentType, "collection");
        var assignCurrent = Expression.Assign(parameter, current);

        // Handle IDictionary<TKey, TValue> - use TryGetValue
        if (TryCreateDictionaryTryGetExpression(currentType, parameter, indices, out var dictionaryExpr))
        {
            return Expression.Block([parameter], assignCurrent, dictionaryExpr);
        }

        // Handle (generic) IList and ICollection - bounds checking
        if (TryCreateIListOrICollectionBoundsCheckExpression(currentType, parameter, indices, out var listExpr))
        {
            return Expression.Block([parameter], assignCurrent, listExpr);
        }

        // Handle generic indexers - check if indexer exists
        if (TryCreateGenericIndexerExpression(currentType, parameter, indices, out var indexerExpr))
        {
            return Expression.Block([parameter], assignCurrent, indexerExpr);
        }

        // If no safe indexer can be created, return null
        return Expression.Constant(null, typeof(object));
    }

    /// <summary>
    /// Creates a TryGetValue expression for IDictionary types.
    /// </summary>
    private static bool TryCreateDictionaryTryGetExpression(Type type, ParameterExpression parameter, object[] indices, out Expression expression)
    {
        expression = null!;

        if (indices.Length != 1)
            return false;

        // Find IDictionary<TKey, TValue> interface
        var dictionaryInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionaryInterface == null)
            return false;  //apparently not a dictionary

        var genericArgs = dictionaryInterface.GetGenericArguments();
        var keyType = genericArgs[0];
        var valueType = genericArgs[1];

        // Check if the index type matches the key type
        if (!keyType.IsAssignableFrom(indices[0].GetType()))
        {
            // Type mismatch - return an expression that always returns null
            expression = Expression.Constant(null, typeof(object));
            return true;
        }

        // Get TryGetValue method
        var tryGetValueMethod = dictionaryInterface.GetMethod("TryGetValue");
        if (tryGetValueMethod == null)
            throw new InvalidOperationException($"The dictionary type {type} has no TryGetValue method");    // should not happen

        // Create variables for the key and output value
        var keyExpr = Expression.Constant(indices[0], keyType);
        var valueVar = Expression.Parameter(valueType, "value");

        // Call TryGetValue
        var tryGetCall = Expression.Call(parameter, tryGetValueMethod, keyExpr, valueVar);

        // Return value if found, null if not found
        expression = Expression.Block(
            [valueVar],
            Expression.Condition(
                tryGetCall,
                Expression.Convert(valueVar, typeof(object)),
                Expression.Constant(null, typeof(object))
            )
        );

        return true;
    }

    /// <summary>
    /// Creates a bounds-checked expression for IList and ICollection types.
    /// </summary>
    private static bool TryCreateIListOrICollectionBoundsCheckExpression(Type type, ParameterExpression parameter, object[] indices, out Expression expression)
    {
        expression = null!;

        if (indices.Length != 1 || indices[0] is not int index)
            return false;

        // Try to find an indexer property with the appropriate parameter types
        var indexerProperty = type.GetProperty("Item", [typeof(int)]);
        if (indexerProperty == null)
            return false;  // No indexer found

        // Check for IList<T>
        var listInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                            ( i.GetGenericTypeDefinition() == typeof(IList<>) || 
                              i.GetGenericTypeDefinition() == typeof(ICollection<>)
                            ));

        if (listInterface != null || 
            typeof(IList).IsAssignableFrom(type) ||     // Does have an indexer
            typeof(ICollection).IsAssignableFrom(type)  // Has no indexer, but we can still check bounds; since it derives from ICollection at the least, the indexer is expected to be aimed at the collection
           )
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(indices), $"Index for (generic) IList/ICollection cannot be negative: {index}");

            var countProperty = type.GetProperty("Count");

            if (indexerProperty != null && countProperty != null)
            {
                var indexExpr = Expression.Constant(index);
                var countExpr = Expression.Property(parameter, countProperty);
                var boundsCheck = Expression.LessThan(indexExpr, countExpr);

                expression = Expression.Condition(
                    boundsCheck,
                    Expression.Convert(Expression.Property(parameter, indexerProperty, indexExpr), typeof(object)),
                    Expression.Constant(null, typeof(object))
                );

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Creates an expression for generic indexers, with try-catch to handle potential exceptions.
    /// Returns null if indexer access fails for any reason.
    /// </summary>
    private static bool TryCreateGenericIndexerExpression(Type type, ParameterExpression parameter, object[] indices, out Expression expression)
    {
        expression = null!;

        // Try to find an indexer property with the appropriate parameter types
        var indexerTypes = indices.Select(idx => idx.GetType()).ToArray();
        var indexerProperty = type.GetProperty("Item", indexerTypes);

        if (indexerProperty == null)
            return false;

        // Create constant expressions for each index
        var indexExpressions = indices.Select(Expression.Constant).ToArray();

        // Create the indexer access
        var indexerAccess = Expression.Property(parameter, indexerProperty, indexExpressions);

        // Wrap in try-catch to handle any exceptions that might occur during indexer access, due to non-existing keys or out-of-bounds indices
        var tryBlock = Expression.Convert(indexerAccess, typeof(object));
        var catchBlock = Expression.Constant(null, typeof(object));

        // Create try-catch expression - return null for any exception
        expression = Expression.TryCatch(
            tryBlock,
            Expression.Catch(typeof(Exception), catchBlock)
        );

        return true;
    }

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
