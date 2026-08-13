using System.Collections.ObjectModel;

namespace raindesk.ViewModel
{
    public class GradientBGParamList : ObservableCollection<GradientBGParam>
    {
        public static GradientBGParamList GradientBGParams { get; set; } = new GradientBGParamList();
    }
}
