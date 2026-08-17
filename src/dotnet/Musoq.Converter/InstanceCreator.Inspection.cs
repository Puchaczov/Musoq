using System.Linq;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Planning.Printing;
using PhysicalPlanPrinter = Musoq.Evaluator.IR.Physical.PhysicalPlanPrinter;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static QueryInspectionResult CreateInspectionResult(BuildItems items)
    {
        var logicalPlan = items.LogicalPlan ?? throw new InvalidOperationException(
            "Logical plan inspection failed because the compilation pipeline did not produce a logical plan.");
        var physicalPlan = items.PhysicalPlan ?? throw new InvalidOperationException(
            "Physical plan inspection failed because the compilation pipeline did not produce a physical plan.");

        RenderedQueryArtifact renderedArtifact;
        try
        {
            renderedArtifact = items.RenderingArtifact;
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            throw new InvalidOperationException(
                "Generated C# inspection failed because the compilation pipeline did not produce a rendered query artifact.",
                ex);
        }

        var executionPlanText = items.ExecutionPlanText ??
            ExecutionPlanPrinter.PrintUnsupported("Execution IR inspection was not produced by the compilation pipeline.");
        var planningText = items.PlanningText ?? PlanningTextPrinter.Print(items.PlanningResult);
        var initialLogicalPlan = items.InitialLogicalPlan ?? logicalPlan;
        var optimizedLogicalPlan = items.OptimizedLogicalPlan ?? logicalPlan;
        var initialPhysicalPlan = items.InitialPhysicalPlan ?? physicalPlan;
        var optimizedPhysicalPlan = items.OptimizedPhysicalPlan ?? physicalPlan;
        var initialExecutionPlanText = items.InitialExecutionPlan != null
            ? ExecutionPlanPrinter.Print(items.InitialExecutionPlan)
            : executionPlanText;
        var optimizedExecutionPlanText = items.OptimizedExecutionPlan != null
            ? ExecutionPlanPrinter.Print(items.OptimizedExecutionPlan)
            : executionPlanText;
        var diagnostics = items.DiagnosticContext.Diagnostics.ToList();

        return new QueryInspectionResult(
            logicalPlan,
            physicalPlan,
            LogicalPlanPrinter.Print(logicalPlan),
            PhysicalPlanPrinter.Print(physicalPlan),
            InspectGeneratedCSharpCode(renderedArtifact))
        {
            PlanningText = planningText,
            ExecutionPlanText = executionPlanText,
            ExecutionPlan = items.ExecutionPlan,
            InitialLogicalPlanText = LogicalPlanPrinter.Print(initialLogicalPlan),
            OptimizedLogicalPlanText = LogicalPlanPrinter.Print(optimizedLogicalPlan),
            InitialPhysicalPlanText = PhysicalPlanPrinter.Print(initialPhysicalPlan),
            OptimizedPhysicalPlanText = PhysicalPlanPrinter.Print(optimizedPhysicalPlan),
            InitialExecutionPlanText = initialExecutionPlanText,
            OptimizedExecutionPlanText = optimizedExecutionPlanText,
            OptimizerTraceText = items.OptimizerTraceText ?? "OptimizerTrace [not produced]",
            Diagnostics = diagnostics,
            Warnings = diagnostics.Where(static diagnostic => diagnostic.IsWarning).ToList()
        };
    }

    private static string InspectGeneratedCSharpCode(RenderedQueryArtifact renderedArtifact)
    {
        var inspection = ExecutionTargetCatalog.InspectArtifact(renderedArtifact);

        if (inspection.TargetId != ExecutionTargetIds.CSharpClr)
            throw new InvalidOperationException(
                $"Generated C# inspection requires execution target '{ExecutionTargetIds.CSharpClr}', but got '{inspection.TargetId}'.");

        return inspection.GeneratedCSharpCode ?? string.Empty;
    }
}
