using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Controls;

/// <summary>
/// Represents a time editing element for the TableViewTimeColumn.
/// </summary>
public partial class TableViewTimePicker : Control
{
    private TextBlock? _timeText;
    private readonly TimePickerFlyout _flyout;

    /// <summary>
    /// Initializes a new instance of the TableViewTimePicker class.
    /// </summary>
    public TableViewTimePicker()
    {
        DefaultStyleKey = typeof(TableViewTimePicker);

        _flyout = new() { Placement = FlyoutPlacementMode.Bottom };
        _flyout.TimePicked += OnTimePicked;

        ClockIdentifier = _flyout.ClockIdentifier;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _timeText = GetTemplateChild("TimeText") as TextBlock;

        UpdateTimeText();
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        ShowFlyout();
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is VirtualKey.Space)
        {
            ShowFlyout();
        }
    }

    /// <summary>
    /// Shows the time picker flyout.
    /// </summary>
    private void ShowFlyout()
    {
        _flyout.Time = SelectedTime switch
        {
            TimeSpan timeSpan => timeSpan,
            TimeOnly timeOnly => timeOnly.ToTimeSpan(),
            DateTime dateTime => dateTime.TimeOfDay,
            DateTimeOffset dateTimeOffset => dateTimeOffset.TimeOfDay,
            _ => _flyout.Time
        };

        _flyout.ClockIdentifier = ClockIdentifier;
        _flyout.MinuteIncrement = MinuteIncrement;
        _flyout.ShowAt(this);
    }

    /// <summary>
    /// Handles the TimePicked event of the flyout.
    /// </summary>
    private void OnTimePicked(TimePickerFlyout sender, TimePickedEventArgs args)
    {
        var oldTime = SelectedTime is null ? TimeSpan.Zero : args.OldTime;

        if (SourceType.IsTimeSpan())
        {
            SelectedTime = args.NewTime;
        }
        else if (SourceType.IsTimeOnly())
        {
            SelectedTime = TimeOnly.FromTimeSpan(args.NewTime);
        }
        else if (SourceType.IsDateTime())
        {
            var dateTime = (DateTime?)SelectedTime ?? DateTime.Today;
            SelectedTime = dateTime.Subtract(oldTime).Add(args.NewTime);
        }
        else if (SourceType.IsDateTimeOffset())
        {
            var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Today);
            var dateTimeOffset = (DateTimeOffset?)SelectedTime ?? new DateTimeOffset(DateTime.Today, offset);
            SelectedTime = dateTimeOffset.Subtract(oldTime).Add(args.NewTime);
        }
    }

    /// <summary>
    /// Updates the text displayed in the time picker.
    /// </summary>
    private void UpdateTimeText()
    {
        if (_timeText is null) return;

        var formatter = DateTimeFormatHelper.GetDateTimeFormatter("shorttime", ClockIdentifier);

        _timeText.Text = SelectedTime switch
        {
            TimeSpan timeSpan => formatter.Format(timeSpan.ToDateTimeOffset()),
            TimeOnly timeOnly => formatter.Format(timeOnly.ToDateTimeOffset()),
            DateTime dateTime => formatter.Format(dateTime.ToDateTimeOffset()),
            DateTimeOffset dateTimeOffset => formatter.Format(dateTimeOffset),
            null => PlaceholderText,
            _ => throw new FormatException()
        };
    }

    /// <summary>
    /// Handles changes to the SelectedTime property.
    /// </summary>
    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
            timePicker.SourceType ??= e.NewValue?.GetType();
        }
    }

    /// <summary>
    /// Handles changes to the PlaceholderText property.
    /// </summary>
    private static void OnPlaceHolderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
        }
    }

    /// <summary>
    /// Handles changes to the ClockIdentifier property.
    /// </summary>
    private static void OnClockIdentifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
        }
    }

    /// <summary>
    /// Gets or sets the source type of the time picker.
    /// The value could be TimeSpan, TimeOnly, DateTime, or DateTimeOffset.
    /// </summary>
    internal Type? SourceType { get; set; }

    /// <summary>
    /// Gets or sets the selected time.
    /// </summary>
    public object? SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text for the time picker.
    /// </summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the clock identifier for the time picker.
    /// </summary>
    public string ClockIdentifier
    {
        get => (string)GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    /// <summary>
    /// Gets or sets the minute increment for the time picker.
    /// </summary>
    public int MinuteIncrement
    {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>
    /// Identifies the MinuteIncrement dependency property.
    /// </summary>
    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(nameof(MinuteIncrement), typeof(int), typeof(TableViewTimePicker), new PropertyMetadata(1));

    /// <summary>
    /// Identifies the SelectedTime dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(nameof(SelectedTime), typeof(object), typeof(TableViewTimePicker), new PropertyMetadata(default, OnSelectedTimeChanged));

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TableViewTimePicker), new PropertyMetadata("pick a time", OnPlaceHolderTextChanged));

    /// <summary>
    /// Identifies the ClockIdentifier dependency property.
    /// </summary>
    public static readonly DependencyProperty ClockIdentifierProperty = DependencyProperty.Register(nameof(ClockIdentifier), typeof(string), typeof(TableViewTimePicker), new PropertyMetadata(default, OnClockIdentifierChanged));
}
