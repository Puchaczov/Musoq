using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;
using Musoq.Parser.Nodes;
using ExpressionConverter = Musoq.Evaluator.IR.Expressions.ExpressionConverter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public partial class ExpressionIrTests
{
    [TestInitialize]
    public void Setup()
    {
        _converter = new ExpressionConverter();
    }

    private ExpressionConverter _converter = null!;

    #region Step 1.1: Core Expression Types

    [TestMethod]
    public void ColumnRef_WhenConstructed_ShouldPreserveProperties()
    {
        var col = new ColumnRef("t", "Name", typeof(string));

        Assert.AreEqual("t", col.Alias);
        Assert.AreEqual("Name", col.ColumnName);
        Assert.AreEqual(typeof(string), col.ReturnType);
    }

    [TestMethod]
    public void ColumnRef_WhenEqualValues_ShouldBeEqualByRecordSemantics()
    {
        var col1 = new ColumnRef("t", "Name", typeof(string));
        var col2 = new ColumnRef("t", "Name", typeof(string));

        Assert.AreEqual(col1, col2);
    }

    [TestMethod]
    public void ColumnRef_WhenDifferentValues_ShouldNotBeEqual()
    {
        var col1 = new ColumnRef("t", "Name", typeof(string));
        var col2 = new ColumnRef("t", "Age", typeof(int));

        Assert.AreNotEqual(col1, col2);
    }

    [TestMethod]
    public void Literal_WhenIntegerValue_ShouldPreserveTypeAndValue()
    {
        var lit = new Literal(42, typeof(int));

        Assert.AreEqual(42, lit.Value);
        Assert.AreEqual(typeof(int), lit.ReturnType);
    }

    [TestMethod]
    public void Literal_WhenStringValue_ShouldPreserveTypeAndValue()
    {
        var lit = new Literal("hello", typeof(string));

        Assert.AreEqual("hello", lit.Value);
        Assert.AreEqual(typeof(string), lit.ReturnType);
    }

    [TestMethod]
    public void Literal_WhenNullValue_ShouldPreserveNullAndType()
    {
        var lit = new Literal(null, typeof(object));

        Assert.IsNull(lit.Value);
        Assert.AreEqual(typeof(object), lit.ReturnType);
    }

    [TestMethod]
    public void Literal_WhenBooleanValue_ShouldPreserveTypeAndValue()
    {
        var lit = new Literal(true, typeof(bool));

        Assert.IsTrue((bool?)lit.Value);
        Assert.AreEqual(typeof(bool), lit.ReturnType);
    }

    [TestMethod]
    public void Literal_WhenDecimalValue_ShouldPreserveTypeAndValue()
    {
        var lit = new Literal(3.14m, typeof(decimal));

        Assert.AreEqual(3.14m, lit.Value);
        Assert.AreEqual(typeof(decimal), lit.ReturnType);
    }

    [TestMethod]
    public void Literal_WhenEqualValues_ShouldBeEqualByRecordSemantics()
    {
        var lit1 = new Literal(42, typeof(int));
        var lit2 = new Literal(42, typeof(int));

        Assert.AreEqual(lit1, lit2);
    }

    [TestMethod]
    public void WildcardLiteral_WhenConstructed_ShouldHaveReturnType()
    {
        var wc = new WildcardLiteral(typeof(void));

        Assert.AreEqual(typeof(void), wc.ReturnType);
    }

    [TestMethod]
    public void WildcardLiteral_WhenEqualTypes_ShouldBeEqualByRecordSemantics()
    {
        var wc1 = new WildcardLiteral(typeof(void));
        var wc2 = new WildcardLiteral(typeof(void));

        Assert.AreEqual(wc1, wc2);
    }

    [TestMethod]
    public void ExpressionConverter_WhenParameterReferenceNode_ShouldCreateScriptParameterRef()
    {
        var converted = _converter.Convert(new ParameterReferenceNode("author", typeof(string)));

        Assert.IsInstanceOfType<ScriptParameterRef>(converted);
        var parameter = (ScriptParameterRef)converted;
        Assert.AreEqual("author", parameter.Name);
        Assert.AreEqual(typeof(string), parameter.ReturnType);
    }

    #endregion

    #region Step 1.2: BinaryOp and UnaryOp

    [TestMethod]
    public void BinaryOp_WhenAddition_ShouldPreserveKindAndOperands()
    {
        var left = new Literal(1, typeof(int));
        var right = new Literal(2, typeof(int));
        var op = new BinaryOp(BinaryOpKind.Add, left, right, typeof(int));

        Assert.AreEqual(BinaryOpKind.Add, op.Kind);
        Assert.AreEqual(left, op.Left);
        Assert.AreEqual(right, op.Right);
        Assert.AreEqual(typeof(int), op.ReturnType);
    }

    [TestMethod]
    public void BinaryOp_WhenNestedTree_ShouldPreserveStructure()
    {
        // (1 + 2) * 3
        var one = new Literal(1, typeof(int));
        var two = new Literal(2, typeof(int));
        var three = new Literal(3, typeof(int));

        var add = new BinaryOp(BinaryOpKind.Add, one, two, typeof(int));
        var mul = new BinaryOp(BinaryOpKind.Multiply, add, three, typeof(int));

        Assert.AreEqual(BinaryOpKind.Multiply, mul.Kind);
        Assert.IsInstanceOfType<BinaryOp>(mul.Left);
        Assert.AreEqual(BinaryOpKind.Add, ((BinaryOp)mul.Left).Kind);
    }

    [TestMethod]
    public void BinaryOp_WhenComparison_ShouldReturnBool()
    {
        var left = new ColumnRef("t", "Age", typeof(int));
        var right = new Literal(18, typeof(int));
        var op = new BinaryOp(BinaryOpKind.GreaterThan, left, right, typeof(bool));

        Assert.AreEqual(typeof(bool), op.ReturnType);
    }

    [TestMethod]
    public void BinaryOp_WhenLogicalAnd_ShouldReturnBool()
    {
        var left = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool));
        var right = new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "City", typeof(string)), new Literal("NYC", typeof(string)), typeof(bool));
        var op = new BinaryOp(BinaryOpKind.And, left, right, typeof(bool));

        Assert.AreEqual(typeof(bool), op.ReturnType);
        Assert.AreEqual(BinaryOpKind.And, op.Kind);
    }

    [TestMethod]
    public void BinaryOp_WhenEqual_ShouldBeEqualByRecordSemantics()
    {
        var left = new Literal(1, typeof(int));
        var right = new Literal(2, typeof(int));
        var op1 = new BinaryOp(BinaryOpKind.Add, left, right, typeof(int));
        var op2 = new BinaryOp(BinaryOpKind.Add, left, right, typeof(int));

        Assert.AreEqual(op1, op2);
    }

    [TestMethod]
    public void UnaryOp_WhenNot_ShouldPreserveKindAndOperand()
    {
        var operand = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool));
        var op = new UnaryOp(UnaryOpKind.Not, operand, typeof(bool));

        Assert.AreEqual(UnaryOpKind.Not, op.Kind);
        Assert.AreEqual(operand, op.Operand);
        Assert.AreEqual(typeof(bool), op.ReturnType);
    }

    [TestMethod]
    public void UnaryOp_WhenNegate_ShouldPreserveKindAndReturnType()
    {
        var operand = new Literal(5, typeof(int));
        var op = new UnaryOp(UnaryOpKind.Negate, operand, typeof(int));

        Assert.AreEqual(UnaryOpKind.Negate, op.Kind);
        Assert.AreEqual(typeof(int), op.ReturnType);
    }

    #endregion

    #region Step 1.3: MethodCall and Special Expressions

    [TestMethod]
    public void MethodCall_WhenConstructed_ShouldPreserveMethodAndArguments()
    {
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var args = new IrExpression[] { new ColumnRef("t", "Name", typeof(string)) };
        var call = new MethodCall(method, args, null, typeof(string));

        Assert.AreEqual(method, call.Method);
        Assert.HasCount(1, call.Arguments);
        Assert.AreEqual(typeof(string), call.ReturnType);
    }

    [TestMethod]
    public void IsNullCheck_WhenIsNull_ShouldNotBeNegated()
    {
        var expr = new ColumnRef("t", "Name", typeof(string));
        var check = new IsNullCheck(expr, false, typeof(bool));

        Assert.AreEqual(expr, check.Expression);
        Assert.IsFalse(check.IsNegated);
        Assert.AreEqual(typeof(bool), check.ReturnType);
    }

    [TestMethod]
    public void IsNullCheck_WhenIsNotNull_ShouldBeNegated()
    {
        var expr = new ColumnRef("t", "Name", typeof(string));
        var check = new IsNullCheck(expr, true, typeof(bool));

        Assert.IsTrue(check.IsNegated);
    }

    [TestMethod]
    public void InCheck_WhenConstructed_ShouldPreserveExpressionAndValues()
    {
        var expr = new ColumnRef("t", "City", typeof(string));
        var values = new IrExpression[]
        {
            new Literal("NYC", typeof(string)),
            new Literal("LA", typeof(string)),
            new Literal("SF", typeof(string))
        };
        var check = new InCheck(expr, values, typeof(bool));

        Assert.AreEqual(expr, check.Expression);
        Assert.HasCount(3, check.Values);
        Assert.AreEqual(typeof(bool), check.ReturnType);
    }

    [TestMethod]
    public void PatternMatch_WhenLike_ShouldPreserveKind()
    {
        var expr = new ColumnRef("t", "Name", typeof(string));
        var pattern = new Literal("%John%", typeof(string));
        var match = new PatternMatch(expr, pattern, PatternKind.Like, typeof(bool));

        Assert.AreEqual(PatternKind.Like, match.Kind);
        Assert.AreEqual(typeof(bool), match.ReturnType);
    }

    [TestMethod]
    public void PatternMatch_WhenRLike_ShouldPreserveKind()
    {
        var expr = new ColumnRef("t", "Name", typeof(string));
        var pattern = new Literal("^J.*n$", typeof(string));
        var match = new PatternMatch(expr, pattern, PatternKind.RLike, typeof(bool));

        Assert.AreEqual(PatternKind.RLike, match.Kind);
    }

    [TestMethod]
    public void Between_WhenConstructed_ShouldPreserveBounds()
    {
        var expr = new ColumnRef("t", "Age", typeof(int));
        var low = new Literal(18, typeof(int));
        var high = new Literal(65, typeof(int));
        var between = new Between(expr, low, high, typeof(bool));

        Assert.AreEqual(expr, between.Expression);
        Assert.AreEqual(low, between.Low);
        Assert.AreEqual(high, between.High);
        Assert.AreEqual(typeof(bool), between.ReturnType);
    }

    #endregion

    #region Step 1.4: Conditional and Aggregate Expressions

    [TestMethod]
    public void CaseWhen_WhenSingleBranch_ShouldPreserveBranchAndElse()
    {
        var condition = new BinaryOp(BinaryOpKind.GreaterThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool));
        var result = new Literal("Adult", typeof(string));
        var elseExpr = new Literal("Minor", typeof(string));
        var branches = new[] { new CaseWhenBranch(condition, result) };
        var caseWhen = new CaseWhen(branches, elseExpr, typeof(string));

        Assert.HasCount(1, caseWhen.Branches);
        Assert.AreEqual(condition, caseWhen.Branches[0].Condition);
        Assert.AreEqual(result, caseWhen.Branches[0].Result);
        Assert.AreEqual(elseExpr, caseWhen.ElseExpression);
        Assert.AreEqual(typeof(string), caseWhen.ReturnType);
    }

    [TestMethod]
    public void CaseWhen_WhenMultipleBranches_ShouldPreserveAll()
    {
        var branches = new[]
        {
            new CaseWhenBranch(
                new BinaryOp(BinaryOpKind.LessThan, new ColumnRef("t", "Age", typeof(int)), new Literal(13, typeof(int)), typeof(bool)),
                new Literal("Child", typeof(string))),
            new CaseWhenBranch(
                new BinaryOp(BinaryOpKind.LessThan, new ColumnRef("t", "Age", typeof(int)), new Literal(18, typeof(int)), typeof(bool)),
                new Literal("Teen", typeof(string)))
        };
        var caseWhen = new CaseWhen(branches, new Literal("Adult", typeof(string)), typeof(string));

        Assert.HasCount(2, caseWhen.Branches);
    }

    [TestMethod]
    public void CaseWhen_WhenNoElse_ShouldHaveNullElseExpression()
    {
        var branches = new[]
        {
            new CaseWhenBranch(
                new BinaryOp(BinaryOpKind.Equal, new ColumnRef("t", "Status", typeof(int)), new Literal(1, typeof(int)), typeof(bool)),
                new Literal("Active", typeof(string)))
        };
        var caseWhen = new CaseWhen(branches, null, typeof(string));

        Assert.IsNull(caseWhen.ElseExpression);
    }

    [TestMethod]
    public void Coalesce_WhenConstructed_ShouldPreserveExpressions()
    {
        var expressions = new IrExpression[]
        {
            new ColumnRef("t", "Name", typeof(string)),
            new ColumnRef("t", "Alias", typeof(string)),
            new Literal("Unknown", typeof(string))
        };
        var coalesce = new Coalesce(expressions, typeof(string));

        Assert.HasCount(3, coalesce.Expressions);
        Assert.AreEqual(typeof(string), coalesce.ReturnType);
    }

    [TestMethod]
    public void AggregateRef_WhenConstructed_ShouldPreserveIdentifier()
    {
        var aggRef = new AggregateRef("count_0", typeof(long));

        Assert.AreEqual("count_0", aggRef.Identifier);
        Assert.AreEqual(typeof(long), aggRef.ReturnType);
    }

    [TestMethod]
    public void AggregateRef_WhenEqualValues_ShouldBeEqualByRecordSemantics()
    {
        var ref1 = new AggregateRef("count_0", typeof(long));
        var ref2 = new AggregateRef("count_0", typeof(long));

        Assert.AreEqual(ref1, ref2);
    }

    [TestMethod]
    public void WindowFunctionRef_WhenConstructed_ShouldPreserveIndex()
    {
        var winRef = new WindowFunctionRef(0, typeof(long));

        Assert.AreEqual(0, winRef.WindowIndex);
        Assert.AreEqual(typeof(long), winRef.ReturnType);
    }

    [TestMethod]
    public void CollectionInCheck_WhenConstructed_ShouldPreserveExpressionAndCollection()
    {
        var expression = new ColumnRef("t", "Id", typeof(int));
        var collection = new ScriptParameterRef("ids", typeof(int[]));
        var check = new CollectionInCheck(expression, collection, typeof(int), typeof(bool));

        Assert.AreSame(expression, check.Expression);
        Assert.AreSame(collection, check.Collection);
        Assert.AreEqual(typeof(int), check.ElementType);
        Assert.AreEqual(typeof(bool), check.ReturnType);
    }

    #endregion

    #region Step 1.5: Visitor Pattern

    private sealed class TypeNameVisitor : IrExpressionVisitor<string>
    {
        protected override string VisitColumnRef(ColumnRef node) => "ColumnRef";
        protected override string VisitScriptParameterRef(ScriptParameterRef node) => "ScriptParameterRef";
        protected override string VisitScriptVariableRef(ScriptVariableRef node) => "ScriptVariableRef";
        protected override string VisitLiteral(Literal node) => "Literal";
        protected override string VisitWildcardLiteral(WildcardLiteral node) => "WildcardLiteral";
        protected override string VisitBinaryOp(BinaryOp node) => "BinaryOp";
        protected override string VisitUnaryOp(UnaryOp node) => "UnaryOp";
        protected override string VisitMethodCall(MethodCall node) => "MethodCall";
        protected override string VisitStrictCast(StrictCast node) => "StrictCast";
        protected override string VisitIsNullCheck(IsNullCheck node) => "IsNullCheck";
        protected override string VisitRowPresence(RowPresence node) => "RowPresence";
        protected override string VisitInCheck(InCheck node) => "InCheck";
        protected override string VisitCollectionInCheck(CollectionInCheck node) => "CollectionInCheck";
        protected override string VisitPatternMatch(PatternMatch node) => "PatternMatch";
        protected override string VisitBetween(Between node) => "Between";
        protected override string VisitCaseWhen(CaseWhen node) => "CaseWhen";
        protected override string VisitCoalesce(Coalesce node) => "Coalesce";
        protected override string VisitAggregateRef(AggregateRef node) => "AggregateRef";
        protected override string VisitWindowFunctionRef(WindowFunctionRef node) => "WindowFunctionRef";
        protected override string VisitArrayAccess(ArrayAccess node) => "ArrayAccess";
        protected override string VisitCteTableRef(CteTableRef node) => "CteTableRef";
    }

    #endregion
}
