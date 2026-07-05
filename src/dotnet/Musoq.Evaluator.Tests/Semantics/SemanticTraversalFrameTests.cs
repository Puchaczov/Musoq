using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Semantics;

[TestClass]
public sealed class SemanticTraversalFrameTests
{
    [TestMethod]
    public void SemanticAnalysisState_ShouldExposePrivateStacksThroughTraversalFrame()
    {
        var state = new SemanticAnalysisState();
        var node = new IdentifierNode("Name");

        state.Traversal.PushNode(node);
        state.Traversal.PushMethod("items");

        Assert.AreSame(node, state.Traversal.PeekNode("visitor", "peek-node"));
        Assert.AreEqual(1, state.Traversal.NodeCount);
        Assert.AreEqual(1, state.Traversal.MethodCount);
        Assert.AreEqual("items", state.Traversal.PopMethod("visitor", "pop-method"));
        Assert.AreEqual(0, state.Traversal.MethodCount);
    }

    [TestMethod]
    public void SemanticNodeResult_ShouldApplyNodeToTraversalFrame()
    {
        var state = new SemanticAnalysisState();
        var root = new RootNode(new IdentifierNode("Name"));

        SemanticNodeResult.From(root).ApplyTo(state.Traversal);

        Assert.AreSame(root, state.Traversal.PeekNode<RootNode>("visitor", "peek"));
    }

    [TestMethod]
    public void SemanticTraversalFrame_WhenNodeStackUnderflows_ShouldThrowVisitorException()
    {
        var state = new SemanticAnalysisState();

        var exception = Assert.ThrowsExactly<VisitorException>(() =>
            state.Traversal.PopNode("Visitor", "pop"));

        StringAssert.Contains(exception.Message, "pop");
    }

    [TestMethod]
    public void SemanticTraversalFrame_PopNodes_ShouldPreserveOldSafePopMultipleOrdering()
    {
        var state = new SemanticAnalysisState();
        var left = new IdentifierNode("left");
        var right = new IdentifierNode("right");

        state.Traversal.PushNode(left);
        state.Traversal.PushNode(right);

        var nodes = state.Traversal.PopNodes("Visitor", 2, "binary");

        Assert.AreSame(left, nodes[0]);
        Assert.AreSame(right, nodes[1]);
        Assert.AreEqual(0, state.Traversal.NodeCount);
    }

    [TestMethod]
    public void SemanticTraversalFrame_WhenTypedNodeDoesNotMatch_ShouldThrowVisitorException()
    {
        var state = new SemanticAnalysisState();
        state.Traversal.PushNode(new StringNode("alpha"));

        var exception = Assert.ThrowsExactly<VisitorException>(() =>
            state.Traversal.PopNode<RootNode>("Visitor", "typed-pop"));

        StringAssert.Contains(exception.Message, "typed-pop");
        StringAssert.Contains(exception.Message, nameof(RootNode));
    }
}
