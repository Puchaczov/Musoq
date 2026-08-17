using System.Linq;
using System.Text;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Build;

public class TurnQueryIntoRunnableCode(BuildChain? successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var phase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("emission");
        try
        {
            var artifacts = Finalize(items.RenderingArtifacts, items.EmitPdb, items.FinalizationPurpose);
            items.CompilationArtifacts = artifacts;

            if (!artifacts.FinalizationResult.Success)
                throw new CompilationException(CreateCompilationErrorText(artifacts.FinalizationResult));
        }
        finally
        {
            phase.Dispose();
        }

        Successor?.Build(items);
    }

    private static CompilationBuildArtifacts Finalize(RenderingBuildArtifacts rendering, bool emitPdb, TargetFinalizationPurpose purpose)
    {
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            rendering.Artifact.TargetId,
            purpose == TargetFinalizationPurpose.Execution ? new TargetFinalizationOptionsContext(emitPdb) : new TargetFinalizationOptionsContext(emitPdb, purpose));
        return CompilationBuildArtifacts.From(
            ExecutionTargetCatalog.FinalizeArtifact(rendering.Artifact, options));
    }

    private static string CreateCompilationErrorText(TargetFinalizationResult result)
    {
        var all = new StringBuilder();

        foreach (var diagnostic in result.Diagnostics.Where(static diagnostic =>
                     diagnostic.Severity == TargetDiagnosticSeverity.Error))
        {
            all.AppendLine(diagnostic.Message);
            if (!string.IsNullOrWhiteSpace(diagnostic.SourceSnippet))
                all.AppendLine(diagnostic.SourceSnippet);
        }

        return all.ToString();
    }
}
