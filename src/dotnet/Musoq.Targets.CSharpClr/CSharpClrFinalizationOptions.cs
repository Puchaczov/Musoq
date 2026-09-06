using System.Threading;

namespace Musoq.Targets.CSharpClr;

internal sealed record CSharpClrFinalizationOptions(
    bool EmitPdb,
    TargetFinalizationPurpose Purpose = TargetFinalizationPurpose.Execution,
    CancellationToken cancellationToken = default) : TargetFinalizationOptions
{
    public override CancellationToken CancellationToken => cancellationToken;
}
