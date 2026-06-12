using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
    // Regex to split binding paths into segments; property names and indexers (for cases like e.g. "[2].Foo[0].Bar", where Foo might be a Property that returns an array)
    [GeneratedRegex(@"([^.[]+)|(\[[^\]]+\])", RegexOptions.Compiled)]
    private static partial Regex BindingPathRegex();

    /// <summary>
    /// Creates and returns a compiled lambda expression for accessing the binding path on instances, with runtime type checking and casting support.
    /// </summary>
    /// <param name="dataItem">The data item instance to use for runtime type evaluation.</param>
    /// <param name="bindingPath">The binding path to access, e.g. "[0].SubPropertyArray[0].SubSubProperty".</param>
    /// <returns>A compiled function that takes an instance and returns the property value, or null if the binding path is invalid.</returns>
    internal static Func<object, object?>? GetCompiledValueGetter(this object dataItem, string bindingPath)
    {
        try
        {
            // Build the property access expression chain with runtime type checking
            var parameterObj = Expression.Parameter(typeof(object), "obj");
            var expressionTree = BuildGetterExpressionTree(parameterObj, bindingPath, dataItem);

            // Compile the lambda expression
            var lambda = Expression.Lambda<Func<object, object?>>(expressionTree, parameterObj);
            return lambda.Compile();   // To DEBUG, set a breakpoint here and inspect the "DebugView" property on the variable "lambda"
        }
        catch
        {
            return null;
        }
    }

    internal static Action<object, object?>? GetCompiledValueSetter(this object dataItem, string bindingPath)
    {
        try
        {
            var parameterObj = Expression.Parameter(typeof(object), "obj");
            var parameterValue = Expression.Parameter(typeof(object), "value");

            var expressionTree = BuildSetterExpressionTree(
                parameterObj,
                parameterValue,
                bindingPath,
                dataItem);

            var lambda = Expression.Lambda<Action<object, object?>>(
                expressionTree,
                parameterObj,
                parameterValue);

            return lambda.Compile();
        }
        catch
        {
            return null;
        }
    }

    private static Expression BuildSetterExpressionTree(ParameterExpression parameterObj, ParameterExpression parameterValue, string bindingPath, object dataItem)
    {
        var matches = BindingPathRegex().Matches(bindingPath);

        if (matches.Count == 0)
            throw new ArgumentException("Binding path is empty.", nameof(bindingPath));

        // Reuse the getter's navigation logic to reach the parent of the final segment.
        // Only the final segment is setter-specific (a write target instead of a read).
        var current = BuildPathNavigationExpression(parameterObj, matches, dataItem, matches.Count - 1);

        var finalPart = matches[^1].Value;

        return finalPart.StartsWith('[') && finalPart.EndsWith(']')
            ? BuildIndexerSetterExpression(current, finalPart, parameterValue)
            : BuildPropertySetterExpression(current, finalPart, parameterValue);
    }

    private static Expression BuildPropertyGetterExpression(Expression current, string propertyName)
    {
        var propertyInfo = current.Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{current.Type.Name}'.");

        return Expression.Property(current, propertyInfo);
    }

    private static Expression BuildIndexerGetterExpression(Expression current, string indexerPart)
    {
        var indices = GetIndices(indexerPart[1..^1]);

        if (current.Type.IsArray)
        {
            if (!indices.All(index => index is int))
                throw new ArgumentException($"Arrays only support integer indexing: {indexerPart}");

            return AddArrayAccessWithBoundsCheck(current, [.. indices.Select(index => (int)index)]);
        }

        return AddIndexerAccessWithSafetyChecks(current, indices);
    }

    private static Expression BuildPropertySetterExpression(Expression current, string propertyName, Expression value)
    {
        var propertyInfo = current.Type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{current.Type.Name}'.");

        if (!propertyInfo.CanWrite)
            throw new ArgumentException($"Property '{propertyName}' is read-only.");

        var target = Expression.Property(current, propertyInfo);

        return BuildConvertedAssignExpression(target, value, propertyInfo.PropertyType);
    }

    private static Expression BuildIndexerSetterExpression(Expression current, string indexerPart, Expression value)
    {
        var indices = GetIndices(indexerPart[1..^1]);

        if (current.Type.IsArray)
            return BuildArraySetterExpression(current, indices, value);

        return BuildObjectIndexerSetterExpression(current, indices, value);
    }

    private static Expression BuildArraySetterExpression(Expression current, object[] indices, Expression value)
    {
        if (!indices.All(index => index is int))
            throw new ArgumentException("Arrays only support integer indexers.");

        var arrayType = current.Type;
        var elementType = arrayType.GetElementType()!;
        var rank = arrayType.GetArrayRank();

        if (indices.Length != rank)
            throw new ArgumentException($"Array rank mismatch. Expected {rank} index(es).");

        var arrayVar = Expression.Parameter(arrayType, "array");
        var assignArray = Expression.Assign(arrayVar, current);

        var indexExpressions = indices
            .Cast<int>()
            .Select(index => Expression.Constant(index))
            .ToArray();

        Expression? boundsCheck = null;

        var getLengthMethod = typeof(Array).GetMethod(nameof(Array.GetLength))!;

        for (var i = 0; i < indexExpressions.Length; i++)
        {
            var index = (int)indices[i];

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(indices));

            var length = Expression.Call(arrayVar, getLengthMethod, Expression.Constant(i));
            var check = Expression.LessThan(indexExpressions[i], length);

            boundsCheck = boundsCheck is null
                ? check
                : Expression.AndAlso(boundsCheck, check);
        }

        var assignValue = BuildConvertedAssignExpression(
            Expression.ArrayAccess(arrayVar, indexExpressions),
            value,
            elementType);

        return Expression.Block(
            [arrayVar],
            assignArray,
            Expression.IfThen(boundsCheck!, assignValue));
    }

    private static Expression BuildObjectIndexerSetterExpression(Expression current, object[] indices, Expression value)
    {
        if (indices.Length == 1)
        {
            var dictionaryInterface = current.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                     i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryInterface is not null)
            {
                var args = dictionaryInterface.GetGenericArguments();
                var keyType = args[0];
                var valueType = args[1];

                if (!keyType.IsAssignableFrom(indices[0].GetType()))
                    return Expression.Empty();

                var indexer = dictionaryInterface.GetProperty("Item")!;

                var target = Expression.Property(Expression.Convert(current, dictionaryInterface),
                    indexer,
                    Expression.Constant(indices[0], keyType));

                return BuildConvertedAssignExpression(target, value, valueType);
            }
        }

        var indexerTypes = indices.Select(index => index.GetType()).ToArray();

        var indexerProperty = current.Type.GetProperty("Item", indexerTypes)
            ?? throw new ArgumentException($"Indexer not found on type '{current.Type.Name}'.");

        if (!indexerProperty.CanWrite)
            throw new ArgumentException($"Indexer on type '{current.Type.Name}' is read-only.");

        var indexExpressions = indices
            .Select(index => Expression.Constant(index, index.GetType()))
            .ToArray();

        // Add bounds checking for IList/ICollection types with integer indexers
        if (indices.Length == 1 && indices[0] is int intIndex)
        {
            var listInterface = current.Type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                (i.GetGenericTypeDefinition() == typeof(IList<>) ||
                                 i.GetGenericTypeDefinition() == typeof(ICollection<>)));

            if (listInterface != null ||
                typeof(IList).IsAssignableFrom(current.Type) ||
                typeof(ICollection).IsAssignableFrom(current.Type))
            {
                // Count may be declared on a base interface (e.g. IList inherits Count from ICollection),
                // and reflection does not surface inherited interface members, so search interfaces too.
                // The container type is no longer specialized to the concrete runtime type during navigation,
                // so current.Type can legitimately be an interface like IList here.
                var countProperty = current.Type.GetProperty("Count")
                    ?? current.Type.GetInterfaces()
                        .Select(i => i.GetProperty("Count"))
                        .FirstOrDefault(p => p is not null);

                if (countProperty != null)
                {
                    var collectionVar = Expression.Parameter(current.Type, "collection");
                    var assignCollection = Expression.Assign(collectionVar, current);

                    var countExpr = Expression.Property(collectionVar, countProperty);
                    var indexExpr = Expression.Constant(intIndex);

                    // Check if index >= 0 && index < Count
                    var boundsCheck = Expression.AndAlso(
                        Expression.GreaterThanOrEqual(indexExpr, Expression.Constant(0)),
                        Expression.LessThan(indexExpr, countExpr));

                    var assignValue = BuildConvertedAssignExpression(
                        Expression.Property(collectionVar, indexerProperty, indexExpressions),
                        value,
                        indexerProperty.PropertyType);

                    return Expression.Block(
                        [collectionVar],
                        assignCollection,
                        Expression.IfThen(boundsCheck, assignValue));
                }
            }
        }

        return BuildConvertedAssignExpression(
            Expression.Property(current, indexerProperty, indexExpressions),
            value,
            indexerProperty.PropertyType);
    }

    private static Expression BuildConvertedAssignExpression(Expression target, Expression value, Type targetType)
    {
        var convertedValue = Expression.Variable(typeof(object), "convertedValue");
        var error = Expression.Variable(typeof(string), "error");

        var tryConvertMethod = typeof(ObjectExtensions)
            .GetMethod(
                nameof(TryConvertValue),
                BindingFlags.Static | BindingFlags.NonPublic)!;

        var tryConvertCall = Expression.Call(
            tryConvertMethod,
            value,
            Expression.Constant(targetType),
            convertedValue,
            error);

        return Expression.Block(
            [convertedValue, error],
            Expression.IfThen(
                tryConvertCall,
                Expression.Assign(
                    target,
                    Expression.Convert(convertedValue, targetType))));
    }

    private static bool TryConvertValue(object? value, Type targetType, out object? convertedValue, out string? error)
    {
        if (targetType == typeof(object))
        {
            error = null;
            convertedValue = value;
            return true;
        }

        if (value is string || value is null)
            return TryConvertToTargetType(value as string, targetType, out convertedValue, out error);

        return TryConvertObject(value, targetType, out convertedValue, out error);
    }

    private static bool TryConvertToTargetType(string? stringValue, Type targetType, out object? convertedValue, out string? error)
    {
        error = null;
        convertedValue = null;

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var actualTargetType = underlyingType ?? targetType;

        if (actualTargetType == typeof(string))
        {
            convertedValue = stringValue ?? string.Empty;
            return true;
        }

        if (stringValue is null)
        {
            if (underlyingType is not null || !targetType.IsValueType)
            {
                return true;
            }

            error = $"The target type '{targetType.Name}' does not accept null values.";
            return false;
        }

        if (stringValue.Length == 0)
        {
            if (underlyingType is not null || !targetType.IsValueType)
            {
                return true;
            }

            error = $"The target type '{targetType.Name}' does not accept empty values.";
            return false;
        }

        try
        {
            if (actualTargetType == typeof(bool))
            {
                if (bool.TryParse(stringValue, out var boolValue))
                {
                    convertedValue = boolValue;
                    return true;
                }

                if (stringValue == "1" || stringValue.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    convertedValue = true;
                    return true;
                }

                if (stringValue == "0" || stringValue.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    convertedValue = false;
                    return true;
                }
            }
            else if (actualTargetType == typeof(DateOnly))
            {
                convertedValue = DateOnly.Parse(stringValue, CultureInfo.CurrentCulture);
                return true;
            }
            else if (actualTargetType == typeof(TimeOnly))
            {
                convertedValue = TimeOnly.Parse(stringValue, CultureInfo.CurrentCulture);
                return true;
            }
            else if (actualTargetType == typeof(DateTime))
            {
                convertedValue = DateTime.Parse(stringValue, CultureInfo.CurrentCulture);
                return true;
            }
            else if (actualTargetType == typeof(DateTimeOffset))
            {
                convertedValue = DateTimeOffset.Parse(stringValue, CultureInfo.CurrentCulture);
                return true;
            }
            else if (actualTargetType == typeof(TimeSpan))
            {
                convertedValue = TimeSpan.Parse(stringValue, CultureInfo.CurrentCulture);
                return true;
            }
            else if (actualTargetType == typeof(Guid))
            {
                convertedValue = Guid.Parse(stringValue);
                return true;
            }
            else if (actualTargetType == typeof(Uri))
            {
                convertedValue = new Uri(stringValue, UriKind.RelativeOrAbsolute);
                return true;
            }
            else if (actualTargetType.IsEnum)
            {
                convertedValue = Enum.Parse(actualTargetType, stringValue, ignoreCase: true);
                return true;
            }

            var converter = TypeDescriptor.GetConverter(actualTargetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                convertedValue = converter.ConvertFrom(null, CultureInfo.CurrentCulture, stringValue);
                return true;
            }

            convertedValue = Convert.ChangeType(stringValue, actualTargetType, CultureInfo.CurrentCulture);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Unable to convert '{stringValue}' to '{actualTargetType.Name}': {ex.Message}";
            return false;
        }
    }

    private static bool TryConvertObject(object? value, Type targetType, out object? convertedValue, out string? error)
    {
        error = null;
        convertedValue = null;

        if (value is null)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null)
            {
                return true;
            }

            error = $"The target type '{targetType.Name}' does not accept null values.";
            return false;
        }

        var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (actualTargetType.IsInstanceOfType(value))
        {
            convertedValue = value;
            return true;
        }

        try
        {
            convertedValue = Convert.ChangeType(value, actualTargetType, CultureInfo.CurrentCulture);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Unable to convert '{value}' to '{actualTargetType.Name}': {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Builds an expression tree for accessing a binding path on the given instance expression, with runtime type checking and casting support.
    /// </summary>
    /// <param name="parameterObj">The expression representing the instance parameter for which the binding path will be evaluated.</param>
    /// <param name="bindingPath">The binding path to access.</param>
    /// <param name="dataItem">The actual data item to use for runtime type evaluation, to help with any needed subclass type conversions.</param>
    /// <returns>An expression that accesses the property value specified by the binding path for the provided dataItem instance.</returns>
    private static Expression BuildGetterExpressionTree(ParameterExpression parameterObj, string bindingPath, object dataItem)
    {
        var matches = BindingPathRegex().Matches(bindingPath);

        // Navigate through every segment to reach the final value.
        var current = BuildPathNavigationExpression(parameterObj, matches, dataItem, matches.Count);

        return EnsureObjectCompatibleResult(current);
    }

    /// <summary>
    /// Builds the expression that navigates the first <paramref name="navigateCount"/> segments of a binding path on
    /// the given instance expression, with runtime null-checks and mixed-collection-friendly type specialization.
    /// Shared by both the getter (which navigates every segment) and the setter (which navigates to the parent of the
    /// final segment), so the navigation logic only lives in one place.
    /// </summary>
    /// <param name="parameterObj">The expression representing the instance parameter for which the binding path will be evaluated.</param>
    /// <param name="matches">The parsed binding path segments.</param>
    /// <param name="dataItem">The actual data item to use for runtime type evaluation, to help with any needed subclass type conversions.</param>
    /// <param name="navigateCount">The number of leading segments to navigate into.</param>
    /// <returns>An expression that accesses the value at the requested depth for the provided dataItem instance.</returns>
    private static Expression BuildPathNavigationExpression(ParameterExpression parameterObj, MatchCollection matches, object dataItem, int navigateCount)
    {
        Expression current = parameterObj;

        // The function uses a generic object input parameter to allow for any type of data item,
        // but we cast only to the root type that actually declares the first path segment.
        // This keeps the accessor compatible with sibling subclasses in mixed collections.
        {
            var t = dataItem.GetType();
            // Resolve the declaring type for the first segment (property or indexer).
            // If we cannot resolve it, keep the original runtime type as fallback.
            var typeRoot = matches.Count > 0 ? GetDeclaringTypeForPathSegment(t, matches[0].Value) ?? t : t;
            if (current.Type != typeRoot && !typeRoot.IsValueType)
                current = Expression.Convert(current, typeRoot);
        }

        for (var matchIndex = 0; matchIndex < navigateCount; matchIndex++)
        {
            var part = matches[matchIndex].Value;

            var nextPropertyAccess = part.StartsWith('[') && part.EndsWith(']')
                ? BuildIndexerGetterExpression(current, part)
                : BuildPropertyGetterExpression(current, part);

            if (nextPropertyAccess.Type.IsValueType && !nextPropertyAccess.Type.IsNullableType())
            {
                // Value types cannot be null, so don't need to check for null, and we can directly assign the property access
                current = nextPropertyAccess;
            }
            else
            {
                // Add null check: if current is null, stop and return null; otherwise, continue with the next access
                var notNullCheck = Expression.NotEqual(current, Expression.Constant(null));
                current = Expression.Condition(
                    notNullCheck,
                    nextPropertyAccess,
                    Expression.Constant(null, nextPropertyAccess.Type)
                );
            }

            // Only check for type compatibility (i.e.: the need for conversion) if there is a following segment.
            // We only specialize when the expression type is still object, to avoid over-specializing
            // to a sample instance subtype and breaking mixed type rows.
            if (matchIndex + 1 < matches.Count && current.Type == typeof(object))
            {
                var lambdaTemp = Expression.Lambda<Func<object, object?>>(EnsureObjectCompatibleResult(current), parameterObj);
                var funcCurrent = lambdaTemp.Compile();
                var result = funcCurrent(dataItem);

                // The partial result gives us the runtime container for the NEXT segment.
                // Convert to the most general declaring type for that next segment (property/indexer)
                // instead of converting directly to the concrete runtime subtype.
                var typeResult = result?.GetType();
                if (typeResult != null)
                {
                    var nextPart = matches[matchIndex + 1].Value;
                    var typeCompatible = GetDeclaringTypeForPathSegment(typeResult, nextPart) ?? typeResult;

                    if (current.Type != typeCompatible)
                    {
                        current = Expression.Convert(current, typeCompatible);
                    }
                }
            }
        }

        return current;
    }

    private static Expression EnsureObjectCompatibleResult(Expression expression)
    {
        // Ensure the final expression can be used in an object-returning lambda (boxing value types, including nullable value types)
        if (expression.Type.IsValueType)
            return Expression.Convert(expression, typeof(object));
        return expression;
    }

    /// <summary>
    /// Resolves the declaring type for one binding-path segment on the provided candidate type.
    /// </summary>
    /// <param name="candidateType">The type on which the segment should be resolved.</param>
    /// <param name="segment">One binding path segment, either a property name or an indexer token like "[0]".</param>
    /// <returns>The segment declaring type when resolved; otherwise <see langword="null"/>.</returns>
    private static Type? GetDeclaringTypeForPathSegment(Type candidateType, string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return null;

        // Indexer segment
        if (segment.StartsWith('[') && segment.EndsWith(']'))
        {
            // Infer CLR argument types from parsed index values.
            var indices = GetIndices(segment[1..^1]);
            var indexTypes = indices.Select(i => i.GetType()).ToArray();

            // Find an indexer whose parameter list is assignment-compatible with parsed index types.
            // GetProperties includes inherited members, so we can resolve indexers declared on a base class as well.
            var indexerInfo = candidateType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.GetIndexParameters().Length == indexTypes.Length)
                .FirstOrDefault(p =>
                {
                    var indexParameters = p.GetIndexParameters();
                    for (var i = 0; i < indexParameters.Length; i++)
                    {
                        if (!indexParameters[i].ParameterType.IsAssignableFrom(indexTypes[i]))
                            return false;
                    }
                    return true;
                });

            return indexerInfo?.DeclaringType;
        }

        // Property segment
        var propertyInfo = candidateType.GetProperty(segment, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return propertyInfo?.DeclaringType;
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
                            (i.GetGenericTypeDefinition() == typeof(IList<>) ||
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
        var isWinRTObject = false;

        // If it's a generic enumerable, get the generic type.

        // Unfortunately, if data source is fed from a bare IEnumerable, TypeHelper will report an element type of object,
        // which is not particularly interesting.  It is dealt with it further on.
        itemType = listType.GetEnumerableItemType();

        if (itemType != null)
        {
            isICustomTypeProvider = typeof(ICustomTypeProvider).IsAssignableFrom(itemType);
#if WINDOWS
            isWinRTObject = typeof(WinRT.IInspectable).IsAssignableFrom(itemType);
#endif
        }

        // Bare IEnumerables mean that result type will be object.  In that case, try to get something more interesting.
        // Or, if the itemType implements ICustomTypeProvider, try to retrieve the custom type from one of the object instances.
        if (itemType == null || itemType == typeof(object) || isICustomTypeProvider || isWinRTObject)
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
