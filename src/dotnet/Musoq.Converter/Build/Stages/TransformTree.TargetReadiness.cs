using Musoq.Targets.Execution.Analysis;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static ExecutionTargetReadinessReport? CreateReadinessReport(
        TransformPipelineContext context,
        TargetRenderRequest request)
    {
        return context.CompilationPurpose == CompilationPurpose.PortableArtifactPackaging
            ? ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(
                request.CompatibilityReport,
                request.RuntimeContract,
                request.SemanticsContract)
            : null;
    }
}
