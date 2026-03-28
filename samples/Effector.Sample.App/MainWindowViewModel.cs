using Avalonia.Media;
using Effector.Sample.Effects;

namespace Effector.Sample.App;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel()
    {
        BoundTintEffect = new TintEffect
        {
            Color = Color.Parse("#0F9D8E"),
            Strength = 0.7d
        };
    }

    public TintEffect BoundTintEffect { get; }
}
