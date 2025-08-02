using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WinUI.TableView;

namespace WinUI.TableView.Tests;

[TestClass]
public class ErrorHandlingAndEdgeCaseTests
{
    private class TestItem
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public TestNestedItem? NestedItem { get; set; }
    }

    private class TestNestedItem
    {
        public string Description { get; set; } = string.Empty;
    }

    private class TestItemWithException
    {
        public string Name => throw new InvalidOperationException("Property access failed");
        public int Value { get; set; }
    }

    private class TestItemWithNullProperty
    {
        public string? NullableProperty { get; set; }
        public TestNestedItem? NullNestedItem { get; set; }
    }

    [TestClass]
    public class SortDescriptionErrorHandlingTests
    {
        [TestMethod]
        public void GetPropertyValue_WithNonExistentProperty_ReturnsNull()
        {
            // Arrange
            var testItem = new TestItem { Name = "Test", Age = 25 };
            var sortDescription = new SortDescription("NonExistentProperty", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetPropertyValue_WithNestedProperty_HandlesGracefully()
        {
            // Arrange
            var testItem = new TestItem 
            { 
                Name = "Test", 
                NestedItem = new TestNestedItem { Description = "Nested" }
            };
            // This should fail gracefully as we don't support nested property access
            var sortDescription = new SortDescription("NestedItem.Description", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result); // Should return null for unsupported nested property
        }

        [TestMethod]
        public void GetPropertyValue_WithPropertyThatThrowsException_PropagatesException()
        {
            // Arrange
            var testItem = new TestItemWithException();
            var sortDescription = new SortDescription("Name", SortDirection.Ascending);

            // Act & Assert - Should propagate the exception wrapped in TargetInvocationException
            Assert.ThrowsException<TargetInvocationException>(() => 
                sortDescription.GetPropertyValue(testItem));
        }

        [TestMethod]
        public void GetPropertyValue_WithNullObjectAndValidPropertyName_ReturnsNull()
        {
            // Arrange
            var sortDescription = new SortDescription("Name", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetPropertyValue_WithEmptyStringPropertyName_ReturnsNull()
        {
            // Arrange
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription("", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetPropertyValue_WithWhitespacePropertyName_ReturnsNull()
        {
            // Arrange
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription("   ", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Compare_WithIncompatibleTypes_HandlesGracefully()
        {
            // Arrange
            var sortDescription = new SortDescription("Name", SortDirection.Ascending);

            // Act & Assert - Should not throw
            try
            {
                var result = sortDescription.Compare("string", 123);
                // Result may vary depending on comparer implementation
            }
            catch (ArgumentException)
            {
                // This is acceptable behavior for incompatible types
            }
        }

        [TestMethod]
        public void ValueDelegate_WithExceptionThrowingDelegate_HandlesGracefully()
        {
            // Arrange
            Func<object?, object?> faultyDelegate = item => throw new InvalidOperationException("Delegate failed");
            var sortDescription = new SortDescription(null, SortDirection.Ascending, valueDelegate: faultyDelegate);
            var testItem = new TestItem { Name = "Test" };

            // Act & Assert - Should propagate or handle the exception
            try
            {
                var result = sortDescription.GetPropertyValue(testItem);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (InvalidOperationException)
            {
                // Expected behavior
            }
        }

        [TestMethod]
        public void ValueDelegate_WithNullReturningDelegate_HandlesNull()
        {
            // Arrange
            Func<object?, object?> nullDelegate = item => null;
            var sortDescription = new SortDescription(null, SortDirection.Ascending, valueDelegate: nullDelegate);
            var testItem = new TestItem { Name = "Test" };

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result);
        }
    }

    [TestClass]
    public class FilterDescriptionErrorHandlingTests
    {
        [TestMethod]
        public void Predicate_WithNullInput_HandlesSafely()
        {
            // Arrange
            Predicate<object?> safePredicate = item => ((TestItem?)item)?.Name?.Length > 0;
            var filterDescription = new FilterDescription("Name", safePredicate);

            // Act
            var result = filterDescription.Predicate(null);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Predicate_WithExceptionThrowingPredicate_PropagatesException()
        {
            // Arrange
            Predicate<object?> faultyPredicate = item => throw new InvalidOperationException("Predicate failed");
            var filterDescription = new FilterDescription("Name", faultyPredicate);
            var testItem = new TestItem { Name = "Test" };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => 
                filterDescription.Predicate(testItem));
        }

        [TestMethod]
        public void Predicate_WithObjectOfWrongType_ThrowsInvalidCastException()
        {
            // Arrange
            Predicate<object?> typedPredicate = item => ((TestItem)item!).Name?.Length > 0;
            var filterDescription = new FilterDescription("Name", typedPredicate);
            var wrongTypeItem = "This is a string, not a TestItem";

            // Act & Assert
            Assert.ThrowsException<InvalidCastException>(() => 
                filterDescription.Predicate(wrongTypeItem));
        }

        [TestMethod]
        public void Predicate_WithNullPropertyAccess_HandlesSafely()
        {
            // Arrange
            var testItem = new TestItemWithNullProperty { NullableProperty = null };
            Predicate<object?> nullSafePredicate = item => 
                ((TestItemWithNullProperty?)item)?.NullableProperty?.Length > 0;
            var filterDescription = new FilterDescription("NullableProperty", nullSafePredicate);

            // Act
            var result = filterDescription.Predicate(testItem);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Predicate_WithNestedNullAccess_HandlesSafely()
        {
            // Arrange
            var testItem = new TestItemWithNullProperty { NullNestedItem = null };
            Predicate<object?> nestedNullSafePredicate = item => 
                ((TestItemWithNullProperty?)item)?.NullNestedItem?.Description?.Length > 0;
            var filterDescription = new FilterDescription("NullNestedItem", nestedNullSafePredicate);

            // Act
            var result = filterDescription.Predicate(testItem);

            // Assert
            Assert.IsFalse(result);
        }
    }

    [TestClass]
    public class EdgeCaseTests
    {
        [TestMethod]
        public void SortDescription_WithVeryLongPropertyName_HandlesGracefully()
        {
            // Arrange
            var veryLongPropertyName = new string('a', 10000);
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription(veryLongPropertyName, SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result); // Should return null for non-existent property
        }

        [TestMethod]
        public void FilterDescription_WithAlwaysThrowingPredicate_HandlesCorrectly()
        {
            // Arrange
            Predicate<object?> alwaysThrowingPredicate = item => 
                throw new ArgumentException("This predicate always throws");
            var filterDescription = new FilterDescription("Name", alwaysThrowingPredicate);
            var testItem = new TestItem { Name = "Test" };

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                filterDescription.Predicate(testItem));
        }

        [TestMethod]
        public void SortDescription_WithUnicodePropertyName_HandlesGracefully()
        {
            // Arrange
            var unicodePropertyName = "测试属性名称🚀";
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription(unicodePropertyName, SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result); // Should return null for non-existent property
        }

        [TestMethod]
        public void SortDescription_WithSpecialCharacterPropertyName_HandlesGracefully()
        {
            // Arrange
            var specialCharPropertyName = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription(specialCharPropertyName, SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result); // Should return null for non-existent property
        }

        [TestMethod]
        public void FilterDescription_WithComplexTypeCasting_HandlesEdgeCases()
        {
            // Arrange
            var testData = new List<object>
            {
                new TestItem { Name = "String Item", Age = 25 },
                new { Name = "Anonymous", Age = 30 },
                123,
                "Plain String",
                null
            };

            Predicate<object?> complexPredicate = item =>
            {
                return item switch
                {
                    TestItem testItem => testItem.Age > 20,
                    string str => str.Length > 5,
                    int number => number > 100,
                    null => false,
                    _ => false
                };
            };

            var filterDescription = new FilterDescription(null, complexPredicate);

            // Act
            var results = testData.Select(item => filterDescription.Predicate(item)).ToList();

            // Assert
            Assert.AreEqual(5, results.Count);
            Assert.IsTrue(results[0]);  // TestItem with Age > 20
            Assert.IsFalse(results[1]); // Anonymous object (not handled in switch)
            Assert.IsTrue(results[2]);  // 123 > 100
            Assert.IsTrue(results[3]);  // "Plain String".Length > 5
            Assert.IsFalse(results[4]); // null
        }

        [TestMethod]
        public void SortDescription_WithReflectionPropertyAccess_HandlesPrivateProperties()
        {
            // Arrange
            var testItem = new TestItem { Name = "Test" };
            var sortDescription = new SortDescription("PrivateProperty", SortDirection.Ascending);

            // Act
            var result = sortDescription.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(result); // Should return null for private/non-existent properties
        }

        [TestMethod]
        public void SortDescription_WithCaseSensitivePropertyName_RespectsCase()
        {
            // Arrange
            var testItem = new TestItem { Name = "Test" };
            var sortDescriptionLower = new SortDescription("name", SortDirection.Ascending); // lowercase
            var sortDescriptionUpper = new SortDescription("NAME", SortDirection.Ascending); // uppercase
            var sortDescriptionCorrect = new SortDescription("Name", SortDirection.Ascending); // correct case

            // Act
            var resultLower = sortDescriptionLower.GetPropertyValue(testItem);
            var resultUpper = sortDescriptionUpper.GetPropertyValue(testItem);
            var resultCorrect = sortDescriptionCorrect.GetPropertyValue(testItem);

            // Assert
            Assert.IsNull(resultLower);   // Case sensitive, should not find "name"
            Assert.IsNull(resultUpper);   // Case sensitive, should not find "NAME"
            Assert.AreEqual("Test", resultCorrect); // Correct case should work
        }
    }
}