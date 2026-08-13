using raindesk.Controls;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.ViewModel.Temp;
using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Features.Appearance.Contracts;
using raindesk.Services;

namespace raindesk.Control.Other
{
    /// <summary>
    /// WinUI 3 Gradient Background dialog.
    /// Displays preset and custom gradient backgrounds for selection.
    /// </summary>
    public partial class GradientBGDialog : UserControl
    {
        private readonly IAppearanceQueryHandler _appearanceQueries =
            AppServices.Get<IAppearanceQueryHandler>();
        private readonly IAppearanceCommandHandler _appearanceCommands =
            AppServices.Get<IAppearanceCommandHandler>();
        public Microsoft.UI.Xaml.Window? dialog;

        public GradientBGDialog()
        {
            ObservableCollection<GradientBGParam> bgArr = DeepCopyUtil.DeepCopy(ViewModel.Temp.GradientBGParamList.GradientBGParams);
            foreach (AppearanceGradientSnapshot bg in _appearanceQueries.GetCustomGradients())
            {
                bgArr.Add(new GradientBGParam(bg.Id ?? string.Empty, bg.Name, bg.StartColor, bg.EndColor));
            }

            this.DataContext = DeepCopyUtil.DeepCopy(bgArr);
            this.InitializeComponent();

            if (GradientBGs != null)
            {
                GradientBGs.ItemsSource = this.DataContext as ObservableCollection<GradientBGParam>;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            dialog?.Close();
        }

        private void BGBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is GradientBGParam bgParam)
            {
                AppearanceCommandResult result = _appearanceCommands.SelectGradient(
                    new SelectAppearanceGradientCommand(
                        bgParam.Id,
                        bgParam.Name,
                        bgParam.Color1,
                        bgParam.Color2));
                if (result.Success)
                {
                    BGSettingUtil.BGSetting();
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.DataContext is GradientBGParam bg)
            {
                var bgArr = this.DataContext as ObservableCollection<GradientBGParam>;
                AppearanceCommandResult result = _appearanceCommands.DeleteCustomGradient(
                    new DeleteCustomAppearanceGradientCommand(bg.Id, bg.Color1, bg.Color2));
                if (result.Success && result.Changed)
                {
                    bgArr?.Remove(bg);
                }
            }
        }
    }
}
