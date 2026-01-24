using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace WinUI.TableView;

/// <summary>
/// Names and helpers for visual states in the control.
/// </summary>
internal static class VisualStates
{
    // GroupCommon

    /// <summary>
    /// Normal state
    /// </summary>
    public const string StateNormal = "Normal";

    /// <summary>
    /// PointerOver state
    /// </summary>
    public const string StatePointerOver = "PointerOver";

    /// <summary>
    /// Pressed state
    /// </summary>
    public const string StatePressed = "Pressed";

    /// <summary>
    /// Disabled state
    /// </summary>
    public const string StateDisabled = "Disabled";

    /// <summary>
    /// Common state group
    /// </summary>
    public const string GroupCommon = "CommonStates";

    // GroupExpanded

    /// <summary>
    /// Expanded state
    /// </summary>
    public const string StateExpanded = "Expanded";

    /// <summary>
    /// Collapsed state
    /// </summary>
    public const string StateCollapsed = "Collapsed";

    /// <summary>
    /// Empty state
    /// </summary>
    public const string StateEmpty = "Empty";

    // GroupFocus

    /// <summary>
    /// Unfocused state
    /// </summary>
    public const string StateUnfocused = "Unfocused";

    /// <summary>
    /// Focused state
    /// </summary>
    public const string StateFocused = "Focused";

    /// <summary>
    /// Focus state group
    /// </summary>
    public const string GroupFocus = "FocusStates";

    // GroupSelection

    /// <summary>
    /// Selected state
    /// </summary>
    public const string StateSelected = "Selected";

    /// <summary>
    /// Unselected state
    /// </summary>
    public const string StateUnselected = "Unselected";

    /// <summary>
    /// Selection state group
    /// </summary>
    public const string GroupSelection = "SelectionStates";

    // GroupActive

    /// <summary>
    /// Active state
    /// </summary>
    public const string StateActive = "Active";

    /// <summary>
    /// Inactive state
    /// </summary>
    public const string StateInactive = "Inactive";

    /// <summary>
    /// Active state group
    /// </summary>
    public const string GroupActive = "ActiveStates";

    // GroupCurrent

    /// <summary>
    /// Regular state
    /// </summary>
    public const string StateRegular = "Regular";

    /// <summary>
    /// Current state
    /// </summary>
    public const string StateCurrent = "Current";

    /// <summary>
    /// CurrentWithFocus state
    /// </summary>
    public const string StateCurrentWithFocus = "CurrentWithFocus";

    /// <summary>
    /// Current state group
    /// </summary>
    public const string GroupCurrent = "CurrentStates";

    // GroupInteraction

    /// <summary>
    /// Display state
    /// </summary>
    public const string StateDisplay = "Display";

    /// <summary>
    /// Editing state
    /// </summary>
    public const string StateEditing = "Editing";

    /// <summary>
    /// Interaction state group
    /// </summary>
    public const string GroupInteraction = "InteractionStates";

    // GroupSort

    /// <summary>
    /// Unsorted state
    /// </summary>
    public const string StateUnsorted = "Unsorted";

    /// <summary>
    /// Sort Ascending state
    /// </summary>
    public const string StateSortAscending = "SortAscending";

    /// <summary>
    /// Sort Descending state
    /// </summary>
    public const string StateSortDescending = "SortDescending";

    /// <summary>
    /// Sort state group
    /// </summary>
    public const string GroupSort = "SortStates";

    /// <summary>
    /// Unfiltered state
    /// </summary>
    public const string StateUnfiltered = "Unfiltered";

    /// <summary>
    /// Filtered state
    /// </summary>
    public const string StateFiltered = "Filtered";

    /// <summary>
    /// Filter state group
    /// </summary>
    public const string GroupFilter = "FilterStates";

    // GroupValidation

    /// <summary>
    /// Invalid state
    /// </summary>
    public const string StateInvalid = "Invalid";

    /// <summary>
    /// RowInvalid state
    /// </summary>
    public const string StateRowInvalid = "RowInvalid";

    /// <summary>
    /// RowValid state
    /// </summary>
    public const string StateRowValid = "RowValid";

    /// <summary>
    /// Valid state
    /// </summary>
    public const string StateValid = "Valid";

#if FEATURE_VALIDATION
    // RuntimeValidationStates
    public const string StateInvalidUnfocused = "InvalidUnfocused";
#endif

    /// <summary>
    /// Validation state group
    /// </summary>
    public const string GroupValidation = "ValidationStates";

