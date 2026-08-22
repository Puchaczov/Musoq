using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void OptimizerOwnership_ProjectionPruning_ShouldStayOutOfBuildersLowerersAndRenderers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var files = EnumeratePhysicalBuilderLowererRendererFiles(repositoryRoot)
            .Concat(Directory.EnumerateFiles(
                Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter", "Build"),
                "TransformTree*.cs"))
            .ToArray();
        string[] markers =
        [
            "ProjectionPruningPhysicalPass",
            "ProjectionPruningRewriter",
            "PlanningDecisionCategory.ProjectionPruning",
            "RequiredColumnMappingPlan",
            "CreateRequiredColumnMappingPlans"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, files, markers);

        Assert.IsEmpty(
            offenders,
            "Projection pruning and required-column mapping decisions must stay in planning/physical optimizer passes: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_RowShapePlanning_ShouldNotBeRecreatedByBuildersLowerersOrRenderers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var files = EnumeratePhysicalBuilderLowererRendererFiles(repositoryRoot);
        string[] markers =
        [
            "new BoundaryRowShapePlan(",
            "new RowWidthPruningPlan(",
            "BoundaryRowShapePlanner.Plan(",
            "RowWidthPruningPlanner.Plan("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, files, markers);

        Assert.IsEmpty(
            offenders,
            "Boundary row-shape and row-width pruning decisions must be created by planning and then consumed by lowerers: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_Renderers_ShouldNotOwnRowWidthPruning()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rendererFiles = Directory
            .EnumerateFiles(
                Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Targets.CSharpClr", "Rendering", "Execution"),
                "ExecutionCSharpRenderer*.cs")
            .ToArray();
        string[] markers =
        [
            "RowWidthPruning",
            "BoundaryRowShape",
            "FutureDroppableColumns",
            "BoundaryOnlyColumns"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, rendererFiles, markers);

        Assert.IsEmpty(
            offenders,
            "Renderers must faithfully emit optimized Execution IR and must not make row-width pruning decisions: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_SubqueryVisitors_ShouldOnlyRunThroughPreLogicalNormalizer()
    {
        var repositoryRoot = FindRepositoryRoot();
        string[] allowedPaths =
        [
            "src/dotnet/Musoq.Evaluator/IR/Optimization/Logical/PreLogicalNormalizer.cs",
            "src/dotnet/Musoq.Evaluator/IR/Optimization/Logical/DistinctToGroupByNormalizationPass.cs",
            "src/dotnet/Musoq.Evaluator/IR/Optimization/Logical/SubqueryToCteNormalizationPass.cs"
        ];
        var files = EnumerateProductionSourceFiles(repositoryRoot)
            .Where(file => !allowedPaths.Contains(
                Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                StringComparer.Ordinal))
            .ToArray();
        string[] markers =
        [
            "new DistinctToGroupByVisitor(",
            "new DistinctToGroupByTraverseVisitor(",
            "new SubqueryToCteRewriteVisitor(",
            "new SubqueryToCteRewriteTraverseVisitor("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, files, markers);

        Assert.IsEmpty(
            offenders,
            "Pre-logical AST normalization visitors must be invoked only by PreLogicalNormalizer: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_Renderers_ShouldNotOwnHelperExtractionReadability()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rendererFiles = Directory
            .EnumerateFiles(
                Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Targets.CSharpClr", "Rendering", "Execution"),
                "ExecutionCSharpRenderer*.cs")
            .ToArray();
        string[] markers =
        [
            "HelperExtractionReadabilityPass",
            "HelperExtractionAnnotationKind",
            "Musoq.HelperExtractionReadability"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, rendererFiles, markers);

        Assert.IsEmpty(
            offenders,
            "Renderers may emit helper extraction candidate metadata, but readability approval belongs to CodegenReadabilityGroup: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_HelperExtractionApproval_ShouldStayInCodegenReadabilityPasses()
    {
        var repositoryRoot = FindRepositoryRoot();
        var allowedPaths = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/dotnet/Musoq.Targets.CSharpClr/Optimization/Codegen/HelperExtractionReadabilityPass.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/Optimization/Codegen/HelperExtractionReadabilityApproval.cs",
            "src/dotnet/Musoq.Targets.CSharpClr/Optimization/Codegen/ExecutionCodegenOptimizationPass.cs"
        };
        var files = EnumerateProductionSourceFiles(repositoryRoot)
            .Where(file => !allowedPaths.Contains(
                Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/')))
            .ToArray();
        string[] markers =
        [
            "HelperExtractionAnnotationKind",
            "Musoq.HelperExtractionReadability",
            "CreateHelperAnnotation(",
            "CreateHelperCallAnnotation(",
            "TryExtractInlineBlock(",
            "CreateInlineHelper("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, files, markers);

        Assert.IsEmpty(
            offenders,
            "Helper extraction approval and inline helper creation must stay in CodegenReadabilityGroup passes: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_ExecutionLowerersAndRenderers_ShouldNotRunRuleBasedOptimizerPasses()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionFiles = EnumerateExecutionLowererAndRendererFiles(repositoryRoot);
        string[] markers =
        [
            "ProjectionPruningPhysicalPass",
            "PredicateMovementPhysicalPass",
            "ExpressionCseHoistingPass",
            "FieldExpressionHoistingPass",
            "SingleUsePipelineFusionPass",
            "HelperExtractionReadabilityPass",
            "RowWidthPruningPlanner.Plan(",
            "BoundaryRowShapePlanner.Plan(",
            "PredicateMovementPlanner.Plan(",
            "PredicatePlacementPlanner.Plan("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, executionFiles, markers);

        Assert.IsEmpty(
            offenders,
            "Execution lowerers and renderers must consume optimized IR or planner metadata, not run rule-based optimizer passes: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_RenderersAndLowerers_ShouldNotOwnSourceCapabilityNegotiation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionFiles = EnumerateExecutionLowererAndRendererFiles(repositoryRoot);

        var offenders = FindFilesContainingAny(repositoryRoot, executionFiles, SourceCapabilityDecisionMarkers);

        Assert.IsEmpty(
            offenders,
            "Source capability decisions must stay in source planning and physical source rewrite passes, not renderers or Execution IR lowerers: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_GeneratedSamples_ShouldNotOwnSourceCapabilityNegotiation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var samplesDirectory = GeneratedCodeSampleArtifacts.SamplesDirectory;
        var sampleFiles = Directory.Exists(samplesDirectory)
            ? Directory.EnumerateFiles(samplesDirectory, "*.cs", SearchOption.TopDirectoryOnly).ToArray()
            : [];

        var offenders = FindFilesContainingAny(repositoryRoot, sampleFiles, SourceCapabilityDecisionMarkers);

        Assert.IsEmpty(
            offenders,
            "Generated C# must consume immutable source execution plans only; source capability negotiation must not be synthesized in generated helpers: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void OptimizerOwnership_GeneratedSamples_ShouldNotContainOptimizerImplementationNames()
    {
        var repositoryRoot = FindRepositoryRoot();
        var samplesDirectory = GeneratedCodeSampleArtifacts.SamplesDirectory;
        var sampleFiles = Directory.Exists(samplesDirectory)
            ? Directory.EnumerateFiles(samplesDirectory, "*.cs", SearchOption.TopDirectoryOnly).ToArray()
            : [];
        string[] markers =
        [
            "ProjectionPruningPhysicalPass",
            "RowWidthPruningPlanner",
            "PredicateMovementPlanner",
            "PredicatePlacementPlanner",
            "ExpressionCseHoistingPass",
            "SingleUsePipelineFusionPass",
            "HelperExtractionReadabilityPass"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, sampleFiles, markers);

        Assert.IsEmpty(
            offenders,
            "Generated C# samples must contain query output and inspection text, not optimizer implementation type names: " +
            string.Join(", ", offenders));
    }

    private static string[] EnumeratePhysicalBuilderLowererRendererFiles(string repositoryRoot)
    {
        return Directory
            .EnumerateFiles(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR"), "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var name = Path.GetFileName(file);
                return name.StartsWith("PhysicalPlanBuilder", StringComparison.Ordinal) ||
                       name.StartsWith("PhysicalLoweringImplementation", StringComparison.Ordinal) ||
                       name.StartsWith("ExecutionCSharpRenderer", StringComparison.Ordinal);
            })
            .ToArray();
    }

    private static string[] EnumerateExecutionLowererAndRendererFiles(string repositoryRoot)
    {
        var executionRoot = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Targets.CSharpClr", "Rendering", "Execution");
        return Directory
            .EnumerateFiles(executionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
            {
                var relativePath = Path.GetRelativePath(executionRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                var name = Path.GetFileName(file);
                return name.StartsWith("ExecutionCSharpRenderer", StringComparison.Ordinal) ||
                       name.StartsWith("PhysicalLoweringImplementation", StringComparison.Ordinal) ||
                       relativePath.StartsWith("Lowering/", StringComparison.Ordinal) ||
                       relativePath.StartsWith("Rendering/", StringComparison.Ordinal);
            })
            .ToArray();
    }

    private static readonly string[] SourceCapabilityDecisionMarkers =
    [
        "AcceptedPredicate",
        "AcceptedOrderBy",
        "AcceptedSkip",
        "AcceptedTake",
        "AcceptedColumns",
        ".ResidualPredicate",
        "ResidualOrderBy",
        "ResidualSkip",
        "ResidualTake",
        "SourcePlanRequest",
        "SourcePlanResult",
        "TryPlanSource("
    ];
}
