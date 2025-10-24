using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class FieldProcessingHelperTests
{
    [TestMethod]
    public void CreateFields_WhenOldFieldsProvided_ShouldCreateFieldsFromStack()
    {
        // Arrange
        var oldFields = new FieldNode[]
        {
            new FieldNode(new IntegerNode("1"), 0, "field1"),
            new FieldNode(new IntegerNode("2"), 1, "field2"),
        };

        var nodes = new Stack<Node>();
        nodes.Push(new FieldNode(new StringNode("test1"), 0, "newfield1"));
        nodes.Push(new FieldNode(new StringNode("test2"), 1, "newfield2"));

        // Act
        var result = FieldProcessingHelper.CreateFields(oldFields, nodes);

        // Assert
        Assert.HasCount(2, result);
        Assert.AreEqual("newfield1", result[0].FieldName);
        Assert.AreEqual("newfield2", result[1].FieldName);
    }

    [TestMethod]
    public void CreateFields_WhenStackHasFewerItems_ShouldHandleGracefully()
    {
        // Arrange
        var oldFields = new FieldNode[]
        {
            new FieldNode(new IntegerNode("1"), 0, "field1"),
        };

        var nodes = new Stack<Node>();
        nodes.Push(new FieldNode(new StringNode("test1"), 0, "newfield1"));

        // Act
        var result = FieldProcessingHelper.CreateFields(oldFields, nodes);

        // Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("newfield1", result[0].FieldName);
    }

    [TestMethod]
    public void CreateFields_WhenEmptyOldFields_ShouldReturnEmptyArray()
    {
        // Arrange
        var oldFields = new FieldNode[0];
        var nodes = new Stack<Node>();

        // Act
        var result = FieldProcessingHelper.CreateFields(oldFields, nodes);

        // Assert
        Assert.IsEmpty(result);
    }
}
