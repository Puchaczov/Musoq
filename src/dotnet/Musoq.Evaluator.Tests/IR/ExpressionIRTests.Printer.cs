using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{

    [TestMethod]
    public void Printer_WhenColumnRef_ShouldPrintAliasAndName()
    {
        var col = new ColumnRef("t", "Name", typeof(string));

        Assert.AreEqual("t.Name", IrExpressionPrinter.Print(col));
    }

    [TestMethod]
    public void Printer_WhenColumnRefWithEmptyAlias_ShouldPrintNameOnly()
    {
        var col = new ColumnRef("", "Name", typeof(string));

        Assert.AreEqual("Name", IrExpressionPrinter.Print(col));
    }

    [TestMethod]
    public void Printer_WhenIntegerLiteral_ShouldPrintValue()
    {
        var lit = new Literal(42, typeof(int));

        Assert.AreEqual("42", IrExpressionPrinter.Print(lit));
    }

    [TestMethod]
    public void Printer_WhenStringLiteral_ShouldPrintQuoted()
    {
        var lit = new Literal("John", typeof(string));

        Assert.AreEqual("'John'", IrExpressionPrinter.Print(lit));
    }

    [TestMethod]
    public void Printer_WhenBoolLiteral_ShouldPrintTrueOrFalse()
    {
        Assert.AreEqual("TRUE", IrExpressionPrinter.Print(new Literal(true, typeof(bool))));
        Assert.AreEqual("FALSE", IrExpressionPrinter.Print(new Literal(false, typeof(bool))));
    }

    [TestMethod]
    public void Printer_WhenNullLiteral_ShouldPrintNull()
    {
        Assert.AreEqual("NULL", IrExpressionPrinter.Print(new Literal(null, typeof(object))));
    }

    [TestMethod]
    public void Printer_WhenWildcard_ShouldPrintStar()
    {
        Assert.AreEqual("*", IrExpressionPrinter.Print(new WildcardLiteral(typeof(void))));
    }

    [TestMethod]
    public void Printer_WhenBinaryAdd_ShouldPrintParenthesized()
    {
        var expr = new BinaryOp(BinaryOpKind.Add, new Literal(1, typeof(int)), new Literal(2, typeof(int)), typeof(int));

        Assert.AreEqual("(1 + 2)", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenNestedArithmetic_ShouldPrintFullTree()
    {
        // 1 + 2 * 3
        var mul = new BinaryOp(BinaryOpKind.Multiply, new Literal(2, typeof(int)), new Literal(3, typeof(int)), typeof(int));
        var add = new BinaryOp(BinaryOpKind.Add, new Literal(1, typeof(int)), mul, typeof(int));

        Assert.AreEqual("(1 + (2 * 3))", IrExpressionPrinter.Print(add));
    }

    [TestMethod]
    public void Printer_WhenComparison_ShouldPrintOperator()
    {
        var expr = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("a", "Name", typeof(string)), new Literal("John", typeof(string)), typeof(bool));

        Assert.AreEqual("(a.Name = 'John')", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenLogicalAnd_ShouldPrintAndKeyword()
    {
        var left = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool));
        var right = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "City", typeof(string)), new Literal("NYC", typeof(string)), typeof(bool));
        var expr = new BinaryOp(BinaryOpKind.And, left, right, typeof(bool));

        Assert.AreEqual("((t.Age > 18) AND (t.City = 'NYC'))", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenUnaryNot_ShouldPrintNotKeyword()
    {
        var operand = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "Active", typeof(bool)), new Literal(true, typeof(bool)), typeof(bool));
        var expr = new UnaryOp(UnaryOpKind.Not, operand, typeof(bool));

        Assert.AreEqual("NOT (t.Active = TRUE)", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenUnaryNegate_ShouldPrintMinus()
    {
        var expr = new UnaryOp(UnaryOpKind.Negate, new Literal(5, typeof(int)), typeof(int));

        Assert.AreEqual("-5", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenMethodCall_ShouldPrintNameAndArgs()
    {
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var expr = new MethodCall(method, [new ColumnRef("t", "Name", typeof(string))], null, typeof(string));

        Assert.AreEqual("ToUpper(t.Name)", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenIsNull_ShouldPrintIsNull()
    {
        var expr = new IsNullCheck(new ColumnRef("t", "Name", typeof(string)), false, typeof(bool));

        Assert.AreEqual("t.Name IS NULL", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenIsNotNull_ShouldPrintIsNotNull()
    {
        var expr = new IsNullCheck(new ColumnRef("t", "Name", typeof(string)), true, typeof(bool));

        Assert.AreEqual("t.Name IS NOT NULL", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenInCheck_ShouldPrintInClause()
    {
        var expr = new InCheck(
            new ColumnRef("t", "City", typeof(string)),
            [new Literal("NYC", typeof(string)), new Literal("LA", typeof(string))],
            typeof(bool));

        Assert.AreEqual("t.City IN ('NYC', 'LA')", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenLikePattern_ShouldPrintLike()
    {
        var expr = new PatternMatch(
            new ColumnRef("t", "Name", typeof(string)),
            new Literal("%John%", typeof(string)),
            PatternKind.Like,
            typeof(bool));

        Assert.AreEqual("t.Name LIKE '%John%'", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenRLikePattern_ShouldPrintRLike()
    {
        var expr = new PatternMatch(
            new ColumnRef("t", "Name", typeof(string)),
            new Literal("^J.*n$", typeof(string)),
            PatternKind.RLike,
            typeof(bool));

        Assert.AreEqual("t.Name RLIKE '^J.*n$'", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenBetween_ShouldPrintBetweenClause()
    {
        var expr = new Between(
            new ColumnRef("t", "Age", typeof(int)),
            new Literal(18, typeof(int)),
            new Literal(65, typeof(int)),
            typeof(bool));

        Assert.AreEqual("t.Age BETWEEN 18 AND 65", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenCaseWhen_ShouldPrintFullCaseExpression()
    {
        var branches = new[]
        {
            new CaseWhenBranch(
                new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool)),
                new Literal("Adult", typeof(string)))
        };
        var expr = new CaseWhen(branches, new Literal("Minor", typeof(string)), typeof(string));

        Assert.AreEqual("CASE WHEN (t.Age > 18) THEN 'Adult' ELSE 'Minor' END", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenCoalesce_ShouldPrintCoalesceFunction()
    {
        var expr = new Coalesce(
            [new ColumnRef("t", "Name", typeof(string)), new Literal("Unknown", typeof(string))],
            typeof(string));

        Assert.AreEqual("COALESCE(t.Name, 'Unknown')", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenAggregateRef_ShouldPrintAggRefLabel()
    {
        var expr = new AggregateRef("count_0", typeof(long));

        Assert.AreEqual("AggRef(count_0)", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenWindowFunctionRef_ShouldPrintWindowRefLabel()
    {
        var expr = new WindowFunctionRef(2, typeof(long));

        Assert.AreEqual("WindowRef(2)", IrExpressionPrinter.Print(expr));
    }

    [TestMethod]
    public void Printer_WhenAllBinaryOperators_ShouldPrintCorrectSymbol()
    {
        var left = new Literal(1, typeof(int));
        var right = new Literal(2, typeof(int));

        Assert.AreEqual("(1 + 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Add, left, right, typeof(int))));
        Assert.AreEqual("(1 - 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Subtract, left, right, typeof(int))));
        Assert.AreEqual("(1 * 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Multiply, left, right, typeof(int))));
        Assert.AreEqual("(1 / 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Divide, left, right, typeof(int))));
        Assert.AreEqual("(1 % 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Modulo, left, right, typeof(int))));
        Assert.AreEqual("(1 = 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.Equal, left, right, typeof(bool))));
        Assert.AreEqual("(1 <> 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.NotEqual, left, right, typeof(bool))));
        Assert.AreEqual("(1 > 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.GreaterThan, left, right, typeof(bool))));
        Assert.AreEqual("(1 < 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.LessThan, left, right, typeof(bool))));
        Assert.AreEqual("(1 >= 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.GreaterOrEqual, left, right, typeof(bool))));
        Assert.AreEqual("(1 <= 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.LessOrEqual, left, right, typeof(bool))));
        Assert.AreEqual("(1 & 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.BitwiseAnd, left, right, typeof(int))));
        Assert.AreEqual("(1 | 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.BitwiseOr, left, right, typeof(int))));
        Assert.AreEqual("(1 ^ 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.BitwiseXor, left, right, typeof(int))));
        Assert.AreEqual("(1 << 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.LeftShift, left, right, typeof(int))));
        Assert.AreEqual("(1 >> 2)", IrExpressionPrinter.Print(new BinaryOp(BinaryOpKind.RightShift, left, right, typeof(int))));
    }

}