    // GroupScrollBarsSeparator

    /// <summary>
    /// SeparatorExpanded state
    /// </summary>
    public const string StateSeparatorExpanded = "SeparatorExpanded";

    /// <summary>
    /// ScrollBarsSeparatorCollapsed state
    /// </summary>
    public const string StateSeparatorCollapsed = "SeparatorCollapsed";

    /// <summary>
    /// SeparatorExpandedWithoutAnimation state
    /// </summary>
    public const string StateSeparatorExpandedWithoutAnimation = "SeparatorExpandedWithoutAnimation";

    /// <summary>
    /// SeparatorCollapsedWithoutAnimation state
    /// </summary>
    public const string StateSeparatorCollapsedWithoutAnimation = "SeparatorCollapsedWithoutAnimation";

    /// <summary>
    /// ScrollBarsSeparator state group
    /// </summary>
    public const string GroupScrollBarsSeparator = "ScrollBarsSeparatorStates";

    // GroupScrollBars

    /// <summary>
    /// TouchIndicator state
    /// </summary>
    public const string StateTouchIndicator = "TouchIndicator";

    /// <summary>
    /// MouseIndicator state
    /// </summary>
    public const string StateMouseIndicator = "MouseIndicator";

    /// <summary>
    /// MouseIndicatorFull state
    /// </summary>
    public const string StateMouseIndicatorFull = "MouseIndicatorFull";

    /// <summary>
    /// NoIndicator state
    /// </summary>
    public const string StateNoIndicator = "NoIndicator";

    /// <summary>
    /// ScrollBars state group
    /// </summary>
    public const string GroupScrollBars = "ScrollBarsStates";

    // Group Corner Button

    /// <summary>
    /// No button state
    /// </summary>
    public const string StateNoButton = "NoButton";

    /// <summary>
    /// Select all button state
    /// </summary>
    public const string StateSelectAllButton = "SelectAllButton";
    
    /// <summary>
    /// Select all button disabled state
    /// </summary>
    public const string StateSelectAllButtonDisabled = "SelectAllButtonDisabled";

    /// <summary>
    /// Select all checkbox state
    /// </summary>
    public const string StateSelectAllCheckBox = "SelectAllCheckBox";

    /// <summary>
    /// Select all checkbox disabled state
    /// </summary>
    public const string StateSelectAllCheckBoxDisabled = "SelectAllCheckBoxDisabled";

    /// <summary>
    /// Options button state
    /// </summary>
    public const string StateOptionsButton = "OptionsButton";
    
    /// <summary>
    /// Options button disabled state
    /// </summary>
    public const string StateOptionsButtonDisabled = "OptionsButtonDisabled";

    /// <summary>
    /// Select all button state group
    /// </summary>
    public const string GroupCornerButton = "CornerButtonStates";

    // Group Row Details

    /// <summary>
    /// Row details are visible state
    /// </summary>
    public const string StateDetailsVisible = "DetailsVisible";

    /// <summary>
    /// Row details are collapsed state
    /// </summary>
    public const string StateDetailsCollapsed = "DetailsCollapsed";

    /// <summary>
    /// Row details state group
    /// </summary>
    public const string GroupRowDetails = "DetailsStates";

    // Group Row Details Button

    /// <summary>
    /// Row details are visible state
    /// </summary>
    public const string StateDetailsButtonVisible = "DetailsButtonVisible";

    /// <summary>
    /// Row details are collapsed state
    /// </summary>
    public const string StateDetailsButtonCollapsed = "DetailsButtonCollapsed";

    /// <summary>
    /// Row details button state group
    /// </summary>
    public const string GroupRowDetailsButton = "DetailsButtonStates";


    /// <summary>
    /// Use VisualStateManager to change the visual state of the control.
    /// </summary>
    /// <param name="control">
    /// Control whose visual state is being changed.
    /// </param>
    /// <param name="useTransitions">
    /// true to use transitions when updating the visual state, false to
    /// snap directly to the new visual state.
    /// </param>
    /// <param name="stateNames">
    /// Ordered list of state names and fallback states to transition into.
    /// Only the first state to be found will be used.
    /// </param>
    public static void GoToState(Control control, bool useTransitions, params string[] stateNames)
    {
        Debug.Assert(control != null, "Expected non-null control.");

        if (stateNames == null)
        {
            return;
        }

        foreach (var name in stateNames)
        {
            if (VisualStateManager.GoToState(control, name, useTransitions))
            {
                break;
            }
        }
    }
}