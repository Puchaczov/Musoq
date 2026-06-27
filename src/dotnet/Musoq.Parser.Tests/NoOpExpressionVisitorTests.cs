using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for NoOpExpressionVisitor to improve coverage
/// </summary>
[TestClass]
public partial class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();

        visitor.Visit((Node)null!);

        Assert.AreEqual(1, visitor.VisitNodeCalled);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitDescNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var schemaFrom = new SchemaFromNode("schema", "method", new ArgsListNode([]), "alias",
            typeof(object), 0);
        var node = new DescNode(schemaFrom, DescForType.Schema);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitStarNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new StarNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitFSlashNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new FSlashNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitModuloNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new ModuloNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAddNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new AddNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitHyphenNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new HyphenNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitAndNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new BooleanNode(true);
        var right = new BooleanNode(false);
        var node = new AndNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitOrNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new BooleanNode(true);
        var right = new BooleanNode(false);
        var node = new OrNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitShortCircuitingNodeLeft_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ShortCircuitingNodeLeft(new BooleanNode(true), TokenType.And);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitShortCircuitingNodeRight_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ShortCircuitingNodeRight(new BooleanNode(true), TokenType.And);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitEqualityNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new EqualityNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitGreaterOrEqualNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new GreaterOrEqualNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitLessOrEqualNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new LessOrEqualNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitGreaterNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new GreaterNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitLessNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new LessNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitDiffNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new IntegerNode("2");
        var node = new DiffNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitNotNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new NotNode(new BooleanNode(true));

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitLikeNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new StringNode("test");
        var right = new StringNode("%es%");
        var node = new LikeNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitRLikeNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new StringNode("test");
        var right = new StringNode(".*");
        var node = new RLikeNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitContainsNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new StringNode("test");
        var right = new ArgsListNode([new StringNode("es")]);
        var node = new ContainsNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitInNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new ArgsListNode([new IntegerNode("1")]);
        var node = new InNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitBetweenNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var expression = new IntegerNode("5");
        var min = new IntegerNode("1");
        var max = new IntegerNode("10");
        var node = new BetweenNode(expression, min, max);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitFieldNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new FieldNode(new IntegerNode("1"), 0, "field");

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitFieldOrderedNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new FieldOrderedNode(new IntegerNode("1"), 0, "field", Order.Ascending);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitSelectNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var fields = new[] { new FieldNode(new IntegerNode("1"), 0, "field") };
        var node = new SelectNode(fields);

        visitor.Visit(node);
    }
    /// <summary>
    ///     Concrete implementation of abstract NoOpExpressionVisitor for testing
    /// </summary>
    private sealed class TestableNoOpVisitor : NoOpExpressionVisitor
    {
        public int VisitNodeCalled { get; private set; }

        public override void Visit(Node node)
        {
            VisitNodeCalled++;
            base.Visit(node);
        }
    }
}
