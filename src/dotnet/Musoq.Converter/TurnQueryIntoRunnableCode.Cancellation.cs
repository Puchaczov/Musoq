using System.Threading;

namespace Musoq.Converter.Build;

public partial class TurnQueryIntoRunnableCode
{
    private static CompilationBuildArtifacts Finalize(
        RenderingBuildArtifacts rendering,
        bool emitPdb,
        TargetFinalizationPurpose purpose,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = cancellationToken == CancellationToken.None
            ? new TargetFinalizationOptionsContext(emitPdb)
            : new TargetFinalizationOptionsContext(emitPdb, CancellationToken: cancellationToken);
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            rendering.Artifact.TargetId,
            purpose == TargetFinalizationPurpose.Execution
                ? context
                : new TargetFinalizationOptionsContext(emitPdb, purpose, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        return CompilationBuildArtifacts.From(
            ExecutionTargetCatalog.FinalizeArtifact(rendering.Artifact, options));
    }
}
