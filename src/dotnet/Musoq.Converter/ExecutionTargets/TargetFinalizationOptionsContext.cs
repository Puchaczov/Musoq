namespace Musoq.Converter.Build;

internal sealed record TargetFinalizationOptionsContext(
    bool EmitPdb,
    TargetFinalizationPurpose Purpose = TargetFinalizationPurpose.Execution);
