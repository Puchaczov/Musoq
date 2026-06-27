using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class LogicalOperationVisitorHelperTests
{
    private static Node IdentityRewriter(Node node)
    {
        return node;
    }

    [TestMethod]
    public void ProcessAndOperation_WhenTwoNodesOnStack_ShouldCreateAndNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new BooleanNode(true);
        var rightNode = new BooleanNode(false);
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        LogicalOperationVisitorHelper.ProcessAndOperation(nodes, IdentityRewriter);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<AndNode>(result);
        var andNode = (AndNode)result;
        Assert.AreEqual(leftNode, andNode.Left);
        Assert.AreEqual(rightNode, andNode.Right);
    }

    [TestMethod]
    public void ProcessOrOperation_WhenTwoNodesOnStack_ShouldCreateOrNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new BooleanNode(true);
        var rightNode = new BooleanNode(false);
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        LogicalOperationVisitorHelper.ProcessOrOperation(nodes, IdentityRewriter);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<OrNode>(result);
        var orNode = (OrNode)result;
        Assert.AreEqual(leftNode, orNode.Left);
        Assert.AreEqual(rightNode, orNode.Right);
    }

    [TestMethod]
    public void ProcessNotOperation_WhenOneNodeOnStack_ShouldCreateNotNode()
    {
        var nodes = new Stack<Node>();
        var operandNode = new BooleanNode(true);
        nodes.Push(operandNode);


        LogicalOperationVisitorHelper.ProcessNotOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<NotNode>(result);
        var notNode = (NotNode)result;
        Assert.AreEqual(operandNode, notNode.Expression);
    }

    [TestMethod]
    public void ProcessContainsOperation_WhenTwoNodesOnStack_ShouldCreateContainsNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new StringNode("Hello World");
        var rightNode = new ArgsListNode([new StringNode("World")]);
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        LogicalOperationVisitorHelper.ProcessContainsOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<ContainsNode>(result);
        var containsNode = (ContainsNode)result;
        Assert.AreEqual(leftNode, containsNode.Left);
        Assert.AreEqual(rightNode, containsNode.Right);
    }

    [TestMethod]
    public void ProcessIsNullOperation_WhenOneNodeOnStack_ShouldCreateIsNullNode()
    {
        var nodes = new Stack<Node>();
        var operandNode = new IdentifierNode("x");
        nodes.Push(operandNode);


        LogicalOperationVisitorHelper.ProcessIsNullOperation(nodes, true);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<IsNullNode>(result);
        var isNullNode = (IsNullNode)result;
        Assert.AreEqual(operandNode, isNullNode.Expression);
        Assert.IsTrue(isNullNode.IsNegated);
    }

    [TestMethod]
    public void ProcessInOperation_WhenTwoNodesOnStack_ShouldCreateOrChain()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IdentifierNode("x");
        var argsNode = new ArgsListNode([
            new IntegerNode("1"),
            new IntegerNode("2"),
            new IntegerNode("3")
        ]);
        nodes.Push(leftNode);
        nodes.Push(argsNode);


        LogicalOperationVisitorHelper.ProcessInOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();


        Assert.IsInstanceOfType<OrNode>(result);
        var outerOr = (OrNode)result;
        Assert.IsInstanceOfType<OrNode>(outerOr.Left);
        Assert.IsInstanceOfType<EqualityNode>(outerOr.Right);
    }

    [TestMethod]
    public void ProcessAndOperation_WithNullableRewriter_ShouldApplyRewriter()
    {
        var nodes = new Stack<Node>();
        var leftNode = new BooleanNode(true);
        var rightNode = new BooleanNode(false);
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        var rewriterCalled = false;

        Node TestRewriter(Node node)
        {
            rewriterCalled = true;
            return node;
        }


        LogicalOperationVisitorHelper.ProcessAndOperation(nodes, TestRewriter);


        Assert.IsTrue(rewriterCalled);
        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<AndNode>(result);
    }

    [TestMethod]
    public void ProcessOrOperation_WithNullableRewriter_ShouldApplyRewriter()
    {
        var nodes = new Stack<Node>();
        var leftNode = new BooleanNode(true);
        var rightNode = new BooleanNode(false);
        nodes.Push(leftNode);
        nodes.Push(rightNode);

        var rewriterCalled = false;

        Node TestRewriter(Node node)
        {
            rewriterCalled = true;
            return node;
        }


        LogicalOperationVisitorHelper.ProcessOrOperation(nodes, TestRewriter);


        Assert.IsTrue(rewriterCalled);
        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<OrNode>(result);
    }

    [TestMethod]
    public void ProcessInOperation_WithSingleValue_ShouldCreateSingleEquality()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IdentifierNode("x");
        var argsNode = new ArgsListNode([
            new IntegerNode("42")
        ]);
        nodes.Push(leftNode);
        nodes.Push(argsNode);


        LogicalOperationVisitorHelper.ProcessInOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<EqualityNode>(result);
        var equalityNode = (EqualityNode)result;
        Assert.AreEqual(leftNode, equalityNode.Left);
    }

    [TestMethod]
    public void ProcessInOperation_WithEmptyArgs_ShouldCreateBooleanFalse()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IdentifierNode("x");
        var argsNode = new ArgsListNode([]);
        nodes.Push(leftNode);
        nodes.Push(argsNode);


        LogicalOperationVisitorHelper.ProcessInOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<BooleanNode>(result);
        var booleanNode = (BooleanNode)result;
        Assert.IsFalse((bool)booleanNode.ObjValue);
    }

    [TestMethod]
    public void ProcessInOperation_WhenAtOrAboveThreshold_ShouldCreateContainsNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IdentifierNode("x");
        var args = new Node[LogicalOperationVisitorHelper.ContainsThreshold];
        for (var i = 0; i < args.Length; i++)
            args[i] = new IntegerNode(i.ToString());
        var argsNode = new ArgsListNode(args);
        nodes.Push(leftNode);
        nodes.Push(argsNode);


        LogicalOperationVisitorHelper.ProcessInOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<ContainsNode>(result);
        var containsNode = (ContainsNode)result;
        Assert.AreEqual(leftNode, containsNode.Left);
        Assert.AreEqual(argsNode, containsNode.ToCompareExpression);
    }

    [TestMethod]
    public void ProcessInOperation_WhenBelowThreshold_ShouldCreateOrChain()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IdentifierNode("x");
        var count = LogicalOperationVisitorHelper.ContainsThreshold - 1;
        var args = new Node[count];
        for (var i = 0; i < args.Length; i++)
            args[i] = new IntegerNode(i.ToString());
        var argsNode = new ArgsListNode(args);
        nodes.Push(leftNode);
        nodes.Push(argsNode);


        LogicalOperationVisitorHelper.ProcessInOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType<OrNode>(result);
    }
}
