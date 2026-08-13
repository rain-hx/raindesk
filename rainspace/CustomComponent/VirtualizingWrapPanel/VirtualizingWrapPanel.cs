using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace rainspace.CustomComponent.VirtualizingWrapPanel
{
    /// <summary>
    /// WinUI3 stub for VirtualizingWrapPanel.
    /// In WinUI3, use ItemsRepeater with a uniform grid layout instead.
    /// This stub exists for compilation compatibility.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanelBase
    {
        public static readonly DependencyProperty SpacingModeProperty =
            DependencyProperty.Register(nameof(SpacingMode), typeof(SpacingMode), typeof(VirtualizingWrapPanel), new PropertyMetadata(SpacingMode.Uniform));

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(VirtualizingWrapPanel), new PropertyMetadata(Orientation.Vertical));

        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register(nameof(ItemSize), typeof(Windows.Foundation.Size), typeof(VirtualizingWrapPanel), new PropertyMetadata(default(Windows.Foundation.Size)));

        public static readonly DependencyProperty StretchItemsProperty =
            DependencyProperty.Register(nameof(StretchItems), typeof(bool), typeof(VirtualizingWrapPanel), new PropertyMetadata(false));

        public SpacingMode SpacingMode { get => (SpacingMode)GetValue(SpacingModeProperty); set => SetValue(SpacingModeProperty, value); }
        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
        public Windows.Foundation.Size ItemSize { get => (Windows.Foundation.Size)GetValue(ItemSizeProperty); set => SetValue(ItemSizeProperty, value); }
        public bool StretchItems { get => (bool)GetValue(StretchItemsProperty); set => SetValue(StretchItemsProperty, value); }

        protected override Windows.Foundation.Size CalculateExtent(Windows.Foundation.Size availableSize)
        {
            return availableSize;
        }

        protected override ItemRange UpdateItemRange()
        {
            return new ItemRange(0, Math.Max(Children.Count - 1, 0));
        }

        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            foreach (var child in Children)
            {
                child.Measure(availableSize);
            }
            return availableSize;
        }

        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            double x = 0;
            double y = 0;
            double rowHeight = 0;

            foreach (var child in Children)
            {
                var desiredSize = child.DesiredSize;
                if (x + desiredSize.Width > finalSize.Width && x > 0)
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }
                child.Arrange(new Windows.Foundation.Rect(x, y, desiredSize.Width, desiredSize.Height));
                x += desiredSize.Width;
                rowHeight = Math.Max(rowHeight, desiredSize.Height);
            }

            return finalSize;
        }
    }
}
