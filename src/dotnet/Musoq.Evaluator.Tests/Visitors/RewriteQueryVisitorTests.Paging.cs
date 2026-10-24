using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests.Visitors;

public partial class RewriteQueryVisitorTests
{
    [TestMethod]
    public void Visit_SkipNode_ShouldProcessCorrectly()
    {
        // Arrange
        var skipNode = new SkipNode(new IntegerNode("10"));
        PushNode(skipNode.Expression);

        // Act
        _visitor.Visit(skipNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after skip processing");
    }

    [TestMethod]
    public void Visit_TakeNode_ShouldProcessCorrectly()
    {
        // Arrange
        var takeNode = new TakeNode(new IntegerNode("20"));
        PushNode(takeNode.Expression);

        // Act
        _visitor.Visit(takeNode);

        // Assert
        Assert.AreEqual(1, GetNodesCount(), "Should have one result node after take processing");
    }

}
