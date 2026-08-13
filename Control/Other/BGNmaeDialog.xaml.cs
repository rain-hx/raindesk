using raindesk.Constant;
using raindesk.Util;
using raindesk.ViewModel;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.Appearance.Contracts;
using raindesk.Services;

namespace raindesk.Control.Other
{
    /// <summary>
    /// WinUI 3 dialog for naming a custom gradient background.
    /// </summary>
    public partial class BGNmaeDialog : UserControl
    {
        private readonly IAppearanceQueryHandler _appearanceQueries =
            AppServices.Get<IAppearanceQueryHandler>();
        private readonly IAppearanceCommandHandler _appearanceCommands =
            AppServices.Get<IAppearanceCommandHandler>();
        public Microsoft.UI.Xaml.Window? dialog;

        public BGNmaeDialog()
        {
            this.InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            AppearanceGradientSnapshot gradient = _appearanceQueries.GetGradient();
            AppearanceCommandResult result = _appearanceCommands.AddCustomGradient(
                new AddCustomAppearanceGradientCommand(
                    BGName.Text,
                    gradient.StartColor,
                    gradient.EndColor));
            if (result.Success)
            {
                dialog?.Close();
            }
        }
    }
}
