using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ArchitectureRemediationBaselineTests
{
    private const int OptimizationRootPassDeclarationCeiling = 39;
    private const int PlanPropertiesConstructionCeiling = 1;
    private const int RendererSessionReferenceCeiling = 0;
    private const int LegacyExecutionStrategyConstructorCeiling = 0;

    private static readonly Regex OptimizationPassDeclaration =
        new(@"IPlanOptimizationPass<(?<plan>[^>]+)>", RegexOptions.Compiled);

    private static readonly Regex BareRenderingCollaboratorConstruction =
        new(@"new\s+(?:ExpressionRenderer|TableControlFlowRenderer|AggregateRenderer|JoinRenderer|WindowRenderer)\s*\(\s*this\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PlanPropertiesMethodParameter =
        new(@"\b(?:public|internal|private|protected|static|sealed|partial|async|virtual|override|readonly|extern|new|unsafe|\s)+[\w<>\[\], ?]+\s+\w+\s*\([^)]*\bPlanProperties\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void OptimizationRoot_ShouldNotGrowStagePassInventory()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var optimizationFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Optimization",
            "*.cs");

        var declarations = optimizationFiles
            .SelectMany(file => OptimizationPassDeclaration.Matches(File.ReadAllText(file)))
            .Select(match => match.Groups["plan"].Value)
            .ToArray();

        Assert.IsLessThanOrEqualTo(
            OptimizationRootPassDeclarationCeiling,
            declarations.Length,
            $"Root optimization namespace pass declarations grew to {declarations.Length}. " +
            "New passes should be stage-owned rather than added to the shared optimization root.");
    }

    [TestMethod]
    public void PlanProperties_DirectConstruction_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator")
            .Concat(RepositorySourceScan.FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator.Tests",
                "*.cs"))
            .Where(static file => !Path.GetFileName(file).Equals(
                nameof(ArchitectureRemediationBaselineTests) + ".cs",
                StringComparison.Ordinal))
            .ToArray();
        var constructions = files
            .Sum(file => File.ReadLines(file)
                .Count(static line => line.Contains("new PlanProperties(", StringComparison.Ordinal)));

        Assert.IsLessThanOrEqualTo(
            PlanPropertiesConstructionCeiling,
            constructions,
            $"Direct PlanProperties construction grew to {constructions}. Use staged planning facts or test builders.");
    }

    [TestMethod]
    public void ExecutionStrategyPlan_LegacyNodeKeyedConstructor_ShouldNotGrow()
    {
        var legacyConstructors = typeof(ExecutionStrategyPlan)
            .GetConstructors()
            .Where(static constructor => constructor
                .GetParameters()
                .Any(static parameter => parameter.ParameterType
                    .GenericTypeArguments
                    .Contains(typeof(PhysicalSingleKeyAggregateNode))))
            .ToArray();

        Assert.AreEqual(
            LegacyExecutionStrategyConstructorCeiling,
            legacyConstructors.Length,
            "ExecutionStrategyPlan must not expose physical-node-keyed compatibility constructors.");
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_MutableSessionReferences_ShouldNotGrow()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "ExecutionCSharpRenderer*.cs");
        var sessionReferences = rendererFiles
            .Sum(file => File.ReadLines(file)
                .Count(static line => line.Contains("_session", StringComparison.Ordinal)));

        Assert.IsLessThanOrEqualTo(
            RendererSessionReferenceCeiling,
            sessionReferences,
            $"ExecutionCSharpRenderer mutable session references grew to {sessionReferences}. " +
            "New renderer state should move toward explicit ExecutionRenderSession ownership.");
    }

    [TestMethod]
    public void ExecutionRenderSession_ShouldBeInternalTopLevelType()
    {
        var rendererType = typeof(ExecutionCSharpRenderer);
        var sessionType = rendererType.Assembly.GetType("Musoq.Evaluator.IR.Execution.ExecutionRenderSession");
        var nestedSessionType = rendererType.GetNestedType("ExecutionRenderSession", BindingFlags.NonPublic);

        Assert.IsNotNull(sessionType, "ExecutionRenderSession should be a named internal session contract.");
        Assert.IsFalse(sessionType!.IsNested, "ExecutionRenderSession should not remain a private nested renderer class.");
        Assert.IsFalse(sessionType.IsPublic, "ExecutionRenderSession is an internal execution rendering contract.");
        Assert.IsNull(nestedSessionType, "ExecutionCSharpRenderer should not own the session type definition.");
    }

    [TestMethod]
    public void ExecutionRenderOptions_ShouldBeInternalTopLevelType()
    {
        var rendererType = typeof(ExecutionCSharpRenderer);
        var optionsType = rendererType.Assembly.GetType("Musoq.Evaluator.IR.Execution.ExecutionRenderOptions");

        Assert.IsNotNull(optionsType, "Constructor-provided render context should live in a named options type.");
        Assert.IsFalse(optionsType!.IsNested, "ExecutionRenderOptions should not be renderer-private nested state.");
        Assert.IsFalse(optionsType.IsPublic, "ExecutionRenderOptions is an internal execution rendering contract.");
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_MutableInstanceFields_ShouldStaySessionOwned()
    {
        var mutableFields = typeof(ExecutionCSharpRenderer)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(static field => !field.IsInitOnly && !field.IsLiteral)
            .ToArray();

        Assert.AreEqual(
            0,
            mutableFields.Length,
            "ExecutionCSharpRenderer mutable render state should be owned by per-render ExecutionRenderSession instances.");
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_SessionProxyProperties_ShouldNotReturn()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFile = Path.Combine(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionCSharpRenderer.cs");
        var text = File.ReadAllText(rendererFile);

        Assert.IsFalse(
            Regex.IsMatch(text, @"private\s+.+\s+_\w+\s*(?:\{|=>)\s*CurrentSession\.", RegexOptions.Multiline),
            "Session-backed renderer state should be accessed through ExecutionRenderSession, not proxy properties.");
    }

    [TestMethod]
    public void ExecutionCSharpRenderer_RenderingCollaborators_ShouldReceiveSessionExplicitly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "ExecutionCSharpRenderer*.cs");

        var offenders = rendererFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => BareRenderingCollaboratorConstruction.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Focused rendering collaborators must receive the active ExecutionRenderSession instead of reusing a renderer-only constructor: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void PlanningInternals_ShouldNotAcceptPlanPropertiesOutsideCompatibilityAdapters()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var planningFiles = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Planning", "*.cs")
            .Where(file =>
            {
                var relative = RepositorySourceScan.ToRelative(repositoryRoot, file).Replace('\\', '/');
                return !relative.Contains("/IR/Planning/Types/", StringComparison.Ordinal) &&
                       !relative.Contains("/IR/Planning/Diagnostics/", StringComparison.Ordinal) &&
                       !relative.Contains("/IR/Planning/Printing/", StringComparison.Ordinal);
            })
            .ToArray();

        var offenders = planningFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => PlanPropertiesMethodParameter.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Planning internals should consume PlanningFacts/components; PlanProperties method parameters belong only at compatibility adapters: " +
            string.Join(Environment.NewLine, offenders));
    }
}
