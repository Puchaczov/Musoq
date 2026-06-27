using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.Tests.IR;

public partial class ExpressionIrTests
{

    [TestMethod]
    public void ColumnRefExtractor_WhenSingleColumn_ShouldReturnOne()
    {
        var col = new ColumnRef("t", "Name", typeof(string));
        var result = ColumnRefExtractor.Extract(col);

        Assert.HasCount(1, result);
        Assert.AreEqual(col, result[0]);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenLiteral_ShouldReturnEmpty()
    {
        var lit = new Literal(42, typeof(int));
        var result = ColumnRefExtractor.Extract(lit);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenBinaryOp_ShouldReturnColumnsFromBothSides()
    {
        var left = new ColumnRef("t", "Age", typeof(int));
        var right = new ColumnRef("t", "Bonus", typeof(int));
        var op = new BinaryOp(BinaryOpKind.Add, left, right, typeof(int));
        var result = ColumnRefExtractor.Extract(op);

        Assert.HasCount(2, result);
        Assert.AreEqual("Age", result[0].ColumnName);
        Assert.AreEqual("Bonus", result[1].ColumnName);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenNestedExpression_ShouldReturnAllColumns()
    {
        // t.Age > 18 AND t.City = 'NYC'
        var ageCol = new ColumnRef("t", "Age", typeof(int));
        var cityCol = new ColumnRef("t", "City", typeof(string));
        var left = new BinaryOp(BinaryOpKind.GreaterThan, ageCol, new Literal(18, typeof(int)), typeof(bool));
        var right = new BinaryOp(BinaryOpKind.Equal, cityCol, new Literal("NYC", typeof(string)), typeof(bool));
        var expr = new BinaryOp(BinaryOpKind.And, left, right, typeof(bool));

        var result = ColumnRefExtractor.Extract(expr);

        Assert.HasCount(2, result);
        Assert.AreEqual("Age", result[0].ColumnName);
        Assert.AreEqual("City", result[1].ColumnName);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenCaseWhen_ShouldReturnColumnsFromAllBranches()
    {
        var branches = new[]
        {
            new CaseWhenBranch(
                new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "Status", typeof(int)), new Literal(1, typeof(int)), typeof(bool)),
                new ColumnRef("t", "ActiveName", typeof(string)))
        };
        var caseWhen = new CaseWhen(branches, new ColumnRef("t", "DefaultName", typeof(string)), typeof(string));

        var result = ColumnRefExtractor.Extract(caseWhen);

        Assert.HasCount(3, result);
        Assert.AreEqual("Status", result[0].ColumnName);
        Assert.AreEqual("ActiveName", result[1].ColumnName);
        Assert.AreEqual("DefaultName", result[2].ColumnName);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenMethodCall_ShouldReturnColumnsFromArguments()
    {
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var expr = new MethodCall(method, [new ColumnRef("t", "Name", typeof(string))], null, typeof(string));

        var result = ColumnRefExtractor.Extract(expr);

        Assert.HasCount(1, result);
        Assert.AreEqual("Name", result[0].ColumnName);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenBetween_ShouldReturnColumnsFromAllParts()
    {
        var expr = new Between(
            new ColumnRef("t", "Age", typeof(int)),
            new ColumnRef("t", "MinAge", typeof(int)),
            new ColumnRef("t", "MaxAge", typeof(int)),
            typeof(bool));

        var result = ColumnRefExtractor.Extract(expr);

        Assert.HasCount(3, result);
    }

    [TestMethod]
    public void ColumnRefExtractor_WhenInCheck_ShouldReturnColumnsFromExpressionAndValues()
    {
        var expr = new InCheck(
            new ColumnRef("t", "City", typeof(string)),
            [new ColumnRef("t", "HomeCity", typeof(string)), new Literal("NYC", typeof(string))],
            typeof(bool));

        var result = ColumnRefExtractor.Extract(expr);

        Assert.HasCount(2, result);
        Assert.AreEqual("City", result[0].ColumnName);
        Assert.AreEqual("HomeCity", result[1].ColumnName);
    }

}
