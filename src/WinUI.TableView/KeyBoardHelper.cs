using Microsoft.UI.Input;
using Windows.System;
using Windows.UI.Core;

namespace WinUI.TableView;
internal static class KeyBoardHelper
{
    public static bool IsShiftKeyDown()
    {
        var shiftKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
        return shiftKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
    }

    public static bool IsCtrlKeyDown()
    {
        var ctrlKey = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        return ctrlKey is CoreVirtualKeyStates.Down or (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
    }
}
