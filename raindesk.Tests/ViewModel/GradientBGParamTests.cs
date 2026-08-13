using raindesk.ViewModel;

namespace raindesk.Tests.ViewModel;

[TestClass]
public class GradientBGParamTests
{
    [TestMethod]
    public void SettingSameColor_DoesNotRaisePropertyChanged()
    {
        var param = new GradientBGParam("id", "Ocean", "#001122", "#334455");
        int changeCount = 0;
        param.PropertyChanged += (_, _) => changeCount++;

        param.Color1 = "#001122";

        Assert.AreEqual(0, changeCount);
    }

    [TestMethod]
    public void SettingDifferentColor_RaisesPropertyChangedOnce()
    {
        var param = new GradientBGParam("id", "Ocean", "#001122", "#334455");
        int changeCount = 0;
        string? propertyName = null;
        param.PropertyChanged += (_, e) =>
        {
            changeCount++;
            propertyName = e.PropertyName;
        };

        param.Color2 = "#556677";

        Assert.AreEqual(1, changeCount);
        Assert.AreEqual(nameof(GradientBGParam.Color2), propertyName);
    }

    [TestMethod]
    public void SettingSameName_DoesNotRaisePropertyChanged()
    {
        var param = new GradientBGParam("id", "Ocean", "#001122", "#334455");
        int changeCount = 0;
        param.PropertyChanged += (_, _) => changeCount++;

        param.Name = "Ocean";

        Assert.AreEqual(0, changeCount);
    }
}
