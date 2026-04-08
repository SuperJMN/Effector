using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Effector;
using Effector.Compiz.Sample.Effects;
using SkiaSharp;

namespace Effector.Compiz.Sample.App.Controls;

public partial class TransitionPresenter : UserControl
{
    private readonly Stopwatch _stopwatch = new();
    private readonly DispatcherTimer _timer;
    private readonly CompizTransitionEffect _transitionEffect;
    private readonly ContentControl _currentHost;
    private readonly ContentControl _nextHost;
    private readonly Border _overlayHost;
    private TaskCompletionSource<bool>? _transitionCompletion;
    private CompizTransitionDescriptor _activeDescriptor = CompizTransitionCatalog.All[0];
    private Control? _pendingContent;
    private SkiaShaderImageHandle _fromHandle;
    private SkiaShaderImageHandle _toHandle;
    private bool _isTransitioning;

    public TransitionPresenter()
    {
        AvaloniaXamlLoader.Load(this);

        _currentHost = RequireControl<ContentControl>("CurrentHost");
        _nextHost = RequireControl<ContentControl>("NextHost");
        _overlayHost = RequireControl<Border>("OverlayHost");
        _transitionEffect = new CompizTransitionEffect();
        _overlayHost.Effect = _transitionEffect;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16d)
        };
        _timer.Tick += OnTransitionTick;

        DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    public void SetInitialPage(Control page)
    {
        ArgumentNullException.ThrowIfNull(page);

        ReleaseHandles();
        _pendingContent = null;
        _isTransitioning = false;
        _timer.Stop();
        _stopwatch.Reset();

        _currentHost.Content = null;
        _currentHost.Content = page;
        _currentHost.IsVisible = true;
        _nextHost.Content = null;
        _nextHost.IsVisible = false;
        _overlayHost.IsVisible = false;
        ResetEffectInputs();
    }

    public async Task TransitionToAsync(Control page, CompizTransitionDescriptor descriptor, Point? click)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_isTransitioning)
        {
            var priorTransitionCompletion = _transitionCompletion;
            _transitionCompletion = null;
            SetInitialPage(page);
            priorTransitionCompletion?.TrySetResult(true);
            return;
        }

        if (_currentHost.Content is null)
        {
            SetInitialPage(page);
            return;
        }

        _activeDescriptor = descriptor;
        _pendingContent = page;
        _nextHost.Content = null;
        _nextHost.Content = page;
        _nextHost.IsVisible = true;

        await Dispatcher.UIThread.InvokeAsync(
            () =>
            {
                UpdateLayout();
                _currentHost.UpdateLayout();
                _nextHost.UpdateLayout();
            },
            DispatcherPriority.Render);

        using var fromBitmap = Capture(_currentHost);
        using var toBitmap = Capture(_nextHost);

        ReleaseHandles();
        _fromHandle = SkiaShaderImageRegistry.Register(fromBitmap);
        _toHandle = SkiaShaderImageRegistry.Register(toBitmap);

        var normalizedClick = NormalizeClick(click);
        _transitionEffect.Kind = descriptor.Kind;
        _transitionEffect.FromImage = _fromHandle;
        _transitionEffect.ToImage = _toHandle;
        _transitionEffect.Progress = 0d;
        _transitionEffect.Time = 0d;
        _transitionEffect.ClickX = normalizedClick.X;
        _transitionEffect.ClickY = normalizedClick.Y;

        _currentHost.IsVisible = false;
        _nextHost.IsVisible = false;
        _overlayHost.IsVisible = true;

        _isTransitioning = true;
        _transitionCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _stopwatch.Restart();
        _timer.Start();

        await _transitionCompletion.Task;
    }

    private void OnTransitionTick(object? sender, EventArgs e)
    {
        var duration = Math.Max(_activeDescriptor.Duration.TotalSeconds, 0.001d);
        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        var raw = Math.Clamp(elapsed / duration, 0d, 1d);

        _transitionEffect.Progress = Ease(raw);
        _transitionEffect.Time = elapsed;

        if (raw >= 1d)
        {
            CompleteTransition();
        }
    }

    private void CompleteTransition()
    {
        _timer.Stop();
        _stopwatch.Reset();

        var nextContent = _pendingContent;
        _pendingContent = null;
        _nextHost.Content = null;

        if (nextContent is not null)
        {
            _currentHost.Content = null;
            _currentHost.Content = nextContent;
        }

        _currentHost.IsVisible = true;
        _nextHost.IsVisible = false;
        _overlayHost.IsVisible = false;

        ResetEffectInputs();
        ReleaseHandles();

        _isTransitioning = false;
        _transitionCompletion?.TrySetResult(true);
        _transitionCompletion = null;
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _timer.Stop();
        _stopwatch.Reset();
        _transitionCompletion?.TrySetResult(true);
        _transitionCompletion = null;
        ReleaseHandles();
    }

    private RenderTargetBitmap Capture(Control host)
    {
        var size = host.Bounds.Size;
        if (size.Width <= 0d || size.Height <= 0d)
        {
            size = Bounds.Size;
        }

        var renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1d;
        var pixelSize = new PixelSize(
            Math.Max(1, (int)Math.Ceiling(size.Width * renderScaling)),
            Math.Max(1, (int)Math.Ceiling(size.Height * renderScaling)));

        var bitmap = new RenderTargetBitmap(
            pixelSize,
            new Vector(96d * renderScaling, 96d * renderScaling));
        bitmap.Render(host);
        return bitmap;
    }

    private Point NormalizeClick(Point? point)
    {
        if (!point.HasValue || Bounds.Width <= 0d || Bounds.Height <= 0d)
        {
            return new Point(0.5d, 0.5d);
        }

        return new Point(
            Math.Clamp(point.Value.X / Bounds.Width, 0d, 1d),
            Math.Clamp(point.Value.Y / Bounds.Height, 0d, 1d));
    }

    private void ResetEffectInputs()
    {
        _transitionEffect.FromImage = default;
        _transitionEffect.ToImage = default;
        _transitionEffect.Progress = 0d;
        _transitionEffect.Time = 0d;
        _transitionEffect.ClickX = 0.5d;
        _transitionEffect.ClickY = 0.5d;
    }

    private void ReleaseHandles()
    {
        SkiaShaderImageRegistry.Release(_fromHandle);
        SkiaShaderImageRegistry.Release(_toHandle);
        _fromHandle = default;
        _toHandle = default;
    }

    private static double Ease(double value) =>
        value < 0.5d
            ? 2d * value * value
            : 1d - (Math.Pow((-2d * value) + 2d, 2d) / 2d);

    private T RequireControl<T>(string name)
        where T : Control =>
        this.FindControl<T>(name)
        ?? throw new InvalidOperationException(
            $"Could not find required control '{name}' in {nameof(TransitionPresenter)}.");
}
