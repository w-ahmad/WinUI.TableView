namespace WinUI.TableView.Helpers;

/// <summary>
/// Represents a value type wrapper that enables implicit conversion between the wrapped value and the wrapper type.
/// </summary>
/// <typeparam name="T">The value type to be wrapped. Must be a struct.</typeparam>
/// <param name="Value">The value to wrap.</param>
internal record TValue<T>(T Value) where T : struct
{
    /// <summary>
    /// Defines an implicit conversion from a TValue<T> instance to its underlying value of type T.
    /// </summary>
    public static implicit operator T(TValue<T> value)
    {
        return value.Value;
    }

    /// <summary>
    /// Defines an implicit conversion from the underlying value type to a TValue<T> instance.
    /// </summary>
    public static implicit operator TValue<T>(T value)
    {
        return new(value);
    }
}