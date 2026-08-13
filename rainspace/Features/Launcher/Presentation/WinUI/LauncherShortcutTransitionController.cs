using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace rainspace.Features.Launcher.Presentation.WinUI;

public sealed class LauncherShortcutTransitionController
{
    private readonly ListView _listView;
    private readonly Func<bool> _isDragActive;
    private readonly Func<bool> _useManualLiveYield;
    private readonly Func<bool> _shouldAnimateLiveYield;
    private readonly Func<bool> _shouldAnimateLiveReorder;
    private readonly Func<bool> _shouldAnimateLayout;
    private readonly Func<bool> _restoreEnabled;
    private readonly TimeSpan _liveReorderDuration;
    private readonly TimeSpan _layoutDuration;
    private TransitionCollection? _savedTransitions;

    public LauncherShortcutTransitionController(
        ListView listView,
        Func<bool> isDragActive,
        Func<bool> useManualLiveYield,
        Func<bool> shouldAnimateLiveYield,
        Func<bool> shouldAnimateLiveReorder,
        Func<bool> shouldAnimateLayout,
        Func<bool> restoreEnabled,
        TimeSpan liveReorderDuration,
        TimeSpan layoutDuration)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        _isDragActive = isDragActive ?? throw new ArgumentNullException(nameof(isDragActive));
        _useManualLiveYield = useManualLiveYield ?? throw new ArgumentNullException(nameof(useManualLiveYield));
        _shouldAnimateLiveYield = shouldAnimateLiveYield ?? throw new ArgumentNullException(nameof(shouldAnimateLiveYield));
        _shouldAnimateLiveReorder = shouldAnimateLiveReorder ?? throw new ArgumentNullException(nameof(shouldAnimateLiveReorder));
        _shouldAnimateLayout = shouldAnimateLayout ?? throw new ArgumentNullException(nameof(shouldAnimateLayout));
        _restoreEnabled = restoreEnabled ?? throw new ArgumentNullException(nameof(restoreEnabled));
        _liveReorderDuration = liveReorderDuration;
        _layoutDuration = layoutDuration;
    }

    public void Apply(bool layoutAnimate = false)
    {
        if (_isDragActive() && _shouldAnimateLiveYield() && _useManualLiveYield())
        {
            Disable();
            _listView.SetDesiredContainerUpdateDuration(TimeSpan.Zero);
            return;
        }

        if (_isDragActive() && _shouldAnimateLiveReorder())
        {
            SaveCurrent();
            _listView.ItemContainerTransitions = new TransitionCollection
            {
                new RepositionThemeTransition { IsStaggeringEnabled = false }
            };
            _listView.SetDesiredContainerUpdateDuration(_liveReorderDuration);
            return;
        }

        if (layoutAnimate && _shouldAnimateLayout())
        {
            SetEnabled(true);
            _listView.SetDesiredContainerUpdateDuration(_layoutDuration);
            return;
        }

        _listView.SetDesiredContainerUpdateDuration(TimeSpan.Zero);
        Disable();
    }

    public void Restore() => SetEnabled(_restoreEnabled());

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            Disable();
            return;
        }

        if (_savedTransitions != null)
        {
            _listView.ItemContainerTransitions = _savedTransitions;
        }
        else if (_listView.ItemContainerTransitions.Count == 0)
        {
            _listView.ItemContainerTransitions =
            [
                new RepositionThemeTransition(),
                new AddDeleteThemeTransition()
            ];
        }
    }

    private void Disable()
    {
        SaveCurrent();
        _listView.ItemContainerTransitions = new TransitionCollection();
    }

    private void SaveCurrent()
    {
        _savedTransitions ??= _listView.ItemContainerTransitions;
    }
}
