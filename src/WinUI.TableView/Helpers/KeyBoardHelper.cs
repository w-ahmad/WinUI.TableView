using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides helper methods for keyboard keys state checks.
/// </summary>
internal static class KeyboardHelper
{
    /// <summary>
    /// Determines whether the Shift key is currently pressed.
    /// </summary>
    /// <returns>True if the Shift key is down; otherwise, false.</returns>
    public static bool IsShiftKeyDown()
    {
        var shiftKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        return shiftKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
    }

    /// <summary>
    /// Determines whether the Ctrl key is currently pressed.
    /// </summary>
    /// <returns>True if the Ctrl key is down; otherwise, false.</returns>
    public static bool IsCtrlKeyDown()
    {
        var ctrlKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return ctrlKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
    }
}
