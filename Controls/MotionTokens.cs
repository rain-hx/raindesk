using Microsoft.UI.Xaml.Media.Animation;

namespace raindesk.Controls;

internal static class MotionTokens
{
    public static readonly TimeSpan Feedback = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(180);
    public static readonly TimeSpan Standard = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan Exit = TimeSpan.FromMilliseconds(160);

    public static EasingFunctionBase EaseOut() =>
        new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5d };

    public static EasingFunctionBase EaseIn() =>
        new CubicEase { EasingMode = EasingMode.EaseIn };
}
