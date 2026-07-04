using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class PhysicalPlanningOwnershipGuardrailTests
{
    [TestMethod]
    public void PhysicalPlanningPipeline_ShouldLiveUnderPlanningOwnership()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var oldPath = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization",
            "PhysicalPlanningPipeline.cs");
        var newPath = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Planning",
            "Physical",
            "PhysicalPlanningPipeline.cs");

        Assert.IsFalse(File.Exists(oldPath), "Physical planning orchestration must not live under IR/Optimization.");
        Assert.IsTrue(File.Exists(newPath), "Physical planning orchestration should stay under IR/Planning/Physical.");
        StringAssert.Contains(File.ReadAllText(newPath), "namespace Musoq.Evaluator.IR.Planning;");
    }

    [TestMethod]
    public void OptimizationNamespace_ShouldNotInvokePhysicalPlanningSelectors()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var optimizationFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Optimization",
            "*.cs");
        string[] forbiddenMarkers =
        [
            "PhysicalStrategyPlanner.Plan(",
            "CardinalityFactPlanner.Plan(",
            "new PhysicalPlanBuilder(",
            ".Lower(context.LogicalPlan)"
        ];

        var offenders = optimizationFiles
            .Where(file => forbiddenMarkers.Any(marker => File.ReadAllText(file).Contains(marker, StringComparison.Ordinal)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "IR/Optimization must run optimizer passes only; physical strategy selection, physical lowering, and cardinality planning belong to planning ownership: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void PhysicalOptimizationPasses_ShouldNotReadPlanPropertiesDirectly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        string[] adapterFiles =
        [
            "PhysicalOptimizationFacts.cs",
            "PhysicalOptimizationResult.cs",
            "PhysicalOptimizationSession.cs",
            "PhysicalOptimizationState.cs",
            "PhysicalOptimizer.cs",
            "SourceRewriteFacts.cs"
        ];
        var optimizationFiles = RepositorySourceScan.FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator/IR/Optimization",
                "*.cs")
            .Concat(RepositorySourceScan.FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator/IR/Optimization/Facts",
                "*.cs"))
            .Where(file => !adapterFiles.Contains(Path.GetFileName(file), StringComparer.Ordinal))
            .ToArray();

        var offenders = optimizationFiles
            .Where(file => File.ReadAllText(file).Contains("PlanProperties", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .OrderBy(static file => file, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Physical optimization passes must consume PhysicalOptimizationFacts/SourceRewriteFacts instead of PlanProperties directly: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void PhysicalOptimizer_ShouldRequireShapeFacts()
    {
        var optimizeMethods = typeof(PhysicalOptimizer)
            .GetMethods()
            .Where(static method => method.Name == nameof(PhysicalOptimizer.Optimize))
            .ToArray();
        var twoArgumentPropertyOverloads = optimizeMethods
            .Where(static method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2 &&
                       typeof(PhysicalNode).IsAssignableFrom(parameters[0].ParameterType) &&
                       parameters[1].ParameterType == typeof(PlanProperties);
            })
            .ToArray();
        var optionalShapeResolvers = optimizeMethods
            .SelectMany(static method => method.GetParameters())
            .Where(static parameter => parameter.ParameterType == typeof(IPlanningShapeResolver))
            .Where(static parameter => parameter.IsOptional)
            .ToArray();

        Assert.IsEmpty(twoArgumentPropertyOverloads, "Physical optimizer must not expose an overload that can run without shape facts.");
        Assert.IsEmpty(optionalShapeResolvers, "Physical optimizer shape resolver parameters must be required.");
    }

    [TestMethod]
    public void ExecutionStrategyPlan_ShouldStoreSelectionsByPhysicalNodeId()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Planning",
            "Types",
            "ExecutionStrategy",
            "ExecutionStrategyPlan.cs");
        var text = File.ReadAllText(file);
        string[] forbiddenFields =
        [
            "private readonly IReadOnlySet<PhysicalSingleKeyAggregateNode>",
            "private readonly IReadOnlySet<PhysicalProjectNode>",
            "private readonly IReadOnlyDictionary<PhysicalCteNode",
            "private readonly IReadOnlyDictionary<PhysicalSetOperationNode"
        ];
        var offenders = forbiddenFields
            .Where(marker => text.Contains(marker, StringComparison.Ordinal))
            .ToArray();

        StringAssert.Contains(text, "PhysicalNodeIdentityMap");
        StringAssert.Contains(text, "IReadOnlySet<PhysicalNodeId>");
        StringAssert.Contains(text, "IReadOnlyDictionary<PhysicalNodeId");
        Assert.IsEmpty(
            offenders,
            "ExecutionStrategyPlan must store strategy selections by PhysicalNodeId, not physical node object keys: " +
            string.Join(", ", offenders));
    }
}
