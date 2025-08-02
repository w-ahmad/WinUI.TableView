using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WinUI.TableView;

namespace WinUI.TableView.Tests;

[TestClass]
public class PerformanceTests
{
    private class TestDataItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public double Value { get; set; }
        public bool IsActive { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    private static List<TestDataItem> GenerateLargeDataset(int count)
    {
        var random = new Random(42); // Fixed seed for consistent results
        var categories = new[] { "Category1", "Category2", "Category3", "Category4", "Category5" };
        var data = new List<TestDataItem>(count);

        for (int i = 0; i < count; i++)
        {
            data.Add(new TestDataItem
            {
                Id = i,
                Name = $"Item_{i:D6}",
                CreatedDate = DateTime.Now.AddDays(-random.Next(365)),
                Value = random.NextDouble() * 1000,
                IsActive = random.Next(2) == 1,
                Category = categories[random.Next(categories.Length)]
            });
        }

        return data;
    }

    [TestMethod]
    public void SortDescription_GetPropertyValue_WithLargeDataset_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(10000);
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var results = new List<object?>();
        foreach (var item in dataset)
        {
            results.Add(sortDescription.GetPropertyValue(item));
        }
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(dataset.Count, results.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Performance test failed. Expected < 1000ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify correctness
        Assert.AreEqual("Item_000000", results[0]);
        Assert.AreEqual("Item_009999", results[9999]);
    }

    [TestMethod]
    public void SortDescription_Compare_WithLargeDataset_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(1000);
        var sortDescription = new SortDescription("Value", SortDirection.Ascending);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var sortedList = dataset.OrderBy(x => (double)sortDescription.GetPropertyValue(x)!).ToList();
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(dataset.Count, sortedList.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, 
            $"Sort performance test failed. Expected < 500ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify sorting correctness
        for (int i = 1; i < sortedList.Count; i++)
        {
            var prev = (double)sortDescription.GetPropertyValue(sortedList[i - 1])!;
            var curr = (double)sortDescription.GetPropertyValue(sortedList[i])!;
            Assert.IsTrue(prev <= curr, $"Sort order incorrect at index {i}");
        }
    }

    [TestMethod]
    public void FilterDescription_Predicate_WithLargeDataset_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(50000);
        var filterDescription = new FilterDescription("IsActive", 
            item => ((TestDataItem?)item)?.IsActive == true);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var filteredItems = dataset.Where(item => filterDescription.Predicate(item)).ToList();
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(filteredItems.Count > 0);
        Assert.IsTrue(filteredItems.Count < dataset.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, 
            $"Filter performance test failed. Expected < 1000ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify filtering correctness
        Assert.IsTrue(filteredItems.All(item => item.IsActive));
    }

    [TestMethod]
    public void ComplexFiltering_WithMultipleConditions_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(20000);
        var complexFilter = new FilterDescription(null, item =>
        {
            var typedItem = (TestDataItem?)item;
            return typedItem?.Value > 500 && 
                   typedItem.IsActive && 
                   typedItem.Name.Contains("5") &&
                   typedItem.Category == "Category1";
        });
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var filteredItems = dataset.Where(item => complexFilter.Predicate(item)).ToList();
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, 
            $"Complex filter performance test failed. Expected < 500ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify filtering correctness
        foreach (var item in filteredItems)
        {
            Assert.IsTrue(item.Value > 500);
            Assert.IsTrue(item.IsActive);
            Assert.IsTrue(item.Name.Contains("5"));
            Assert.AreEqual("Category1", item.Category);
        }
    }

