using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void FocusedArchitecture_Renderers_ShouldNotReferencePlannerContracts()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution");
        var rendererFiles = Directory
            .EnumerateFiles(executionDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                Path.GetFileName(file).StartsWith("ExecutionCSharpRenderer", StringComparison.Ordinal) ||
                Path.GetRelativePath(executionDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .StartsWith("Rendering/", StringComparison.Ordinal))
            .ToArray();
        string[] plannerContractMarkers =
        [
            "Musoq.Evaluator.IR.Planning",
            "PlanningResult",
            "PlanProperties",
            "ExecutionStrategyPlan",
            "PlanningDecision",
            "PlanningPropertyDeriver",
            "QueryPlanner",
            "PhysicalPlanningPipeline",
            "PhysicalStrategyPlanner",
            "ExecutionStrategyPlanner"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, rendererFiles, plannerContractMarkers);

        Assert.IsEmpty(
            offenders,
            "Execution C# renderers must consume Execution IR only, not planner contracts: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_ExecutionRendering_ShouldRouteFocusedDomainsThroughCollaborators()
    {
        var repositoryRoot = FindRepositoryRoot();
        var renderingDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/Rendering");
        string[] expectedRendererFiles =
        [
            "ExecutionCSharpRenderer.ExpressionRenderer.cs",
            "ExecutionCSharpRenderer.TableControlFlowRenderer.cs",
            "ExecutionCSharpRenderer.AggregateRenderer.cs",
            "ExecutionCSharpRenderer.JoinRenderer.cs",
            "ExecutionCSharpRenderer.WindowRenderer.cs"
        ];

        var missing = expectedRendererFiles
            .Select(file => Path.Combine(renderingDirectory, file))
            .Where(file => !File.Exists(file))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();

        Assert.IsEmpty(missing, "Focused rendering collaborators should stay under IR/Execution/Rendering: " + string.Join(", ", missing));

        var nodeDispatchText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.NodeDispatch.cs"));
        var expressionDispatchText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Expressions.Dispatch.cs"));

        Assert.Contains("TableControlFlowRenderer", nodeDispatchText);
        Assert.Contains("AggregateRenderer", nodeDispatchText);
        Assert.Contains("JoinRenderer", nodeDispatchText);
        Assert.Contains("WindowRenderer", nodeDispatchText);
        Assert.Contains("ExpressionRenderer", expressionDispatchText);
    }

    [TestMethod]
    public void FocusedArchitecture_RenderingCollaborators_ShouldNotInvokeLoweringOrPlanning()
    {
        var repositoryRoot = FindRepositoryRoot();
        var rendererFiles = Directory
            .EnumerateFiles(
                ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/Rendering"),
                "*.cs")
            .ToArray();
        string[] decisionMarkers =
        [
            "Musoq.Evaluator.IR.Planning",
            "QueryPlanner",
            "PhysicalPlanningPipeline",
            "ExecutionStrategyPlanner",
            "PhysicalToExecutionPlanBuilder(",
            "BuildWithExecutionStrategies(",
            "BuildPlanTable("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, rendererFiles, decisionMarkers);

        Assert.IsEmpty(
            offenders,
            "Rendering collaborators must emit Execution IR only, without calling planning or lowering decision surfaces: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_PhysicalToExecutionLowerer_ShouldNotInvokePlanningSelectionApis()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution");
        var lowererFiles = Directory
            .EnumerateFiles(executionDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                Path.GetFileName(file).StartsWith("PhysicalLoweringImplementation", StringComparison.Ordinal) ||
                Path.GetRelativePath(executionDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '/')
                    .StartsWith("Lowering/", StringComparison.Ordinal))
            .ToArray();
        string[] planningSelectionMarkers =
        [
            "PhysicalStrategyPlanner.Plan(",
            "ExecutionStrategyPlanner.Plan(",
            "SubqueryLoweringStrategyPlanner.Plan(",
            "BoundaryRowShapePlanner.Plan(",
            "RequiredColumnBoundaryPlanner.Plan(",
            "RowWidthPruningPlanner.Plan(",
            "CardinalityFactPlanner.Plan(",
            "MaterializationPlanner.Plan(",
            "PredicateMovementPlanner.Plan(",
            "PredicatePlacementPlanner.Plan("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, lowererFiles, planningSelectionMarkers);

        Assert.IsEmpty(
            offenders,
            "Physical-to-Execution lowering must consume planner decisions instead of invoking selection/planning APIs: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_ChunkSpecificExecutionIrTypes_ShouldNotReturn()
    {
        var repositoryRoot = FindRepositoryRoot();
        var executionDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution");
        var files = Directory
            .EnumerateFiles(executionDirectory, "*.cs", SearchOption.AllDirectories)
            .ToArray();
        string[] forbiddenMarkers =
        [
            "ExecutionChunkedRows",
            "ExecutionChunkedForEach",
            "ExecutionChunkedForEachWithOrdinality",
            "ExecutionMaterializeChunkedList",
            "ExecutionMaterializeFilteredChunkedList",
            "ExecutionMaterializeChunkedExpandoList"
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, files, forbiddenMarkers);
        var typeOffenders = typeof(ExecutionNode).Assembly
            .GetTypes()
            .Where(static type => type.Namespace == typeof(ExecutionNode).Namespace)
            .Where(static type => type.Name.StartsWith("ExecutionChunked", StringComparison.Ordinal))
            .Select(static type => type.FullName)
            .ToArray();

        Assert.IsEmpty(offenders, "Execution IR must use ExecutionRowStreamKind instead of chunk-specific node/expression types: " + string.Join(", ", offenders));
        Assert.IsEmpty(typeOffenders, "Execution IR chunk-specific runtime types must not return: " + string.Join(", ", typeOffenders));
    }

    [TestMethod]
    public void FocusedArchitecture_PerformanceHarnesses_ShouldNotUseAccidentalGiantSourceChunks()
    {
        var repositoryRoot = FindRepositoryRoot();
        var harnessDirectories = new[]
        {
            ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Benchmarks"),
            ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Playground")
        };
        var files = harnessDirectories
            .Where(Directory.Exists)
            .SelectMany(static directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Where(static file => !string.Equals(Path.GetFileName(file), "BenchmarkSourceChunks.cs", StringComparison.Ordinal))
            .ToArray();

        var offenders = files
            .SelectMany(file => File.ReadLines(file).Select((line, index) => new
            {
                File = file,
                Line = line,
                LineNumber = index + 1
            }))
            .Where(static item =>
                item.Line.Contains("Chunks => [", StringComparison.Ordinal) ||
                Regex.IsMatch(item.Line, @"new EntitySource<[^>]+>\(\s*\["))
            .Select(item => $"{Path.GetRelativePath(repositoryRoot, item.File).Replace(Path.DirectorySeparatorChar, '/')}:{item.LineNumber}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Benchmark-like source providers must use fixed-size BenchmarkSourceChunks or RowChunk views; only explicit SingleGiant cases belong in BenchmarkSourceChunks: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_PhysicalToExecutionLowering_ShouldRouteFocusedDomainsThroughCoordinators()
    {
        var repositoryRoot = FindRepositoryRoot();
        var loweringAssembly = typeof(PhysicalToExecutionPlanBuilder).Assembly;
        var coordinatorTypes = new[]
        {
            loweringAssembly.GetType("Musoq.Evaluator.IR.Execution.Lowering.Coordinators.AggregatePlanLowerer"),
            loweringAssembly.GetType("Musoq.Evaluator.IR.Execution.Lowering.WindowPlanLowerer"),
            loweringAssembly.GetType("Musoq.Evaluator.IR.Execution.Lowering.JoinPlanLowerer"),
            loweringAssembly.GetType("Musoq.Evaluator.IR.Execution.Lowering.CtePlanLowerer"),
            loweringAssembly.GetType("Musoq.Evaluator.IR.Execution.Lowering.PipelinePlanLowerer")
        };

        Assert.IsFalse(coordinatorTypes.Any(static type => type == null), "All focused lowerer coordinators must be top-level types.");
        Assert.IsFalse(coordinatorTypes.Any(static type => type!.IsNested), "Focused lowerer coordinators must not be nested in the builder.");

        var planDispatchText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.cs"));
        var tableDispatchText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.TableDispatch.cs"));
        var registryDispatchText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.DispatchRegistry.cs"));
        var joinText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.Joins.cs"));

        Assert.Contains("_physicalLoweringFacade.BuildPlan", planDispatchText);
        Assert.Contains("_physicalLoweringFacade.BuildTable", tableDispatchText);
        Assert.Contains("CreateAggregatePlanLowerer", registryDispatchText);
        Assert.Contains("CreateWindowPlanLowerer", registryDispatchText);
        Assert.Contains("CreateJoinPlanLowerer", registryDispatchText);
        Assert.Contains("CreateCtePlanLowerer", registryDispatchText);
        Assert.Contains("CreatePipelinePlanLowerer", registryDispatchText);
        Assert.Contains("_joinPlanLowerer", joinText);

        var dependencyDirectory = ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering/CoordinatorDependencies");
        Assert.IsFalse(
            Directory.Exists(dependencyDirectory) && Directory.EnumerateFiles(dependencyDirectory, "*.cs").Any(),
            "Lowering coordinators must receive typed handler records, not builder dependency partials.");
    }

    [TestMethod]
    public void FocusedArchitecture_PlanningStages_ShouldUseTypedArtifactHandoffs()
    {
        var repositoryRoot = FindRepositoryRoot();
        var planningTypesText = string.Concat(
            Directory
                .EnumerateFiles(
                    ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Planning/Types/Common"),
                    "*.cs")
                .Select(File.ReadAllText));
        var queryPlannerText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Planning/QueryPlanner.cs"));
        var physicalPipelineText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Planning/Physical/PhysicalPlanningPipeline.cs"));
        var executionIrText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Converter/Build/TransformTree.ExecutionIr.cs"));

        Assert.Contains("internal sealed record LogicalPlanningArtifacts", planningTypesText);
        Assert.Contains("internal sealed record PhysicalPlanningArtifacts", planningTypesText);
        Assert.Contains("internal sealed record ExecutionPlanningArtifacts", planningTypesText);
        Assert.Contains("new PhysicalPlanningArtifacts(", physicalPipelineText);
        Assert.Contains("var physicalPlanningArtifacts = physicalPlanningResult.Artifacts;", queryPlannerText);
        Assert.Contains("new ExecutionPlanningArtifacts(", queryPlannerText);
        Assert.Contains("PlanningResult?.ExecutionArtifacts", executionIrText);
        Assert.IsFalse(
            executionIrText.Contains("PlanningResult?.ExecutionStrategies", StringComparison.Ordinal),
            "Execution lowering should receive the typed execution artifact handoff, not a loose strategy property.");
    }

    [TestMethod]
    public void FocusedArchitecture_PhysicalPlanBuilder_ShouldNotInvokeStrategyPlanner()
    {
        var repositoryRoot = FindRepositoryRoot();
        var builderFiles = Directory
            .EnumerateFiles(
                ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Physical"),
                "PhysicalPlanBuilder*.cs",
                SearchOption.TopDirectoryOnly)
            .ToArray();

        var offenders = FindFilesContainingAny(repositoryRoot, builderFiles, ["PhysicalStrategyPlanner.Plan("]);

        Assert.IsEmpty(
            offenders,
            "PhysicalPlanBuilder must consume explicit physical strategy metadata, not invoke strategy planning: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_SemanticVisitor_ShouldKeepFacadeAndDelegateToServices()
    {
        var repositoryRoot = FindRepositoryRoot();
        var visitorsDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/Visitors");
        string[] expectedServiceFiles =
        [
            "SemanticAnalysisState.cs",
            "SemanticSourceBindingService.cs",
            "SemanticMethodBindingService.cs",
            "Semantics/SemanticResultShapeBindingService.cs",
            "Semantics/SemanticQueryValidationService.cs",
            "Semantics/SemanticDiagnosticReporter.cs",
            "Semantics/SemanticExpressionDiagnosticFacts.cs",
            "Semantics/SemanticExpressionBindingService.cs",
            "Semantics/SemanticColumnPropertyBindingService.cs",
            "Semantics/SemanticSetOperatorFactService.cs"
        ];

        var missing = expectedServiceFiles
            .Select(file => Path.Combine(visitorsDirectory, file))
            .Where(file => !File.Exists(file))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();

        Assert.IsEmpty(missing, "Semantic visitor services should stay inside Musoq.Evaluator.Visitors: " + string.Join(", ", missing));

        var visitorText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.cs"));
        var servicesText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.SemanticVisitorServices.cs"));
        var stateFacadeText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors/SemanticVisitorState.cs"));
        var validationText = File.ReadAllText(ToAbsolutePath(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.QueryValidation.cs"));

        Assert.Contains("SemanticAnalysisState _semanticState", stateFacadeText);
        Assert.DoesNotContain("private sealed record", stateFacadeText);
        Assert.Contains("new SemanticSourceBindingService", visitorText);
        Assert.Contains("new SemanticColumnPropertyBindingService", visitorText);
        Assert.Contains("new SemanticExpressionBindingService", visitorText);
        Assert.Contains("new SemanticMethodBindingService", visitorText);
        Assert.Contains("new SemanticResultShapeBindingService", visitorText);
        Assert.Contains("new SemanticQueryValidationService", visitorText);
        Assert.Contains("new SemanticDiagnosticReporter", visitorText);
        Assert.Contains("_sourceBindingService", servicesText);
        Assert.Contains("_columnPropertyBindingService", servicesText);
        Assert.Contains("_expressionBindingService", servicesText);
        Assert.Contains("_methodBindingService", servicesText);
        Assert.Contains("_resultShapeBindingService", servicesText);
        Assert.Contains("_queryValidationService.ValidateExpressionIsBoolean", validationText);
    }

    [TestMethod]
    public void FocusedArchitecture_SemanticServices_ShouldNotReturnAsVisitorPrivateNestedTypes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var visitorsDirectory = ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Evaluator/Visitors");
        var legacyServiceFiles = Directory
            .EnumerateFiles(visitorsDirectory, "BuildMetadataAndInferTypesVisitor.Semantic*.cs", SearchOption.TopDirectoryOnly)
            .Where(file => !Path.GetFileName(file).Equals(
                "BuildMetadataAndInferTypesVisitor.SemanticVisitorServices.cs",
                StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .ToArray();
        var nestedSemanticTypes = typeof(BuildMetadataAndInferTypesVisitor)
            .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public)
            .Where(static type => type.Name.StartsWith("Semantic", StringComparison.Ordinal))
            .Select(static type => type.Name)
            .ToArray();

        Assert.IsEmpty(
            legacyServiceFiles,
            "Semantic services must stay as directly testable internal services, not visitor partial artifacts: " +
            string.Join(", ", legacyServiceFiles));
        Assert.IsEmpty(
            nestedSemanticTypes,
            "BuildMetadataAndInferTypesVisitor should remain a traversal/delegation facade, not own private semantic services: " +
            string.Join(", ", nestedSemanticTypes));
    }

    [TestMethod]
    public void FocusedArchitecture_OptimizerPassImplementations_ShouldLiveInOptimizationNamespaceAndFolder()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = EnumerateProductionSourceFiles(repositoryRoot)
            .Select(file => new
            {
                RelativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                Text = File.ReadAllText(file)
            })
            .Where(file => file.Text.Contains("IPlanOptimizationPass<", StringComparison.Ordinal))
            .Where(file =>
                !file.RelativePath.StartsWith(
                    "src/dotnet/Musoq.Evaluator/IR/Optimization/",
                    StringComparison.Ordinal) &&
                !file.RelativePath.StartsWith(
                    "src/dotnet/Musoq.Targets.CSharpClr/Optimization/Codegen/",
                    StringComparison.Ordinal))
            .Select(file => file.RelativePath)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Target-neutral optimizer pass implementations must stay under evaluator IR/Optimization; generated C# readability passes belong under the CSharpClr target package: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_ConverterPlanningAndLoweringOrchestration_ShouldStayInTransformTree()
    {
        var repositoryRoot = FindRepositoryRoot();
        var converterFiles = Directory
            .EnumerateFiles(
                ToAbsolutePath(repositoryRoot, "src/dotnet/Musoq.Converter"),
                "*.cs",
                SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).StartsWith("TransformTree", StringComparison.Ordinal))
            .ToArray();
        string[] orchestrationMarkers =
        [
            "new LogicalPlanBuilder(",
            "new QueryPlanner(",
            "new PhysicalToExecutionPlanBuilder(",
            "new ExecutionIrOptimizer(",
            "BuildPlans(",
            "BuildExecutionInspection(",
            "BuildWithIrRenderer("
        ];

        var offenders = FindFilesContainingAny(repositoryRoot, converterFiles, orchestrationMarkers);

        Assert.IsEmpty(
            offenders,
            "Converter planning and execution lowering orchestration must stay in TransformTree: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void FocusedArchitecture_CoreFamilyTotals_ShouldStayAtOrBelowFocusedBaseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        SourceFamilyTotalBudget[] focusedBudgets =
        [
            new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution", "declares:ExecutionCSharpRenderer", 27957),
            new("src/dotnet/Musoq.Evaluator/IR/Execution", "PhysicalLoweringImplementation*.cs", 19778),
            new("src/dotnet/Musoq.Evaluator/Visitors", "BuildMetadataAndInferTypesVisitor*.cs", 9333),
            new("src/dotnet/Musoq.Evaluator/IR/Execution/Lowering", "*.cs", 594),
            new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/Rendering", "*.cs", 220),
            new("src/dotnet/Musoq.Evaluator/Visitors", "Semantic*.cs", 502),
            new("src/dotnet/Musoq.Parser/Traversal", "*.cs", 188)
        ];

        var offenders = focusedBudgets
            .Select(budget =>
            {
                var directory = ToAbsolutePath(repositoryRoot, budget.RelativeDirectory);
                var lineCount = EnumerateSourceFamilyFiles(directory, budget.SearchPattern)
                    .Sum(CountBudgetedLines);

                return new SourceFileBudget(
                    $"{budget.RelativeDirectory}/{budget.SearchPattern}",
                    lineCount,
                    budget.MaxTotalLines);
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Focused architecture hotspot families should split before their total line count grows: " +
            string.Join(", ", offenders));
    }
}
