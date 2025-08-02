using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class DateTimeExtensionsTests
{
    [Fact]
    public void ToDateTimeOffset_WithDateTime_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var dateTime = new DateTime(2023, 6, 15, 14, 30, 0);

        // Act
        var result = dateTime.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateTime, result.DateTime);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(dateTime), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithDateTimeMin_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var dateTime = DateTime.MinValue;

        // Act
        var result = dateTime.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateTime, result.DateTime);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(dateTime), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithDateTimeMax_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var dateTime = DateTime.MaxValue;

        // Act
        var result = dateTime.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateTime, result.DateTime);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(dateTime), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeSpan_ReturnsDateTimeOffsetWithCurrentDate()
    {
        // Arrange
        var timeSpan = new TimeSpan(14, 30, 45); // 2:30:45 PM
        var today = DateTime.Today;

        // Act
        var result = timeSpan.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Add(timeSpan).Date, result.Date);
        Assert.Equal(timeSpan, result.TimeOfDay);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(today.Add(timeSpan)), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeSpanZero_ReturnsDateTimeOffsetWithCurrentDateMidnight()
    {
        // Arrange
        var timeSpan = TimeSpan.Zero;
        var today = DateTime.Today;

        // Act
        var result = timeSpan.ToDateTimeOffset();

        // Assert
        Assert.Equal(today, result.Date);
        Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(today), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeSpanNearMidnight_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var timeSpan = new TimeSpan(23, 59, 59); // 11:59:59 PM
        var today = DateTime.Today;

        // Act
        var result = timeSpan.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Add(timeSpan).Date, result.Date);
        Assert.Equal(timeSpan, result.TimeOfDay);
    }

    [Fact]
    public void ToDateTimeOffset_WithDateOnly_ReturnsDateTimeOffsetWithMinimumTime()
    {
        // Arrange
        var dateOnly = new DateOnly(2023, 6, 15);

        // Act
        var result = dateOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateOnly, DateOnly.FromDateTime(result.Date));
        Assert.Equal(TimeOnly.MinValue.ToTimeSpan(), result.TimeOfDay);
        var expectedDateTime = dateOnly.ToDateTime(TimeOnly.MinValue);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(expectedDateTime), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithDateOnlyMin_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var dateOnly = DateOnly.MinValue;

        // Act
        var result = dateOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateOnly, DateOnly.FromDateTime(result.Date));
        Assert.Equal(TimeOnly.MinValue.ToTimeSpan(), result.TimeOfDay);
    }

    [Fact]
    public void ToDateTimeOffset_WithDateOnlyMax_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var dateOnly = DateOnly.MaxValue;

        // Act
        var result = dateOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateOnly, DateOnly.FromDateTime(result.Date));
        Assert.Equal(TimeOnly.MinValue.ToTimeSpan(), result.TimeOfDay);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeOnly_ReturnsDateTimeOffsetWithCurrentDate()
    {
        // Arrange
        var timeOnly = new TimeOnly(14, 30, 45); // 2:30:45 PM
        var today = DateTime.Today;

        // Act
        var result = timeOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Date, result.Date);
        Assert.Equal(timeOnly.ToTimeSpan(), result.TimeOfDay);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(today.Add(timeOnly.ToTimeSpan())), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeOnlyMin_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var timeOnly = TimeOnly.MinValue;
        var today = DateTime.Today;

        // Act
        var result = timeOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Date, result.Date);
        Assert.Equal(TimeSpan.Zero, result.TimeOfDay);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(today), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeOnlyMax_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var timeOnly = TimeOnly.MaxValue;
        var today = DateTime.Today;

        // Act
        var result = timeOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Date, result.Date);
        Assert.Equal(timeOnly.ToTimeSpan(), result.TimeOfDay);
    }

    [Fact]
    public void ToDateTimeOffset_WithTimeOnlyNoon_ReturnsCorrectDateTimeOffset()
    {
        // Arrange
        var timeOnly = new TimeOnly(12, 0, 0); // Noon
        var today = DateTime.Today;

        // Act
        var result = timeOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(today.Date, result.Date);
        Assert.Equal(new TimeSpan(12, 0, 0), result.TimeOfDay);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(today.AddHours(12)), result.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_ConsistencyBetweenTimeSpanAndTimeOnly()
    {
        // Arrange
        var timeSpan = new TimeSpan(10, 15, 30);
        var timeOnly = new TimeOnly(10, 15, 30);

        // Act
        var timeSpanResult = timeSpan.ToDateTimeOffset();
        var timeOnlyResult = timeOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(timeSpanResult.TimeOfDay, timeOnlyResult.TimeOfDay);
        Assert.Equal(timeSpanResult.Date, timeOnlyResult.Date);
        Assert.Equal(timeSpanResult.Offset, timeOnlyResult.Offset);
    }

    [Fact]
    public void ToDateTimeOffset_ConsistencyBetweenDateTimeAndDateOnly()
    {
        // Arrange
        var dateTime = new DateTime(2023, 6, 15);
        var dateOnly = new DateOnly(2023, 6, 15);

        // Act
        var dateTimeResult = dateTime.ToDateTimeOffset();
        var dateOnlyResult = dateOnly.ToDateTimeOffset();

        // Assert
        Assert.Equal(dateTimeResult.Date, dateOnlyResult.Date);
        Assert.Equal(TimeSpan.Zero, dateTimeResult.TimeOfDay);
        Assert.Equal(TimeSpan.Zero, dateOnlyResult.TimeOfDay);
    }
}