using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace rainspace.CustomComponent.VirtualizingWrapPanel
{
    /// <summary>
    /// WinUI3 stub for VirtualizingPanelBase.
    /// In WinUI3, use ItemsRepeater or ItemsView instead.
    /// This stub exists for compilation compatibility.
    /// </summary>
    public abstract class VirtualizingPanelBase : Panel
    {
        protected int ItemsCount { get; set; }

        protected abstract Windows.Foundation.Size CalculateExtent(Windows.Foundation.Size availableSize);
        protected abstract ItemRange UpdateItemRange();
    }
}
