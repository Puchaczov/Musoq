using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitStringNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new StringNode("test");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitDecimalNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new DecimalNode("3.14");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIntegerNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IntegerNode("42");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitBooleanNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new BooleanNode(true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitWordNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new WordNode("word");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitNullNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new NullNode(typeof(object));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitArgsListNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ArgsListNode([new IntegerNode("1")]);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitWhereNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new WhereNode(new BooleanNode(true));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitGroupByNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var fields = new[] { new FieldNode(new IntegerNode("1"), 0, "field") };
        var node = new GroupByNode(fields, null);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitHavingNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new HavingNode(new BooleanNode(true));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitTakeNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new TakeNode(new IntegerNode("10"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitSkipNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new SkipNode(new IntegerNode("5"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAccessColumnNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new AccessColumnNode("column", "alias", TextSpan.Empty);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAllColumnsNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new AllColumnsNode();

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitSchemaFromNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new SchemaFromNode("schema", "method", new ArgsListNode([]), "alias",
            typeof(object), 0);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitOrderByNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var fields = new[] { new FieldOrderedNode(new IntegerNode("1"), 0, "field", Order.Ascending) };
        var node = new OrderByNode(fields);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitUnionNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new UnionNode("result", ["key1"], new IntegerNode("1"), new IntegerNode("2"), false, true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitUnionAllNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new UnionAllNode("result", ["key1"], new IntegerNode("1"), new IntegerNode("2"), false,
            true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitExceptNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ExceptNode("result", ["key1"], new IntegerNode("1"), new IntegerNode("2"), false, true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIntersectNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IntersectNode("result", ["key1"], new IntegerNode("1"), new IntegerNode("2"), false,
            true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitCaseNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var whenThen = (new BooleanNode(true) as Node, new IntegerNode("1") as Node);
        var node = new CaseNode([whenThen], new IntegerNode("0"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitWhenNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new WhenNode(new BooleanNode(true));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitThenNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ThenNode(new IntegerNode("1"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitElseNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ElseNode(new IntegerNode("0"));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIsNullNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IsNullNode(new NullNode(typeof(object)), false);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAccessObjectArrayNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var token = new NumericAccessToken("array", "0", TextSpan.Empty);
        var node = new AccessObjectArrayNode(token);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAccessObjectKeyNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var token = new KeyAccessToken("obj", "'key'", TextSpan.Empty);
        var node = new AccessObjectKeyNode(token);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitPropertyValueNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new PropertyValueNode("propName");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitDotNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new DotNode(new IntegerNode("1"), new IntegerNode("2"), "test");

        visitor.Visit(node);
    }
}
