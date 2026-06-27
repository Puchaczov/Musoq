using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void OptimizerOwnership_RetiredLowererHoistingFiles_ShouldStayDeleted()
    {
        var repositoryRoot = FindRepositoryRoot();
        string[] retiredFiles =
        [
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.FieldReadHoisting.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.BlockFieldReadHoisting.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.RawAggregatePipelines.HoistedReads.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionHoistCandidateLoweringHelpers.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionMethodTargetCandidateHoisting.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.ExpressionHoisting.Collection.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.ExpressionHoisting.Plan.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.ExpressionHoisting.Replacement.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.ExpressionHoisting.Signatures.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.ExpressionHoisting.Usage.cs",
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.Types.HoistingAndResults.cs"
        ];

        var existing = retiredFiles
            .Where(path => File.Exists(ToAbsolutePath(repositoryRoot, path)))
            .ToArray();

        Assert.IsEmpty(
            existing,
            "Retired lowerer-owned field-read hoisting files must not be reintroduced: " +
            string.Join(", ", existing));
    }

    [TestMethod]
    public void OptimizerOwnership_ExecutionLowering_ShouldNotContainHoistingShims()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionLoweringDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution");
        var lowererFiles = Directory
            .EnumerateFiles(executionLoweringDirectory, "PhysicalToExecutionPlanBuilder*.cs")
            .ToArray();
        string[] hoistingMarkers =
        [
            "CreateHoistCandidateLets(",
            "HoistRepeatedExpressions(",
            "HoistExpressionsSharedWithCondition(",
            "HoistExpressionsSharedWithBlock("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, hoistingMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution IR lowering must leave expression hoisting to Execution IR passes: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_TransformTree_ShouldNotInvokeRetiredAstOptimizationVisitors()
    {
        var repositoryRoot = FindRepositoryRoot();
        var transformTreeFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter", "Build"), "TransformTree*.cs")
            .ToArray();
        string[] retiredInvocationMarkers =
        [
            "new ConstantFoldingVisitor",
            "new ConstantFoldingTraverseVisitor",
            "DeadCteEliminator."
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, transformTreeFiles, retiredInvocationMarkers);

        Assert.IsEmpty(
            offenders,
            "Pre-logical AST optimization visitors must not be invoked from TransformTree: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_Renderer_ShouldNotSynthesizeReusableMethodTargets()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rendererFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution"), "ExecutionCSharpRenderer*.cs")
            .ToArray();
        string[] synthesisMarkers =
        [
            "new LibraryBase(",
            "new LibraryBase()",
            "Activator.CreateInstance(methodCall.Method.DeclaringType"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, rendererFiles, synthesisMarkers);

        Assert.IsEmpty(
            offenders,
            "Renderer must reject unbound reusable method calls instead of synthesizing targets: " +
            string.Join(", ", offenders));

        var guardFile = ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionCSharpRenderer.RendererMethodTargetGuard.cs");
        var guardText = File.ReadAllText(guardFile);
        Assert.Contains(
            "requires a reusable target assigned by MethodTargetReusePass",
            guardText,
            "Renderer method-target diagnostics must keep pointing ownership at MethodTargetReusePass.");
    }

    [TestMethod]
    public void OptimizerOwnership_CteLowerer_ShouldEmitCandidatesInsteadOfFinalStoreLoadNodes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lowererFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution"), "PhysicalToExecutionPlanBuilder*.cs")
            .ToArray();
        string[] finalNodeMarkers =
        [
            "new ExecutionStoreCteIndex(",
            "new ExecutionLoadCteIndex("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, finalNodeMarkers);

        Assert.IsEmpty(
            offenders,
            "CTE lowering must emit CTE strategy candidates and leave final store/load nodes to CteSidecarIndexLoweringPass: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_CteSidecarLowerer_ShouldNotEmitFinalBuildAppendRuntimeNodes()
    {
        var repositoryRoot = FindRepositoryRoot();
        string[] sidecarLowererFiles =
        [
            ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.CteSidecarIndexes.cs"),
            ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.CteSidecarIndexAppends.cs")
        ];
        string[] finalRuntimeNodeMarkers =
        [
            "new ExecutionCreateHash(",
            "new ExecutionCreateKeySet(",
            "new ExecutionCreateGeneratedRow(",
            "new ExecutionAppendExistingRow(",
            "new ExecutionCreateHashPayload(",
            "new ExecutionHashAdd(",
            "new ExecutionKeySetAdd(",
            "new ExecutionCteSidecarIndexBuildCandidate(new ExecutionBlock",
            "new ExecutionCteSidecarAppendRewriteCandidate(new ExecutionBlock"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, sidecarLowererFiles, finalRuntimeNodeMarkers);

        Assert.IsEmpty(
            offenders,
            "CTE sidecar lowering must emit structured strategy candidates; final build/append runtime nodes belong to CteSidecarIndexLoweringPass: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_CteLowerer_ShouldNotDisablePlannerSelectedSidecars()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lowererFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution"), "PhysicalToExecutionPlanBuilder*.cs")
            .ToArray();
        string[] disabledSidecarMarkers =
        [
            "DisableCteSidecarIndexes",
            "_disabledCteSidecarIndexSlots"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, disabledSidecarMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution IR lowering must fail clearly instead of disabling planner-selected CTE sidecar indexes: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_ExecutionLowerer_ShouldNotPlanStrategies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lowererFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution"), "*.cs")
            .ToArray();
        string[] planningMarkers =
        [
            "ExecutionStrategyPlanner.Plan("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, planningMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution IR lowering must consume planner-owned execution strategies instead of self-planning: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_ExecutionLowerer_ShouldNotCreateJoinStrategyFallbacks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var lowererFiles = Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution"), "PhysicalToExecutionPlanBuilder*.cs")
            .ToArray();
        string[] fallbackMarkers =
        [
            "CreateNestedLoopJoinFallback",
            "RequiresNestedLoopHashJoinFallback",
            "Hash join fallback requires"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, fallbackMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution IR lowering must not create replacement physical join strategies: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_ExecutionLowering_ShouldNotCreateMethodTargetRegistries()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionLoweringDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution");
        var lowererFiles = Directory
            .EnumerateFiles(executionLoweringDirectory, "PhysicalToExecutionPlanBuilder*.cs")
            .ToArray();
        string[] registryMarkers =
        [
            "new MethodTargetRegistry(",
            "new WindowMethodTargetRegistry(",
            "CreateMethodTargetDeclarations(",
            "RegisterMethodTargetCandidates(",
            "RegisterWindowMethodTargetCandidates("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, registryMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution IR lowering must leave method-target binding to MethodTargetReusePass: " +
            string.Join(", ", offenders));
    }

    private static string[] FindFilesContainingAny(
        string repositoryRoot,
        string[] files,
        string[] markers)
    {
        return files
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return markers.Any(marker => text.Contains(marker, StringComparison.Ordinal));
            })
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
    }
}
