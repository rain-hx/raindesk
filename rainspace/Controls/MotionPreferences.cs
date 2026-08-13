using rainspace.Services;
using rainspace.Services.Interfaces;
using Windows.UI.ViewManagement;

namespace rainspace.Controls;

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
