using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using WinUI.TableView;

namespace WinUI.TableView.Tests;

[TestClass]
public class SortDescriptionTests
{
    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    [TestMethod]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var propertyName = "Name";
        var direction = SortDirection.Ascending;
        var comparer = StringComparer.OrdinalIgnoreCase;
        Func<object?, object?> valueDelegate = item => ((TestItem?)item)?.Name;

        // Act
        var sortDescription = new SortDescription(propertyName, direction, comparer, valueDelegate);

        // Assert
        Assert.AreEqual(propertyName, sortDescription.PropertyName);
        Assert.AreEqual(direction, sortDescription.Direction);
        Assert.AreEqual(comparer, sortDescription.Comparer);
        Assert.AreEqual(valueDelegate, sortDescription.ValueDelegate);
    }

    [TestMethod]
    public void Constructor_WithNullPropertyName_AllowsNullPropertyName()
    {
        // Arrange & Act
        var sortDescription = new SortDescription(null, SortDirection.Ascending);

        // Assert
        Assert.IsNull(sortDescription.PropertyName);
    }

    [TestMethod]
    public void GetPropertyValue_WithValidPropertyName_ReturnsCorrectValue()
    {
        // Arrange
        var testItem = new TestItem { Name = "John", Age = 30 };
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.AreEqual("John", result);
    }

    [TestMethod]
    public void GetPropertyValue_WithValueDelegate_UsesDelegate()
    {
        // Arrange
        var testItem = new TestItem { Name = "John", Age = 30 };
        Func<object?, object?> valueDelegate = item => ((TestItem?)item)?.Name?.ToUpper();
        var sortDescription = new SortDescription("Name", SortDirection.Ascending, valueDelegate: valueDelegate);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.AreEqual("JOHN", result);
    }

    [TestMethod]
    public void GetPropertyValue_WithNullItem_ReturnsNull()
    {
        // Arrange
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(null);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetPropertyValue_WithInvalidPropertyName_ReturnsNull()
    {
        // Arrange
        var testItem = new TestItem { Name = "John", Age = 30 };
        var sortDescription = new SortDescription("NonExistentProperty", SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetPropertyValue_WithNullPropertyName_ReturnsDefault()
    {
        // Arrange
        var testItem = new TestItem { Name = "John", Age = 30 };
        var sortDescription = new SortDescription(null, SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void Compare_WithDefaultComparer_ComparesCorrectly()
    {
        // Arrange
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);

        // Act
        var result = sortDescription.Compare("Apple", "Banana");

        // Assert
        Assert.IsTrue(result < 0); // "Apple" should come before "Banana"
    }

    [TestMethod]
    public void Compare_WithCustomComparer_UsesCustomComparer()
    {
        // Arrange
        var customComparer = StringComparer.OrdinalIgnoreCase;
        var sortDescription = new SortDescription("Name", SortDirection.Ascending, customComparer);

        // Act
        var result1 = sortDescription.Compare("apple", "APPLE");
        var result2 = sortDescription.Compare("apple", "banana");

        // Assert
        Assert.AreEqual(0, result1); // Should be equal with case-insensitive comparer
        Assert.IsTrue(result2 < 0); // "apple" should come before "banana"
    }

    [TestMethod]
    public void Compare_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);

        // Act
        var result1 = sortDescription.Compare(null, "test");
        var result2 = sortDescription.Compare("test", null);
        var result3 = sortDescription.Compare(null, null);

        // Assert
        Assert.IsTrue(result1 < 0); // null should come before non-null
        Assert.IsTrue(result2 > 0); // non-null should come after null
        Assert.AreEqual(0, result3); // null equals null
    }

    [TestMethod]
    public void SortDirection_DescendingEnumValue_IsCorrect()
    {
        // Arrange & Act
        var sortDescription = new SortDescription("Name", SortDirection.Descending);

        // Assert
        Assert.AreEqual(SortDirection.Descending, sortDescription.Direction);
    }

    [TestMethod]
    public void GetPropertyValue_WithComplexProperty_ReturnsCorrectValue()
    {
        // Arrange
        var testItem = new TestItem { CreatedDate = new DateTime(2023, 1, 1) };
        var sortDescription = new SortDescription("CreatedDate", SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.AreEqual(new DateTime(2023, 1, 1), result);
    }

    [TestMethod]
    public void GetPropertyValue_WithNumericProperty_ReturnsCorrectValue()
    {
        // Arrange
        var testItem = new TestItem { Age = 25 };
        var sortDescription = new SortDescription("Age", SortDirection.Ascending);

        // Act
        var result = sortDescription.GetPropertyValue(testItem);

        // Assert
        Assert.AreEqual(25, result);
    }
}