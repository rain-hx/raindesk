using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using raindesk.Services;

namespace raindesk.Tests;

[TestClass]
public class MainWindowMessagePolicyTests
{
    [TestMethod]
    public void ShouldScheduleHideOnLostFocus_WhenSizingWindow_ReturnsFalse()
    {
        Assert.IsTrue(MainWindowMessagePolicy.IsBeginMoveSizeMessage(
            NativeWindowMethods.WM_SYSCOMMAND,
            new IntPtr(NativeWindowMethods.SC_SIZE)));

        bool shouldHide = MainWindowMessagePolicy.ShouldScheduleHideOnLostFocus(
            NativeWindowMethods.WM_ACTIVATE,
            IntPtr.Zero,
            suppressLostFocusHide: true);

        Assert.IsFalse(shouldHide);
    }

    [TestMethod]
    public void ShouldHideOnLostFocus_WhenSizingWindow_ReturnsFalse()
    {
        bool shouldHide = MainWindowMessagePolicy.ShouldHideOnLostFocus(
            isConfiguredForLostFocusHide: true,
            suppressLostFocusHide: true);

        Assert.IsFalse(shouldHide);
    }

    [TestMethod]
    public void ShouldSuppressLostFocusHide_WhenIconDragActive_ReturnsTrue()
    {
        bool suppress = MainWindowMessagePolicy.ShouldSuppressLostFocusHide(
            isInMoveSizeLoop: false,
            isIconDragActive: true);

        Assert.IsTrue(suppress);
    }

    [TestMethod]
    public void ShouldScheduleHideOnLostFocus_WhenIconDragActive_ReturnsFalse()
    {
        bool shouldHide = MainWindowMessagePolicy.ShouldScheduleHideOnLostFocus(
            NativeWindowMethods.WM_KILLFOCUS,
            IntPtr.Zero,
            suppressLostFocusHide: MainWindowMessagePolicy.ShouldSuppressLostFocusHide(
                isInMoveSizeLoop: false,
                isIconDragActive: true));

        Assert.IsFalse(shouldHide);
    }
}
