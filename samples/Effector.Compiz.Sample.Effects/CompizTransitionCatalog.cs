using System;
using Avalonia.Media;

namespace Effector.Compiz.Sample.Effects;

public enum CompizTransitionKind
{
    Dissolve,
    Burn,
    Cube,
    Wobbly,
    Genie,
    Magnetic
}

public sealed record CompizTransitionDescriptor(
    CompizTransitionKind Kind,
    string Id,
    string DisplayName,
    TimeSpan Duration,
    Color Accent,
    string Description);

public static class CompizTransitionCatalog
{
    public static readonly CompizTransitionDescriptor[] All =
    {
        new(
            CompizTransitionKind.Dissolve,
            "dissolve",
            "Dissolve",
            TimeSpan.FromMilliseconds(1000d),
            Color.Parse("#8B5CF6"),
            "Organic noise sweep with a warm edge that reveals the next page from the click point outward."),
        new(
            CompizTransitionKind.Burn,
            "burn",
            "Burn",
            TimeSpan.FromMilliseconds(1400d),
            Color.Parse("#F97316"),
            "Heat, ember, and smoke layers consume the old page before the new one breaks through."),
        new(
            CompizTransitionKind.Cube,
            "cube",
            "Cube",
            TimeSpan.FromMilliseconds(1200d),
            Color.Parse("#F59E0B"),
            "A full-frame cube turn ray-casts the outgoing and incoming faces based on which side of the window you click."),
        new(
            CompizTransitionKind.Wobbly,
            "wobbly",
            "Wobbly",
            TimeSpan.FromMilliseconds(1200d),
            Color.Parse("#6366F1"),
            "A click-driven wave front distorts both pages while the new one chases the ripple."),
        new(
            CompizTransitionKind.Genie,
            "genie",
            "Genie",
            TimeSpan.FromMilliseconds(800d),
            Color.Parse("#10B981"),
            "The current page is squeezed toward a focus point while the next page settles in behind it."),
        new(
            CompizTransitionKind.Magnetic,
            "magnetic",
            "Magnetic",
            TimeSpan.FromMilliseconds(1200d),
            Color.Parse("#EC4899"),
            "Pixels pull toward the click point, then reassemble into the destination page with an electric glow.")
    };

    public static CompizTransitionDescriptor Get(CompizTransitionKind kind)
    {
        foreach (var descriptor in All)
        {
            if (descriptor.Kind == kind)
            {
                return descriptor;
            }
        }

        return All[0];
    }

    public static CompizTransitionDescriptor Get(string id)
    {
        foreach (var descriptor in All)
        {
            if (string.Equals(descriptor.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return descriptor;
            }
        }

        return All[0];
    }
}
