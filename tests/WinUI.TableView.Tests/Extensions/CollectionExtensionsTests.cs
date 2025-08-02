using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class CollectionExtensionsTests
{
    [Fact]
    public void AddRange_WithValidItems_AddsAllItems()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 4, 5, 6 };

        // Act
        collection.AddRange(itemsToAdd);

        // Assert
        Assert.Equal(6, collection.Count);
        Assert.Contains(4, collection);
        Assert.Contains(5, collection);
        Assert.Contains(6, collection);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, collection);
    }

    [Fact]
    public void AddRange_WithEmptyCollection_AddsItemsToEmptyCollection()
    {
        // Arrange
        var collection = new List<string>();
        var itemsToAdd = new[] { "a", "b", "c" };

        // Act
        collection.AddRange(itemsToAdd);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(new[] { "a", "b", "c" }, collection);
    }

    [Fact]
    public void AddRange_WithEmptyItems_DoesNotChangeCollection()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };
        var itemsToAdd = Array.Empty<int>();

        // Act
        collection.AddRange(itemsToAdd);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(new[] { 1, 2, 3 }, collection);
    }

    [Fact]
    public void AddRange_WithDifferentCollectionTypes_Works()
    {
        // Arrange
        var collection = new List<int> { 1, 2 };
        var hashSet = new HashSet<int> { 3, 4, 5 };

        // Act
        collection.AddRange(hashSet);

        // Assert
        Assert.Equal(5, collection.Count);
        Assert.Contains(3, collection);
        Assert.Contains(4, collection);
        Assert.Contains(5, collection);
    }

    [Fact]
    public void RemoveWhere_WithMatchingPredicate_RemovesMatchingItems()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3, 4, 5, 6 };

        // Act
        collection.RemoveWhere(x => x % 2 == 0); // Remove even numbers

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(new[] { 1, 3, 5 }, collection);
    }

    [Fact]
    public void RemoveWhere_WithNoMatchingItems_DoesNotChangeCollection()
    {
        // Arrange
        var collection = new List<string> { "apple", "banana", "cherry" };

        // Act
        collection.RemoveWhere(x => x.StartsWith("z")); // No items start with 'z'

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Equal(new[] { "apple", "banana", "cherry" }, collection);
    }

    [Fact]
    public void RemoveWhere_WithAllItemsMatching_RemovesAllItems()
    {
        // Arrange
        var collection = new List<int> { 2, 4, 6, 8 };

        // Act
        collection.RemoveWhere(x => x % 2 == 0); // All are even

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void RemoveWhere_WithEmptyCollection_DoesNothing()
    {
        // Arrange
        var collection = new List<int>();

        // Act
        collection.RemoveWhere(x => x > 0);

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void RemoveWhere_WithComplexPredicate_RemovesCorrectItems()
    {
        // Arrange
        var collection = new List<string> { "apple", "banana", "apricot", "cherry", "avocado" };

        // Act
        collection.RemoveWhere(x => x.StartsWith("a") && x.Length > 5);

        // Assert
        Assert.Equal(4, collection.Count);
        Assert.Contains("apple", collection);
        Assert.Contains("banana", collection);
        Assert.Contains("cherry", collection);
        Assert.DoesNotContain("apricot", collection);
        Assert.DoesNotContain("avocado", collection);
    }

    [Fact]
    public void RemoveWhere_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collection.RemoveWhere(null!));
    }

    public class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void RemoveWhere_WithCustomObjects_RemovesCorrectItems()
    {
        // Arrange
        var collection = new List<TestObject>
        {
            new() { Name = "Alice", Value = 25 },
            new() { Name = "Bob", Value = 30 },
            new() { Name = "Charlie", Value = 35 },
            new() { Name = "David", Value = 20 }
        };

        // Act
        collection.RemoveWhere(x => x.Value < 30);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Contains(collection, x => x.Name == "Bob");
        Assert.Contains(collection, x => x.Name == "Charlie");
        Assert.DoesNotContain(collection, x => x.Name == "Alice");
        Assert.DoesNotContain(collection, x => x.Name == "David");
    }

    [Fact]
    public void AddRange_WithHashSetCollection_AddsUniqueItems()
    {
        // Arrange
        var collection = new HashSet<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 3, 4, 5 }; // 3 is duplicate

        // Cast to IList<int> for the extension method
        var listView = collection.ToList();

        // Act
        listView.AddRange(itemsToAdd);

        // Assert
        Assert.Equal(6, listView.Count);
        Assert.Contains(1, listView);
        Assert.Contains(2, listView);
        Assert.Contains(3, listView);
        Assert.Contains(4, listView);
        Assert.Contains(5, listView);
        // Note: The duplicate 3 will be added as this is a List, not a HashSet
        Assert.Equal(2, listView.Count(x => x == 3));
    }

    [Fact]
    public void RemoveWhere_WithDifferentCollectionTypes_Works()
    {
        // Arrange
        var collection = new HashSet<string> { "apple", "banana", "cherry", "date" };

        // Act
        collection.RemoveWhere(x => x.Length == 4); // Remove 4-letter words

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Contains("apple", collection);
        Assert.Contains("banana", collection);
        Assert.DoesNotContain("cherry", collection);
        Assert.DoesNotContain("date", collection);
    }
}