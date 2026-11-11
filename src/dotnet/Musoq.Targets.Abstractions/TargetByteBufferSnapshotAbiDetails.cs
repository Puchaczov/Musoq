using System.Collections.Generic;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetByteBufferSnapshotAbiDetails : TargetHostAbiImportDetails
{
    public override TargetHostAbiImportKind Kind => TargetHostAbiImportKind.ByteBufferSnapshot;
    internal override string CanonicalDefinition =>
        CreateCanonicalDefinition(Kind.ToString(), builder => AppendScalar(builder, "ownership", "independent-array"));
    public override IReadOnlyDictionary<string, string> Attributes { get; } =
        BuildAttributes(("ownership", "independent-array"));
}
