using Microsoft.VisualStudio.TestTools.UnitTesting;
using rainspace.Services;
using rainspace.Util;

namespace rainspace.Tests;

[TestClass]
public class HotkeyServiceTests
{
    [TestMethod]
    public void NormalizeModifiers_StripsNoRepeatFlag()
    {
        HotkeyModifiers modifiers = HotkeyModifiers.MOD_ALT | HotkeyModifiers.MOD_NOREPEAT;
        HotkeyModifiers normalized = HotkeyService.NormalizeModifiers(modifiers);
        Assert.AreEqual(HotkeyModifiers.MOD_ALT, normalized);
    }

    [TestMethod]
    public void IsAltWHotKey_RecognizesDefaultShortcut()
    {
        Assert.IsTrue(HotkeyService.IsAltWHotKey(HotkeyModifiers.MOD_ALT, VirtualKey.W));
        Assert.IsFalse(HotkeyService.IsAltWHotKey(HotkeyModifiers.MOD_CONTROL, VirtualKey.W));
    }

    [TestMethod]
    public void IsAltOnlyHotKey_RecognizesCustomAltShortcuts()
    {
        Assert.IsTrue(HotkeyService.IsAltOnlyHotKey(HotkeyModifiers.MOD_ALT, VirtualKey.F));
        Assert.IsTrue(HotkeyService.IsAltOnlyHotKey(HotkeyModifiers.MOD_ALT | HotkeyModifiers.MOD_NOREPEAT, VirtualKey.Q));
        Assert.IsFalse(HotkeyService.IsAltOnlyHotKey(HotkeyModifiers.MOD_ALT | HotkeyModifiers.MOD_SHIFT, VirtualKey.F));
        Assert.IsFalse(HotkeyService.IsAltOnlyHotKey(HotkeyModifiers.MOD_ALT, VirtualKey.None));
    }

    [TestMethod]
    public void HotkeyInput_RecognizesGenericModifierKeys()
    {
        Assert.IsTrue(HotkeyInput.IsModifierKey(Windows.System.VirtualKey.Control));
        Assert.IsTrue(HotkeyInput.IsModifierKey(Windows.System.VirtualKey.Shift));
        Assert.IsTrue(HotkeyInput.IsModifierKey(Windows.System.VirtualKey.Menu));

        Assert.AreEqual(HotkeyModifiers.MOD_CONTROL, HotkeyInput.GetModifier(Windows.System.VirtualKey.Control));
        Assert.AreEqual(HotkeyModifiers.MOD_SHIFT, HotkeyInput.GetModifier(Windows.System.VirtualKey.Shift));
        Assert.AreEqual(HotkeyModifiers.MOD_ALT, HotkeyInput.GetModifier(Windows.System.VirtualKey.Menu));
    }

    [TestMethod]
    public void IsValidHotKey_RejectsEmptyCombination()
    {
        Assert.IsFalse(HotkeyService.IsValidHotKey(HotkeyModifiers.None, VirtualKey.W));
        Assert.IsFalse(HotkeyService.IsValidHotKey(HotkeyModifiers.MOD_ALT, VirtualKey.None));
        Assert.IsTrue(HotkeyService.IsValidHotKey(HotkeyModifiers.MOD_ALT, VirtualKey.Q));
    }
}
