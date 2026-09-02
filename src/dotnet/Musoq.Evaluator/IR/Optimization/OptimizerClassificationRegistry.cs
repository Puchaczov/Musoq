using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;

namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// Central inventory for transformations that can change scalar evaluation
/// count, order, timing, or materialization boundaries.
/// </summary>
internal static class OptimizerClassificationRegistry
{
    private static readonly IReadOnlyDictionary<string, OptimizationEvaluationClassification> Classifications =
        new Dictionary<string, OptimizationEvaluationClassification>(StringComparer.Ordinal)
        {
            ["DistinctToGroupByNormalization"] = OptimizationEvaluationClassification.EvaluationPreserving,
            ["SubqueryToCteNormalization"] = OptimizationEvaluationClassification.RegionChecked,
            ["LogicalConstantFolding"] = OptimizationEvaluationClassification.StabilityChecked,
            ["LogicalSourceAliasAnalysis"] = OptimizationEvaluationClassification.NotApplicable,
            ["DeadCteElimination"] = OptimizationEvaluationClassification.RegionChecked,
            ["SourcePredicateMetadata"] = OptimizationEvaluationClassification.StabilityChecked,
            ["SourceProjectionMetadata"] = OptimizationEvaluationClassification.StabilityChecked,
            ["ProjectionPruning"] = OptimizationEvaluationClassification.StabilityChecked,
            ["AggregateStrategySelection"] = OptimizationEvaluationClassification.RegionChecked,
            ["PredicateMovement"] = OptimizationEvaluationClassification.StabilityChecked,
            ["JoinStrategySelection"] = OptimizationEvaluationClassification.StabilityChecked,
            ["OrderingStrategySelection"] = OptimizationEvaluationClassification.StabilityChecked,
            ["WindowMaterialization"] = OptimizationEvaluationClassification.RegionChecked,
            ["SourcePredicatePhysicalRewrite"] = OptimizationEvaluationClassification.StabilityChecked,
            ["SourcePlanPhysicalRewrite"] = OptimizationEvaluationClassification.StabilityChecked,
            ["RecursiveCteInvariantPlanning"] = OptimizationEvaluationClassification.StabilityChecked,
            ["SingleUsePipelineFusion"] = OptimizationEvaluationClassification.StabilityChecked,
            ["CteReadOnceFusion"] = OptimizationEvaluationClassification.StabilityChecked,
            ["CteSidecarIndexLowering"] = OptimizationEvaluationClassification.StabilityChecked,
            ["MethodTargetReuse"] = OptimizationEvaluationClassification.StabilityChecked,
            ["LoopInvariantCodeMotion"] = OptimizationEvaluationClassification.RegionChecked,
            ["FieldExpressionHoisting"] = OptimizationEvaluationClassification.StabilityChecked,
            ["ExpressionCseHoisting"] = OptimizationEvaluationClassification.StabilityChecked,
            ["CapacityHints"] = OptimizationEvaluationClassification.NotApplicable,
            ["DeterministicMemberOrdering"] = OptimizationEvaluationClassification.EvaluationPreserving,
            ["LocalDeclarationNormalization"] = OptimizationEvaluationClassification.EvaluationPreserving,
            ["DeadTemporaryCleanup"] = OptimizationEvaluationClassification.StabilityChecked,
            ["ControlFlowNormalization"] = OptimizationEvaluationClassification.RegionChecked,
            ["HelperExtractionReadability"] = OptimizationEvaluationClassification.RegionChecked,
            ["ReadabilityDecisionTrace"] = OptimizationEvaluationClassification.NotApplicable,
            ["ExecutionCodegenOptimization"] = OptimizationEvaluationClassification.RegionChecked
        };

    public static OptimizationEvaluationClassification Resolve<TPlan>(IPlanOptimizationPass<TPlan> pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        return Classifications.TryGetValue(pass.Name, out var classification)
            ? classification
            : OptimizationEvaluationClassification.Unknown;
    }

    public static void Require<TPlan>(IPlanOptimizationPass<TPlan> pass)
    {
        if (Resolve(pass) == OptimizationEvaluationClassification.Unknown)
        {
            throw new InvalidOperationException(
                $"Optimizer pass '{pass.Name}' is missing an evaluation classification.");
        }
    }
}
