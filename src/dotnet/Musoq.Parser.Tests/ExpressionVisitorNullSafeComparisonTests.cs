using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class ExpressionVisitorNullSafeComparisonTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitIsDistinctFromNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IsDistinctFromNode(new IntegerNode("1"), new IntegerNode("2"), false);

        visitor.Visit(node);
    }

    private sealed class TestableNoOpVisitor : NoOpExpressionVisitor;
}
