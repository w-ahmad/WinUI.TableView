using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;

namespace WinUI.TableView;

/// <summary>
/// Horizontal touch scrolling, selection by tap, and the horizontal scrollbar indicator on Uno.
/// </summary>
public partial class TableView
{
    private UIElement? _touchPressedElement;
#if WINDOWS

    // System leaves the vertical pan to the scroll viewer; the sideways part arrives here as a manipulation.
    private void InitializeTouchPan(UIElement rowsHost)
    {
        rowsHost.ManipulationMode = ManipulationModes.System | ManipulationModes.TranslateX | ManipulationModes.TranslateInertia;
        rowsHost.ManipulationStarted += OnTouchPanStarted;
        rowsHost.ManipulationDelta += OnTouchPanDelta;
    }

    private void DetachTouchPan(UIElement rowsHost)
    {
        rowsHost.ManipulationStarted -= OnTouchPanStarted;
        rowsHost.ManipulationDelta -= OnTouchPanDelta;
    }
#else
    private bool _isPointerOverHorizontalScrollBar;
    private bool _isTouchIndicator;
    private DispatcherTimer? _hideIndicatorTimer;

    // Uno only drives ScrollingIndicatorStates for scrollbars it materializes by name; HorizontalScrollBar2 isn't one (issue #421).
    private void InitializeHorizontalScrollIndicator(ScrollBar? xScrollBar)
    {
        if (_hideIndicatorTimer is not null) return;

        xScrollBar?.PointerEntered += (_, _) => _isPointerOverHorizontalScrollBar = true;
        xScrollBar?.PointerExited += (_, _) => _isPointerOverHorizontalScrollBar = false;
        _hideIndicatorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _hideIndicatorTimer.Tick += (_, _) =>
        {
            if (_isPointerOverHorizontalScrollBar) return;

            _hideIndicatorTimer.Stop();
            VisualStateManager.GoToState(_scrollViewer, "NoIndicator", true);
        };
        if (_scrollViewer is not null)
        {
            _scrollViewer.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateInertia;
            _scrollViewer.ManipulationStarted += OnTouchPanStarted;
            _scrollViewer.ManipulationDelta += OnTouchPanDelta;
        }

        _scrollViewer?.AddHandler(PointerMovedEvent,
            new PointerEventHandler((_, e) =>
            {
                _isTouchIndicator = e.Pointer.PointerDeviceType is PointerDeviceType.Touch;
                ShowHorizontalScrollIndicator();
            }),
            true);
    }

    private void ShowHorizontalScrollIndicator()
    {
        if (_scrollViewer?.ComputedHorizontalScrollBarVisibility is not Visibility.Visible) return;

        VisualStateManager.GoToState(_scrollViewer, _isTouchIndicator ? "TouchIndicator" : "MouseIndicator", true);
        _hideIndicatorTimer?.Stop();
        _hideIndicatorTimer?.Start();
    }
#endif

    // Touch pan scrolls horizontally by driving HorizontalOffset, the same path the wheel uses, so bar, header and frozen columns move together.
    private void OnTouchPanStarted(object sender, ManipulationStartedRoutedEventArgs e)
    {
        if (e.PointerDeviceType is not PointerDeviceType.Touch)
        {
            e.Complete();
            return;
        }
#if !WINDOWS

        _isTouchIndicator = true;
#endif
    }

    private void OnTouchPanDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        if (_scrollViewer is null) return;

        SetValue(HorizontalOffsetProperty, Math.Clamp(HorizontalOffset - e.Delta.Translation.X, 0, _scrollViewer.ScrollableWidth));
    }

    private bool RememberTouchPressedElement(UIElement pressedElement, Windows.Foundation.Point? canvasPoint)
    {
        _touchPressedElement = null;

        if (SelectionMode is ListViewSelectionMode.None
            || canvasPoint is null
            || canvasPoint.Value.Y < 0
            || pressedElement == this
            || canvasPoint.Value.X < CellsHorizontalOffset)
        {
            return false;
        }

        _touchPressedElement = pressedElement;
        return true;
    }

    /// <inheritdoc/>
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        if (e.PointerDeviceType is not PointerDeviceType.Touch || _touchPressedElement is not { } element) return;

        _touchPressedElement = null;
        SetSelectionStart(element);
        SelectPressed(SelectionMode is ListViewSelectionMode.Multiple);
    }
}
