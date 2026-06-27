using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvSourcePlanningTests
{
    [TestMethod]
    public void Plan_WhenProjectionPredicateOrderAndSliceAreSupported_ShouldAcceptAll()
    {
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.And,
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.GreaterOrEqual,
                new SourcePredicateColumn(new SourceColumnRef("Amount")),
                new SourcePredicateLiteral(10m)),
            new SourcePredicateIn(
                new SourcePredicateColumn(new SourceColumnRef("Status")),
                [new SourcePredicateLiteral("Open"), new SourcePredicateLiteral("Closed")]));
        var request = SourcePlanRequest.Empty(new SourceIdentity("#csv", "file", "source-id", "Rows")) with
        {
            RequiredColumns =
            [
                new SourceColumnRef("Name"),
                new SourceColumnRef("Amount"),
                new SourceColumnRef("Status")
            ],
            Predicate = predicate,
            OrderBy = [new OrderByExpression(new SourceColumnRef("Name"), OrderDirection.Descending)],
            Skip = 1,
            Take = 2
        };

        var result = CsvSourcePlan.Create(request);

        Assert.AreEqual(3, result.AcceptedColumns.Count);
        Assert.AreEqual(predicate, result.AcceptedPredicate);
        Assert.IsNull(result.ResidualPredicate);
        Assert.AreEqual(1, result.AcceptedOrderBy.Count);
        Assert.AreEqual(1L, result.AcceptedSkip);
        Assert.AreEqual(2L, result.AcceptedTake);
        Assert.AreEqual(result.AcceptedPredicate, result.ExecutionPlan.AcceptedPredicate);
        Assert.AreEqual(result.AcceptedTake, result.ExecutionPlan.AcceptedTake);
    }

    [TestMethod]
    public void Plan_WhenAndPredicateHasUnsupportedSide_ShouldAcceptSafeSideAndKeepSliceResidual()
    {
        var accepted = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef("Name")),
            new SourcePredicateLiteral("Ada"));
        var residual = new UnsupportedPredicateExpression();
        var request = SourcePlanRequest.Empty(new SourceIdentity("#csv", "file", "source-id", "Rows")) with
        {
            RequiredColumns = [new SourceColumnRef("Name")],
            Predicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, accepted, residual),
            OrderBy = [new OrderByExpression(new SourceColumnRef("Name"), OrderDirection.Ascending)],
            Skip = 1,
            Take = 2
        };

        var result = CsvSourcePlan.Create(request);

        Assert.AreEqual(accepted, result.AcceptedPredicate);
        Assert.AreEqual(residual, result.ResidualPredicate);
        Assert.AreEqual(1, result.AcceptedOrderBy.Count);
        Assert.IsNull(result.AcceptedSkip);
        Assert.AreEqual(1L, result.ResidualSkip);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreEqual(2L, result.ResidualTake);
    }

    [TestMethod]
    public void Plan_WhenOrPredicateIsSupported_ShouldAcceptWholePredicate()
    {
        var predicate = new SourcePredicateLogical(
            SourcePredicateLogicalOperator.Or,
            new SourcePredicateComparison(
                SourcePredicateComparisonOperator.Equal,
                new SourcePredicateColumn(new SourceColumnRef("Status")),
                new SourcePredicateLiteral("Open")),
            new SourcePredicateNullCheck(new SourcePredicateColumn(new SourceColumnRef("Status"))));
        var request = SourcePlanRequest.Empty(new SourceIdentity("#csv", "file", "source-id", "Rows")) with
        {
            RequiredColumns = [new SourceColumnRef("Status")],
            Predicate = predicate
        };

        var result = CsvSourcePlan.Create(request);

        Assert.AreEqual(predicate, result.AcceptedPredicate);
        Assert.IsNull(result.ResidualPredicate);
    }

    [TestMethod]
    public void Plan_WhenOrderColumnIsUnknown_ShouldLeaveOrderAndSliceResidual()
    {
        var request = SourcePlanRequest.Empty(new SourceIdentity("#csv", "file", "source-id", "Rows")) with
        {
            RequiredColumns = [new SourceColumnRef("Name")],
            OrderBy = [new OrderByExpression(new SourceColumnRef("Missing"), OrderDirection.Ascending)],
            Take = 1
        };

        var result = CsvSourcePlan.Create(request);

        Assert.AreEqual(0, result.AcceptedOrderBy.Count);
        Assert.AreEqual(1, result.ResidualOrderBy.Count);
        Assert.IsNull(result.AcceptedTake);
        Assert.AreEqual(1L, result.ResidualTake);
    }

    private sealed record UnsupportedPredicateExpression : SourcePredicateExpression;
}
