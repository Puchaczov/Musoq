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
        Assert.IsInstanceOfType(result, typeof(AndNode));
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
        Assert.IsInstanceOfType(result, typeof(OrNode));
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
        Assert.IsInstanceOfType(result, typeof(NotNode));
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
        Assert.IsInstanceOfType(result, typeof(ContainsNode));
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
        Assert.IsInstanceOfType(result, typeof(IsNullNode));
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


        Assert.IsInstanceOfType(result, typeof(OrNode));
        var outerOr = (OrNode)result;
        Assert.IsInstanceOfType(outerOr.Left, typeof(OrNode));
        Assert.IsInstanceOfType(outerOr.Right, typeof(EqualityNode));
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
        Assert.IsInstanceOfType(result, typeof(AndNode));
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
        Assert.IsInstanceOfType(result, typeof(OrNode));
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
        Assert.IsInstanceOfType(result, typeof(EqualityNode));
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
        Assert.IsInstanceOfType(result, typeof(BooleanNode));
        var booleanNode = (BooleanNode)result;
        Assert.IsFalse((bool)booleanNode.ObjValue);
    }
}
