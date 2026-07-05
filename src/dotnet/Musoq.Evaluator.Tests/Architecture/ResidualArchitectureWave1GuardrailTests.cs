using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave1GuardrailTests
{
    private const int RendererAmbientSessionMarkerCeiling = 0;
    private const int CodeGenerationRendererInternalCallCeiling = 12;
    private const int StaticStringKeyedRuntimeCacheCeiling = 3;
    private const int OperatorsRegexWithoutTimeoutCeiling = 2;
    private const int BroadNotSupportedFallbackCatchCeiling = 3;

    private static readonly Regex RendererAmbientSessionMarker = new(
        @"\bAsyncLocal<ExecutionRenderSession\?>\b|\bRenderSessionSlot\b|\bprivate\s+ExecutionRenderSession\s+RenderSession\s*=>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex CodeGenerationRendererInternalCall = new(
        @"\b(?:EnterTypedSinkRendering|EnterQueryRunContextRendering|CreateTypedSinkEntryStatements|RenderExpressionForTypedSink|RenderSourceScanForTypedSink|RenderSetupNodeForTypedSink|RenderOptionalGeneratedRowProjectionForTypedSink)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StaticStringKeyedRuntimeCache = new(
        @"\bprivate\s+static\s+readonly\s+ConcurrentDictionary<string,",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex RegexWithoutTimeoutConstruction = new(
        @"\bnew\s+Regex\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BroadNotSupportedFallbackCatch = new(
        @"\bcatch\s*\(\s*NotSupportedException\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void ExecutionRenderer_AmbientAsyncLocalSessionSurface_ShouldBeRemoved()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "ExecutionCSharpRenderer*.cs");

        var markers = FindMatchingLines(repositoryRoot, rendererFiles, RendererAmbientSessionMarker);

        Assert.IsTrue(
            markers.Length <= RendererAmbientSessionMarkerCeiling,
            $"ExecutionCSharpRenderer ambient render-session markers must stay at zero, but found {markers.Length}. " +
            "Renderer session state must flow through explicit ExecutionRenderContext plumbing: " +
            string.Join(Environment.NewLine, markers));
    }

    [TestMethod]
    public void CodeGeneration_ShouldNotGrowCallsIntoExecutionRendererTypedSinkInternals()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var codegenFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/CodeGeneration",
            "CSharpRenderer*.cs");

        var calls = FindMatchingLines(repositoryRoot, codegenFiles, CodeGenerationRendererInternalCall);

        Assert.IsTrue(
            calls.Length <= CodeGenerationRendererInternalCallCeiling,
            $"Code generation direct calls into execution-renderer sink/query-context internals grew to {calls.Length}. " +
            "Wave 14-15 should move this boundary behind execution render artifacts: " +
            string.Join(Environment.NewLine, calls));
    }

    [TestMethod]
    public void Runtime_StaticStringKeyedCaches_ShouldNotGrowBeforeCacheContracts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var caches = FindMatchingLines(repositoryRoot, productionFiles, StaticStringKeyedRuntimeCache);

        Assert.IsTrue(
            caches.Length <= StaticStringKeyedRuntimeCacheCeiling,
            $"Static string-keyed runtime caches grew to {caches.Length}. " +
            "Wave 16-17 should move growable caches behind explicit bounded/type-scoped cache contracts: " +
            string.Join(Environment.NewLine, caches));
    }

    [TestMethod]
    public void Operators_RegexConstructionWithoutTimeout_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var operatorsFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Operators.cs");

        var regexConstructions = FindMatchingLines(
            repositoryRoot,
            [operatorsFile],
            RegexWithoutTimeoutConstruction);

        Assert.IsTrue(
            regexConstructions.Length <= OperatorsRegexWithoutTimeoutCeiling,
            $"Operators regex constructions without an explicit timeout grew to {regexConstructions.Length}. " +
            "Wave 16 should add bounded pattern caches and regex timeouts: " +
            string.Join(Environment.NewLine, regexConstructions));
    }

    [TestMethod]
    public void UnsupportedShapeFallbacks_ShouldNotGrowBroadNotSupportedCatches()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var productionFiles = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var catches = FindMatchingLines(repositoryRoot, productionFiles, BroadNotSupportedFallbackCatch);

        Assert.IsTrue(
            catches.Length <= BroadNotSupportedFallbackCatchCeiling,
            $"Broad NotSupportedException fallback catches grew to {catches.Length}. " +
            "Wave 18 should replace unsupported-shape control flow with typed results or dedicated exceptions: " +
            string.Join(Environment.NewLine, catches));
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
