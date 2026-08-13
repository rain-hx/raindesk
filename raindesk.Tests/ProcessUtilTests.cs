using raindesk.Constant;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Tests;

[TestClass]
public class ProcessUtilTests
{
    [TestMethod]
    public void ShouldOpenAsUrl_ReturnsFalse_ForExistingLocalFileMarkedAsUrl()
    {
        string filePath = Path.GetTempFileName();
        try
        {
            var icon = new IconInfo
            {
                IconType = IconType.URL,
                Path_NoWrite = filePath
            };

            Assert.IsFalse(ProcessUtil.ShouldOpenAsUrl(icon));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [TestMethod]
    public void NormalizeUrlForLaunch_PreservesRegisteredUriSchemes()
    {
        Assert.AreEqual("ms-settings:", ProcessUtil.NormalizeUrlForLaunch("ms-settings:"));
    }

    [TestMethod]
    public void NormalizeUrlForLaunch_AddsHttpsForBareDomains()
    {
        Assert.AreEqual("https://example.com", ProcessUtil.NormalizeUrlForLaunch("example.com"));
    }
}
