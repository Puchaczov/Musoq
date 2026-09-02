using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class EnumSourcePredicatePlanningTests
{
    [TestMethod]
    public void Converter_WhenEnumComparisonAndNegatedIn_AreOfferedAsUnboxedEnumLiterals()
    {
        var descriptor = CreateDescriptor();
        var column = CreateColumn(descriptor);
        var equality = new BinaryOp(
            BinaryOpKind.Equal,
            column,
            new Literal(1u, typeof(uint)),
            typeof(bool));
        var inCheck = new InCheck(
            column,
            [new Literal(1u, typeof(uint)), new Literal(2u, typeof(uint))],
            typeof(bool),
            IsNegated: true);

        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(equality, "s", out var equalityPredicate));
        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(inCheck, "s", out var inPredicate));

        var comparison = Assert.IsInstanceOfType<SourcePredicateComparison>(equalityPredicate);
        var literal = Assert.IsInstanceOfType<SourcePredicateEnumLiteral>(comparison.Right);
        Assert.AreEqual(EnumScalarValue.FromUInt32(1), literal.Value);
        Assert.AreEqual(descriptor.Fingerprint, literal.EnumFingerprint);

        var sourceIn = Assert.IsInstanceOfType<SourcePredicateIn>(inPredicate);
        Assert.IsTrue(sourceIn.IsNegated);
        Assert.IsTrue(sourceIn.Values.All(static value => value is SourcePredicateEnumLiteral));
    }

    [TestMethod]
    public void Converter_WhenFlagsHelpersArePositive_UsesExplicitAnyAndAllMasks()
    {
        var descriptor = CreateDescriptor();
        var any = CreateFlagsCall(EnumIntrinsicKind.HasAnyFlags, descriptor);
        var all = CreateFlagsCall(EnumIntrinsicKind.HasAllFlags, descriptor);

        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(any, "s", out var anyPredicate));
        Assert.IsTrue(SourcePredicateExpressionConverter.TryConvertPredicate(all, "s", out var allPredicate));

        var anyFlags = Assert.IsInstanceOfType<SourcePredicateFlags>(anyPredicate);
        var allFlags = Assert.IsInstanceOfType<SourcePredicateFlags>(allPredicate);
        Assert.AreEqual(SourcePredicateFlagsMatchMode.Any, anyFlags.MatchMode);
        Assert.AreEqual(SourcePredicateFlagsMatchMode.All, allFlags.MatchMode);
        Assert.AreEqual(EnumScalarValue.FromUInt32(3), anyFlags.Mask.Value);
        Assert.AreEqual(descriptor.Fingerprint, allFlags.Mask.EnumFingerprint);
    }

    [TestMethod]
    public void Converter_WhenFlagsHelperIsNegated_LeavesItForCoreResidualEvaluation()
    {
        var descriptor = CreateDescriptor();
        var negated = new UnaryOp(
            UnaryOpKind.Not,
            CreateFlagsCall(EnumIntrinsicKind.HasAnyFlags, descriptor),
            typeof(bool));

        Assert.IsFalse(SourcePredicateExpressionConverter.TryConvertPredicate(negated, "s", out _));
    }

    [TestMethod]
    public void Comparer_WhenEnumFingerprintsDiffer_DoesNotTreatPredicatesAsEqual()
    {
        var descriptor = CreateDescriptor();
        var sameValue = EnumScalarValue.FromUInt32(1);
        var left = new SourcePredicateEnumLiteral(sameValue, descriptor.Fingerprint);
        var right = new SourcePredicateEnumLiteral(sameValue, new string('A', 64));

        Assert.IsFalse(SourcePredicateExpressionComparer.Instance.Equals(left, right));
    }

    [TestMethod]
    public void PlanContract_WhenAcceptedAndResidualAreAnExactPartition_AcceptsIt()
    {
        var descriptor = CreateDescriptor();
        var first = CreateEnumComparison(descriptor, 1u);
        var second = CreateEnumComparison(descriptor, 2u);
        var requestPredicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, first, second);
        var request = CreateRequest(requestPredicate);
        var result = new SourcePlanResult
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with { AcceptedPredicate = first },
            AcceptedPredicate = first,
            ResidualPredicate = second
        };

        SourcePredicatePlanContractValidator.Validate(request, result, TextSpan.Empty);
    }

    [TestMethod]
    public void PlanContract_WhenDatasourceChangesEnumFingerprint_RejectsIt()
    {
        var descriptor = CreateDescriptor();
        var requested = CreateEnumComparison(descriptor, 1u);
        var request = CreateRequest(requested);
        var corrupted = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Access")),
            new SourcePredicateEnumLiteral(EnumScalarValue.FromUInt32(1), new string('A', 64)));
        var result = new SourcePlanResult
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with { AcceptedPredicate = corrupted },
            AcceptedPredicate = corrupted
        };

        Assert.Throws<EnumDescriptorMismatchException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, result, TextSpan.Empty));
    }

    [TestMethod]
    public void PlanContract_WhenAcceptedPredicateIsAlsoReturnedAsResidual_RejectsIt()
    {
        var predicate = CreateEnumComparison(CreateDescriptor(), 1u);
        var request = CreateRequest(predicate);
        var result = new SourcePlanResult
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity) with { AcceptedPredicate = predicate },
            AcceptedPredicate = predicate,
            ResidualPredicate = predicate
        };

        Assert.Throws<SourcePlanContractException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, result, TextSpan.Empty));
    }

    [TestMethod]
    public void PlanContract_WhenExecutionPlanDoesNotRepeatAcceptedPredicate_RejectsIt()
    {
        var predicate = CreateEnumComparison(CreateDescriptor(), 1u);
        var request = CreateRequest(predicate);
        var result = new SourcePlanResult
        {
            ExecutionPlan = SourceExecutionPlan.Empty(request.Identity),
            AcceptedPredicate = predicate
        };

        Assert.Throws<SourcePlanContractException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, result, TextSpan.Empty));
    }

    private static MethodCall CreateFlagsCall(EnumIntrinsicKind kind, EnumTypeDescriptor descriptor)
    {
        return new MethodCall(
            EnumIntrinsicMethodFacts.Bind(kind, typeof(uint)),
            [CreateColumn(descriptor), new Literal(3u, typeof(uint))],
            null,
            typeof(bool))
        {
            EnumIntrinsic = kind,
            OperandEnumType = descriptor,
            EnumMask = EnumScalarValue.FromUInt32(3)
        };
    }

    private static ColumnRef CreateColumn(EnumTypeDescriptor descriptor)
    {
        return new ColumnRef("s", "Access", typeof(uint)) { EnumType = descriptor };
    }

    private static SourcePredicateComparison CreateEnumComparison(EnumTypeDescriptor descriptor, uint value)
    {
        return new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Access")),
            new SourcePredicateEnumLiteral(EnumScalarValue.FromUInt32(value), descriptor.Fingerprint));
    }

    private static SourcePlanRequest CreateRequest(SourcePredicateExpression predicate)
    {
        return new SourcePlanRequest
        {
            Identity = new SourceIdentity("test", "rows", "source:0", "s"),
            Predicate = predicate
        };
    }

    private static EnumTypeDescriptor CreateDescriptor()
    {
        return new EnumTypeDescriptor(
            "FileAccess",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.UInt32,
            true,
            [
                new EnumMemberDescriptor("None", EnumScalarValue.FromUInt32(0)),
                new EnumMemberDescriptor("Read", EnumScalarValue.FromUInt32(1)),
                new EnumMemberDescriptor("Write", EnumScalarValue.FromUInt32(2)),
                new EnumMemberDescriptor("ReadWrite", EnumScalarValue.FromUInt32(3))
            ]);
    }
}
