using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DraggAnimatedPanel
{
    /// <summary>
    /// WinUI 3 stub for DraggAnimatedPanel drag functionality.
    /// </summary>
    public partial class DraggAnimatedPanel
    {
        UIElement _draggedElement;

        public UIElement DraggedElement
        {
            get { return _draggedElement; }
            set { _draggedElement = value; }
        }
    }
}
