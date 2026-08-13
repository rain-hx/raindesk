using Microsoft.UI.Xaml;

// This file adds DataContext and Topmost properties to all Window subclasses
// that need them, using partial class declarations.

namespace rainspace.Control.Windows
{
    public partial class ConfigWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class IconfontWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class SystemItemWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class UpdateWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class GlobalColorPickerWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class PixelColorPickerWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class ToDoInfoWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

    public partial class ToDoWindow
    {
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set { _dataContext = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }

}

namespace ShowSeconds
{
    public partial class SecondsWindow
    {
        private object _dataContext2;
        public object DataContext
        {
            get => _dataContext2;
            set { _dataContext2 = value; if (Content is FrameworkElement fe) fe.DataContext = value; }
        }
        public bool Topmost { get; set; }
    }
}
