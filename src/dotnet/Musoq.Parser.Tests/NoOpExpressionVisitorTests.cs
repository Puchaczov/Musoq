using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for NoOpExpressionVisitor to improve coverage
/// </summary>
[TestClass]
public class NoOpExpressionVisitorTests
{
    [TestMethod]
    public void NoOpExpressionVisitor_VisitNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();

        visitor.Visit((Node)null);

        Assert.AreEqual(1, visitor.VisitNodeCalled);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitDescNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var schemaFrom = new SchemaFromNode("schema", "method", new ArgsListNode(Array.Empty<Node>()), "alias",
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
        var right = new ArgsListNode(new Node[] { new StringNode("es") });
        var node = new ContainsNode(left, right);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitInNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var left = new IntegerNode("1");
        var right = new ArgsListNode(new[] { new IntegerNode("1") });
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
        var node = new ArgsListNode(new[] { new IntegerNode("1") });

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
        var node = new SchemaFromNode("schema", "method", new ArgsListNode(Array.Empty<Node>()), "alias",
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
        var node = new UnionNode("result", new[] { "key1" }, new IntegerNode("1"), new IntegerNode("2"), false, true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitUnionAllNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new UnionAllNode("result", new[] { "key1" }, new IntegerNode("1"), new IntegerNode("2"), false,
            true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitExceptNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new ExceptNode("result", new[] { "key1" }, new IntegerNode("1"), new IntegerNode("2"), false, true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitIntersectNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new IntersectNode("result", new[] { "key1" }, new IntegerNode("1"), new IntegerNode("2"), false,
            true);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitCaseNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var whenThen = (new BooleanNode(true) as Node, new IntegerNode("1") as Node);
        var node = new CaseNode(new[] { whenThen }, new IntegerNode("0"));

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

    [TestMethod]
    public void NoOpExpressionVisitor_VisitQueryNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode(new[] { new FieldNode(new IntegerNode("1"), 0, "f") });
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode(Array.Empty<Node>()), "a", typeof(object), 0);
        var node = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitRootNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode(new[] { new FieldNode(new IntegerNode("1"), 0, "f") });
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode(Array.Empty<Node>()), "a", typeof(object), 0);
        var query = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);
        var node = new RootNode(query);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitTranslatedSetTreeNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new TranslatedSetTreeNode(new List<TranslatedSetOperatorNode>());

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
    public void NoOpExpressionVisitor_VisitRefreshNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new RefreshNode(Array.Empty<AccessMethodNode>());

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitSingleSetNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var selectNode = new SelectNode(new[] { new FieldNode(new IntegerNode("1"), 0, "f") });
        var schemaFrom = new SchemaFromNode("s", "m", new ArgsListNode(Array.Empty<Node>()), "a", typeof(object), 0);
        var query = new QueryNode(selectNode, schemaFrom, null, null, null, null, null);
        var node = new SingleSetNode(query);

        visitor.Visit(node);
    }

    [TestMethod]
    public void NoOpExpressionVisitor_VisitFieldLinkNode_DoesNotThrow()
    {
        var visitor = new TestableNoOpVisitor();
        var node = new FieldLinkNode("::5");

        visitor.Visit(node);
    }

    /// <summary>
    ///     Concrete implementation of abstract NoOpExpressionVisitor for testing
    /// </summary>
    private class TestableNoOpVisitor : NoOpExpressionVisitor
    {
        public int VisitNodeCalled { get; private set; }

        public override void Visit(Node node)
        {
            VisitNodeCalled++;
            base.Visit(node);
        }
    }
}
