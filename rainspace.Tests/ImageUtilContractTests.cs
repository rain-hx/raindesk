namespace rainspace.Tests;

[TestClass]
public class ImageUtilContractTests
{
    [TestMethod]
    public void ShortcutIconBytes_ResolveLnkBeforeExtractingIcon()
    {
        string source = ReadImageUtilSource();
        int methodIndex = source.IndexOf("public static byte[] GetImageBytesByPath", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("public static BitmapImage GetBitmapIconByUnknownPath", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate GetImageBytesByPath body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("filePath = ResolveShortcutIconPathForDisplay(filePath)", StringComparison.Ordinal)
            && body.IndexOf("ResolveShortcutIconPathForDisplay(filePath)", StringComparison.Ordinal)
                < body.IndexOf("FileIcon.GetIconBytes(filePath)", StringComparison.Ordinal),
            "Icon bytes for .lnk files must be extracted from the shortcut target or explicit icon location, not from the .lnk file with the Windows arrow overlay.");
    }

    [TestMethod]
    public void EnsureIconBitmap_RefreshesStoredLnkOverlayIconsWithoutBlockingFirstFrame()
    {
        string source = ReadImageUtilSource();
        int methodIndex = source.IndexOf("public static void EnsureIconBitmap", StringComparison.Ordinal);
        int nextMethodIndex = source.IndexOf("public static void PreloadImages", methodIndex, StringComparison.Ordinal);
        Assert.IsTrue(methodIndex >= 0 && nextMethodIndex > methodIndex, "Could not locate EnsureIconBitmap body.");

        string body = source[methodIndex..nextMethodIndex];
        Assert.IsTrue(
            body.Contains("TryRefreshShortcutIconBitmap(icon)", StringComparison.Ordinal)
            && body.Contains("QueueShortcutIconRefresh(icon, shortcutPath!, dispatcher)", StringComparison.Ordinal)
            && body.Contains("BitmapImage? cachedBitmap = icon.BitmapImage_NoWrite", StringComparison.Ordinal),
            "Stored .lnk icons should render from cache first and refresh legacy overlays asynchronously.");
    }

    private static string ReadImageUtilSource()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            string candidate = Path.Combine(directory, "rainspace", "Util", "ImageUtil.cs");
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        Assert.Fail("Could not locate ImageUtil.cs from the test output directory.");
        return string.Empty;
    }
}
