using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenQueryIsValid_ShouldExposeStagedPlanSnapshotTexts()
    {
        var result = CreateInspection();

        Assert.AreEqual(result.LogicalPlanText, result.InitialLogicalPlanText);
        Assert.AreEqual(result.LogicalPlanText, result.OptimizedLogicalPlanText);
        Assert.AreEqual(result.PhysicalPlanText, result.InitialPhysicalPlanText);
        Assert.AreEqual(result.PhysicalPlanText, result.OptimizedPhysicalPlanText);
        Assert.AreEqual(result.ExecutionPlanText, result.InitialExecutionPlanText);
        Assert.AreEqual(result.ExecutionPlanText, result.OptimizedExecutionPlanText);
        StringAssert.StartsWith(result.OptimizerTraceText, "OptimizerTrace");
        StringAssert.Contains(result.OptimizerTraceText, "PreLogicalNormalization [DistinctToGroupByNormalization]");
        StringAssert.Contains(result.OptimizerTraceText, "PreLogicalNormalization [SubqueryToCteNormalization]");
        StringAssert.Contains(result.OptimizerTraceText, "PhysicalOptimization [SourcePredicatePhysicalRewrite]");
        StringAssert.Contains(result.OptimizerTraceText, "PhysicalOptimization [SourcePlanPhysicalRewrite]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [SingleUsePipelineFusion]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [CteReadOnceFusion]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [CteSidecarIndexLowering]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [FieldExpressionHoisting]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [CapacityHints]");
        StringAssert.Contains(result.OptimizerTraceText, "ExecutionIrOptimization [MethodTargetReuse]");
        StringAssert.Contains(result.OptimizerTraceText, "CodegenReadability [DeterministicMemberOrdering]");
        StringAssert.Contains(result.OptimizerTraceText, "CodegenReadability [ReadabilityDecisionTrace]");
    }
}
