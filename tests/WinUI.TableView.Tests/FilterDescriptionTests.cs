using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using WinUI.TableView;

namespace WinUI.TableView.Tests;

[TestClass]
public class FilterDescriptionTests
{
    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [TestMethod]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var propertyName = "Name";
        Predicate<object?> predicate = item => ((TestItem?)item)?.Name?.StartsWith("A") == true;

        // Act
        var filterDescription = new FilterDescription(propertyName, predicate);

        // Assert
        Assert.AreEqual(propertyName, filterDescription.PropertyName);
        Assert.AreEqual(predicate, filterDescription.Predicate);
    }

    [TestMethod]
    public void Constructor_WithNullPropertyName_AllowsNullPropertyName()
    {
        // Arrange
        Predicate<object?> predicate = item => true;

        // Act
        var filterDescription = new FilterDescription(null, predicate);

        // Assert
        Assert.IsNull(filterDescription.PropertyName);
        Assert.AreEqual(predicate, filterDescription.Predicate);
    }

    [TestMethod]
    public void Constructor_WithNullPredicate_AllowsNullPredicate()
    {
        // Arrange & Act
        var filterDescription = new FilterDescription("Name", null!);

        // Assert
        Assert.AreEqual("Name", filterDescription.PropertyName);
        Assert.IsNull(filterDescription.Predicate);
    }

    [TestMethod]
    public void Predicate_WithStringFilter_FiltersCorrectly()
    {
        // Arrange
        var testItem1 = new TestItem { Name = "Alice", Age = 25 };
        var testItem2 = new TestItem { Name = "Bob", Age = 30 };
        Predicate<object?> predicate = item => ((TestItem?)item)?.Name?.StartsWith("A") == true;
        var filterDescription = new FilterDescription("Name", predicate);

        // Act
        var result1 = filterDescription.Predicate(testItem1);
        var result2 = filterDescription.Predicate(testItem2);

        // Assert
        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [TestMethod]
    public void Predicate_WithNumericFilter_FiltersCorrectly()
    {
        // Arrange
        var testItem1 = new TestItem { Name = "Alice", Age = 25 };
        var testItem2 = new TestItem { Name = "Bob", Age = 35 };
        Predicate<object?> predicate = item => ((TestItem?)item)?.Age > 30;
        var filterDescription = new FilterDescription("Age", predicate);

        // Act
        var result1 = filterDescription.Predicate(testItem1);
        var result2 = filterDescription.Predicate(testItem2);

        // Assert
        Assert.IsFalse(result1);
        Assert.IsTrue(result2);
    }

    [TestMethod]
    public void Predicate_WithBooleanFilter_FiltersCorrectly()
    {
        // Arrange
        var testItem1 = new TestItem { Name = "Alice", IsActive = true };
        var testItem2 = new TestItem { Name = "Bob", IsActive = false };
        Predicate<object?> predicate = item => ((TestItem?)item)?.IsActive == true;
        var filterDescription = new FilterDescription("IsActive", predicate);

        // Act
        var result1 = filterDescription.Predicate(testItem1);
        var result2 = filterDescription.Predicate(testItem2);

        // Assert
        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [TestMethod]
    public void Predicate_WithNullItem_HandlesGracefully()
    {
        // Arrange
        Predicate<object?> predicate = item => ((TestItem?)item)?.Name?.Length > 0;
        var filterDescription = new FilterDescription("Name", predicate);

        // Act
        var result = filterDescription.Predicate(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Predicate_WithComplexFilter_FiltersCorrectly()
    {
        // Arrange
        var testItem1 = new TestItem { Name = "Alice", Age = 25, IsActive = true };
        var testItem2 = new TestItem { Name = "Bob", Age = 35, IsActive = false };
        var testItem3 = new TestItem { Name = "Charlie", Age = 28, IsActive = true };
        
        Predicate<object?> complexPredicate = item =>
        {
            var typedItem = (TestItem?)item;
            return typedItem?.Age > 26 && typedItem.IsActive;
        };
        
        var filterDescription = new FilterDescription(null, complexPredicate);

        // Act
        var result1 = filterDescription.Predicate(testItem1);
        var result2 = filterDescription.Predicate(testItem2);
        var result3 = filterDescription.Predicate(testItem3);

        // Assert
        Assert.IsFalse(result1); // Age 25 <= 26
        Assert.IsFalse(result2); // Age > 26 but not active
        Assert.IsTrue(result3);  // Age > 26 and active
    }

    [TestMethod]
    public void Predicate_WithAlwaysTruePredicate_ReturnsTrue()
    {
        // Arrange
        var testItem = new TestItem { Name = "Test" };
        Predicate<object?> predicate = item => true;
        var filterDescription = new FilterDescription("Name", predicate);

        // Act
        var result = filterDescription.Predicate(testItem);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Predicate_WithAlwaysFalsePredicate_ReturnsFalse()
    {
        // Arrange
        var testItem = new TestItem { Name = "Test" };
        Predicate<object?> predicate = item => false;
        var filterDescription = new FilterDescription("Name", predicate);

        // Act
        var result = filterDescription.Predicate(testItem);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Predicate_WithCaseInsensitiveStringFilter_FiltersCorrectly()
    {
        // Arrange
        var testItem1 = new TestItem { Name = "ALICE" };
        var testItem2 = new TestItem { Name = "alice" };
        var testItem3 = new TestItem { Name = "Bob" };
        
        Predicate<object?> predicate = item => 
            ((TestItem?)item)?.Name?.Equals("alice", StringComparison.OrdinalIgnoreCase) == true;
        var filterDescription = new FilterDescription("Name", predicate);

        // Act
        var result1 = filterDescription.Predicate(testItem1);
        var result2 = filterDescription.Predicate(testItem2);
        var result3 = filterDescription.Predicate(testItem3);

        // Assert
        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
        Assert.IsFalse(result3);
    }
}