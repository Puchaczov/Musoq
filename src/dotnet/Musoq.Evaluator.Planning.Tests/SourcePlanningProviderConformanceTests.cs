using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourcePlanningProviderConformanceTests
{
    [TestMethod]
    public void ComparisonsWithNull_ShouldNeverAdmitUnknownRows()
    {
        foreach (var operation in Enum.GetValues<SourcePredicateComparisonOperator>())
        {
            AssertAdmission(new SourcePredicateComparison(operation, new SourcePredicateLiteral(null),
                new SourcePredicateLiteral(null)), false, operation.ToString());
            AssertAdmission(new SourcePredicateComparison(operation, new SourcePredicateLiteral(null),
                new SourcePredicateLiteral(1)), false, operation.ToString());
        }
    }

    [TestMethod]
    public void MixedNumericComparisons_ShouldPromoteBothOperands()
    {
        foreach (var value in new object[] { 1L, 1m, 1d, 1f, (short)1 })
        {
            AssertAdmission(new SourcePredicateComparison(SourcePredicateComparisonOperator.Equal,
                new SourcePredicateLiteral(1), new SourcePredicateLiteral(value)), true, value.GetType().Name);
            AssertAdmission(new SourcePredicateComparison(SourcePredicateComparisonOperator.GreaterThan,
                new SourcePredicateLiteral(2), new SourcePredicateLiteral(value)), true, value.GetType().Name);
        }
    }

    [TestMethod]
    public void Membership_ShouldUseNumericPromotionAndSqlNullAdmission()
    {
        AssertAdmission(new SourcePredicateIn(new SourcePredicateLiteral(1), [new SourcePredicateLiteral(1L)]), true);
        AssertAdmission(new SourcePredicateIn(new SourcePredicateLiteral(null), [new SourcePredicateLiteral(null)]), false);
        AssertAdmission(new SourcePredicateIn(new SourcePredicateLiteral(2), [new SourcePredicateLiteral(1),
            new SourcePredicateLiteral(null)], IsNegated: true), false);
        AssertAdmission(new SourcePredicateIn(new SourcePredicateLiteral(1), [new SourcePredicateLiteral(null),
            new SourcePredicateLiteral(1L)]), true);
    }

    [TestMethod]
    public void FloatingPromotionAndNaN_ShouldFollowNumericOperatorSemantics()
    {
        AssertAdmission(new SourcePredicateComparison(SourcePredicateComparisonOperator.Equal,
            new SourcePredicateLiteral(16777217), new SourcePredicateLiteral(16777216f)), true);
        AssertAdmission(new SourcePredicateIn(new SourcePredicateLiteral(float.NaN), [new SourcePredicateLiteral(float.NaN)]), false);
        AssertAdmission(new SourcePredicateComparison(SourcePredicateComparisonOperator.Equal,
            new SourcePredicateLiteral(double.NaN), new SourcePredicateLiteral(double.NaN)), false);
        AssertAdmission(new SourcePredicateComparison(SourcePredicateComparisonOperator.NotEqual,
            new SourcePredicateLiteral(double.NaN), new SourcePredicateLiteral(double.NaN)), true);
    }

    [TestMethod]
    public void UnsupportedScalarPredicate_ShouldBeRejectedByAcceptingFixture()
    {
        Assert.IsFalse(SourcePlanningRowExecution.CanEvaluatePredicate(new SourcePredicateLiteral(2)));
        Assert.IsTrue(SourcePlanningRowExecution.CanEvaluatePredicate(new SourcePredicateLiteral(false)));
    }

    [TestMethod]
    public void UnsupportedAcceptedPredicate_ShouldKeepSlicingResidual()
    {
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with { Predicate = new SourcePredicateLiteral(2), Take = 3 };
        var result = SourcePlanningPlanResultBuilder.CreateAccepted(request, [], [], null, null, 3, null,
            acceptedPredicate: request.Predicate, predicateAccepted: true);
        Assert.IsNull(result.AcceptedPredicate);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreSame(request.Predicate, result.ResidualPredicate);
        Assert.AreEqual(3L, result.ResidualTake);
    }

    private static void AssertAdmission(SourcePredicateExpression predicate, bool expected, string context = "")
    {
        var plan = SourceExecutionPlan.Empty(SourceIdentity.Empty) with { AcceptedPredicate = predicate };
        var options = new SourcePlanningRowExecutionOptions<int>("strategy", _ => row => row,
            row => row, (rows, _) => rows);
        Assert.AreEqual(expected ? 1 : 0, SourcePlanningRowExecution.ApplyPlan([1], plan, options).Count(), context);
    }
}