    [TestMethod]
    public void MultipleSortDescriptions_WithLargeDataset_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(5000);
        var categorySort = new SortDescription("Category", SortDirection.Ascending);
        var valueSort = new SortDescription("Value", SortDirection.Descending);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var sortedList = dataset
            .OrderBy(x => categorySort.GetPropertyValue(x))
            .ThenByDescending(x => valueSort.GetPropertyValue(x))
            .ToList();
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(dataset.Count, sortedList.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
            $"Multiple sort performance test failed. Expected < 300ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify multi-level sorting correctness
        string? previousCategory = null;
        double? previousValueInCategory = null;
        
        foreach (var item in sortedList)
        {
            var currentCategory = (string)categorySort.GetPropertyValue(item)!;
            var currentValue = (double)valueSort.GetPropertyValue(item)!;
            
            if (previousCategory == null || currentCategory != previousCategory)
            {
                // New category group
                previousCategory = currentCategory;
                previousValueInCategory = currentValue;
            }
            else
            {
                // Same category, value should be descending
                Assert.IsTrue(currentValue <= previousValueInCategory, 
                    $"Values not in descending order within category {currentCategory}");
                previousValueInCategory = currentValue;
            }
        }
    }

    [TestMethod]
    public void SortDescription_WithCustomComparer_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(8000);
        var customComparer = StringComparer.OrdinalIgnoreCase;
        var sortDescription = new SortDescription("Name", SortDirection.Ascending, customComparer);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var sortedList = dataset.OrderBy(x => (string)sortDescription.GetPropertyValue(x)!, customComparer).ToList();
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(dataset.Count, sortedList.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 400, 
            $"Custom comparer performance test failed. Expected < 400ms, actual: {stopwatch.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public void FilterDescription_WithNullValues_HandlesLargeDatasetGracefully()
    {
        // Arrange
        var dataset = GenerateLargeDataset(3000);
        // Add some null entries
        for (int i = 0; i < 500; i++)
        {
            dataset.Add(null!);
        }
        
        var filterDescription = new FilterDescription("Name", 
            item => ((TestDataItem?)item)?.Name?.StartsWith("Item_00") == true);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var filteredItems = dataset.Where(item => filterDescription.Predicate(item)).ToList();
        stopwatch.Stop();

        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 200, 
            $"Null handling performance test failed. Expected < 200ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify no nulls in results and correct filtering
        Assert.IsTrue(filteredItems.All(item => item != null && item.Name.StartsWith("Item_00")));
    }

    [TestMethod]
    public void SortDescription_WithValueDelegate_PerformsReasonably()
    {
        // Arrange
        var dataset = GenerateLargeDataset(10000);
        Func<object?, object?> valueDelegate = item => ((TestDataItem?)item)?.Name?.Length ?? 0;
        var sortDescription = new SortDescription(null, SortDirection.Ascending, valueDelegate: valueDelegate);
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var results = dataset.Select(item => sortDescription.GetPropertyValue(item)).ToList();
        stopwatch.Stop();

        // Assert
        Assert.AreEqual(dataset.Count, results.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 300, 
            $"Value delegate performance test failed. Expected < 300ms, actual: {stopwatch.ElapsedMilliseconds}ms");
        
        // Verify delegate was used correctly
        Assert.IsTrue(results.All(result => result is int && (int)result > 0));
    }

    [TestMethod]
    public void MemoryUsage_WithLargeDataset_RemainsReasonable()
    {
        // Arrange
        const int datasetSize = 100000;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var dataset = GenerateLargeDataset(datasetSize);
        var sortDescription = new SortDescription("Name", SortDirection.Ascending);
        var filterDescription = new FilterDescription("IsActive", 
            item => ((TestDataItem?)item)?.IsActive == true);

        // Perform operations
        var filteredAndSorted = dataset
            .Where(item => filterDescription.Predicate(item))
            .OrderBy(item => sortDescription.GetPropertyValue(item))
            .ToList();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        // Assert
        Assert.IsTrue(filteredAndSorted.Count > 0);
        
        // Memory usage should be reasonable (less than 100MB for this test)
        const long maxExpectedMemory = 100 * 1024 * 1024; // 100MB
        Assert.IsTrue(memoryUsed < maxExpectedMemory, 
            $"Memory usage too high: {memoryUsed / (1024 * 1024)}MB");
    }
}