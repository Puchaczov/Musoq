using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ProjectionAndApplyLoweringModelTests
{
    [TestMethod]
    public void ProjectionFieldCollectionResult_Factories_ShouldPreserveSupportState()
    {
        var success = ProjectionFieldCollectionResult.Success();
        var unsupported = ProjectionFieldCollectionResult.Unsupported("unsupported projection");

        Assert.IsTrue(success.Supported);
        Assert.AreEqual(string.Empty, success.UnsupportedReason);
        Assert.IsFalse(unsupported.Supported);
        Assert.AreEqual("unsupported projection", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void NullExtendedProjectionBuildResult_Unsupported_ShouldExposeEmptySentinelAppendRows()
    {
        var result = NullExtendedProjectionBuildResult.Unsupported("cannot substitute");

        Assert.IsFalse(result.Supported);
        Assert.AreEqual("cannot substitute", result.UnsupportedReason);
        Assert.AreEqual(string.Empty, result.ResultShape.TypeName);
        Assert.HasCount(0, result.ResultShape.Fields);
        Assert.AreSame(result.MatchedAppendRow, result.UnmatchedAppendRow);
        Assert.AreSame(result.ResultShape, result.MatchedAppendRow.RowShape);
    }

    [TestMethod]
    public void FullOuterNullExtendedProjectionBuildResult_Unsupported_ShouldExposeEmptySentinelAppendRows()
    {
        var result = FullOuterNullExtendedProjectionBuildResult.Unsupported("cannot substitute");

        Assert.IsFalse(result.Supported);
        Assert.AreEqual("cannot substitute", result.UnsupportedReason);
        Assert.AreEqual(string.Empty, result.ResultShape.TypeName);
        Assert.HasCount(0, result.ResultShape.Fields);
        Assert.AreSame(result.MatchedAppendRow, result.LeftOnlyAppendRow);
        Assert.AreSame(result.MatchedAppendRow, result.RightOnlyAppendRow);
        Assert.AreSame(result.ResultShape, result.MatchedAppendRow.RowShape);
    }

    [TestMethod]
    public void OuterApplyFilterBuildResult_Factories_ShouldPreserveBlocks()
    {
        var matched = new ExecutionBlock([new ExecutionContinue()]);
        var unmatched = new ExecutionBlock([new ExecutionBreak()]);
        var success = OuterApplyFilterBuildResult.Success(matched, unmatched);
        var unsupported = OuterApplyFilterBuildResult.Unsupported("bad filter");

        Assert.IsTrue(success.Supported);
        Assert.AreSame(matched, success.MatchedAppendBlock);
        Assert.AreSame(unmatched, success.UnmatchedAppendBlock);
        Assert.IsFalse(unsupported.Supported);
        Assert.AreSame(ExecutionBlock.Empty, unsupported.MatchedAppendBlock);
        Assert.AreSame(ExecutionBlock.Empty, unsupported.UnmatchedAppendBlock);
        Assert.AreEqual("bad filter", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionResult_Factories_ShouldPreserveKnownUnknownAndUnsupportedStates()
    {
        var expression = new ExecutionLiteral(42, typeof(int));
        var known = OuterApplyNullSubstitutionResult.Known(expression);
        var unknown = OuterApplyNullSubstitutionResult.Unknown();
        var unsupported = OuterApplyNullSubstitutionResult.Unsupported("raw expression");

        Assert.IsTrue(known.Supported);
        Assert.IsFalse(known.IsUnknown);
        Assert.AreSame(expression, known.Expression);
        Assert.IsTrue(unknown.Supported);
        Assert.IsTrue(unknown.IsUnknown);
        Assert.IsInstanceOfType<ExecutionLiteral>(unknown.Expression);
        Assert.AreEqual(typeof(bool), unknown.Expression.ReturnType.ClrType);
        Assert.IsFalse(unsupported.Supported);
        Assert.IsTrue(unsupported.IsUnknown);
        Assert.AreEqual("raw expression", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void OuterApplyArgumentSubstitutionResult_Factories_ShouldPreserveExpressionsAndUnknownFlag()
    {
        var expressions = new ExecutionExpression[]
        {
            new ExecutionLiteral("alpha", typeof(string))
        };
        var success = OuterApplyArgumentSubstitutionResult.Success(expressions, hasUnknown: true);
        var unsupported = OuterApplyArgumentSubstitutionResult.Unsupported("bad argument");

        Assert.IsTrue(success.Supported);
        Assert.AreSame(expressions, success.Expressions);
        Assert.IsTrue(success.HasUnknown);
        Assert.IsFalse(unsupported.Supported);
        Assert.HasCount(0, unsupported.Expressions);
        Assert.IsTrue(unsupported.HasUnknown);
        Assert.AreEqual("bad argument", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void OuterApplyCaseElseSubstitutionResult_Factories_ShouldPreserveKnownUnknownAndUnsupportedStates()
    {
        var expression = new ExecutionLiteral("fallback", typeof(string));
        var known = OuterApplyCaseElseSubstitutionResult.Known(expression);
        var knownNull = OuterApplyCaseElseSubstitutionResult.Known(null);
        var unknown = OuterApplyCaseElseSubstitutionResult.Unknown();
        var unsupported = OuterApplyCaseElseSubstitutionResult.Unsupported("bad else");

        Assert.IsTrue(known.Supported);
        Assert.IsFalse(known.IsUnknown);
        Assert.AreSame(expression, known.Expression);
        Assert.IsTrue(knownNull.Supported);
        Assert.IsFalse(knownNull.IsUnknown);
        Assert.IsNull(knownNull.Expression);
        Assert.IsTrue(unknown.Supported);
        Assert.IsTrue(unknown.IsUnknown);
        Assert.IsNull(unknown.Expression);
        Assert.IsFalse(unsupported.Supported);
        Assert.IsTrue(unsupported.IsUnknown);
        Assert.AreEqual("bad else", unsupported.UnsupportedReason);
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionService_WhenExpressionReadsRightAlias_ShouldReturnUnknown()
    {
        var expression = new ExecutionFieldRead("orders", "Total", typeof(decimal));

        var result = OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, "orders");

        Assert.IsTrue(result.Supported);
        Assert.IsTrue(result.IsUnknown);
        Assert.IsInstanceOfType<ExecutionLiteral>(result.Expression);
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionService_WhenCoalesceContainsRightAlias_ShouldDropUnknownArguments()
    {
        var fallback = new ExecutionLiteral("fallback", typeof(string));
        var expression = new ExecutionCoalesce(
            [
                new ExecutionFieldRead("orders", "Code", typeof(string)),
                fallback
            ],
            typeof(string));

        var result = OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, "orders");

        Assert.IsTrue(result.Supported);
        Assert.IsFalse(result.IsUnknown);
        Assert.AreSame(fallback, result.Expression);
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionService_WhenCaseBranchResultReadsRightAlias_ShouldLiftResultType()
    {
        var expression = new ExecutionCaseWhen(
            [
                new ExecutionCaseWhenBranch(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionFieldRead("orders", "Total", typeof(int)))
            ],
            new ExecutionLiteral(5, typeof(int)),
            typeof(int));

        var result = OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, "orders");

        Assert.IsTrue(result.Supported);
        Assert.IsFalse(result.IsUnknown);
        var rewritten = Assert.IsInstanceOfType<ExecutionCaseWhen>(result.Expression);
        Assert.AreEqual(typeof(int?), rewritten.ReturnType.ClrType);
        var branchLiteral = Assert.IsInstanceOfType<ExecutionLiteral>(rewritten.Branches[0].Result);
        Assert.AreEqual(ExecutionConstantKind.Null, branchLiteral.Value.Kind);
        Assert.AreEqual(typeof(int?), branchLiteral.ReturnType.ClrType);
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionService_WhenNormalizingNullableBoolean_ShouldCompareToTrue()
    {
        var expression = new ExecutionFieldRead("orders", "IsOpen", typeof(bool?));

        var result = OuterApplyNullSubstitutionService.NormalizeBooleanOperand(expression);

        Assert.IsTrue(result.Supported);
        var binary = Assert.IsInstanceOfType<ExecutionBinary>(result.Value);
        Assert.AreEqual(BinaryOpKind.Equal, binary.Kind);
        Assert.AreSame(expression, binary.Left);
        var literal = Assert.IsInstanceOfType<ExecutionLiteral>(binary.Right);
        Assert.AreEqual(true, literal.Value.ToClrValue());
    }

    [TestMethod]
    public void OuterApplyNullSubstitutionService_WhenAggregateResultReferenceIsUnresolved_ShouldBeUnsupported()
    {
        var expression = new ExecutionAggregateResultRef("sum-orders-total", "Sum(orders.Total)", ExecutionTypeRef.FromClr(typeof(decimal)));

        var result = OuterApplyNullSubstitutionService.SubstituteRightAlias(expression, "orders");

        Assert.IsFalse(result.Supported);
        StringAssert.Contains(result.UnsupportedReason, nameof(ExecutionAggregateResultRef));
    }
}
