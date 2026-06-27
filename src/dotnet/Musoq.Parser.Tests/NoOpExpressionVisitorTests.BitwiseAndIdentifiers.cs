using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitBitwiseAndNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new BitwiseAndNode(new IntegerNode("1"), new IntegerNode("2"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitBitwiseOrNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new BitwiseOrNode(new IntegerNode("1"), new IntegerNode("2"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitBitwiseXorNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new BitwiseXorNode(new IntegerNode("1"), new IntegerNode("2"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitLeftShiftNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new LeftShiftNode(new IntegerNode("1"), new IntegerNode("2"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitRightShiftNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new RightShiftNode(new IntegerNode("1"), new IntegerNode("2"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitHexIntegerNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new HexIntegerNode("0xFF");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitBinaryIntegerNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new BinaryIntegerNode("0b1010");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitOctalIntegerNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new OctalIntegerNode("0o77");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIdentifierNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new AccessColumnNode("name", "alias", TextSpan.Empty);

        visitor.Visit((IdentifierNode)node);
    }
}
