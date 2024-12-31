using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Controls;

public class TableViewTimePicker : Control
{
    private TextBlock? _timeText;
    private readonly TimePickerFlyout _flyout;

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

    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

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

    private void UpdateTimeText()
    {
        if (_timeText is null) return;

        var formatter = ClockIdentifier is "24HourClock" ? DateTimeFormatHelper._24HourClockFormatter : DateTimeFormatHelper._12HourClockFormatter;

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

    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
            timePicker.SourceType ??= e.NewValue?.GetType();
        }
    }

    private static void OnPlaceHolderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
        }
    }

    private static void OnClockIdentifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewTimePicker timePicker)
        {
            timePicker.UpdateTimeText();
        }
    }

    internal Type? SourceType { get; set; }

    public object? SelectedTime
    {
        get => GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public string ClockIdentifier
    {
        get => (string)GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    public int MinuteIncrement
    {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(nameof(MinuteIncrement), typeof(int), typeof(TableViewTimePicker), new PropertyMetadata(1));
    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(nameof(SelectedTime), typeof(object), typeof(TableViewTimePicker), new PropertyMetadata(default, OnSelectedTimeChanged));
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TableViewTimePicker), new PropertyMetadata("pick a time", OnPlaceHolderTextChanged));
    public static readonly DependencyProperty ClockIdentifierProperty = DependencyProperty.Register(nameof(ClockIdentifier), typeof(string), typeof(TableViewTimePicker), new PropertyMetadata(default, OnClockIdentifierChanged));
}
