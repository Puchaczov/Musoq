using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PlanningDependencyGuardrailTests
{
    [TestMethod]
    public void PlanningSources_ShouldNotDependOnExecutionNamespace()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var planningFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Planning",
            "*.cs");
        string[] forbiddenMarkers =
        [
            "using Musoq.Evaluator.IR.Execution",
            "Musoq.Evaluator.IR.Execution.",
            "ExecutionExpressionConverter"
        ];

        var offenders = planningFiles
            .Where(file => forbiddenMarkers.Any(marker => File.ReadAllText(file).Contains(marker, System.StringComparison.Ordinal)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static file => file, System.StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Planning must consume planning-owned expression and shape facts instead of depending on execution IR: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void ParallelPlanningEligibility_ShouldUseSharedIrExpressionTraversal()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Planning",
            "ParallelPlanningEligibilityRules.cs");
        var text = File.ReadAllText(file);
        string[] broadTraversalMarkers =
        [
            "BinaryOp binary =>",
            "UnaryOp unary =>",
            "ArrayAccess arrayAccess =>",
            "InCheck inCheck =>",
            "PatternMatch patternMatch =>",
            "Between between =>",
            "CaseWhen caseWhen =>",
            "StrictCast strictCast =>"
        ];
        var offenders = broadTraversalMarkers
            .Where(marker => text.Contains(marker, System.StringComparison.Ordinal))
            .ToArray();

        StringAssert.Contains(text, "IrExpressionTraversal");
        StringAssert.Contains(text, "IrExpressionFacts.ContainsMethodCall");
        Assert.IsEmpty(
            offenders,
            "Parallel planning must use shared IrExpressionTraversal instead of owning a broad recursive expression inventory: " +
            string.Join(", ", offenders));
    }
}
