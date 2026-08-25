using Microsoft.UI.Xaml.Media.Animation;

namespace raindesk.Controls;

internal static class MotionTokens
{
    // ── 时长 ──────────────────────────────────────────────────────────
    // Feedback: 按压、标签内容交叉淡化和即时状态反馈。
    // Fast:     拖拽放下、菜单和轻量状态切换。
    // Standard: 普通面板和层级切换。
    // Exit:     退出约为进入时长的 70%-75%。
    public static readonly TimeSpan Feedback = TimeSpan.FromMilliseconds(120);
    public static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(180);
    public static readonly TimeSpan Standard = TimeSpan.FromMilliseconds(220);
    public static readonly TimeSpan Exit = TimeSpan.FromMilliseconds(160);

    // ── 位移 ──────────────────────────────────────────────────────────
    // 普通内容 6px；二级内容 4px；轻微反馈 3px。
    public const double DisplacementPrimary = 6d;
    public const double DisplacementSecondary = 4d;
    public const double DisplacementFeedback = 3d;

    // ── 缩放 ──────────────────────────────────────────────────────────
    // 普通弹窗进入 0.975、退出 0.985；文件夹浮层进入 0.97；拖拽拿起 1.02。
    public const double DialogEnterScale = 0.975d;
    public const double DialogExitScale = 0.985d;
    public const double FolderOverlayEnterScale = 0.97d;
    public const double DragPickupScale = 1.02d;

    /// <summary>所有进入统一使用 Exponential EaseOut，不使用 bounce 或 elastic。</summary>
    public static EasingFunctionBase EaseOut() =>
        new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 5d };

    /// <summary>所有退出统一使用 EaseIn，不使用 bounce 或 elastic。</summary>
    public static EasingFunctionBase EaseIn() =>
        new CubicEase { EasingMode = EasingMode.EaseIn };
}
