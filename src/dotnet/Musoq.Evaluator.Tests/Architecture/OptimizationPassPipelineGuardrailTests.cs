using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Targets.CSharpClr.Optimization.Codegen;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class OptimizationPassPipelineGuardrailTests
{
    [TestMethod]
    public void EveryPipeline_ShouldCarryItsStageAndRunMode()
    {
        Assert.AreEqual(OptimizationStage.PreLogicalNormalization, PreLogicalNormalizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.LogicalNormalization, LogicalNormalizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.LogicalOptimization, LogicalOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.PhysicalOptimization, PhysicalOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.ExecutionIrOptimization, ExecutionIrOptimizationGroup.Pipeline.Stage);
        Assert.AreEqual(OptimizationStage.CodegenReadability, CodegenReadabilityGroup.Pipeline.Stage);

        Assert.AreEqual(OptimizationPassRunMode.Once, PreLogicalNormalizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, LogicalNormalizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, LogicalOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, PhysicalOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, ExecutionIrOptimizationGroup.Pipeline.RunMode);
        Assert.AreEqual(OptimizationPassRunMode.Once, CodegenReadabilityGroup.Pipeline.RunMode);
    }

    [TestMethod]
    public void EveryPipelineStep_ShouldDeclareANonEmptyReason()
    {
        var reasons = AllStepReasons();

        Assert.IsNotEmpty(reasons);
        Assert.IsEmpty(
            reasons.Where(string.IsNullOrWhiteSpace),
            "Every optimization pipeline step must declare a non-empty reason.");
    }

    [TestMethod]
    public void ExecutionIrPipeline_ShouldDocumentBothMethodTargetReuseRunsDistinctly()
    {
        var reuseReasons = ExecutionIrOptimizationGroup.Pipeline.Steps
            .Where(step => step.Name == "MethodTargetReuse")
            .Select(step => step.Reason)
            .ToArray();

        Assert.HasCount(2, reuseReasons);
        Assert.AreNotEqual(
            reuseReasons[0],
            reuseReasons[1],
            "The two intentional MethodTargetReusePass runs should document distinct reasons.");
    }

    [TestMethod]
    public void PipelinePasses_ShouldMatchPipelineSteps()
    {
        AssertPassesMatchSteps(PreLogicalNormalizationGroup.Pipeline);
        AssertPassesMatchSteps(LogicalNormalizationGroup.Pipeline);
        AssertPassesMatchSteps(LogicalOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(PhysicalOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(ExecutionIrOptimizationGroup.Pipeline);
        AssertPassesMatchSteps(CodegenReadabilityGroup.Pipeline);
    }

    [TestMethod]
    public void StageSpecificPassContracts_ShouldBindToExpectedPlanTypes()
    {
        AssertStageContract<IPreLogicalNormalizationPass, RootNode>();
        AssertStageContract<ILogicalNormalizationPass, LogicalNode>();
        AssertStageContract<ILogicalOptimizationPass, LogicalNode>();
        AssertStageContract<IPhysicalOptimizationPass, PhysicalNode>();
        AssertStageContract<IExecutionIrOptimizationPass, ExecutionPlan>();
        AssertStageContract<ICodegenReadabilityOptimizationPass, CompilationUnitSyntax>();
    }

    [TestMethod]
    public void PipelinePasses_ShouldImplementStageSpecificContracts()
    {
        AssertPassContracts(PreLogicalNormalizationGroup.Pipeline.Passes, typeof(IPreLogicalNormalizationPass));
        AssertPassContracts(LogicalNormalizationGroup.Pipeline.Passes, typeof(ILogicalNormalizationPass));
        AssertPassContracts(LogicalOptimizationGroup.Pipeline.Passes, typeof(ILogicalOptimizationPass));
        AssertPassContracts(PhysicalOptimizationGroup.Pipeline.Passes, typeof(IPhysicalOptimizationPass));
        AssertPassContracts(ExecutionIrOptimizationGroup.Pipeline.Passes, typeof(IExecutionIrOptimizationPass));
        AssertPassContracts(CodegenReadabilityGroup.Pipeline.Passes, typeof(ICodegenReadabilityOptimizationPass));
    }

    [TestMethod]
    public void LogicalOptimizationPasses_ShouldLiveUnderLogicalOwnershipFolder()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var optimizationRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization");
        var offenders = Directory
            .EnumerateFiles(optimizationRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Where(static name =>
                name!.StartsWith("Logical", System.StringComparison.Ordinal) ||
                name.StartsWith("PreLogical", System.StringComparison.Ordinal) ||
                name is "DeadCteEliminationLogicalPass.cs" ||
                name is "DistinctToGroupByNormalizationPass.cs" ||
                name is "SubqueryToCteNormalizationPass.cs")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Logical and pre-logical optimizer ownership belongs under IR/Optimization/Logical: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void PhysicalOptimizationPasses_ShouldLiveUnderPhysicalOwnershipFolder()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var optimizationRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization");
        var offenders = Directory
            .EnumerateFiles(optimizationRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Where(static name =>
                name!.StartsWith("Physical", System.StringComparison.Ordinal) ||
                name.StartsWith("ProjectionPruning", System.StringComparison.Ordinal) ||
                name.StartsWith("AggregateStrategy", System.StringComparison.Ordinal) ||
                name.StartsWith("PredicateMovementPhysical", System.StringComparison.Ordinal) ||
                name.StartsWith("JoinStrategy", System.StringComparison.Ordinal) ||
                name.StartsWith("OrderingStrategy", System.StringComparison.Ordinal) ||
                name.StartsWith("WindowMaterialization", System.StringComparison.Ordinal) ||
                name.StartsWith("SourcePredicate", System.StringComparison.Ordinal) ||
                name.StartsWith("SourceProjection", System.StringComparison.Ordinal) ||
                name.StartsWith("SourcePlanPhysical", System.StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Physical optimizer ownership belongs under IR/Optimization/Physical: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void ExecutionIrOptimizationPasses_ShouldLiveUnderExecutionOwnershipFolder()
    {
        var offenders = FindRootOptimizationFiles(
            static name =>
                name.StartsWith("Execution", System.StringComparison.Ordinal) ||
                name.StartsWith("Expression", System.StringComparison.Ordinal) ||
                name.StartsWith("MethodTarget", System.StringComparison.Ordinal) ||
                name.StartsWith("Cte", System.StringComparison.Ordinal) ||
                name.StartsWith("CapacityHint", System.StringComparison.Ordinal) ||
                name.StartsWith("FieldExpression", System.StringComparison.Ordinal) ||
                name.StartsWith("SingleUsePipeline", System.StringComparison.Ordinal));

        Assert.IsEmpty(
            offenders,
            "Execution IR optimizer ownership belongs under IR/Optimization/Execution: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void CodegenReadabilityPasses_ShouldLiveUnderCSharpTargetOwnershipFolder()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var evaluatorCodegenRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization",
            "Codegen");
        Assert.IsFalse(
            Directory.Exists(evaluatorCodegenRoot),
            "Generated C# readability optimization belongs to Musoq.Targets.CSharpClr, not evaluator IR.");

        var targetCodegenRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Optimization",
            "Codegen");
        string[] expectedFiles =
        [
            "CodegenReadabilityGroup.cs",
            "CodegenReadabilityOptimizationResult.cs",
            "CodegenReadabilityOptimizer.cs",
            "CodegenReadabilitySyntaxFacts.cs",
            "ControlFlowNormalizationPass.cs",
            "DeadTemporaryCleanupPass.cs",
            "DeterministicMemberOrderingPass.cs",
            "HelperExtractionReadabilityApproval.cs",
            "HelperExtractionReadabilityPass.cs",
            "ICodegenReadabilityOptimizationPass.cs",
            "LocalDeclarationNormalizationPass.cs",
            "ReadabilityDecisionTracePass.cs"
        ];
        var missing = expectedFiles
            .Where(file => !File.Exists(Path.Combine(targetCodegenRoot, file)))
            .ToArray();

        Assert.IsEmpty(
            missing,
            "Expected generated C# readability optimization files under the CSharpClr target package: " +
            string.Join(", ", missing));

        var misplaced = FindRootOptimizationFiles(
            static name =>
                name.StartsWith("Codegen", System.StringComparison.Ordinal) ||
                name.StartsWith("DeterministicMember", System.StringComparison.Ordinal) ||
                name.StartsWith("LocalDeclaration", System.StringComparison.Ordinal) ||
                name.StartsWith("DeadTemporary", System.StringComparison.Ordinal) ||
                name.StartsWith("ControlFlow", System.StringComparison.Ordinal) ||
                name.StartsWith("HelperExtraction", System.StringComparison.Ordinal) ||
                name.StartsWith("ReadabilityDecision", System.StringComparison.Ordinal));

        Assert.IsEmpty(
            misplaced,
            "Generated C# readability optimizer ownership belongs under Musoq.Targets.CSharpClr/Optimization/Codegen: " +
            string.Join(", ", misplaced));
    }

    [TestMethod]
    public void EvaluatorOptimizationContracts_ShouldNotReferenceRoslynSyntaxTypes()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var contractFile = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization",
            "IPlanOptimizationPass.cs");
        var contents = File.ReadAllText(contractFile);

        Assert.IsFalse(
            contents.Contains("Microsoft.CodeAnalysis", StringComparison.Ordinal) ||
            contents.Contains("CompilationUnitSyntax", StringComparison.Ordinal),
            "Evaluator optimization pass contracts must stay target-neutral; generated C# syntax contracts belong to Musoq.Targets.CSharpClr.");
    }

    private static void AssertPassesMatchSteps<TPlan>(OptimizationPassPipeline<TPlan> pipeline)
    {
        CollectionAssert.AreEqual(
            pipeline.Steps.Select(step => step.Pass).ToArray(),
            pipeline.Passes.ToArray(),
            $"Pipeline for stage {pipeline.Stage} exposes passes that diverge from its declared steps.");
    }

    private static void AssertStageContract<TContract, TPlan>()
        where TContract : IPlanOptimizationPass<TPlan>
    {
        Assert.IsTrue(typeof(IPlanOptimizationPass<TPlan>).IsAssignableFrom(typeof(TContract)));
    }

    private static void AssertPassContracts<TPlan>(
        IReadOnlyList<IPlanOptimizationPass<TPlan>> passes,
        System.Type expectedContract)
    {
        var offenders = passes
            .Where(pass => !expectedContract.IsInstanceOfType(pass))
            .Select(pass => pass.GetType().Name)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            $"Pipeline contains pass(es) that do not implement {expectedContract.Name}: " +
            string.Join(", ", offenders));
    }

    private static string[] FindRootOptimizationFiles(Func<string, bool> isForbiddenFileName)
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var optimizationRoot = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Optimization");

        return Directory
            .EnumerateFiles(optimizationRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .Where(isForbiddenFileName)
            .ToArray();
    }

    private static IReadOnlyList<string> AllStepReasons()
    {
        return
        [
            .. PreLogicalNormalizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. LogicalNormalizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. LogicalOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. PhysicalOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. ExecutionIrOptimizationGroup.Pipeline.Steps.Select(step => step.Reason),
            .. CodegenReadabilityGroup.Pipeline.Steps.Select(step => step.Reason)
        ];
    }
}
