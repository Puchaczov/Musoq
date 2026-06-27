using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Tests.Visitors;

[TestClass]
public sealed class ParserNodeChildTraversalTests
{
    [TestMethod]
    public void QueryChildren_ShouldUseCanonicalClauseOrder()
    {
        var from = Source("a");
        var where = new WhereNode(Id("where"));
        var select = new SelectNode([new FieldNode(Id("select"), 0, "select")]);
        var take = new TakeNode(new IntegerNode(10));
        var skip = new SkipNode(new IntegerNode(2));
        var groupBy = new GroupByNode([new FieldNode(Id("group"), 0, "group")], null);
        var window = new WindowNode([new WindowDefinitionNode("w", new WindowSpecificationNode([], []))]);
        var qualify = new QualifyNode(Id("qualify"));
        var orderBy = new OrderByNode([new FieldOrderedNode(Id("order"), 0, "order", Order.Ascending)]);
        var query = new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, TextSpan.Empty);

        AssertChildren(query, from, where, select, take, skip, groupBy, window, qualify, orderBy);
    }

    [TestMethod]
    public void SetAndCteChildren_ShouldUseCanonicalDependencyOrder()
    {
        var left = Id("left");
        var right = Id("right");
        var set = new UnionNode("result", [], left, right, false, true);

        AssertChildren(set, left, right);

        var firstInner = new CteInnerExpressionNode(Id("firstInner"), "first");
        var secondInner = new CteInnerExpressionNode(Id("secondInner"), "second");
        var outer = Id("outer");
        var cte = new CteExpressionNode([firstInner, secondInner], outer);

        AssertChildren(cte, outer, firstInner, secondInner);
        AssertCteInnerFirstChildren(cte, firstInner, secondInner, outer);
        AssertChildren(firstInner, firstInner.Value);
    }

    [TestMethod]
    public void SourceChildren_ShouldUseCanonicalSourceOrder()
    {
        var first = Source("first");
        var second = Source("second");
        var expression = Id("on");
        var join = new JoinSourcesTableFromNode(first, second, expression, JoinType.Inner, typeof(object));

        AssertChildren(join, expression, first, second);

        var valueA = Id("valueA");
        var valueB = Id("valueB");
        var values = new ValuesFromNode(
            [new ValuesRowNode([new ValuesFieldNode("a", valueA), new ValuesFieldNode("b", valueB)])],
            "v");

        AssertChildren(values, valueA, valueB);
    }

    [TestMethod]
    public void InterpretationSchemaChildren_ShouldUseCanonicalExpressionOrder()
    {
        var dataSource = Id("data");
        var offset = Id("offset");
        var call = new InterpretAtCallNode(dataSource, offset, "Packet");

        AssertChildren(call, dataSource, offset);

        var atOffset = Id("at");
        var whenCondition = Id("when");
        var constraintExpression = Id("constraint");
        var constraint = new FieldConstraintNode(constraintExpression);
        var field = new FieldDefinitionNode(
            "Payload",
            new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian),
            constraint,
            atOffset,
            whenCondition);

        AssertChildren(field, atOffset, whenCondition, constraint);
        AssertChildren(constraint, constraintExpression);
    }

    [TestMethod]
    public void WindowChildren_ShouldUseCanonicalWindowOrder()
    {
        var partition = new FieldNode(Id("partition"), 0, "partition");
        var order = new FieldOrderedNode(Id("order"), 0, "order", Order.Descending);
        var specification = new WindowSpecificationNode([partition], [order]);
        var function = new AccessMethodNode(
            new FunctionToken("row_number", TextSpan.Empty),
            ArgsListNode.Empty,
            ArgsListNode.Empty,
            false);
        var windowFunction = new WindowFunctionNode(function, specification);
        var definition = new WindowDefinitionNode("w", specification);
        var window = new WindowNode([definition]);

        AssertChildren(windowFunction, function, specification);
        AssertChildren(specification, partition, order);
        AssertChildren(definition, specification);
        AssertChildren(window, definition);
    }

    [TestMethod]
    public void WindowFrameChildren_ShouldTraverseFrameAndBounds()
    {
        var partition = new FieldNode(Id("partition"), 0, "partition");
        var order = new FieldOrderedNode(Id("order"), 0, "order", Order.Ascending);
        var start = new WindowFrameBoundNode(WindowFrameBoundType.OffsetPreceding, 2);
        var end = new WindowFrameBoundNode(WindowFrameBoundType.CurrentRow);
        var frame = new WindowFrameNode(WindowFrameType.Rows, start, end);
        var specification = new WindowSpecificationNode([partition], [order], frame);

        AssertChildren(specification, partition, order, frame);
        AssertChildren(frame, start, end);
    }
    [TestMethod]
    public void ScriptVariableChildren_ShouldTraverseInitializerOnly()
    {
        var initializer = Id("initializer");
        var declaration = new ScriptVariableDeclarationNode("answer", "int", false, initializer);

        AssertChildren(declaration, initializer);
    }

    private static IdentifierNode Id(string name)
    {
        return new IdentifierNode(name);
    }

    private static SchemaFromNode Source(string alias)
    {
        return new SchemaFromNode("schema", "method", ArgsListNode.Empty, alias, typeof(object), 0);
    }

    private static void AssertChildren(Node node, params Node[] expected)
    {
        var actual = ParserNodeChildTraversal.EnumerateChildren(node).ToArray();

        Assert.AreEqual(
            expected.Length,
            actual.Length,
            $"{node.GetType().Name} child count should match.");

        for (var i = 0; i < expected.Length; i++)
            Assert.IsTrue(
                ReferenceEquals(expected[i], actual[i]),
                $"{node.GetType().Name} child {i} should be {expected[i].GetType().Name} but was {actual[i].GetType().Name}.");
    }

    private static void AssertCteInnerFirstChildren(CteExpressionNode node, params Node[] expected)
    {
        var visitor = new RecordingVisitor();

        ParserNodeChildTraversal.TraverseCteInnerExpressionsThenOuter(node, visitor);

        var actual = visitor.Nodes.ToArray();
        Assert.AreEqual(expected.Length, actual.Length);

        for (var i = 0; i < expected.Length; i++)
            Assert.IsTrue(ReferenceEquals(expected[i], actual[i]));
    }

    private sealed class RecordingVisitor : NoOpExpressionVisitor
    {
        public List<Node> Nodes { get; } = [];

        public override void Visit(CteInnerExpressionNode node)
        {
            Nodes.Add(node);
        }

        public override void Visit(IdentifierNode node)
        {
            Nodes.Add(node);
        }
    }
}
