#if !WINDOWS
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class RepeatedTapDetectorTests
{
    private static object? Tap(RepeatedTapDetector detector, uint id, object target, Point? releasedAt = null)
    {
        detector.Pressed(id, new Point(10, 10), target);
        return detector.Released(id, releasedAt ?? new Point(11, 11));
    }

    [TestMethod]
    public void SecondTapOnTheSameTarget_IsARepeat()
    {
        var detector = new RepeatedTapDetector();
        var target = new object();

        Assert.IsNull(Tap(detector, 1, target));
        Assert.AreSame(target, Tap(detector, 2, target));
    }

    [TestMethod]
    public void ThirdTap_StartsOver()
    {
        var detector = new RepeatedTapDetector();
        var target = new object();

        Tap(detector, 1, target);
        Tap(detector, 2, target);

        Assert.IsNull(Tap(detector, 3, target));
    }

    [TestMethod]
    public void TapOnAnotherTarget_IsNotARepeat()
    {
        var detector = new RepeatedTapDetector();

        Tap(detector, 1, new object());

        Assert.IsNull(Tap(detector, 2, new object()));
    }

    [TestMethod]
    public void Drag_IsNotATap()
    {
        var detector = new RepeatedTapDetector();
        var target = new object();

        Tap(detector, 1, target);

        Assert.IsNull(Tap(detector, 2, target, new Point(60, 10)));
        Assert.IsNull(Tap(detector, 3, target));
    }

    [TestMethod]
    public void FirstTargetOfAContact_IsKept()
    {
        var detector = new RepeatedTapDetector();
        var cell = new object();

        Tap(detector, 1, cell);
        detector.Pressed(2, new Point(10, 10), cell);
        detector.Pressed(2, new Point(10, 10), new object());

        Assert.AreSame(cell, detector.Released(2, new Point(10, 10)));
    }

    [TestMethod]
    public void ReleaseOfAnUnknownContact_IsIgnored()
    {
        var detector = new RepeatedTapDetector();

        Assert.IsNull(detector.Released(5, new Point(10, 10)));
    }
}
#endif
