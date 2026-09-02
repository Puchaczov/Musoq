using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;
namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{

    [TestMethod]
    public void Converter_WhenIntegerNode_ShouldReturnLiteral()
    {
        var node = new IntegerNode("42", string.Empty, default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        var lit = (Literal)result;
        Assert.AreEqual(42, lit.Value);
    }

    [TestMethod]
    public void Converter_WhenStringNode_ShouldReturnLiteral()
    {
        var node = new StringNode("hello", default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        var lit = (Literal)result;
        Assert.AreEqual("hello", lit.Value);
        Assert.AreEqual(typeof(string), lit.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenDecimalNode_ShouldReturnLiteral()
    {
        var node = new DecimalNode("3.14", default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        var lit = (Literal)result;
        Assert.AreEqual(3.14m, lit.Value);
        Assert.AreEqual(typeof(decimal), lit.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenBooleanNode_ShouldReturnLiteral()
    {
        var node = new BooleanNode(true, default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        var lit = (Literal)result;
        Assert.IsTrue((bool?)lit.Value);
        Assert.AreEqual(typeof(bool), lit.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenNullNode_ShouldReturnNullLiteral()
    {
        var node = new NullNode(default(TextSpan));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        var lit = (Literal)result;
        Assert.IsNull(lit.Value);
    }

    [TestMethod]
    public void Converter_WhenAccessColumnNode_ShouldReturnColumnRef()
    {
        var node = new AccessColumnNode("Name", "t", typeof(string), default);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<ColumnRef>(result);
        var col = (ColumnRef)result;
        Assert.AreEqual("t", col.Alias);
        Assert.AreEqual("Name", col.ColumnName);
        Assert.AreEqual(typeof(string), col.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenAddNode_ShouldReturnBinaryOpAdd()
    {
        var node = new AddNode(
            new IntegerNode("1", string.Empty, default),
            new IntegerNode("2", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        var op = (BinaryOp)result;
        Assert.AreEqual(BinaryOpKind.Add, op.Kind);
    }

    [TestMethod]
    public void Converter_WhenHyphenNode_ShouldReturnBinaryOpSubtract()
    {
        var node = new HyphenNode(
            new IntegerNode("5", string.Empty, default),
            new IntegerNode("3", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.Subtract, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenStarNode_ShouldReturnBinaryOpMultiply()
    {
        var node = new StarNode(
            new IntegerNode("2", string.Empty, default),
            new IntegerNode("3", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.Multiply, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenFSlashNode_ShouldReturnBinaryOpDivide()
    {
        var node = new FSlashNode(
            new IntegerNode("10", string.Empty, default),
            new IntegerNode("2", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.Divide, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenModuloNode_ShouldReturnBinaryOpModulo()
    {
        var node = new ModuloNode(
            new IntegerNode("10", string.Empty, default),
            new IntegerNode("3", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.Modulo, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenEqualityNode_ShouldReturnBinaryOpEqual()
    {
        var node = new EqualityNode(
            new AccessColumnNode("Name", "t", typeof(string), default),
            new StringNode("John", default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        var op = (BinaryOp)result;
        Assert.AreEqual(BinaryOpKind.Equal, op.Kind);
        Assert.AreEqual(typeof(bool?), op.ReturnType);
    }

    [TestMethod]
    public void Converter_WhenDiffNode_ShouldReturnBinaryOpNotEqual()
    {
        var node = new DiffNode(
            new AccessColumnNode("Name", "t", typeof(string), default),
            new StringNode("John", default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.NotEqual, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenGreaterNode_ShouldReturnBinaryOpGreaterThan()
    {
        var node = new GreaterNode(
            new AccessColumnNode("Age", "t", typeof(int), default),
            new IntegerNode("18", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.GreaterThan, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenLessNode_ShouldReturnBinaryOpLessThan()
    {
        var node = new LessNode(
            new AccessColumnNode("Age", "t", typeof(int), default),
            new IntegerNode("18", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.LessThan, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenAndNode_ShouldReturnBinaryOpAnd()
    {
        var node = new AndNode(
            new BooleanNode(true, default),
            new BooleanNode(false, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.And, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenOrNode_ShouldReturnBinaryOpOr()
    {
        var node = new OrNode(
            new BooleanNode(true, default),
            new BooleanNode(false, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<BinaryOp>(result);
        Assert.AreEqual(BinaryOpKind.Or, ((BinaryOp)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenNotNode_ShouldReturnUnaryOpNot()
    {
        var node = new NotNode(new BooleanNode(true, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<UnaryOp>(result);
        var op = (UnaryOp)result;
        Assert.AreEqual(UnaryOpKind.Not, op.Kind);
    }

    [TestMethod]
    public void Converter_WhenIsNullNode_ShouldReturnIsNullCheck()
    {
        var node = new IsNullNode(new AccessColumnNode("Name", "t", typeof(string), default), false);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<IsNullCheck>(result);
        var check = (IsNullCheck)result;
        Assert.IsFalse(check.IsNegated);
    }

    [TestMethod]
    public void Converter_WhenIsNotNullNode_ShouldReturnNegatedIsNullCheck()
    {
        var node = new IsNullNode(new AccessColumnNode("Name", "t", typeof(string), default), true);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<IsNullCheck>(result);
        Assert.IsTrue(((IsNullCheck)result).IsNegated);
    }

    [TestMethod]
    public void Converter_WhenInNode_ShouldReturnInCheck()
    {
        var args = new ArgsListNode(
            [new StringNode("NYC", default), new StringNode("LA", default)],
            default);
        var node = new InNode(
            new AccessColumnNode("City", "t", typeof(string), default),
            args);

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<InCheck>(result);
        var check = (InCheck)result;
        Assert.HasCount(2, check.Values);
    }

    [TestMethod]
    public void Converter_WhenLikeNode_ShouldReturnPatternMatchLike()
    {
        var node = new LikeNode(
            new AccessColumnNode("Name", "t", typeof(string), default),
            new StringNode("%John%", default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<PatternMatch>(result);
        Assert.AreEqual(PatternKind.Like, ((PatternMatch)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenRLikeNode_ShouldReturnPatternMatchRLike()
    {
        var node = new RLikeNode(
            new AccessColumnNode("Name", "t", typeof(string), default),
            new StringNode("^J.*n$", default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<PatternMatch>(result);
        Assert.AreEqual(PatternKind.RLike, ((PatternMatch)result).Kind);
    }

    [TestMethod]
    public void Converter_WhenBetweenNode_ShouldReturnBetween()
    {
        var node = new BetweenNode(
            new AccessColumnNode("Age", "t", typeof(int), default),
            new IntegerNode("18", string.Empty, default),
            new IntegerNode("65", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Between>(result);
        var between = (Between)result;
        Assert.IsInstanceOfType<ColumnRef>(between.Expression);
        Assert.IsInstanceOfType<Literal>(between.Low);
        Assert.IsInstanceOfType<Literal>(between.High);
    }

    [TestMethod]
    public void Converter_WhenCaseNode_ShouldReturnCaseWhen()
    {
        var whenThenPairs = new (Node, Node)[]
        {
            (new BooleanNode(true, default), new StringNode("yes", default))
        };
        var node = new CaseNode(whenThenPairs, new StringNode("no", default), typeof(string));

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<CaseWhen>(result);
        var caseWhen = (CaseWhen)result;
        Assert.HasCount(1, caseWhen.Branches);
        Assert.IsNotNull(caseWhen.ElseExpression);
    }

    [TestMethod]
    public void Converter_WhenNestedBinaryOperations_ShouldBuildCorrectTree()
    {
        // 1 + 2 * 3 → AddNode(1, StarNode(2, 3))
        var mul = new StarNode(
            new IntegerNode("2", string.Empty, default),
            new IntegerNode("3", string.Empty, default));
        var add = new AddNode(
            new IntegerNode("1", string.Empty, default),
            mul);

        var result = _converter.Convert(add);

        Assert.IsInstanceOfType<BinaryOp>(result);
        var addOp = (BinaryOp)result;
        Assert.AreEqual(BinaryOpKind.Add, addOp.Kind);
        Assert.IsInstanceOfType<BinaryOp>(addOp.Right);
        Assert.AreEqual(BinaryOpKind.Multiply, ((BinaryOp)addOp.Right).Kind);
    }

    [TestMethod]
    public void Converter_WhenNestedBinaryPrinted_ShouldMatchExpected()
    {
        // 1 + 2 * 3
        var mul = new StarNode(
            new IntegerNode("2", string.Empty, default),
            new IntegerNode("3", string.Empty, default));
        var add = new AddNode(
            new IntegerNode("1", string.Empty, default),
            mul);

        var result = _converter.Convert(add);

        Assert.AreEqual("(1 + (2 * 3))", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenComparisonExpression_ShouldPrintCorrectly()
    {
        // Name = 'John'
        var node = new EqualityNode(
            new AccessColumnNode("Name", "a", typeof(string), default),
            new StringNode("John", default));

        var result = _converter.Convert(node);

        Assert.AreEqual("(a.Name = 'John')", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenLogicalExpression_ShouldPrintCorrectly()
    {
        // Age > 18 AND City = 'NYC'
        var left = new GreaterNode(
            new AccessColumnNode("Age", "t", typeof(int), default),
            new IntegerNode("18", string.Empty, default));
        var right = new EqualityNode(
            new AccessColumnNode("City", "t", typeof(string), default),
            new StringNode("NYC", default));
        var node = new AndNode(left, right);

        var result = _converter.Convert(node);

        Assert.AreEqual("((t.Age > 18) AND (t.City = 'NYC'))", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenBetweenExpression_ShouldPrintCorrectly()
    {
        var node = new BetweenNode(
            new AccessColumnNode("Age", "t", typeof(int), default),
            new IntegerNode("18", string.Empty, default),
            new IntegerNode("65", string.Empty, default));

        var result = _converter.Convert(node);

        Assert.AreEqual("t.Age BETWEEN 18 AND 65", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenDotNodeWithAccessColumnRoot_ShouldPreserveAllPathSegments()
    {
        var node = new DotNode(
            new AccessColumnNode("Text", "p", typeof(object), default),
            new IdentifierNode("Content", typeof(string)),
            string.Empty,
            typeof(string));

        var result = _converter.Convert(node);

        Assert.AreEqual("p.Text.Content", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenDotNodeRightSideCarriesAlias_ShouldIncludeAliasInPath()
    {
        var node = new DotNode(
            new AccessColumnNode("p", string.Empty, typeof(object), default),
            new AccessColumnNode("Content", "Text", typeof(string), default),
            string.Empty,
            typeof(string));

        var result = _converter.Convert(node);

        Assert.AreEqual("p.Text.Content", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenInExpression_ShouldPrintCorrectly()
    {
        var args = new ArgsListNode(
            [new StringNode("NYC", default), new StringNode("LA", default), new StringNode("SF", default)],
            default);
        var node = new InNode(
            new AccessColumnNode("City", "t", typeof(string), default),
            args);

        var result = _converter.Convert(node);

        Assert.AreEqual("t.City IN ('NYC', 'LA', 'SF')", IrExpressionPrinter.Print(result));
    }

    [TestMethod]
    public void Converter_WhenAllColumnsNode_ShouldReturnWildcardLiteral()
    {
        var node = new AllColumnsNode();

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<WildcardLiteral>(result);
    }

    [TestMethod]
    public void Converter_WhenFieldNodeWrapsExpression_ShouldUnwrapToInnerExpression()
    {
        var inner = new IntegerNode("42", string.Empty, default);
        var node = new FieldNode(inner, 0, "alias");

        var result = _converter.Convert(node);

        Assert.IsInstanceOfType<Literal>(result);
        Assert.AreEqual(42, ((Literal)result).Value);
    }

    [TestMethod]
    public void Converter_WhenUnsupportedNodeType_ShouldThrowNotSupportedException()
    {
        var node = new SkipNode(new IntegerNode("5", string.Empty, default));

        Assert.Throws<NotSupportedException>(() => _converter.Convert(node));
    }

}
