using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave3GuardrailTests
{
    [TestMethod]
    public void ExecutionRenderer_RenderBlockEntrypoint_ShouldNotReadAmbientSessionSlotDirectly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var entryPointsFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "ExecutionCSharpRenderer.EntryPoints.cs");
        var text = File.ReadAllText(entryPointsFile);

        Assert.IsFalse(
            text.Contains("RenderSessionSlot", StringComparison.Ordinal),
            "RenderBlock/RenderMethod entrypoint plumbing should create and pass ExecutionRenderContext; direct slot access belongs only to temporary compatibility adapters.");
    }

    [TestMethod]
    public void ExecutionRenderer_NodeDispatch_ShouldPassRenderContextToRendererFamilies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var dispatchFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "ExecutionCSharpRenderer.NodeDispatch.cs");
        var text = File.ReadAllText(dispatchFile);

        Assert.Contains("RenderNode(ExecutionNode node, ExecutionRenderContext context)", text);
        Assert.Contains("new TableControlFlowRenderer(this, context)", text);
        Assert.Contains("new AggregateRenderer(this, context)", text);
        Assert.Contains("new JoinRenderer(this, context)", text);
        Assert.Contains("new WindowRenderer(this, context)", text);
        Assert.IsFalse(
            text.Contains("var session = RenderSession;", StringComparison.Ordinal),
            "Node dispatch should use the explicit ExecutionRenderContext instead of re-reading ambient render session state.");
    }
}
