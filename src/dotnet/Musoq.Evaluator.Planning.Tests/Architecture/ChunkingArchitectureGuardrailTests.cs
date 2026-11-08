using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ChunkingArchitectureGuardrailTests
{
    [TestMethod]
    public void ExecutionIr_ShouldNotReintroduceChunkSpecificNodeTypes()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.ProductionSourceFiles(root, "Musoq.Evaluator");
        var offenders = files
            .Where(static file => File.ReadAllText(file).Contains("ExecutionChunked", StringComparison.Ordinal))
            .Select(file => RepositorySourceScan.ToRelative(root, file))
            .ToArray();

        Assert.IsEmpty(offenders, $"Chunk-specific execution IR types are forbidden: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void ExpressionRenderer_ShouldNotInferTableRowsFromTableShapeMap()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Rendering",
            "Execution",
            "Rendering",
            "ExecutionCSharpRenderer.ExpressionRenderer.cs");
        var content = File.ReadAllText(file);

        Assert.IsFalse(
            content.Contains("_tableRowShapesByVariableName.ContainsKey(rows.Variable.Name)", StringComparison.Ordinal),
            "ExecutionRowStream must carry explicit table-row access metadata; renderer-side variable-name inference is forbidden.");
    }

    [TestMethod]
    public void TableControlFlowRenderer_ShouldNotBranchOnChunkedRowStreams()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Targets.CSharpClr",
            "Rendering",
            "Execution",
            "Rendering",
            "ExecutionCSharpRenderer.TableControlFlowRenderer.cs");
        var content = File.ReadAllText(file);

        Assert.IsFalse(
            content.Contains("ExecutionRowStreams.IsChunked", StringComparison.Ordinal),
            "Chunk-vs-row rendering strategy must stay centralized outside the table control-flow switch.");
    }
}
