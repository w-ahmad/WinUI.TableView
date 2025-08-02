using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class TypeExtensionsTests
{
    [Fact]
    public void IsBoolean_WithBooleanTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(bool).IsBoolean());
        Assert.True(typeof(bool?).IsBoolean());
    }

    [Fact]
    public void IsBoolean_WithNonBooleanTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(int).IsBoolean());
        Assert.False(typeof(string).IsBoolean());
        Assert.False(typeof(DateTime).IsBoolean());
        Assert.False(typeof(object).IsBoolean());
    }

    [Fact]
    public void IsNumeric_WithNumericTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(byte).IsNumeric());
        Assert.True(typeof(byte?).IsNumeric());
        Assert.True(typeof(sbyte).IsNumeric());
        Assert.True(typeof(sbyte?).IsNumeric());
        Assert.True(typeof(short).IsNumeric());
        Assert.True(typeof(short?).IsNumeric());
        Assert.True(typeof(ushort).IsNumeric());
        Assert.True(typeof(ushort?).IsNumeric());
        Assert.True(typeof(int).IsNumeric());
        Assert.True(typeof(int?).IsNumeric());
        Assert.True(typeof(uint).IsNumeric());
        Assert.True(typeof(uint?).IsNumeric());
        Assert.True(typeof(long).IsNumeric());
        Assert.True(typeof(long?).IsNumeric());
        Assert.True(typeof(ulong).IsNumeric());
        Assert.True(typeof(ulong?).IsNumeric());
        Assert.True(typeof(float).IsNumeric());
        Assert.True(typeof(float?).IsNumeric());
        Assert.True(typeof(double).IsNumeric());
        Assert.True(typeof(double?).IsNumeric());
        Assert.True(typeof(decimal).IsNumeric());
        Assert.True(typeof(decimal?).IsNumeric());
    }

    [Fact]
    public void IsNumeric_WithNonNumericTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(string).IsNumeric());
        Assert.False(typeof(bool).IsNumeric());
        Assert.False(typeof(DateTime).IsNumeric());
        Assert.False(typeof(object).IsNumeric());
        Assert.False(typeof(char).IsNumeric());
        Assert.False(typeof(Guid).IsNumeric());
    }

    [Fact]
    public void IsTimeSpan_WithTimeSpanTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(TimeSpan).IsTimeSpan());
        Assert.True(typeof(TimeSpan?).IsTimeSpan());
    }

    [Fact]
    public void IsTimeSpan_WithNonTimeSpanTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(DateTime).IsTimeSpan());
        Assert.False(typeof(string).IsTimeSpan());
        Assert.False(typeof(int).IsTimeSpan());
    }

    [Fact]
    public void IsTimeSpan_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsTimeSpan());
    }

    [Fact]
    public void IsTimeOnly_WithTimeOnlyTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(TimeOnly).IsTimeOnly());
        Assert.True(typeof(TimeOnly?).IsTimeOnly());
    }

    [Fact]
    public void IsTimeOnly_WithNonTimeOnlyTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(DateTime).IsTimeOnly());
        Assert.False(typeof(TimeSpan).IsTimeOnly());
        Assert.False(typeof(string).IsTimeOnly());
    }

    [Fact]
    public void IsTimeOnly_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsTimeOnly());
    }

    [Fact]
    public void IsDateOnly_WithDateOnlyTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(DateOnly).IsDateOnly());
        Assert.True(typeof(DateOnly?).IsDateOnly());
    }

    [Fact]
    public void IsDateOnly_WithNonDateOnlyTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(DateTime).IsDateOnly());
        Assert.False(typeof(TimeOnly).IsDateOnly());
        Assert.False(typeof(string).IsDateOnly());
    }

    [Fact]
    public void IsDateOnly_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsDateOnly());
    }

    [Fact]
    public void IsDateTime_WithDateTimeTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(DateTime).IsDateTime());
        Assert.True(typeof(DateTime?).IsDateTime());
    }

    [Fact]
    public void IsDateTime_WithNonDateTimeTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(DateOnly).IsDateTime());
        Assert.False(typeof(TimeOnly).IsDateTime());
        Assert.False(typeof(DateTimeOffset).IsDateTime());
        Assert.False(typeof(string).IsDateTime());
    }

    [Fact]
    public void IsDateTime_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsDateTime());
    }

    [Fact]
    public void IsDateTimeOffset_WithDateTimeOffsetTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(DateTimeOffset).IsDateTimeOffset());
        Assert.True(typeof(DateTimeOffset?).IsDateTimeOffset());
    }

    [Fact]
    public void IsDateTimeOffset_WithNonDateTimeOffsetTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(DateTime).IsDateTimeOffset());
        Assert.False(typeof(DateOnly).IsDateTimeOffset());
        Assert.False(typeof(TimeOnly).IsDateTimeOffset());
        Assert.False(typeof(string).IsDateTimeOffset());
    }

    [Fact]
    public void IsDateTimeOffset_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsDateTimeOffset());
    }

    [Fact]
    public void IsNullableType_WithNullableTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(int?).IsNullableType());
        Assert.True(typeof(bool?).IsNullableType());
        Assert.True(typeof(DateTime?).IsNullableType());
        Assert.True(typeof(decimal?).IsNullableType());
    }

    [Fact]
    public void IsNullableType_WithNonNullableTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(int).IsNullableType());
        Assert.False(typeof(string).IsNullableType());
        Assert.False(typeof(DateTime).IsNullableType());
        Assert.False(typeof(object).IsNullableType());
    }

    [Fact]
    public void IsNullableType_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsNullableType());
    }

    [Fact]
    public void GetNonNullableType_WithNullableTypes_ReturnsUnderlyingType()
    {
        // Arrange & Act & Assert
        Assert.Equal(typeof(int), typeof(int?).GetNonNullableType());
        Assert.Equal(typeof(bool), typeof(bool?).GetNonNullableType());
        Assert.Equal(typeof(DateTime), typeof(DateTime?).GetNonNullableType());
        Assert.Equal(typeof(decimal), typeof(decimal?).GetNonNullableType());
    }

    [Fact]
    public void GetNonNullableType_WithNonNullableTypes_ReturnsSameType()
    {
        // Arrange & Act & Assert
        Assert.Equal(typeof(int), typeof(int).GetNonNullableType());
        Assert.Equal(typeof(string), typeof(string).GetNonNullableType());
        Assert.Equal(typeof(DateTime), typeof(DateTime).GetNonNullableType());
        Assert.Equal(typeof(object), typeof(object).GetNonNullableType());
    }

    [Fact]
    public void IsPrimitive_WithPrimitiveTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(int).IsPrimitive());
        Assert.True(typeof(bool).IsPrimitive());
        Assert.True(typeof(char).IsPrimitive());
        Assert.True(typeof(byte).IsPrimitive());
        Assert.True(typeof(string).IsPrimitive());
        Assert.True(typeof(decimal).IsPrimitive());
        Assert.True(typeof(DateTime).IsPrimitive());
    }

    [Fact]
    public void IsPrimitive_WithNonPrimitiveTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(object).IsPrimitive());
        Assert.False(typeof(List<int>).IsPrimitive());
        Assert.False(typeof(Dictionary<string, int>).IsPrimitive());
        Assert.False(typeof(TypeExtensionsTests).IsPrimitive());
    }

    [Fact]
    public void IsPrimitive_WithNullType_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(((Type?)null).IsPrimitive());
    }

    [Fact]
    public void IsInheritedFromIComparable_WithComparableTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(int).IsInheritedFromIComparable());
        Assert.True(typeof(string).IsInheritedFromIComparable());
        Assert.True(typeof(DateTime).IsInheritedFromIComparable());
        Assert.True(typeof(decimal).IsInheritedFromIComparable());
    }

    [Fact]
    public void IsInheritedFromIComparable_WithNonComparableTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(object).IsInheritedFromIComparable());
        Assert.False(typeof(List<int>).IsInheritedFromIComparable());
    }

    public class NonComparableClass
    {
        public string Name { get; set; } = "";
    }

    public class ComparableClass : IComparable
    {
        public string Name { get; set; } = "";
        public int CompareTo(object? obj) => 0;
    }

    [Fact]
    public void IsInheritedFromIComparable_WithCustomComparableClass_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(ComparableClass).IsInheritedFromIComparable());
    }

    [Fact]
    public void IsInheritedFromIComparable_WithCustomNonComparableClass_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(NonComparableClass).IsInheritedFromIComparable());
    }

    [Fact]
    public void GetEnumerableItemType_WithGenericEnumerable_ReturnsItemType()
    {
        // Arrange & Act & Assert
        Assert.Equal(typeof(int), typeof(List<int>).GetEnumerableItemType());
        Assert.Equal(typeof(string), typeof(IEnumerable<string>).GetEnumerableItemType());
        Assert.Equal(typeof(DateTime), typeof(DateTime[]).GetEnumerableItemType());
    }

    [Fact]
    public void GetEnumerableItemType_WithNonGenericEnumerable_ReturnsOriginalType()
    {
        // Arrange & Act & Assert
        Assert.Equal(typeof(System.Collections.ArrayList), typeof(System.Collections.ArrayList).GetEnumerableItemType());
        Assert.Equal(typeof(string), typeof(string).GetEnumerableItemType());
    }

    [Fact]
    public void IsEnumerableType_WithEnumerableTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(typeof(List<int>).IsEnumerableType());
        Assert.True(typeof(IEnumerable<string>).IsEnumerableType());
        Assert.True(typeof(string[]).IsEnumerableType());
        Assert.True(typeof(HashSet<DateTime>).IsEnumerableType());
    }

    [Fact]
    public void IsEnumerableType_WithNonEnumerableTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False(typeof(int).IsEnumerableType());
        Assert.False(typeof(string).IsEnumerableType());
        Assert.False(typeof(DateTime).IsEnumerableType());
        Assert.False(typeof(object).IsEnumerableType());
        Assert.False(typeof(System.Collections.ArrayList).IsEnumerableType());
    }
}