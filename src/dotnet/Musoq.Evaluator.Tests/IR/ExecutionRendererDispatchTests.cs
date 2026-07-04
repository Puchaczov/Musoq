using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionRendererDispatchTests
{
    [TestMethod]
    public void GetFamily_WhenTableControlFlowNode_ShouldReturnTableControlFlow()
    {
        var family = ExecutionNodeRegistry.GetRendererFamily(new ExecutionContinue());

        Assert.AreEqual(ExecutionRendererNodeFamily.TableControlFlow, family);
    }

    [TestMethod]
    public void GetFamily_WhenCteIndexStorageNode_ShouldReturnTableControlFlow()
    {
        var node = new ExecutionStoreCteIndex(
            new ExecutionVariable("cteIndex", typeof(object)),
            0,
            ExecutionCteSidecarIndexKind.KeySet,
            typeof(int));

        var family = ExecutionNodeRegistry.GetRendererFamily(node);

        Assert.AreEqual(ExecutionRendererNodeFamily.TableControlFlow, family);
    }

    [TestMethod]
    public void GetFamily_WhenAggregateNode_ShouldReturnAggregate()
    {
        var node = new ExecutionCreateAggregateLibrary(new ExecutionVariable("library", typeof(object)), typeof(object));

        var family = ExecutionNodeRegistry.GetRendererFamily(node);

        Assert.AreEqual(ExecutionRendererNodeFamily.Aggregate, family);
    }

    [TestMethod]
    public void GetFamily_WhenJoinNode_ShouldReturnJoin()
    {
        var node = new ExecutionCreateHashPayload(
            new ExecutionVariable("payload", typeof(object), "PayloadRow"),
            new HashPayloadShape("PayloadRow", []),
            []);

        var family = ExecutionNodeRegistry.GetRendererFamily(node);

        Assert.AreEqual(ExecutionRendererNodeFamily.Join, family);
    }

    [TestMethod]
    public void GetFamily_WhenWindowNode_ShouldReturnWindow()
    {
        var node = new ExecutionWindowKernelPlan(
            "window:empty",
            ExecutionWindowKernelPlanStrategy.NoPartition,
            []);

        var family = ExecutionNodeRegistry.GetRendererFamily(node);

        Assert.AreEqual(ExecutionRendererNodeFamily.Window, family);
    }

    [TestMethod]
    public void GetFamily_WhenIndexNodes_ShouldReturnIndex()
    {
        var nodes = new ExecutionNode[]
        {
            new ExecutionHashProbe(
                new ExecutionVariable("hash", typeof(object)),
                new ExecutionVariable("matches", typeof(object)),
                new ExecutionLiteral(1, typeof(int)),
                typeof(int),
                typeof(object),
                new ExecutionBlock([new ExecutionContinue()])),
            new ExecutionCreateKeySet(new ExecutionVariable("keys", typeof(object)), typeof(int))
        };

        foreach (var node in nodes)
            Assert.AreEqual(ExecutionRendererNodeFamily.Index, ExecutionNodeRegistry.GetRendererFamily(node));
    }

    [TestMethod]
    public void GetFamily_WhenUnsupportedNode_ShouldReturnUnsupported()
    {
        var family = ExecutionNodeRegistry.GetRendererFamily(new UnsupportedExecutionNode());

        Assert.AreEqual(ExecutionRendererNodeFamily.Unsupported, family);
    }

    [TestMethod]
    public void Registry_WhenRenderableNodesAreRegistered_ShouldExposeDescriptors()
    {
        var registeredTypes = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType)
            .ToHashSet();

        Assert.Contains(typeof(ExecutionContinue), registeredTypes);
        Assert.Contains(typeof(ExecutionCreateAggregateLibrary), registeredTypes);
        Assert.Contains(typeof(ExecutionCreateHashPayload), registeredTypes);
        Assert.Contains(typeof(ExecutionWindowKernelPlan), registeredTypes);
        Assert.Contains(typeof(ExecutionCreateKeySet), registeredTypes);
    }

    [TestMethod]
    public void Registry_WhenBlockOwningNodesAreRegistered_ShouldExposeChildBlockShape()
    {
        AssertDescriptorChildShape<ExecutionForEach>(ExecutionNodeChildBlockShape.Single);
        AssertDescriptorChildShape<ExecutionParallelBlock>(ExecutionNodeChildBlockShape.Multiple);
        AssertDescriptorChildShape<ExecutionHashProbe>(ExecutionNodeChildBlockShape.Multiple);
        AssertDescriptorChildShape<ExecutionContinue>(ExecutionNodeChildBlockShape.None);
    }

    [TestMethod]
    public void Registry_WhenNodesOwnChildren_ShouldExposeTraversalBlocks()
    {
        var loopBody = new ExecutionBlock([new ExecutionBreak()]);
        var taskBody = new ExecutionBlock([new ExecutionContinue()]);
        var mergeBody = new ExecutionBlock([new ExecutionBreak()]);
        var probeBody = new ExecutionBlock([new ExecutionContinue()]);
        var noMatchBody = new ExecutionBlock([new ExecutionBreak()]);
        var kernel = new ExecutionContinue();

        var loopBlocks = ExecutionNodeRegistry.GetChildBlocks(
            new ExecutionForEach(
                new ExecutionVariable("item", typeof(object)),
                new ExecutionStoredTableRows(0),
                loopBody));
        var parallelBlocks = ExecutionNodeRegistry.GetChildBlocks(
            new ExecutionParallelBlock(
                "parallel",
                2,
                [new ExecutionParallelTask("task", new ExecutionVariable("output", typeof(object)), taskBody)],
                new ExecutionParallelMerge(mergeBody)));
        var probeBlocks = ExecutionNodeRegistry.GetChildBlocks(
            new ExecutionHashProbe(
                new ExecutionVariable("hash", typeof(object)),
                new ExecutionVariable("matches", typeof(object)),
                new ExecutionLiteral(1, typeof(int)),
                typeof(int),
                typeof(object),
                probeBody,
                noMatchBody));
        var windowBlocks = ExecutionNodeRegistry.GetChildBlocks(
            new ExecutionWindowKernelPlan(
                "window:one",
                ExecutionWindowKernelPlanStrategy.NoPartition,
                [kernel]));

        Assert.AreSame(loopBody, loopBlocks.Single());
        CollectionAssert.AreEqual(new[] { taskBody, mergeBody }, parallelBlocks.ToArray());
        CollectionAssert.AreEqual(new[] { probeBody, noMatchBody }, probeBlocks.ToArray());
        Assert.AreSame(kernel, windowBlocks.Single().Nodes.Single());
    }

    [TestMethod]
    public void Registry_WhenOptimizationCandidateIsRegistered_ShouldMarkUnsupportedRendererButKeepTraversal()
    {
        var body = new ExecutionBlock([new ExecutionContinue()]);
        var candidate = new ExecutionSingleUsePipelineFusionCandidate(1, body);

        var descriptor = ExecutionNodeRegistry.Descriptors.Single(item => item.NodeType == typeof(ExecutionSingleUsePipelineFusionCandidate));

        Assert.AreEqual(ExecutionRendererNodeFamily.Unsupported, descriptor.RendererFamily);
        Assert.IsFalse(ExecutionCSharpRenderer.CanRenderNode(candidate));
        Assert.AreSame(body, ExecutionNodeRegistry.GetChildBlocks(candidate).Single());
    }

    [TestMethod]
    public void CanRenderNode_WhenRenderableNodesAreProvided_ShouldReturnTrue()
    {
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderNode(new ExecutionContinue()));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderNode(
            new ExecutionCreateAggregateLibrary(new ExecutionVariable("library", typeof(object)), typeof(object))));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderNode(
            new ExecutionCreateKeySet(new ExecutionVariable("keys", typeof(object)), typeof(int))));
        Assert.IsTrue(ExecutionCSharpRenderer.CanRenderNode(
            new ExecutionWindowKernelPlan("window:empty", ExecutionWindowKernelPlanStrategy.NoPartition, [])));
    }

    [TestMethod]
    public void CanRenderNode_WhenUnsupportedOrInvalidNodesAreProvided_ShouldReturnFalse()
    {
        Assert.IsFalse(ExecutionCSharpRenderer.CanRenderNode(new UnsupportedExecutionNode()));
        Assert.IsFalse(ExecutionCSharpRenderer.CanRenderNode(
            new ExecutionLoadCteIndex(
                new ExecutionVariable("hashIndex", typeof(object)),
                0,
                ExecutionCteSidecarIndexKind.Hash,
                typeof(int))));
    }

    private sealed record UnsupportedExecutionNode : ExecutionNode;

    private static void AssertDescriptorChildShape<TNode>(ExecutionNodeChildBlockShape expected)
        where TNode : ExecutionNode
    {
        var descriptor = ExecutionNodeRegistry.Descriptors.Single(item => item.NodeType == typeof(TNode));

        Assert.AreEqual(expected, descriptor.ChildBlockShape);
    }
}
