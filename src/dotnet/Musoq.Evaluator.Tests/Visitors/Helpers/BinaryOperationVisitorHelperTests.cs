using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class BinaryOperationVisitorHelperTests
{
    [TestMethod]
    public void ProcessStarOperation_WhenTwoNodesOnStack_ShouldCreateStarNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("5");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        BinaryOperationVisitorHelper.ProcessStarOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(StarNode));
        var starNode = (StarNode)result;
        Assert.AreEqual(leftNode, starNode.Left);
        Assert.AreEqual(rightNode, starNode.Right);
    }

    [TestMethod]
    public void ProcessFSlashOperation_WhenTwoNodesOnStack_ShouldCreateFSlashNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("10");
        var rightNode = new IntegerNode("2");
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        BinaryOperationVisitorHelper.ProcessFSlashOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(FSlashNode));
        var fSlashNode = (FSlashNode)result;
        Assert.AreEqual(leftNode, fSlashNode.Left);
        Assert.AreEqual(rightNode, fSlashNode.Right);
    }

    [TestMethod]
    public void ProcessModuloOperation_WhenTwoNodesOnStack_ShouldCreateModuloNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("7");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        BinaryOperationVisitorHelper.ProcessModuloOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(ModuloNode));
        var moduloNode = (ModuloNode)result;
        Assert.AreEqual(leftNode, moduloNode.Left);
        Assert.AreEqual(rightNode, moduloNode.Right);
    }

    [TestMethod]
    public void ProcessAddOperation_WhenTwoNodesOnStack_ShouldCreateAddNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("4");
        var rightNode = new IntegerNode("6");
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        BinaryOperationVisitorHelper.ProcessAddOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(AddNode));
        var addNode = (AddNode)result;
        Assert.AreEqual(leftNode, addNode.Left);
        Assert.AreEqual(rightNode, addNode.Right);
    }

    [TestMethod]
    public void ProcessHyphenOperation_WhenTwoNodesOnStack_ShouldCreateHyphenNode()
    {
        var nodes = new Stack<Node>();
        var leftNode = new IntegerNode("8");
        var rightNode = new IntegerNode("3");
        nodes.Push(leftNode);
        nodes.Push(rightNode);


        BinaryOperationVisitorHelper.ProcessHyphenOperation(nodes);


        Assert.HasCount(1, nodes);
        var result = nodes.Pop();
        Assert.IsInstanceOfType(result, typeof(HyphenNode));
        var hyphenNode = (HyphenNode)result;
        Assert.AreEqual(leftNode, hyphenNode.Left);
        Assert.AreEqual(rightNode, hyphenNode.Right);
    }

    [TestMethod]
    public void ProcessStarOperation_WhenNullStack_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BinaryOperationVisitorHelper.ProcessStarOperation(null));
    }

    [TestMethod]
    public void ProcessStarOperation_WhenInsufficientNodes_ShouldThrowInvalidOperationException()
    {
        var nodes = new Stack<Node>();
        nodes.Push(new IntegerNode("5"));


        Assert.Throws<InvalidOperationException>(() =>
            BinaryOperationVisitorHelper.ProcessStarOperation(nodes));
    }
}
