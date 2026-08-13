using raindesk.Services;
using raindesk.Services.Interfaces;
using Windows.UI.ViewManagement;

namespace raindesk.Controls;

internal static class MotionPreferences
{
    private static readonly UISettings? UiSettings = CreateUiSettings();

    public static bool ShouldAnimate
    {
        get
        {
            bool systemAnimationsEnabled = true;
            if (UiSettings != null)
            {
                try
                {
                    systemAnimationsEnabled = UiSettings.AnimationsEnabled;
                }
                catch
                {
                    systemAnimationsEnabled = true;
                }
            }

            bool appAnimationsEnabled = !AppServices.IsConfigured
                || AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig.AppAnimation != false;
            return systemAnimationsEnabled && appAnimationsEnabled;
        }
    }

    private static UISettings? CreateUiSettings()
    {
        try
        {
            return new UISettings();
        }
        catch
        {
            return null;
        }
    }
}
