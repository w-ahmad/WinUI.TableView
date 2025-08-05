using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class DateTimeExtensionsTests
{
    [TestMethod]
    public void ToDateTimeOffset_FromDateTime_UsesLocalOffset()
    {
        var dt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var dto = dt.ToDateTimeOffset();
        var expected = new DateTimeOffset(dt, TimeZoneInfo.Local.GetUtcOffset(dt));
        Assert.AreEqual(expected, dto);
    }

    [TestMethod]
    public void ToDateTimeOffset_FromTimeSpan_UsesTodayDate()
    {
        var ts = new TimeSpan(12, 34, 56);
        var today = DateTime.Today;
        var expected = new DateTimeOffset(today.Add(ts), TimeZoneInfo.Local.GetUtcOffset(today.Add(ts)));
        var dto = ts.ToDateTimeOffset();
        Assert.AreEqual(expected, dto);
    }

    [TestMethod]
    public void ToDateTimeOffset_FromDateOnly_MinTimeOfDay()
    {
        var date = new DateOnly(2024, 5, 6);
        var expectedDateTime = date.ToDateTime(TimeOnly.MinValue);
        var expected = new DateTimeOffset(expectedDateTime, TimeZoneInfo.Local.GetUtcOffset(expectedDateTime));
        var dto = date.ToDateTimeOffset();
        Assert.AreEqual(expected, dto);
    }

    [TestMethod]
    public void ToDateTimeOffset_FromTimeOnly_UsesTodayDate()
    {
        var time = new TimeOnly(8, 9, 10);
        var today = DateTime.Today;
        var expectedDateTime = today.Add(time.ToTimeSpan());
        var expected = new DateTimeOffset(expectedDateTime, TimeZoneInfo.Local.GetUtcOffset(expectedDateTime));
        var dto = time.ToDateTimeOffset();
        Assert.AreEqual(expected, dto);
    }
}
