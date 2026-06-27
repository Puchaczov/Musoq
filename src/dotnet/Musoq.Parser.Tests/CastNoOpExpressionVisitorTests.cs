using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public class CastNoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitCastNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new CastNode(new IdentifierNode("Column"), "Int32");

        visitor.Visit(node);
    }

    private sealed class TestableNoOpVisitor : NoOpExpressionVisitor;
}
