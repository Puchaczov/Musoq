using Musoq.Targets.Execution;

namespace Musoq.Converter.Build;

internal static class TargetRenderPurposeFactory
{
    internal static TargetRenderPurpose CreatePurpose(CompilationPurpose purpose)
    {
        return purpose switch
        {
            CompilationPurpose.Execution => TargetRenderPurpose.Execution,
            CompilationPurpose.Inspection => TargetRenderPurpose.Inspection,
            CompilationPurpose.PortableArtifactPackaging => TargetRenderPurpose.PortablePackaging,
            CompilationPurpose.ArtifactValidation => TargetRenderPurpose.StrictValidation,
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown compilation purpose.")
        };
    }

    internal static TargetRenderProfile CreateProfile(CompilationPurpose purpose, bool emitPdb)
    {
        return purpose == CompilationPurpose.Execution && !emitPdb
            ? TargetRenderProfile.ExecutionFast
            : TargetRenderProfile.StableArtifact;
    }
}
