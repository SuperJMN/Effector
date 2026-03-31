using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace EffectorAndroidRepro;

public partial class MainView : UserControl
{
    private readonly ScaleTransform _scale = new() { ScaleX = 1, ScaleY = 1 };
    private OverlayShaderEffect? _effect;
    private bool _effectActive;

    public MainView()
    {
        InitializeComponent();
        SpriteHost.RenderTransform = _scale;

        ProgressSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == "Value" && _effect is not null)
                _effect.Progress = ProgressSlider.Value;
        };

        ScaleSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == "Value")
            {
                _scale.ScaleX = ScaleSlider.Value;
                _scale.ScaleY = ScaleSlider.Value;
                UpdateStatus();
            }
        };
    }

    private void OnToggleClick(object? sender, RoutedEventArgs e)
    {
        _effectActive = !_effectActive;

        if (_effectActive)
        {
            _effect = new OverlayShaderEffect { Progress = ProgressSlider.Value };
            SpriteHost.Effect = _effect;
        }
        else
        {
            SpriteHost.Effect = null;
            _effect = null;
        }

        UpdateStatus();
    }

    private void OnAnimateClick(object? sender, RoutedEventArgs e)
    {
        AnimateButton.IsEnabled = false;

        const double durationMs = 800;
        const double targetScale = 1.3;
        var start = DateTime.UtcNow;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
            var t = Math.Clamp(elapsed / durationMs, 0, 1);

            // Triangle wave: 0→1→0 over the duration (peak at 0.5)
            var phase = t < 0.5 ? t * 2 : (1 - t) * 2;
            // Smooth ease
            phase = phase * phase * (3 - 2 * phase);

            var s = 1.0 + (targetScale - 1.0) * phase;
            _scale.ScaleX = s;
            _scale.ScaleY = s;
            ScaleSlider.Value = s;
            UpdateStatus();

            if (t >= 1.0)
            {
                timer.Stop();
                _scale.ScaleX = 1;
                _scale.ScaleY = 1;
                ScaleSlider.Value = 1.0;
                AnimateButton.IsEnabled = true;
                UpdateStatus();
            }
        };
        timer.Start();
    }

    private void UpdateStatus()
    {
        StatusText.Text = $"Effect {(_effectActive ? "ON" : "OFF")}, Scale {_scale.ScaleX:F2}";
    }
}
