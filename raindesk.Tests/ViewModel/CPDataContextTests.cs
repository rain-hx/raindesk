using raindesk.ViewModel;

namespace raindesk.Tests.ViewModel;

[TestClass]
public class CPDataContextTests
{
    [TestMethod]
    public void SettingSamePixelColor_DoesNotRaisePropertyChanged()
    {
        var context = new CPDataContext();
        var color = System.Drawing.Color.FromArgb(255, 12, 34, 56);
        context.PixelColor_D = color;
        int changeCount = 0;
        context.PropertyChanged += (_, _) => changeCount++;

        context.PixelColor_D = color;

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentPixelColor_UpdatesDerivedColorValues()
    {
        var context = new CPDataContext();
        var propertyNames = new List<string?>();
        context.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        context.PixelColor_D = System.Drawing.Color.FromArgb(255, 12, 34, 56);

        CollectionAssert.AreEqual(
            new[] { nameof(CPDataContext.ColorHtml), nameof(CPDataContext.ColorRGB), nameof(CPDataContext.PixelColor) },
            propertyNames);
        Assert.AreEqual("0C2238", context.ColorHtml);
        Assert.AreEqual("12,34,56", context.ColorRGB);
        Assert.AreEqual((byte)255, context.PixelColor.A);
        Assert.AreEqual((byte)12, context.PixelColor.R);
        Assert.AreEqual((byte)34, context.PixelColor.G);
        Assert.AreEqual((byte)56, context.PixelColor.B);
    }
}
