using System.Collections.Generic;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetInterpretationStatusAbiDetails : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.InterpretationStatus;
    internal override string CanonicalDefinition =>
        CreateCanonicalDefinition(
            Kind.ToString(),
            builder =>
            {
                AppendScalar(builder, "status", "typed-v1");
                AppendScalar(builder, "completion", "declaration-slots-v1");
            }
        );
    public override IReadOnlyDictionary<string, string> Attributes { get; } =
        BuildAttributes(("status", "typed-v1"), ("completion", "declaration-slots-v1"));
}
