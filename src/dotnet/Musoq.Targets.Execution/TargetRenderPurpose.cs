namespace Musoq.Targets.Execution;

internal enum TargetRenderPurpose
{
    Execution,
    Inspection,
    PortablePackaging,
    StrictValidation
}

internal enum TargetRenderProfile
{
    ExecutionFast,
    StableArtifact
}

internal static class TargetRenderProfileContract
{
    internal const int Version = 1;
}
