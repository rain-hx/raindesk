using System;
using System.Collections.Specialized;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace raindesk.Control.Other
{
    /// <summary>
    /// 支持虚拟化的WrapPanel，只渲染可见区域的元素
    /// WinUI 3 简化版本 - 使用 ItemsRepeater 替代
    /// </summary>
    public class VirtualizingWrapPanel : Panel
    {
        public double ItemWidth { get; set; } = 80.0;
        public double ItemHeight { get; set; } = 80.0;

        protected override Size MeasureOverride(Size availableSize)
        {
            var childSize = new Size(ItemWidth, ItemHeight);
            double x = 0;
            double y = 0;
            double maxHeight = 0;

            foreach (var child in Children)
            {
                child.Measure(childSize);

                if (x + childSize.Width > availableSize.Width && x > 0)
                {
                    x = 0;
                    y += childSize.Height;
                }

                x += childSize.Width;
                maxHeight = Math.Max(maxHeight, y + childSize.Height);
            }

            return new Size(availableSize.Width, maxHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var childSize = new Size(ItemWidth, ItemHeight);
            double x = 0;
            double y = 0;

            foreach (var child in Children)
            {
                if (x + childSize.Width > finalSize.Width && x > 0)
                {
                    x = 0;
                    y += childSize.Height;
                }

                child.Arrange(new Rect(x, y, childSize.Width, childSize.Height));
                x += childSize.Width;
            }

            return finalSize;
        }
    }
}
