using System;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// 拖拽预览相关的位图工厂。缓存共享，避免各控制器重复实现同一份位图生成逻辑。
/// </summary>
internal static class LauncherDragImageFactory
{
    private static BitmapImage? _emptyDragBitmapImage;

    public static BitmapImage GetEmptyDragBitmapImage()
    {
        if (_emptyDragBitmapImage != null)
        {
            return _emptyDragBitmapImage;
        }

        byte[] png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var stream = new InMemoryRandomAccessStream();
        using (var output = stream.GetOutputStreamAt(0))
        using (var writer = new DataWriter(output))
        {
            writer.WriteBytes(png);
            writer.StoreAsync().AsTask().GetAwaiter().GetResult();
            writer.FlushAsync().AsTask().GetAwaiter().GetResult();
        }

        stream.Seek(0);
        var bitmap = new BitmapImage();
        bitmap.SetSource(stream);
        _emptyDragBitmapImage = bitmap;
        return _emptyDragBitmapImage;
    }
}
