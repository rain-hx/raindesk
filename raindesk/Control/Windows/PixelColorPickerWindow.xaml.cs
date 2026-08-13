using raindesk.Interface;
using raindesk.Util;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Foundation;
using global::Windows.Graphics.Imaging;
using Windows.UI;

namespace raindesk.Control.Windows
{
    /// <summary>
    /// ColorPickerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PixelColorPickerWindow : Window, IWindowCommon
    {
        private static int PIXEL_REC_LENGTH = 10;

        private static readonly int MIN_LENGTH = 10;
        private static readonly int MAX_LENGTH = 50;

        private static WriteableBitmap screenBitmap;

        private readonly Action<Color> onColorSelected;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public PixelColorPickerWindow(Action<Color> onColorSelected)
        {
            InitializeComponent();
            raindesk.Services.ThemeService.RegisterWindow(this);
            this.onColorSelected = onColorSelected;

            try
            {
                SetProcessDPIAware();
            }
            catch (Exception) { }
            ColorPickerWindow_Init();
        }

        private void ColorPickerWindow_Init()
        {
            // WinUI 3: Get screen dimensions via DisplayArea
            var displayArea = DisplayArea.Primary;
            var workArea = displayArea.WorkArea;

            int allWidth = workArea.Width;
            int allHeight = workArea.Height;

            //获取缩放比例
            double scale = ScreenUtil.GetScreenScalingFactor();

            this.AppWindow.Resize(new global::Windows.Graphics.SizeInt32(allWidth, allHeight));
            this.AppWindow.Move(new global::Windows.Graphics.PointInt32(workArea.X, workArea.Y));

            DesktopBG.Width = allWidth;
            DesktopBG.Height = allHeight;
            this.Topmost = true;

            // 使用 System.Drawing 捕获屏幕截图 (仍可在 WinUI 3 中使用)
            System.Drawing.Bitmap bgBitmap = new System.Drawing.Bitmap(
                    (int)(allWidth * scale),
                    (int)(allHeight * scale),
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bgBitmap))
            {
                g.CopyFromScreen(
                    workArea.X,
                    workArea.Y,
                    0,
                    0,
                    bgBitmap.Size
                    );
            }

            // Convert System.Drawing.Bitmap to WinUI 3 WriteableBitmap
            screenBitmap = ConvertToWriteableBitmap(bgBitmap);
            DesktopBG.Source = screenBitmap;

            // WinUI 3: Set cursor via ProtectedCursor
            // this.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(
            //     Microsoft.UI.Input.InputSystemCursorShape.Cross);
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetPixelAbout(null);
        }

