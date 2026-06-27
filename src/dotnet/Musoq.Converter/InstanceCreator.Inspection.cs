using System.Linq;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
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

        if (!items.ContainsKey("COMPILATION"))
            throw new InvalidOperationException(
                "Generated C# inspection failed because the compilation pipeline did not produce a C# compilation.");

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
            ExtractGeneratedCSharpCode(items.Compilation))
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

    private static string ExtractGeneratedCSharpCode(Microsoft.CodeAnalysis.CSharp.CSharpCompilation compilation)
    {
        var syntaxTrees = compilation.SyntaxTrees.ToArray();

        if (syntaxTrees.Length == 0)
            return string.Empty;

        if (syntaxTrees.Length == 1)
            return FormatSyntaxTree(syntaxTrees[0]);

        var builder = new StringBuilder();

        for (var index = 0; index < syntaxTrees.Length; index++)
        {
            if (index > 0)
                builder.AppendLine();

            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"// === SYNTAX TREE {index} ===");
            builder.AppendLine(FormatSyntaxTree(syntaxTrees[index]));
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatSyntaxTree(Microsoft.CodeAnalysis.SyntaxTree syntaxTree)
    {
        return syntaxTree.GetRoot().ToFullString();
    }
}
