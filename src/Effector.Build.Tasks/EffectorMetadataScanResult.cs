using System.Collections.Generic;

namespace Effector.Build.Tasks;

internal sealed class EffectCandidate
{
    public EffectCandidate(int metadataToken, string fullName)
    {
        MetadataToken = metadataToken;
        FullName = fullName;
    }

    public int MetadataToken { get; }

    public string FullName { get; }
}

internal sealed class EffectorMetadataScanResult
{
    public EffectorMetadataScanResult(int inspectedTypeCount, IReadOnlyList<EffectCandidate> candidates)
    {
        InspectedTypeCount = inspectedTypeCount;
        Candidates = candidates;
    }

    public int InspectedTypeCount { get; }

    public IReadOnlyList<EffectCandidate> Candidates { get; }
}
