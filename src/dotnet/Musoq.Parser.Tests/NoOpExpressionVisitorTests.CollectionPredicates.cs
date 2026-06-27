using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitCollectionInNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new ParameterReferenceNode("ids", typeof(int[]));
        var node = new CollectionInNode(left, right);

        visitor.Visit(node);
    }
}