        private static WriteableBitmap ConvertToWriteableBitmap(System.Drawing.Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            WriteableBitmap wb = new WriteableBitmap(width, height);

            using (var stream = wb.PixelBuffer.AsStream())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        System.Drawing.Color pixel = bitmap.GetPixel(x, y);
                        stream.WriteByte(pixel.B);
                        stream.WriteByte(pixel.G);
                        stream.WriteByte(pixel.R);
                        stream.WriteByte(pixel.A);
                    }
                }
            }
            return wb;
        }

        public void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Escape)
            {
                this.DataContext = null;
                this.Close();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            // this.ProtectedCursor = null;
            var pos = e.GetCurrentPoint(this.Content as Microsoft.UI.Xaml.UIElement).Position;
            Color color = GetColorAtPosition(new Point(pos.X, pos.Y));
            onColorSelected?.Invoke(color);
            this.Close();
        }

        private void Window_PreviewMouseMove(object sender, PointerRoutedEventArgs e)
        {
            var pos = e.GetCurrentPoint(this.Content as Microsoft.UI.Xaml.UIElement).Position;
            SetPixelAbout(new Point(pos.X, pos.Y));
        }

        private void SetPixelAbout(Point? pos = null)
        {
            if (screenBitmap == null || pos == null) return;
            Point position = pos.Value;

            // WinUI 3: Update magnifier using cropped WriteableBitmap instead of VisualBrush
            int px = (int)position.X;
            int py = (int)position.Y;

            // Create a magnified view of the area around the cursor
            int srcSize = PIXEL_REC_LENGTH;
            int destSize = 70; // magnifier display size

            if (px >= srcSize / 2 && py >= srcSize / 2 &&
                px + srcSize / 2 < screenBitmap.PixelWidth &&
                py + srcSize / 2 < screenBitmap.PixelHeight)
            {
                // Crop and scale the bitmap for the magnifier
                var croppedBitmap = CropAndScale(screenBitmap,
                    px - srcSize / 2, py - srcSize / 2,
                    srcSize, srcSize, destSize, destSize);
                PixelBG.Source = croppedBitmap;
            }

            double x = position.X + 10;
            double y = position.Y + 10;

            //获取缩放比例
            double scale = ScreenUtil.GetScreenScalingFactor();
            double windowWidth = this.AppWindow.Size.Width;
            double windowHeight = this.AppWindow.Size.Height;
            if (x + ColorCanvas.Width > windowWidth / scale)
            {
                x = position.X - ColorCanvas.Width - 10;
            }
            if (y + ColorCanvas.Height > windowHeight / scale)
            {
                y = position.Y - ColorCanvas.Height - 10;
            }

            Canvas.SetLeft(ColorCanvas, x);
            Canvas.SetTop(ColorCanvas, y);

            Color wColor = GetColorAtPosition(position);

            PixelColor_HTML.Text = "#" + wColor.R.ToString("X2") + wColor.G.ToString("X2") + wColor.B.ToString("X2");
            PixelColor_RGB.Text = wColor.R + "," + wColor.G + "," + wColor.B;
            Pixel_XY.Text = (int)position.X + "*" + (int)position.Y;

            ((SolidColorBrush)PixelColor.Fill).Color = wColor;
        }

        private WriteableBitmap CropAndScale(WriteableBitmap source, int srcX, int srcY, int srcW, int srcH, int destW, int destH)
        {
            var dest = new WriteableBitmap(destW, destH);
            using (var srcStream = source.PixelBuffer.AsStream())
            using (var destStream = dest.PixelBuffer.AsStream())
            {
                byte[] srcPixels = new byte[source.PixelWidth * source.PixelHeight * 4];
                srcStream.Read(srcPixels, 0, srcPixels.Length);

                byte[] destPixels = new byte[destW * destH * 4];

                for (int dy = 0; dy < destH; dy++)
                {
                    int sy = srcY + (dy * srcH / destH);
                    if (sy < 0 || sy >= source.PixelHeight) continue;
                    for (int dx = 0; dx < destW; dx++)
                    {
                        int sx = srcX + (dx * srcW / destW);
                        if (sx < 0 || sx >= source.PixelWidth) continue;

                        int srcIdx = (sy * source.PixelWidth + sx) * 4;
                        int destIdx = (dy * destW + dx) * 4;
                        destPixels[destIdx] = srcPixels[srcIdx];
                        destPixels[destIdx + 1] = srcPixels[srcIdx + 1];
                        destPixels[destIdx + 2] = srcPixels[srcIdx + 2];
                        destPixels[destIdx + 3] = srcPixels[srcIdx + 3];
                    }
                }
                destStream.Write(destPixels, 0, destPixels.Length);
            }
            dest.Invalidate();
            return dest;
        }

        private Color GetColorAtPosition(Point position)
        {
            if (screenBitmap == null) return Colors.Transparent;

            int x = (int)position.X;
            int y = (int)position.Y;

            if (x < 0 || y < 0 || x >= screenBitmap.PixelWidth || y >= screenBitmap.PixelHeight)
                return Colors.Transparent;

            using (var stream = screenBitmap.PixelBuffer.AsStream())
            {
                int offset = (y * screenBitmap.PixelWidth + x) * 4;
                stream.Seek(offset, System.IO.SeekOrigin.Begin);
                byte b = (byte)stream.ReadByte();
                byte g = (byte)stream.ReadByte();
                byte r = (byte)stream.ReadByte();
                byte a = (byte)stream.ReadByte();
                return Color.FromArgb(a, r, g, b);
            }
        }


        /// <summary>
        /// 滚轮控制缩放区域
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseWheel(object sender, PointerRoutedEventArgs e)
        {
            var props = e.GetCurrentPoint(this.Content as Microsoft.UI.Xaml.UIElement).Properties;
            int delta = props.MouseWheelDelta;

            if (delta < 0 && PIXEL_REC_LENGTH < MAX_LENGTH)
            {
                //缩小
                PIXEL_REC_LENGTH += 5;
            }
            else if (delta > 0 && PIXEL_REC_LENGTH > MIN_LENGTH)
            {
                //放大
                PIXEL_REC_LENGTH -= 5;
            }
            var pos = e.GetCurrentPoint(this.Content as Microsoft.UI.Xaml.UIElement).Position;
            SetPixelAbout(new Point(pos.X, pos.Y));
        }

        /// <summary>
        /// 右键按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseRightButtonDown(object sender, RightTappedRoutedEventArgs e)
        {
            // this.ProtectedCursor = null;
            GlobalColorPickerWindow.ShowOrHide();
            //关闭
            this.Close();
        }

    }
}
