using System;
using System.Collections.Generic;

namespace WinUI.TableView;

/// <summary>
/// Identifies a group by the keys leading to it, so the group can be recognised again after the view is
/// re-sorted, re-filtered or re-grouped and the <see cref="TableViewGroup"/> instances have been rebuilt.
/// </summary>
internal readonly struct TableViewGroupPath : IEquatable<TableViewGroupPath>
{
    private readonly object?[] _keys;
    private readonly int _hashCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableViewGroupPath"/> struct from a group's key path.
    /// </summary>
    public TableViewGroupPath(IReadOnlyList<object?> keys)
    {
        _keys = new object?[keys.Count];

        var hash = new HashCode();

        for (var i = 0; i < keys.Count; i++)
        {
            _keys[i] = keys[i];
            hash.Add(keys[i]);
        }

        _hashCode = hash.ToHashCode();
    }

    /// <inheritdoc/>
    public bool Equals(TableViewGroupPath other)
    {
        if (_keys is null || other._keys is null)
        {
            return _keys is null && other._keys is null;
        }

        if (_keys.Length != other._keys.Length || _hashCode != other._hashCode)
        {
            return false;
        }

        for (var i = 0; i < _keys.Length; i++)
        {
            if (!Equals(_keys[i], other._keys[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TableViewGroupPath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _hashCode;

    /// <inheritdoc/>
    public override string ToString() => _keys is null ? string.Empty : string.Join(" / ", _keys);
}
