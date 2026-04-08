using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Effector.Compiz.Sample.App.Controls;
using Effector.Compiz.Sample.App.Pages;
using Effector.Compiz.Sample.Effects;

namespace Effector.Compiz.Sample.App;

public partial class MainWindow : Window
{
    private readonly TransitionOption[] _transitionOptions;
    private readonly Random _random = new();
    private readonly ComboBox _effectPicker;
    private readonly TransitionPresenter _transitionHost;
    private readonly TextBlock _effectSummaryText;
    private readonly Button _homeNavButton;
    private readonly Button _aboutNavButton;
    private readonly Button _workNavButton;
    private readonly Button _contactNavButton;
    private string _currentRoute = "/";
    private Avalonia.Point? _pendingClick;
    private bool _stressStarted;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);

        _effectPicker = RequireControl<ComboBox>("EffectPicker");
        _transitionHost = RequireControl<TransitionPresenter>("TransitionHost");
        _effectSummaryText = RequireControl<TextBlock>("EffectSummaryText");
        _homeNavButton = RequireControl<Button>("HomeNavButton");
        _aboutNavButton = RequireControl<Button>("AboutNavButton");
        _workNavButton = RequireControl<Button>("WorkNavButton");
        _contactNavButton = RequireControl<Button>("ContactNavButton");
        _transitionOptions = CreateTransitionOptions();
        _effectPicker.ItemsSource = _transitionOptions;
        _effectPicker.SelectedItem =
            _transitionOptions.FirstOrDefault(static option => option.Id == TransitionPreferenceStore.Load())
            ?? _transitionOptions[0];
        _effectPicker.SelectionChanged += OnEffectSelectionChanged;

        AddHandler(Button.ClickEvent, OnButtonClicked, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        Opened += OnOpened;

        _transitionHost.SetInitialPage(CreatePage(_currentRoute));
        UpdateNavigationState();
        UpdateEffectSummary(GetSelectedOption(), appliedDescriptor: GetSelectedDescriptor(GetSelectedOption()));
    }

    private async void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button button || button.Tag is not string tag)
        {
            return;
        }

        if (string.Equals(tag, "sample-submit", StringComparison.Ordinal))
        {
            _effectSummaryText.Text = "Sample form submitted locally. The page stays interactive between shader transitions.";
            return;
        }

        if (!tag.StartsWith("/", StringComparison.Ordinal) || string.Equals(tag, _currentRoute, StringComparison.Ordinal))
        {
            return;
        }

        var option = GetSelectedOption();
        var descriptor = GetSelectedDescriptor(option);
        var pendingClick = _pendingClick;
        _pendingClick = null;

        await NavigateToRouteAsync(tag, option, descriptor, pendingClick);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button button && button.Tag is string tag && tag.StartsWith("/", StringComparison.Ordinal))
        {
            _pendingClick = e.GetPosition(_transitionHost);
        }
    }

    private void OnEffectSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var option = GetSelectedOption();
        TransitionPreferenceStore.Save(option.Id);
        UpdateEffectSummary(option, appliedDescriptor: GetSelectedDescriptor(option));
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_stressStarted || !IsStressModeEnabled())
        {
            return;
        }

        _stressStarted = true;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    UpdateLayout();
                    _transitionHost.UpdateLayout();
                },
                DispatcherPriority.Render);

            await RunStressScenarioAsync();

            Console.WriteLine("Compiz stress scenario completed.");
            await Dispatcher.UIThread.InvokeAsync(Close, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Compiz stress scenario failed: " + ex);
            throw;
        }
    }

    private TransitionOption GetSelectedOption() =>
        _effectPicker.SelectedItem as TransitionOption ?? _transitionOptions[0];

    private CompizTransitionDescriptor GetSelectedDescriptor(TransitionOption option)
    {
        if (option.Kind.HasValue)
        {
            return CompizTransitionCatalog.Get(option.Kind.Value);
        }

        return CompizTransitionCatalog.All[_random.Next(CompizTransitionCatalog.All.Length)];
    }

    private Control CreatePage(string route) =>
        route switch
        {
            "/about" => new AboutPage(),
            "/work" => new WorkPage(),
            "/contact" => new ContactPage(),
            _ => new HomePage()
        };

    private void UpdateNavigationState()
    {
        SetNavigationState(_homeNavButton, string.Equals(_currentRoute, "/", StringComparison.Ordinal));
        SetNavigationState(_aboutNavButton, string.Equals(_currentRoute, "/about", StringComparison.Ordinal));
        SetNavigationState(_workNavButton, string.Equals(_currentRoute, "/work", StringComparison.Ordinal));
        SetNavigationState(_contactNavButton, string.Equals(_currentRoute, "/contact", StringComparison.Ordinal));
    }

    private void UpdateEffectSummary(TransitionOption option, CompizTransitionDescriptor appliedDescriptor)
    {
        _effectSummaryText.Text = option.Kind.HasValue
            ? $"{appliedDescriptor.DisplayName}: {appliedDescriptor.Description}"
            : $"Random mode is enabled. The next navigation will pick from {CompizTransitionCatalog.All.Length} transition shaders. Last preview: {appliedDescriptor.DisplayName}.";
    }

    private static void SetNavigationState(Button button, bool isActive)
    {
        button.Background = isActive
            ? new SolidColorBrush(Color.Parse("#284C77FF"))
            : Brushes.Transparent;
        button.Foreground = isActive
            ? Brushes.White
            : new SolidColorBrush(Color.Parse("#D9E7F9"));
    }

    private static TransitionOption[] CreateTransitionOptions()
    {
        var options = CompizTransitionCatalog.All
            .Select(static descriptor => new TransitionOption(descriptor.Id, descriptor.DisplayName, descriptor.Kind))
            .ToList();
        options.Add(new TransitionOption("random", "Random", kind: null));
        return options.ToArray();
    }

    private async Task RunStressScenarioAsync()
    {
        var selectedEffectId = Environment.GetEnvironmentVariable("EFFECTOR_COMPIZ_AUTOSTRESS_EFFECT");
        if (string.IsNullOrWhiteSpace(selectedEffectId))
        {
            selectedEffectId = "cube";
        }

        var selectedOption = _transitionOptions.FirstOrDefault(option =>
            string.Equals(option.Id, selectedEffectId, StringComparison.OrdinalIgnoreCase));
        if (selectedOption is not null)
        {
            _effectPicker.SelectedItem = selectedOption;
        }

        var iterations = ParseInt32EnvironmentVariable("EFFECTOR_COMPIZ_AUTOSTRESS_ITERATIONS", fallback: 20);
        var forceGc = ParseBooleanEnvironmentVariable("EFFECTOR_COMPIZ_AUTOSTRESS_FORCE_GC", fallback: true);
        var routes = new[] { "/about", "/work", "/contact", "/" };

        for (var index = 0; index < iterations; index++)
        {
            var route = routes[index % routes.Length];
            var click = CreateStressClickPoint(index);
            var option = GetSelectedOption();
            var descriptor = GetSelectedDescriptor(option);

            Console.WriteLine(
                $"Compiz stress transition {index + 1}/{iterations}: {_currentRoute} -> {route} using {descriptor.DisplayName}.");

            await NavigateToRouteAsync(route, option, descriptor, click);

            if (forceGc)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        await Task.Delay(250);

        if (forceGc)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private async Task NavigateToRouteAsync(
        string route,
        TransitionOption option,
        CompizTransitionDescriptor descriptor,
        Avalonia.Point? click)
    {
        if (!route.StartsWith("/", StringComparison.Ordinal) || string.Equals(route, _currentRoute, StringComparison.Ordinal))
        {
            return;
        }

        await _transitionHost.TransitionToAsync(CreatePage(route), descriptor, click);
        _currentRoute = route;
        UpdateNavigationState();
        UpdateEffectSummary(option, descriptor);
    }

    private Avalonia.Point CreateStressClickPoint(int index)
    {
        var width = Math.Max(_transitionHost.Bounds.Width, 1d);
        var height = Math.Max(_transitionHost.Bounds.Height, 1d);
        var normalizedX = index % 2 == 0 ? 0.82d : 0.18d;
        return new Avalonia.Point(width * normalizedX, height * 0.18d);
    }

    private static bool IsStressModeEnabled() =>
        ParseBooleanEnvironmentVariable("EFFECTOR_COMPIZ_AUTOSTRESS", fallback: false);

    private static bool ParseBooleanEnvironmentVariable(string name, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => fallback
        };
    }

    private static int ParseInt32EnvironmentVariable(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }

    private sealed class TransitionOption
    {
        public TransitionOption(string id, string displayName, CompizTransitionKind? kind)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
        }

        public string Id { get; }

        public string DisplayName { get; }

        public CompizTransitionKind? Kind { get; }

        public override string ToString() => DisplayName;
    }

    private T RequireControl<T>(string name)
        where T : Control =>
        this.FindControl<T>(name)
        ?? throw new InvalidOperationException(
            $"Could not find required control '{name}' in {nameof(MainWindow)}.");
}
