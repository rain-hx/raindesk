using raindesk.Constant;
using raindesk.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace raindesk.ViewModel
{
    [Serializable]
    public class GradientBGParam : INotifyPropertyChanged
    {
        private string id;

        private string color1;

        private string color2;

        private string name;

        public GradientBGParam() { }

        public GradientBGParam(string id, string name, string color1, string color2)
        {
            this.id = id;
            this.name = name;
            this.color1 = color1;
            this.color2 = color2;
        }

        public string Id
        {
            get
            {
                return id;
            }
            set => SetProperty(ref id, value, nameof(Id));
        }

        public string Color1
        {
            get
            {
                return color1;
            }
            set => SetProperty(ref color1, value, nameof(Color1));
        }

        public string Color2
        {
            get
            {
                return color2;
            }
            set => SetProperty(ref color2, value, nameof(Color2));
        }

        public string Name
        {
            get
            {
                return name;
            }
            set => SetProperty(ref name, value, nameof(Name));
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
