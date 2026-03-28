using System.Collections.Generic;

namespace Effector.Build.Tasks;

internal sealed class EffectorWeaverResult
{
    public int InspectedTypeCount { get; set; }

    public int CandidateCount { get; set; }

    public int RewrittenEffectCount { get; set; }

    public List<string> Warnings { get; } = new();

    public List<string> Errors { get; } = new();
}
