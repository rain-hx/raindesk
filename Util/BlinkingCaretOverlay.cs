using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace raindesk.Util;

/// <summary>
/// Hides the native TextBox caret (opacity 0) and renders a blinking overlay caret with a mirror TextBlock.
/// </summary>
public sealed class BlinkingCaretOverlay
{
    private const double DefaultBlinkMs = 530;
    private static readonly ConditionalWeakTable<TextBox, BlinkingCaretOverlay> Instances = new();

    private readonly TextBox _textBox;
    private readonly TextBlock _mirror;
    private readonly Border _blinker;
    private readonly DispatcherQueue _dispatcher;
    private readonly double _fontSize;
    private readonly FontWeight _fontWeight;
    private readonly bool _centerAlign;
    private DispatcherQueueTimer? _timer;
    private bool _blinkOn = true;
    private bool _hidTextBoxOpacity;
    private bool _managedMirrorVisibility;

    private BlinkingCaretOverlay(
        TextBox textBox,
        TextBlock mirror,
        Border blinker,
        DispatcherQueue dispatcher,
        double fontSize,
        FontWeight fontWeight,
        bool centerAlign)
    {
        _textBox = textBox;
        _mirror = mirror;
        _blinker = blinker;
        _dispatcher = dispatcher;
        _fontSize = fontSize;
        _fontWeight = fontWeight;
        _centerAlign = centerAlign;

        _textBox.GotFocus += OnGotFocus;
        _textBox.LostFocus += OnLostFocus;
        _textBox.TextChanged += OnTextChanged;
        _textBox.SelectionChanged += OnSelectionChanged;
        _textBox.SizeChanged += OnSizeChanged;
        _textBox.Unloaded += OnUnloaded;
    }

    public static void Attach(
        TextBox textBox,
        TextBlock mirror,
        Border blinker,
        DispatcherQueue dispatcher,
        double fontSize = 14,
        bool centerAlign = true,
        FontWeight? fontWeight = null)
    {
        if (Instances.TryGetValue(textBox, out _))
        {
            return;
        }

        var overlay = new BlinkingCaretOverlay(
            textBox,
            mirror,
            blinker,
            dispatcher,
            fontSize,
            fontWeight ?? Microsoft.UI.Text.FontWeights.SemiBold,
            centerAlign);
        Instances.Add(textBox, overlay);

        if (textBox.FocusState != FocusState.Unfocused)
        {
            overlay.Start();
        }
    }

    public static void Detach(TextBox textBox)
    {
        if (!Instances.TryGetValue(textBox, out BlinkingCaretOverlay? overlay))
        {
            return;
        }

        overlay.Stop();
        overlay.Unhook();
        Instances.Remove(textBox);
    }

    public static void PrepareEditAppearance(TextBox textBox)
    {
        if (Instances.TryGetValue(textBox, out BlinkingCaretOverlay? overlay))
        {
            overlay.Start();
        }
    }

    public static bool TryFindHostElements(TextBox textBox, out TextBlock mirror, out Border blinker)
    {
        mirror = null!;
        blinker = null!;
        if (textBox.Parent is not Panel host)
        {
            return false;
        }

        mirror = host.Children.OfType<TextBlock>().FirstOrDefault();
        blinker = host.Children.OfType<Border>().FirstOrDefault();
        return mirror != null && blinker != null;
    }

    private void OnGotFocus(object sender, RoutedEventArgs e) => Start();

    private void OnLostFocus(object sender, RoutedEventArgs e) => Stop();

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_textBox.FocusState != FocusState.Unfocused)
        {
            ApplyMirror();
            UpdatePosition();
        }
    }

    private void OnSelectionChanged(object sender, RoutedEventArgs e) => UpdatePosition();

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdatePosition();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        Stop();
        Unhook();
        Instances.Remove(_textBox);
    }

    private void Unhook()
    {
        _textBox.GotFocus -= OnGotFocus;
        _textBox.LostFocus -= OnLostFocus;
        _textBox.TextChanged -= OnTextChanged;
        _textBox.SelectionChanged -= OnSelectionChanged;
        _textBox.SizeChanged -= OnSizeChanged;
        _textBox.Unloaded -= OnUnloaded;
    }

    private void Start()
    {
        ApplyMirror();
        _blinker.Visibility = Visibility.Visible;
        UpdatePosition();

        if (_timer != null)
        {
            return;
        }

        _blinkOn = true;
        _blinker.Opacity = 1;
        _timer = _dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(DefaultBlinkMs);
        _timer.Tick += (_, _) =>
        {
            _blinkOn = !_blinkOn;
            _blinker.Opacity = _blinkOn ? 1 : 0;
        };
        _timer.Start();
    }

    private void Stop()
    {
        _timer?.Stop();
        _timer = null;
        _blinker.Visibility = Visibility.Collapsed;
        _blinker.Opacity = 1;

        if (_managedMirrorVisibility)
        {
            _mirror.Visibility = Visibility.Collapsed;
            _mirror.Text = string.Empty;
            _managedMirrorVisibility = false;
        }

        if (_hidTextBoxOpacity)
        {
            _textBox.Opacity = 1;
            _hidTextBoxOpacity = false;
        }
    }

    private void ApplyMirror()
    {
        if (_textBox.Opacity > 0)
        {
            _textBox.Opacity = 0;
            _hidTextBoxOpacity = true;
        }

        _mirror.Text = _textBox.Text ?? string.Empty;
        if (_mirror.Visibility != Visibility.Visible)
        {
            _mirror.Visibility = Visibility.Visible;
            _managedMirrorVisibility = true;
        }
    }

    private void UpdatePosition()
    {
        if (_blinker.Visibility != Visibility.Visible)
        {
            return;
        }

        double hostWidth = _textBox.ActualWidth;
        if (hostWidth <= 0.01d)
        {
            return;
        }

        string text = _textBox.Text ?? string.Empty;
        int selectionStart = Math.Clamp(_textBox.SelectionStart, 0, text.Length);
        string textBeforeCaret = text[..selectionStart];
        double textBeforeWidth = MeasureTextWidth(textBeforeCaret);
        double fullTextWidth = MeasureTextWidth(text);
        double caretX = _centerAlign
            ? (hostWidth - fullTextWidth) / 2d + textBeforeWidth
            : textBeforeWidth + GetHorizontalPadding();

        double marginLeft = Math.Max(0d, caretX);
        _blinker.Margin = new Thickness(marginLeft, 0, 0, 0);
    }

    private double GetHorizontalPadding()
    {
        return _textBox.Padding.Left;
    }

    private double MeasureTextWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0d;
        }

        var measureBlock = new TextBlock
        {
            Text = text,
            FontSize = _fontSize,
            FontWeight = _fontWeight,
        };
        measureBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return measureBlock.DesiredSize.Width;
    }
}
