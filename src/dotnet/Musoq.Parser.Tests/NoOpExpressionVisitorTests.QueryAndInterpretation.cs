using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitQueryNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode([new FieldNode(new IntegerNode("1"), 0, "f")]);
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode([]), "a", typeof(object), 0);
        var node = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitRootNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode([new FieldNode(new IntegerNode("1"), 0, "f")]);
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode([]), "a", typeof(object), 0);
        var query = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);
        var node = new RootNode(query);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitTranslatedSetTreeNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new TranslatedSetTreeNode([]);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIntoNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IntoNode("tableName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitInterpretCallNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var dataSource = new IntegerNode("1");
        var node = new InterpretCallNode(dataSource, "schemaName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitTryInterpretCallNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var dataSource = new IntegerNode("1");
        var node = new TryInterpretCallNode(dataSource, "schemaName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitParseCallNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var dataSource = new IntegerNode("1");
        var node = new ParseCallNode(dataSource, "schemaName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitTryParseCallNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var dataSource = new IntegerNode("1");
        var node = new TryParseCallNode(dataSource, "schemaName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitPartialParseCallNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var dataSource = new IntegerNode("1");
        var node = new PartialParseCallNode(dataSource, "schemaName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitRefreshNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new RefreshNode([]);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitSingleSetNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode([new FieldNode(new IntegerNode("1"), 0, "f")]);
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode([]), "a", typeof(object), 0);
        var query = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);
        var node = new SingleSetNode(query);

        visitor.Visit(node);
    }

}
