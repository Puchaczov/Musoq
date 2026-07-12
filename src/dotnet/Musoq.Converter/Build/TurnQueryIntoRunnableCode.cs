using System.Linq;
using System.Text;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Build;

public class TurnQueryIntoRunnableCode(BuildChain? successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var artifacts = Finalize(items.RenderingArtifacts, items.EmitPdb);
        items.CompilationArtifacts = artifacts;

        if (!artifacts.FinalizationResult.Success)
            throw new CompilationException(CreateCompilationErrorText(artifacts.FinalizationResult));

        Successor?.Build(items);
    }

    private static CompilationBuildArtifacts Finalize(RenderingBuildArtifacts rendering, bool emitPdb)
    {
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            rendering.Artifact.TargetId,
            new TargetFinalizationOptionsContext(emitPdb));
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
