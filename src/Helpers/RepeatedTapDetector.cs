#if !WINDOWS
using System;
using Windows.Foundation;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Recognizes a touch double tap from two short contacts on the same target. Uno raises no DoubleTapped
/// for touch, where every contact is a new pointer.
/// </summary>
internal sealed class RepeatedTapDetector
{
    private const int MaxDelay = 500;
    private const double MaxMove = 12;
    private uint? _pressId;
    private Point _pressPoint;
    private object? _pressTarget;
    private long _lastTap;
    private object? _lastTarget;

    /// <summary>
    /// Records the start of a contact. The first target reported for a contact is kept.
    /// </summary>
    public void Pressed(uint pointerId, Point point, object target)
    {
        if (_pressId == pointerId) return;

        _pressId = pointerId;
        _pressPoint = point;
        _pressTarget = target;
    }

    /// <summary>
    /// Ends a contact and returns its target when the contact was a tap that repeats the previous tap.
    /// </summary>
    public object? Released(uint pointerId, Point point)
    {
        if (_pressId != pointerId) return null;

        var target = _pressTarget;
        var isTap = Math.Abs(point.X - _pressPoint.X) <= MaxMove && Math.Abs(point.Y - _pressPoint.Y) <= MaxMove;
        _pressId = null;
        _pressTarget = null;

        if (!isTap)
        {
            _lastTarget = null;
            return null;
        }

        var now = Environment.TickCount64;
        var isRepeat = ReferenceEquals(_lastTarget, target) && now - _lastTap <= MaxDelay;

        _lastTarget = isRepeat ? null : target;
        _lastTap = now;

        return isRepeat ? target : null;
    }
}
#endif
