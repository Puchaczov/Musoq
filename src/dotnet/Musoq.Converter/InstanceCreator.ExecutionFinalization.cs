using System.Linq;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Targets.Abstractions;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static void FinalizeExecutionArtifacts(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var rendering = items.RenderingArtifacts;
        var options = ExecutionTargetCatalog.CreateFinalizationOptions(
            rendering.Artifact.TargetId,
            new TargetFinalizationOptionsContext(items.EmitPdb));
        var finalization = ExecutionTargetCatalog.FinalizeArtifact(rendering.Artifact, options);
        items.CompilationArtifacts = CompilationBuildArtifacts.From(finalization);

        if (finalization.Success)
            return;

        var message = new StringBuilder();
        foreach (var diagnostic in finalization.Diagnostics.Where(static diagnostic =>
                     diagnostic.Severity == TargetDiagnosticSeverity.Error))
        {
            message.AppendLine(diagnostic.Message);
        }

        throw new CompilationException(message.ToString());
    }
}
