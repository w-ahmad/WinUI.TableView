using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class DependencyObjectExtensionsTests
{
    private static Grid BuildVisualTree()
    {
        var root = new Grid();
        var child1 = new StackPanel();
        var child2 = new Button();
        var grandChild = new TextBlock();
        child1.Children.Add(grandChild);
        root.Children.Add(child1);
        root.Children.Add(child2);
        return root;
    }

    [UITestMethod]
    public void FindDescendant_FindsFirstMatchingType()
    {
        var root = BuildVisualTree();
        var result = root.FindDescendant<TextBlock>();
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<TextBlock>(result);
    }

    [UITestMethod]
    public void FindDescendant_WithPredicate_FindsCorrectDescendant()
    {
        var root = BuildVisualTree();
        var result = root.FindDescendant<StackPanel>(sp => sp.Children.Count == 1);
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<StackPanel>(result);
    }

    [UITestMethod]
    public void FindDescendants_EnumeratesAllDescendants()
    {
        var root = BuildVisualTree();
        var descendants = root.FindDescendants().ToList();

        Assert.IsTrue(descendants.OfType<StackPanel>().Any());
        Assert.IsTrue(descendants.OfType<Button>().Any());
        Assert.IsTrue(descendants.OfType<TextBlock>().Any());
    }

    [UITestMethod]
    public async Task FindAscendant_FindsFirstMatchingAscendant()
    {
        var root = BuildVisualTree();
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(root);

        var grandChild = root.FindDescendant<TextBlock>();
        var ascendant = grandChild?.FindAscendant<Grid>();
        Assert.AreEqual(root, ascendant);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(root);
    }

    [UITestMethod]
    public async Task FindAscendants_EnumeratesAllAscendants()
    {
        var root = BuildVisualTree();
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(root);

        var grandChild = root.FindDescendant<TextBlock>();
        var ascendants = grandChild?.FindAscendants().ToList();
        Assert.IsTrue(ascendants?.OfType<StackPanel>().Any() ?? false);
        Assert.IsTrue(ascendants?.OfType<Grid>().Any() ?? false);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(root);
    }
}
