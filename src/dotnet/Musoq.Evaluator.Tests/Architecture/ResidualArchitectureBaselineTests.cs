using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureBaselineTests
{
    private const int BuilderPrivateLoweringModelDeclarationCeiling = 9;
    private const int ProjectionAndApplyPrivateModelDeclarationCeiling = 0;
    private const int WholeBuilderCoordinatorConstructionCeiling = 0;
    private const int RendererSessionAccessSurfaceCeiling = 0;
    private const int SemanticVisitorStackMutationCeiling = 249;
    private const int ParserTraversalInventoryCeiling = 284;
    private const int ProductionBuildItemsParameterCeiling = 21;

    private static readonly Regex BuilderPrivateModelDeclaration = new(
        @"\bprivate\s+(?:(?:sealed|readonly|static|partial)\s+)*(?:record\s+struct|record|class)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WholeBuilderCoordinatorConstruction = new(
        @"\bnew\s+\w+Coordinator\s*\(\s*this\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WholeBuilderCoordinatorConstructor = new(
        @"\b\w+Coordinator\s*\([^)]*\bPhysicalToExecutionPlanBuilder\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RendererSessionAccess = new(
        @"\b(?:_session|CurrentSession|ResetRenderSession)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SemanticVisitorStackMutation = new(
        @"\b(?:Nodes|Methods|InferredColumns|FieldsNames|UsedColumns|AccessRefreshers|UsedColumnsOrUsedWhere|SetOperatorContexts)\.(?:Push|Pop|Peek|Clear)\b|\bSafePop\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ParserVisitOverride = new(
        @"\bpublic\s+(?:override|virtual)\s+void\s+Visit\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex VisitorPrivateSemanticServiceDeclaration = new(
        @"\bprivate\s+(?:(?:sealed|readonly|static|partial)\s+)*(?:record\s+struct|record|class)\s+Semantic\w+Service\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BuildItemsMethodParameter = new(
        @"\b(?:public|internal|private|protected|static|sealed|partial|async|virtual|override|readonly|extern|new|unsafe|\s)+[\w<>\[\], ?]+\s+\w+\s*\([^)]*\bBuildItems\b[^)]*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void PhysicalToExecutionPlanBuilder_PrivateLoweringModels_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "PhysicalToExecutionPlanBuilder*.cs");

        var declarations = FindMatchingLines(repositoryRoot, builderFiles, BuilderPrivateModelDeclaration);

        Assert.IsTrue(
            declarations.Length <= BuilderPrivateLoweringModelDeclarationCeiling,
            $"Builder-private lowering model declarations grew to {declarations.Length}. " +
            "Move new lowering models under IR/Execution/Lowering and test them directly: " +
            string.Join(Environment.NewLine, declarations));
    }

    [TestMethod]
    public void ProjectionAndApply_PrivateLoweringModels_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "PhysicalToExecutionPlanBuilder.Types.ProjectionAndApply.cs");

        var declarations = FindMatchingLines(repositoryRoot, files, BuilderPrivateModelDeclaration);

        Assert.IsTrue(
            declarations.Length <= ProjectionAndApplyPrivateModelDeclarationCeiling,
            $"Projection/apply private lowering model declarations grew to {declarations.Length}. " +
            "Wave 2 should extract these models before adding new ones: " +
            string.Join(Environment.NewLine, declarations));
    }

    [TestMethod]
    public void LoweringCoordinators_ShouldNotAddWholeBuilderDependencies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "PhysicalToExecutionPlanBuilder*.cs");

        var constructions = FindMatchingLines(repositoryRoot, builderFiles, WholeBuilderCoordinatorConstruction);

        Assert.IsTrue(
            constructions.Length <= WholeBuilderCoordinatorConstructionCeiling,
            $"Whole-builder coordinator constructions grew to {constructions.Length}. " +
            "Coordinators should move toward explicit lowerer dependency bundles: " +
            string.Join(Environment.NewLine, constructions));
    }

    [TestMethod]
    public void LoweringCoordinators_ShouldNotAcceptWholeBuilderDependencies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var loweringFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering",
            "*.cs");

        var constructors = FindMatchingLines(repositoryRoot, loweringFiles, WholeBuilderCoordinatorConstructor);

        Assert.AreEqual(
            0,
            constructors.Length,
            "Lowering coordinators must depend on explicit services/delegates, not the whole PhysicalToExecutionPlanBuilder: " +
            string.Join(Environment.NewLine, constructors));
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_SessionAccessSurface_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "ExecutionCSharpRenderer*.cs");

        var sessionAccess = FindMatchingLines(repositoryRoot, rendererFiles, RendererSessionAccess);

        Assert.IsTrue(
            sessionAccess.Length <= RendererSessionAccessSurfaceCeiling,
            $"Renderer session access surface grew to {sessionAccess.Length}. " +
            "Wave 8+ should pass ExecutionRenderSession explicitly instead of adding instance session reads: " +
            string.Join(Environment.NewLine, sessionAccess.Take(30)));
    }

    [TestMethod]
    public void SemanticVisitor_StackMutationSurface_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var visitorFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors",
            "BuildMetadataAndInferTypesVisitor*.cs");

        var mutations = FindMatchingLines(repositoryRoot, visitorFiles, SemanticVisitorStackMutation);

        Assert.IsTrue(
            mutations.Length <= SemanticVisitorStackMutationCeiling,
            $"Semantic visitor stack mutation surface grew to {mutations.Length}. " +
            "New binding behavior should move into services over SemanticAnalysisState: " +
            string.Join(Environment.NewLine, mutations.Take(40)));
    }

    [TestMethod]
    public void ParserTraversalVisitInventories_ShouldNotGrowBeforeRegistryExtraction()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var traversalFiles = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/Visitors", "*.cs")
            .Where(static file =>
            {
                var name = Path.GetFileName(file);
                return name.Contains("TraverseVisitor", StringComparison.Ordinal) ||
                       name.StartsWith("SubqueryToCteRewriteVisitor", StringComparison.Ordinal);
            })
            .ToArray();

        var visits = FindMatchingLines(repositoryRoot, traversalFiles, ParserVisitOverride);

        Assert.IsTrue(
            visits.Length <= ParserTraversalInventoryCeiling,
            $"Parser traversal Visit inventory grew to {visits.Length}. " +
            "New parser-node traversal should go through the upcoming traversal registry: " +
            string.Join(Environment.NewLine, visits.Take(40)));
    }

    [TestMethod]
    public void ParserChildTraversal_ShouldStayRegistryBacked()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var traversalFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "ParserNodeChildTraversal.cs");
        var text = File.ReadAllText(traversalFile);

        Assert.Contains("ParserNodeTraversalRegistry.EnumerateChildren(node)", text);
        Assert.Contains("ParserNodeTraversalRegistry.EnumerateCteInnerExpressionsThenOuter(node)", text);
        Assert.IsFalse(
            text.Contains(" switch", StringComparison.Ordinal),
            "ParserNodeChildTraversal should stay a registry-backed adapter, not regain a local node inventory.");
    }

    [TestMethod]
    public void SemanticVisitorBindingServices_ShouldStayTopLevelServices()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var serviceFieldFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.SemanticVisitorServices.cs");
        var serviceFieldText = File.ReadAllText(serviceFieldFile);
        string[] expectedServices =
        [
            "SemanticColumnPropertyBindingService",
            "SemanticExpressionBindingService",
            "SemanticMethodBindingService",
            "SemanticQueryValidationService",
            "SemanticResultShapeBindingService",
            "SemanticSourceBindingService"
        ];
        var missing = expectedServices
            .Where(service => !serviceFieldText.Contains(service, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            missing,
            "Semantic binding ownership should remain in named services over SemanticAnalysisState: " +
            string.Join(", ", missing));

        var visitorFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/Visitors",
            "BuildMetadataAndInferTypesVisitor*.cs");
        var privateServices = FindMatchingLines(
            repositoryRoot,
            visitorFiles,
            VisitorPrivateSemanticServiceDeclaration);

        Assert.IsEmpty(
            privateServices,
            "Do not reintroduce visitor-private semantic service/model types; keep semantic services top-level and directly testable: " +
            string.Join(Environment.NewLine, privateServices));
    }

    [TestMethod]
    public void ProductionBuildPipeline_ShouldNotAddBuildItemsMethodParameters()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(
            repositoryRoot,
            "Musoq.Converter",
            "Musoq.Evaluator");

        var parameters = FindMatchingLines(repositoryRoot, productionFiles, BuildItemsMethodParameter);

        Assert.IsTrue(
            parameters.Length <= ProductionBuildItemsParameterCeiling,
            $"Production BuildItems method-parameter surface grew to {parameters.Length}. " +
            "BuildItems should shrink toward public boundary adapters and typed artifact contexts: " +
            string.Join(Environment.NewLine, parameters));
    }

    private static string[] FindMatchingLines(
        string repositoryRoot,
        IEnumerable<string> files,
        Regex pattern)
    {
        return files
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => pattern.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();
    }
}
