using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace DraggAnimatedPanel
{
    /// <summary>
    /// WinUI 3 stub for DraggAnimatedPanel.
    /// Provides the same API surface but without WPF-specific drag animation.
    /// </summary>
    public partial class DraggAnimatedPanel : StackPanel
    {
        public DraggAnimatedPanel()
        {
        }

        #region dependency properties
        public static readonly DependencyProperty ItemsWidthProperty =
            DependencyProperty.Register("ItemsWidth", typeof(double), typeof(DraggAnimatedPanel),
                new PropertyMetadata(150d));

        public static readonly DependencyProperty ItemsHeightProperty =
            DependencyProperty.Register("ItemsHeight", typeof(double), typeof(DraggAnimatedPanel),
                new PropertyMetadata(60d));

        public static readonly DependencyProperty ItemSeparationProperty =
            DependencyProperty.Register("ItemSeparation", typeof(Thickness), typeof(DraggAnimatedPanel),
                new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty AnimationMillisecondsProperty =
            DependencyProperty.Register("AnimationMilliseconds", typeof(int), typeof(DraggAnimatedPanel),
                new PropertyMetadata(200));

        public static readonly DependencyProperty SwapCommandProperty =
            DependencyProperty.Register("SwapCommand", typeof(ICommand), typeof(DraggAnimatedPanel),
                new PropertyMetadata(null));
        #endregion

        #region properties
        public double ItemsWidth
        {
            get { return (double)GetValue(ItemsWidthProperty); }
            set { SetValue(ItemsWidthProperty, value); }
        }
        public double ItemsHeight
        {
            get { return (double)GetValue(ItemsHeightProperty); }
            set { SetValue(ItemsHeightProperty, value); }
        }
        public Thickness ItemSeparation
        {
            get { return (Thickness)this.GetValue(ItemSeparationProperty); }
            set { this.SetValue(ItemSeparationProperty, value); }
        }
        public int AnimationMilliseconds
        {
            get { return (int)GetValue(AnimationMillisecondsProperty); }
            set { SetValue(AnimationMillisecondsProperty, value); }
        }
        public ICommand SwapCommand
        {
            get { return (ICommand)GetValue(SwapCommandProperty); }
            set { SetValue(SwapCommandProperty, value); }
        }
        #endregion
    }
}
