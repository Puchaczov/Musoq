namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpClrFinalizationOptions(
    bool EmitPdb,
    TargetFinalizationPurpose Purpose = TargetFinalizationPurpose.Execution) : TargetFinalizationOptions;
