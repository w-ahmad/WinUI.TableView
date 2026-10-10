using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

namespace WinUI.TableView.Tests;

[TestClass]
public class UITestDiscoveryTests
{
    [TestMethod]
    public void UITestMethod_IsDiscoverableAsAnMSTestMethod()
    {
        Assert.IsTrue(typeof(TestMethodAttribute).IsAssignableFrom(typeof(UITestMethodAttribute)),
            "UI tests must be discoverable by MSTest, not only by the in-app test runner.");
    }
}
