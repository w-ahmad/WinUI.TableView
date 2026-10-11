using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TypeExtensionsTests
{
    [TestMethod]
    public void IsBoolean_WorksForBoolAndNullableBool()
    {
        Assert.IsTrue(typeof(bool).IsBoolean());
        Assert.IsTrue(typeof(bool?).IsBoolean());
        Assert.IsFalse(typeof(int).IsBoolean());
    }

    [TestMethod]
    public void IsNumeric_WorksForAllNumericTypes()
    {
        Assert.IsTrue(typeof(byte).IsNumeric());
        Assert.IsTrue(typeof(byte?).IsNumeric());
        Assert.IsTrue(typeof(sbyte).IsNumeric());
        Assert.IsTrue(typeof(short).IsNumeric());
        Assert.IsTrue(typeof(ushort).IsNumeric());
        Assert.IsTrue(typeof(int).IsNumeric());
        Assert.IsTrue(typeof(uint).IsNumeric());
        Assert.IsTrue(typeof(long).IsNumeric());
        Assert.IsTrue(typeof(ulong).IsNumeric());
        Assert.IsTrue(typeof(float).IsNumeric());
        Assert.IsTrue(typeof(double).IsNumeric());
        Assert.IsTrue(typeof(decimal).IsNumeric());
        Assert.IsTrue(typeof(int?).IsNumeric());
        Assert.IsFalse(typeof(string).IsNumeric());
        Assert.IsFalse(typeof(DateTime).IsNumeric());
    }

    [TestMethod]
    public void IsTimeSpan_WorksForTimeSpanAndNullable()
    {
        Assert.IsTrue(typeof(TimeSpan).IsTimeSpan());
        Assert.IsTrue(typeof(TimeSpan?).IsTimeSpan());
        Assert.IsFalse(typeof(TimeOnly).IsTimeSpan());
    }

    [TestMethod]
    public void IsTimeOnly_WorksForTimeOnlyAndNullable()
    {
        Assert.IsTrue(typeof(TimeOnly).IsTimeOnly());
        Assert.IsTrue(typeof(TimeOnly?).IsTimeOnly());
        Assert.IsFalse(typeof(TimeSpan).IsTimeOnly());
    }

    [TestMethod]
    public void IsDateOnly_WorksForDateOnlyAndNullable()
    {
        Assert.IsTrue(typeof(DateOnly).IsDateOnly());
        Assert.IsTrue(typeof(DateOnly?).IsDateOnly());
        Assert.IsFalse(typeof(DateTime).IsDateOnly());
    }

    [TestMethod]
    public void IsDateTime_WorksForDateTimeAndNullable()
    {
        Assert.IsTrue(typeof(DateTime).IsDateTime());
        Assert.IsTrue(typeof(DateTime?).IsDateTime());
        Assert.IsFalse(typeof(DateTimeOffset).IsDateTime());
    }

    [TestMethod]
    public void IsDateTimeOffset_WorksForDateTimeOffsetAndNullable()
    {
        Assert.IsTrue(typeof(DateTimeOffset).IsDateTimeOffset());
        Assert.IsTrue(typeof(DateTimeOffset?).IsDateTimeOffset());
        Assert.IsFalse(typeof(DateTime).IsDateTimeOffset());
    }

    [TestMethod]
    public void IsNullableType_WorksForNullableTypes()
    {
        Assert.IsTrue(typeof(int?).IsNullableType());
        Assert.IsTrue(typeof(DateTime?).IsNullableType());
        Assert.IsFalse(typeof(int).IsNullableType());
        Assert.IsFalse(typeof(string).IsNullableType());
    }

    [TestMethod]
    public void GetNonNullableType_ReturnsUnderlyingType()
    {
        Assert.AreEqual(typeof(int), typeof(int?).GetNonNullableType());
        Assert.AreEqual(typeof(DateTime), typeof(DateTime?).GetNonNullableType());
        Assert.AreEqual(typeof(string), typeof(string).GetNonNullableType());
    }

    [TestMethod]
    public void IsPrimitive_WorksForPrimitiveAndSpecialTypes()
    {
        Assert.IsTrue(typeof(int).IsPrimitive());
        Assert.IsTrue(typeof(string).IsPrimitive());
        Assert.IsTrue(typeof(decimal).IsPrimitive());
        Assert.IsTrue(typeof(DateTime).IsPrimitive());
        Assert.IsFalse(typeof(object).IsPrimitive());
        Assert.IsFalse(typeof(DateOnly).IsPrimitive());
    }

    [TestMethod]
    public void IsInheritedFromIComparable_WorksForComparableTypes()
    {
        Assert.IsTrue(typeof(int).IsInheritedFromIComparable());
        Assert.IsTrue(typeof(string).IsInheritedFromIComparable());
        Assert.IsTrue(typeof(DateOnly).IsInheritedFromIComparable());
        Assert.IsFalse(typeof(object).IsInheritedFromIComparable());
    }

    [TestMethod]
    public void IsUri_WorksForUriType()
    {
        Assert.IsTrue(typeof(Uri).IsUri());
        Assert.IsFalse(typeof(string).IsUri());
        Assert.IsFalse(typeof(object).IsUri());
    }
}
