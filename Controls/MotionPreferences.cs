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

            return systemAnimationsEnabled;
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
