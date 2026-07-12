using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureFinalGuardrailTests
{
    private static readonly Regex BuilderPrivateType = new(
        @"private\s+(?:sealed\s+)?(?:readonly\s+)?(?:record|class|enum|delegate)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StaticConcurrentDictionaryField = new(
        @"private\s+static\s+readonly\s+ConcurrentDictionary<(?<key>[^,>]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BroadNotSupportedCatch = new(
        @"\bcatch\s*\(\s*NotSupportedException\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void Renderer_ShouldHaveNoAmbientSessionState()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "ExecutionCSharpRenderer*.cs");

        var offenders = MatchingLines(files, repositoryRoot, line =>
            line.Contains("AsyncLocal<ExecutionRenderSession", StringComparison.Ordinal) ||
            line.Contains("RenderSessionSlot", StringComparison.Ordinal) ||
            line.Contains("private ExecutionRenderSession RenderSession", StringComparison.Ordinal));

        Assert.IsEmpty(offenders, "Renderer mutable render state must flow through explicit ExecutionRenderContext: " + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void ExecutionLowering_ShouldKeepDispatchAndModelsOutOfBuilderPartials()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "PhysicalToExecutionPlanBuilder*.cs");
        var entryText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.cs"));
        var tableText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.TableDispatch.cs"));

        Assert.Contains("CreatePhysicalLoweringRegistry().TryBuildPlan", entryText);
        Assert.Contains("CreatePhysicalLoweringRegistry().TryBuildTable", tableText);

        var privateTypes = MatchingLines(builderFiles, repositoryRoot, line => BuilderPrivateType.IsMatch(line));
        Assert.IsEmpty(privateTypes, "Builder-private lowering models must stay extracted under IR/Execution/Lowering: " + string.Join(Environment.NewLine, privateTypes));
    }

    [TestMethod]
    public void SemanticVisitor_ShouldNotMutateRawStacksOutsideStateAdapters()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var visitorFiles = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/Visitors", "*.cs")
            .Where(file => File.ReadAllText(file).Contains("partial class BuildMetadataAndInferTypesVisitor", StringComparison.Ordinal))
            .Where(file => !file.EndsWith("BuildMetadataAndInferTypesVisitor.SelectAliasBinding.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string[] forbidden =
        [
            "Nodes.Push(", "Nodes.Pop(", "Nodes.Peek(", "SafePop(Nodes", "SafePeek(Nodes", "SafePopMultiple(Nodes",
            "Methods.Push(", "Methods.Pop(", "Methods.Peek(", "SafePop(Methods", "SafePeek(Methods", "SafePopMultiple(Methods"
        ];

        var offenders = MatchingLines(visitorFiles, repositoryRoot, line => forbidden.Any(marker => line.Contains(marker, StringComparison.Ordinal)));
        Assert.IsEmpty(offenders, "Semantic visitor partials must use SemanticAnalysisState/SemanticTraversalFrame APIs: " + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void CodeGeneration_ShouldDependOnRenderArtifactsNotTypedSinkInternals()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var codegenFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration",
            "*.cs");

        string[] forbidden =
        [
            "EnterTypedSinkRendering", "EnterTypedSinkRenderContext", "EnterQueryRunContextRendering", "EnterQueryRunContextRenderContext",
            "RenderExpressionForTypedSink", "CreateTypedSinkEntryStatements", "RenderSourceScanForTypedSink", "RenderSetupNodeForTypedSink",
            "RenderGeneratedRowCreationForTypedSink", "RenderOptionalGeneratedRowProjectionForTypedSink"
        ];

        var offenders = MatchingLines(codegenFiles, repositoryRoot, line => forbidden.Any(marker => line.Contains(marker, StringComparison.Ordinal)));
        Assert.IsEmpty(offenders, "IR.CodeGeneration must consume execution render artifact/final-sink APIs instead of typed-sink internals: " + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void RuntimeCaches_ShouldUseExplicitCacheContractsForUserShapedKeys()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var offenders = MatchingLines(productionFiles, repositoryRoot, line =>
        {
            var match = StaticConcurrentDictionaryField.Match(line);
            return match.Success && !string.Equals(match.Groups["key"].Value.Trim(), "Type", StringComparison.Ordinal);
        });

        Assert.IsEmpty(offenders, "Static runtime caches keyed by user-shaped values must use BoundedRuntimeCache or an explicit cache contract: " + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void UnsupportedShapeFallback_ShouldUseTypedResultsExceptCompatibilityBoundaries()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");
        var allowedFiles = new HashSet<string>(StringComparer.Ordinal)
        {
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.AsOfJoins.cs",
            "src/dotnet/Musoq.Evaluator/IR/Planning/SourcePredicatePlanner.cs"
        };

        var directApplyText = File.ReadAllText(Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "Visitors", "RewriteQueryVisitor.DirectApply.cs"));
        Assert.Contains(".TryConvert(expression)", directApplyText);

        var offenders = MatchingLines(productionFiles, repositoryRoot, line => BroadNotSupportedCatch.IsMatch(line))
            .Where(line => !allowedFiles.Contains(line.Split(':')[0]))
            .ToArray();

        Assert.IsEmpty(offenders, "Unsupported-shape fallback should use typed results or dedicated exceptions outside documented compatibility boundaries: " + string.Join(Environment.NewLine, offenders));
    }

    private static string[] MatchingLines(
        IEnumerable<string> files,
        string repositoryRoot,
        Func<string, bool> predicate)
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
            .Where(item => predicate(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();
    }
}
