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
        AssertSingleResult<SkipNode>();
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
        AssertSingleResult<TakeNode>();
    }

}
