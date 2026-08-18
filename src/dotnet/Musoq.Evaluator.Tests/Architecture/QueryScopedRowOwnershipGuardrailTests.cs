using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class QueryScopedRowOwnershipGuardrailTests
{
    [TestMethod]
    public void SourceTransferStrategy_ShouldBeOwnedByPlannerAndCarriedByExecutionIr()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var planner = ReadSource(root, "src/dotnet/Musoq.Evaluator/IR/Planning/SourceTransfer/SourceTransferPlanner.cs");
        var transfer = ReadSource(root, "src/dotnet/Musoq.Evaluator/IR/Execution/Nodes/Sources/ExecutionQueryRowSourceTransfer.cs");

        StringAssert.Contains(planner, "SourceTransferStrategyPlan");
        Assert.IsFalse(
            planner.Contains("ExecutionCSharpRenderer", StringComparison.Ordinal),
            "The planner must not depend on a target renderer.");
        StringAssert.Contains(transfer, "ShapeFingerprint");
        StringAssert.Contains(transfer, "FromPlanner");
        StringAssert.Contains(transfer, "ExecutionQueryRowSourceTransfer");
    }

    [TestMethod]
    public void QueryScopedRenderer_ShouldConsumeSelectedTransferWithoutRediscovery()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var renderer = ReadSource(
            root,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Sources.SchemaDeclarations.cs");

        StringAssert.Contains(renderer, "QueryRowSourceTransfer");
        Assert.IsFalse(
            renderer.Contains("SourceTransferPlanner", StringComparison.Ordinal),
            "The renderer must consume the selected IR transfer instead of rerunning planning.");
        Assert.IsFalse(
            renderer.Contains("TransferCapabilities", StringComparison.Ordinal),
            "The renderer must not rediscover provider capabilities.");
    }

    private static string ReadSource(string root, string relativePath)
    {
        return File.ReadAllText(Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
