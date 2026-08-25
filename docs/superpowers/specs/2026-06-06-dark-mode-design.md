# Dark Mode Design

## Goal

rainspace supports a controlled three-state app theme: follow system, light, and dark. Theme switching should apply immediately across the main window, settings surfaces, secondary windows, and dialogs without requiring restart.

## Scope

- Add a persisted app setting for theme mode, defaulting to system.
- Add a visible theme selector in display settings: follow system, light, dark.
- Add a strong-control `ThemeService` that owns all runtime theme changes.
- Make theme application thread-safe: background calls may request a theme change, but XAML updates must run through each window's dispatcher.
- Add dark resources for the app's existing semantic brushes while preserving the current light palette.

## Architecture

`ThemeService` is the only runtime theme authority. Windows register their root `FrameworkElement` and optional title bar update callback with the service. The service stores registrations behind a lock, resolves the configured mode to an `ElementTheme`, and applies changes through `DispatcherQueue.TryEnqueue` when necessary.

`CommonStyle.xaml` provides `Light`, `Dark`, and `Default` theme dictionaries for existing brush keys. The XAML remains mostly unchanged because most surfaces already use `{ThemeResource ...}`.

## Visual Direction

Dark mode keeps the existing "raindrop whisper" character: blue-black background, slightly lifted blue-gray surfaces, soft near-white primary text, blue-gray secondary text, and the existing clear lake blue accent tuned for contrast. The goal is comfortable office-night usage, not pure black or high-drama styling.

## Acceptance

- New installs default to follow system.
- Existing configs without the setting deserialize safely to follow system.
- Switching the setting updates registered windows immediately.
- Windows opened after switching inherit the current theme.
- Theme service calls are safe from non-UI threads.
- Existing unit tests pass and new theme behavior tests pass.
