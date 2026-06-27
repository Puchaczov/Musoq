using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SourcePlanningEvaluatorE2ETests : BasicEntityTestBase
{
    [TestMethod]
    public void TakeAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name from #sp.items() s take 5";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptTake, out var provider);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(5, optimized.Count);
        Assert.AreEqual(5, request.Take);
        Assert.AreEqual(5, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void SkipTakeAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name from #sp.items() s skip 3 take 4";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(4, optimized.Count);
        Assert.AreEqual(3, executionPlan.AcceptedSkip);
        Assert.AreEqual(4, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void OrderAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptOrder, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(SourcePlanningRows.CreateDefault().Count, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(nameof(SourcePlanningEntity.Score), executionPlan.AcceptedOrderBy[0].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, executionPlan.AcceptedOrderBy[0].Direction);
    }

    [TestMethod]
    public void OrderTakeAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc take 6";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(6, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void OrderSkipTakeAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc skip 2 take 5";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(5, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(2, executionPlan.AcceptedSkip);
        Assert.AreEqual(5, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void MultiColumnOrderAcceptedBySource_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Category, s.Score from #sp.items() s order by s.Category, s.Score desc, s.Id";

        AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(3, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(nameof(SourcePlanningEntity.Category), executionPlan.AcceptedOrderBy[0].Column.Name);
        Assert.AreEqual(nameof(SourcePlanningEntity.Score), executionPlan.AcceptedOrderBy[1].Column.Name);
        Assert.AreEqual(OrderDirection.Descending, executionPlan.AcceptedOrderBy[1].Direction);
        Assert.AreEqual(nameof(SourcePlanningEntity.Id), executionPlan.AcceptedOrderBy[2].Column.Name);
    }

    [TestMethod]
    public void AcceptedAscendingStringOrder_ShouldUseMusoqNullAndOrdinalSemantics()
    {
        const string query = "select s.Id, s.Name from #sp.items() s order by s.Name";
        var rows = CreateStringOrderingRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptNaiveOrder, rows, out _);

        AssertSameTable(baseline, optimized);
        CollectionAssert.AreEqual(new object[] { 2, 3, 5, 1, 4 }, ReadColumn(optimized, 0));
    }

    [TestMethod]
    public void AcceptedDescendingStringOrder_ShouldUseMusoqNullAndOrdinalSemantics()
    {
        const string query = "select s.Id, s.Name from #sp.items() s order by s.Name desc";
        var rows = CreateStringOrderingRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptNaiveOrder, rows, out _);

        AssertSameTable(baseline, optimized);
        CollectionAssert.AreEqual(new object[] { 4, 1, 5, 3, 2 }, ReadColumn(optimized, 0));
    }

    [TestMethod]
    public void OrderAcceptedButTakeResidual_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc take 6";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptOrder, out var provider);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(6, request.Take);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.IsFalse(executionPlan.AcceptedTake.HasValue);
    }

    [TestMethod]
    public void NaiveSourceOrderSkipTake_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc skip 2 take 6";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptNaiveOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(2, executionPlan.AcceptedSkip);
        Assert.AreEqual(6, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void TopNSourceOrderSkipTake_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc skip 2 take 6";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptTopNOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(2, executionPlan.AcceptedSkip);
        Assert.AreEqual(6, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void NaturalSourceOrderSkipTake_ShouldMatchResidualRuntimeExecution()
    {
        const string query = "select s.Id, s.Name, s.Score from #sp.items() s order by s.Score desc skip 2 take 6";
        var sortedRows = SourcePlanningRows.CreateDefault()
            .OrderByDescending(static row => row.Score)
            .ThenBy(static row => row.Id)
            .ToArray();
        var rows = new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#sp"] = sortedRows
        };

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptNaturalOrderSkipTake, rows, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(2, executionPlan.AcceptedSkip);
        Assert.AreEqual(6, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void MixedSourceFinalOrderTake_ShouldRemainGlobalAndMatchBaseline()
    {
        const string query = @"
            select l.Id, r.Name
            from #left.items() l
            inner join #right.items() r on l.JoinKey = r.JoinKey
            order by l.Score desc
            take 5";

        var rows = SourcePlanningSchemaProvider.CreatePair(SourcePlanningRows.CreateDefault());
        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptOrderSkipTake, rows, out var provider);

        AssertSameTable(baseline, optimized);
        Assert.AreEqual(2, provider.Requests.Count);
        Assert.IsTrue(provider.Requests.All(static request => request.OrderBy.Count == 0));
        Assert.IsTrue(provider.Requests.All(static request => !request.Skip.HasValue));
        Assert.IsTrue(provider.Requests.All(static request => !request.Take.HasValue));
        Assert.IsTrue(provider.ExecutionPlans.All(static plan => plan.AcceptedOrderBy.Count == 0));
        Assert.IsTrue(provider.ExecutionPlans.All(static plan => !plan.AcceptedTake.HasValue));
    }

    [TestMethod]
    public void ExactCardinality_ShouldBuildInnerHashJoinOnSmallerLeftSource()
    {
        const string query = @"
            select l.Id, r.Id
            from #left.items() l
            inner join #right.items() r on l.JoinKey = r.JoinKey
            order by l.Id, r.Id";
        var rows = CreateCardinalityJoinRows(leftCount: 4, rightCount: 24);

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.RejectAllWithExactCardinality, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.RejectAllWithExactCardinality, rows);

        AssertSameTable(baseline, optimized);
        Assert.Contains("PhysicalHashJoin [Inner] [build: l.JoinKey] [probe: r.JoinKey]", inspection.PhysicalPlanText);
        Assert.Contains("Cardinality fact selected the left source as hash build side", inspection.PlanningText);
        Assert.Contains("CardinalityFacts", inspection.PlanningText);
        Assert.Contains("SourceEstimate -> Exact exact=4 lower=4 upper=4 confidence=1 - Test source knows its exact row count.", inspection.PlanningText);
        Assert.Contains("SourceEstimate -> Exact exact=24 lower=24 upper=24 confidence=1 - Test source knows its exact row count.", inspection.PlanningText);
        Assert.Contains("capacity: 4", inspection.ExecutionPlanText);
        Assert.Contains("new List<Statement0Row0>(24)", inspection.GeneratedCSharpCode);
    }

    [TestMethod]
    public void LowConfidenceCardinality_ShouldKeepLegacyInnerHashJoinBuildSide()
    {
        const string query = @"
            select l.Id, r.Id
            from #left.items() l
            inner join #right.items() r on l.JoinKey = r.JoinKey
            order by l.Id, r.Id";
        var rows = CreateCardinalityJoinRows(leftCount: 4, rightCount: 24);

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.RejectAllWithLowConfidenceCardinality, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.RejectAllWithLowConfidenceCardinality, rows);

        AssertSameTable(baseline, optimized);
        Assert.Contains("PhysicalHashJoin [Inner] [build: r.JoinKey] [probe: l.JoinKey]", inspection.PhysicalPlanText);
        Assert.Contains("Cardinality facts were missing, unknown, or too low-confidence", inspection.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenSourcePartiallyAcceptsCapabilities_ShouldExplainNegotiatedCapabilities()
    {
        const string predicateQuery = @"
            select s.Id, s.Category
            from #sp.items() s
            where s.Category = 'alpha'
              and s.Score > 20";
        const string orderedQuery = @"
            select s.Id, s.Category, s.Score
            from #sp.items() s
            order by s.Score desc
            skip 1
            take 2";
        var rows = CreateSingleRows();

        var predicateInspection = Inspect(predicateQuery, SourcePlanningMode.AcceptFirstPredicate, rows);
        var orderedInspection = Inspect(orderedQuery, SourcePlanningMode.AcceptProjection, rows);

        Assert.Contains("source capability projection: requested=3, accepted=3, residual=0 -> Accepted", predicateInspection.PlanningText);
        Assert.Contains("source capability predicate: requested=yes, accepted=yes, residual=yes -> Partial", predicateInspection.PlanningText);
        Assert.Contains("source capability ordering: requested=1, accepted=0, residual=1 -> Rejected", orderedInspection.PlanningText);
        Assert.Contains("source capability slicing: requested=skip=1, take=2, accepted=skip=null, take=null, residual=skip=1, take=2 -> Rejected", orderedInspection.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenSourceReportsCardinality_ShouldExplainHashBuildUsability()
    {
        const string query = @"
            select l.Id, r.Id
            from #left.items() l
            inner join #right.items() r on l.JoinKey = r.JoinKey
            order by l.Id, r.Id";
        var rows = CreateCardinalityJoinRows(leftCount: 4, rightCount: 24);

        var exactInspection = Inspect(query, SourcePlanningMode.RejectAllWithExactCardinality, rows);
        var lowConfidenceInspection = Inspect(query, SourcePlanningMode.RejectAllWithLowConfidenceCardinality, rows);

        Assert.Contains("source capability cardinality: Exact confidence=1 usableForHashBuild=yes reason=Test source knows its exact row count.", exactInspection.PlanningText);
        Assert.Contains("source capability cardinality: Bounded confidence=0.25 usableForHashBuild=no reason=Test source row count is low confidence.", lowConfidenceInspection.PlanningText);
    }

    [TestMethod]
    public void ExactCardinality_ShouldNotChangeOuterJoinHashBuildSide()
    {
        const string query = @"
            select l.Id, r.Id
            from #left.items() l
            left outer join #right.items() r on l.JoinKey = r.JoinKey
            order by l.Id, r.Id";
        var rows = CreateCardinalityJoinRows(leftCount: 4, rightCount: 24);

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.RejectAllWithExactCardinality, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.RejectAllWithExactCardinality, rows);

        AssertSameTable(baseline, optimized);
        Assert.Contains("PhysicalHashJoin [LeftOuter] [build: r.JoinKey] [probe: l.JoinKey]", inspection.PhysicalPlanText);
        Assert.IsFalse(inspection.PlanningText.Contains("Cardinality selected the left source", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void GroupedFinalOrderTake_ShouldRemainGlobalAndMatchBaseline()
    {
        const string query = @"
            select s.Category, Count(s.Id)
            from #sp.items() s
            group by s.Category
            order by Count(s.Id) desc
            take 2";

        AssertNonSourceLocalQueryMatchesBaseline(query);
    }

    [TestMethod]
    public void DistinctFinalOrderTake_ShouldRemainGlobalAndMatchBaseline()
    {
        const string query = "select distinct s.Category from #sp.items() s order by s.Category take 2";

        AssertNonSourceLocalQueryMatchesBaseline(query);
    }

    [TestMethod]
    public void CteSourceLocalOrderSkipTakeAcceptedBySource_ShouldMatchBaseline()
    {
        const string query = @"
            with ranked as (
                select s.Id as Id, s.Name as Name, s.Score as Score
                from #sp.items() s
                order by s.Score desc
                skip 2
                take 5
            )
            select ranked.Id, ranked.Name, ranked.Score
            from ranked";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptTopNOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(5, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(2, executionPlan.AcceptedSkip);
        Assert.AreEqual(5, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void DerivedTableSourceLocalOrderTakeAcceptedBySource_ShouldMatchBaseline()
    {
        const string query = @"
            select q.Id, q.Name, q.Score
            from (
                select s.Id as Id, s.Name as Name, s.Score as Score
                from #sp.items() s
                order by s.Score desc
                take 6
            ) q";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptTopNOrderSkipTake, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(6, optimized.Count);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(6, executionPlan.AcceptedTake);
    }

    [TestMethod]
    public void CteFinalOrderTake_ShouldRemainGlobalAndMatchBaseline()
    {
        const string query = @"
            with allItems as (
                select s.Id as Id, s.Name as Name, s.Score as Score
                from #sp.items() s
            )
            select allItems.Id, allItems.Name, allItems.Score
            from allItems
            order by allItems.Score desc
            take 5";

        AssertNonSourceLocalQueryMatchesBaseline(query);
    }

    [TestMethod]
    public void ProjectionAcceptedBySource_ShouldPassRequiredColumnsAndAvoidUnusedExpensiveColumn()
    {
        const string query = "select s.Id, s.Name from #sp.items() s";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out var baselineProvider);
        var optimized = Run(query, SourcePlanningMode.AcceptProjection, rows, out var optimizedProvider);
        var request = optimizedProvider.Requests.Single();
        var executionPlan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Id), nameof(SourcePlanningEntity.Name) },
            request.RequiredColumns.Select(static column => column.Name).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Id), nameof(SourcePlanningEntity.Name) },
            executionPlan.AcceptedColumns.Select(static column => column.Name).ToArray());
        Assert.IsFalse(request.RequiredColumns.Any(static column =>
            column.Name == nameof(SourcePlanningEntity.ExpensivePayload)));
        Assert.IsTrue(baselineProvider.Recorder.ExpensivePayloadComputations > 0);
        Assert.AreEqual(0, optimizedProvider.Recorder.ExpensivePayloadComputations);
    }

    [TestMethod]
    public void ProjectionAcceptedBySource_ShouldRequestUsedExpensiveColumn()
    {
        const string query = "select s.Id, s.ExpensivePayload from #sp.items() s take 3";

        var optimized = AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptProjection, out var provider);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        Assert.AreEqual(3, optimized.Count);
        Assert.IsTrue(request.RequiredColumns.Any(static column =>
            column.Name == nameof(SourcePlanningEntity.ExpensivePayload)));
        Assert.IsTrue(executionPlan.AcceptedColumns.Any(static column =>
            column.Name == nameof(SourcePlanningEntity.ExpensivePayload)));
        Assert.IsTrue(provider.Recorder.ExpensivePayloadComputations > 0);
    }

    [TestMethod]
    public void ProjectionAcceptedBySource_WhenCoalesceLeftIsNonNullable_ShouldNotRequestDeadFallbackColumn()
    {
        const string missingColumn = "MissingColumn";
        const string query = "select s.Score ?? s.MissingColumn from #sp.items() s";

        AssertOptimizedMatchesBaseline(query, SourcePlanningMode.AcceptProjection, out var provider);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Score) },
            request.RequiredColumns.Select(static column => column.Name).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Score) },
            executionPlan.AcceptedColumns.Select(static column => column.Name).ToArray());
        Assert.IsFalse(request.RequiredColumns.Any(column => column.Name == missingColumn));
    }

    [TestMethod]
    public void ProjectionAcceptedBySource_WhenCteConsumerNeedsSubset_ShouldRequestConsumerColumnsOnly()
    {
        const string query = @"
            with people as (
                select s.Id as Id, s.Name as Name, s.ExpensivePayload as ExpensivePayload
                from #sp.items() s
            )
            select people.Id
            from people";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out var baselineProvider);
        var optimized = Run(query, SourcePlanningMode.AcceptProjection, rows, out var optimizedProvider);
        var request = optimizedProvider.Requests.Single();
        var executionPlan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Id) },
            request.RequiredColumns.Select(static column => column.Name).ToArray());
        CollectionAssert.AreEquivalent(
            new[] { nameof(SourcePlanningEntity.Id) },
            executionPlan.AcceptedColumns.Select(static column => column.Name).ToArray());
        Assert.IsTrue(baselineProvider.Recorder.ExpensivePayloadComputations > 0);
        Assert.AreEqual(0, optimizedProvider.Recorder.ExpensivePayloadComputations);
    }

    [TestMethod]
    public void PredicateAcceptedBySource_ShouldFilterBeforeRuntimeResidualAndMatchBaseline()
    {
        const string query = @"
            select s.Id, s.Category
            from #sp.items() s
            where s.Category = 'alpha' and s.Score + 1 > 0";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out var baselineProvider);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out var optimizedProvider);
        var request = optimizedProvider.Requests.Single();
        var executionPlan = optimizedProvider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNotNull(request.Predicate);
        Assert.IsNotNull(executionPlan.AcceptedPredicate);
        Assert.IsTrue(optimized.Count > 0);
        Assert.IsTrue(optimizedProvider.Recorder.SourceRowsProduced < baselineProvider.Recorder.SourceRowsProduced);
    }

    [TestMethod]
    public void AcceptedPredicateConjunct_ShouldBeRemovedFromRuntimeFilter()
    {
        const string query = @"
            select s.Id, s.Category
            from #sp.items() s
            where s.Category = 'alpha'";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.AcceptPredicate, rows);

        AssertSameTable(baseline, optimized);
        Assert.IsFalse(inspection.PhysicalPlanText.Contains("PhysicalFilter", System.StringComparison.Ordinal));
    }

    [TestMethod]
    public void PartiallyAcceptedPredicateConjunct_ShouldKeepOnlyResidualRuntimeFilter()
    {
        const string query = @"
            select s.Id, s.Category, s.Score
            from #sp.items() s
            where s.Category = 'alpha'
              and s.Score > 20
              and s.Score + 1 > 0";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptFirstPredicate, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.AcceptFirstPredicate, rows);
        var filterLine = GetSinglePhysicalFilterLine(inspection.PhysicalPlanText);

        AssertSameTable(baseline, optimized);
        Assert.IsFalse(filterLine.Contains("Category", System.StringComparison.Ordinal));
        Assert.Contains("s.Score > 20", filterLine);
        Assert.Contains("(s.Score + 1) > 0", filterLine);
    }

    [TestMethod]
    public void FilteredOrderTakeAcceptedBySource_ShouldRemoveRuntimeFilterAndTopN()
    {
        const string query = @"
            select s.Id, s.Category, s.Score
            from #sp.items() s
            where s.Category = 'alpha'
            order by s.Score desc
            take 3";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicateOrderSkipTake, rows, out var provider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptPredicateOrderSkipTake, rows);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNotNull(request.Predicate);
        Assert.AreEqual(1, request.OrderBy.Count);
        Assert.AreEqual(3, request.Take);
        Assert.IsNotNull(executionPlan.AcceptedPredicate);
        Assert.AreEqual(1, executionPlan.AcceptedOrderBy.Count);
        Assert.AreEqual(3, executionPlan.AcceptedTake);
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.DoesNotContain("PhysicalTopN", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void FilteredOrderTake_WhenPredicateIsResidual_ShouldKeepRuntimeFilterAndTopN()
    {
        const string query = @"
            select s.Id, s.Category, s.Score
            from #sp.items() s
            where s.Category = 'alpha'
            order by s.Score desc
            take 3";
        var rows = CreateSingleRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptTopNOrderSkipTake, rows, out var provider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptTopNOrderSkipTake, rows);
        var request = provider.Requests.Single();
        var executionPlan = provider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNotNull(request.Predicate);
        Assert.AreEqual(1, request.OrderBy.Count);
        Assert.AreEqual(3, request.Take);
        Assert.IsNull(executionPlan.AcceptedPredicate);
        Assert.AreEqual(0, executionPlan.AcceptedOrderBy.Count);
        Assert.IsFalse(executionPlan.AcceptedTake.HasValue);
        Assert.Contains("PhysicalFilter", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalTopN", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void PredicateAcceptedBySource_ShouldSupportInAndNullChecks()
    {
        const string query = @"
            select s.Id, s.Name
            from #sp.items() s
            where s.Name is null or s.Name in ('Alpha', 'beta')";
        var rows = CreateStringOrderingRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out var provider);
        var executionPlan = provider.ExecutionPlans.Single();

        AssertSameTable(baseline, optimized);
        Assert.IsNotNull(executionPlan.AcceptedPredicate);
        CollectionAssert.AreEqual(new object[] { 2, 3, 4 }, ReadColumn(optimized, 0));
    }

    [TestMethod]
    public void AcceptedOrPredicate_WhenSourceReportsNoResidual_ShouldRemoveRuntimeFilter()
    {
        const string query = @"
            select s.Id, s.Name
            from #sp.items() s
            where s.Category = 'alpha' or s.Category = 'beta'";
        var rows = CreateStringOrderingRows();

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out _);
        var inspection = Inspect(query, SourcePlanningMode.AcceptPredicate, rows);

        AssertSameTable(baseline, optimized);
        Assert.DoesNotContain("PhysicalFilter", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void JoinLocalPredicateAcceptedBySource_ShouldPushRequestAndKeepJoinGuard()
    {
        const string query = @"
            select l.Id, r.Id
            from #left.items() l
            inner join #right.items() r
                on l.JoinKey = r.JoinKey
               and l.Category = 'alpha'
            order by l.Id, r.Id";
        var rows = SourcePlanningSchemaProvider.CreatePair(SourcePlanningRows.CreateDefault());

        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out var baselineProvider);
        var optimized = Run(query, SourcePlanningMode.AcceptPredicate, rows, out var optimizedProvider);
        var inspection = Inspect(query, SourcePlanningMode.AcceptPredicate, rows);

        AssertSameTable(baseline, optimized);
        Assert.IsTrue(optimizedProvider.Requests.Any(static request =>
            request.Identity.Alias == "l" && request.Predicate != null));
        Assert.IsTrue(optimizedProvider.ExecutionPlans.Any(static plan =>
            plan.Identity.Alias == "l" && plan.AcceptedPredicate != null));
        Assert.IsTrue(optimizedProvider.Recorder.SourceRowsProduced < baselineProvider.Recorder.SourceRowsProduced);
        Assert.Contains("SourcePredicateMovementExpansion", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin", inspection.PhysicalPlanText);
        Assert.Contains("residual: (l.Category = 'alpha')", inspection.PhysicalPlanText);
    }

    private Table AssertOptimizedMatchesBaseline(
        string query,
        SourcePlanningMode mode,
        out SourcePlanningSchemaProvider optimizedProvider)
    {
        var rows = CreateSingleRows();
        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, mode, rows, out optimizedProvider);

        AssertSameTable(baseline, optimized);
        return optimized;
    }

    private void AssertNonSourceLocalQueryMatchesBaseline(string query)
    {
        var rows = CreateSingleRows();
        var baseline = Run(query, SourcePlanningMode.RejectAll, rows, out _);
        var optimized = Run(query, SourcePlanningMode.AcceptOrderSkipTake, rows, out var provider);

        AssertSameTable(baseline, optimized);
        Assert.AreEqual(1, provider.Requests.Count);
        Assert.IsTrue(provider.Requests.All(static request => request.OrderBy.Count == 0));
        Assert.IsTrue(provider.Requests.All(static request => !request.Skip.HasValue));
        Assert.IsTrue(provider.Requests.All(static request => !request.Take.HasValue));
    }

    private Table Run(
        string query,
        SourcePlanningMode mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> rows,
        out SourcePlanningSchemaProvider provider)
    {
        provider = new SourcePlanningSchemaProvider(mode, rows);
        var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }

    private QueryInspectionResult Inspect(
        string query,
        SourcePlanningMode mode,
        IReadOnlyDictionary<string, IReadOnlyList<SourcePlanningEntity>> rows)
    {
        return InstanceCreator.CompileForInspection(
            query,
            System.Guid.NewGuid().ToString(),
            new SourcePlanningSchemaProvider(mode, rows),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static Dictionary<string, IReadOnlyList<SourcePlanningEntity>> CreateSingleRows()
    {
        return new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#sp"] = SourcePlanningRows.CreateDefault()
        };
    }

    private static Dictionary<string, IReadOnlyList<SourcePlanningEntity>> CreateStringOrderingRows()
    {
        return new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#sp"] =
            [
                new SourcePlanningEntity { Id = 1, Name = "alpha", Category = "case", Score = 1 },
                new SourcePlanningEntity { Id = 2, Name = null, Category = "case", Score = 2 },
                new SourcePlanningEntity { Id = 3, Name = "Alpha", Category = "case", Score = 3 },
                new SourcePlanningEntity { Id = 4, Name = "beta", Category = "case", Score = 4 },
                new SourcePlanningEntity { Id = 5, Name = "Beta", Category = "case", Score = 5 }
            ]
        };
    }

    private static Dictionary<string, IReadOnlyList<SourcePlanningEntity>> CreateCardinalityJoinRows(
        int leftCount,
        int rightCount)
    {
        var rows = SourcePlanningRows.CreateDefault();
        return new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#left"] = rows.Take(leftCount).ToArray(),
            ["#right"] = rows.Take(rightCount).Select(static row => new SourcePlanningEntity
            {
                Id = row.Id + 1000,
                Name = $"right-{row.Name}",
                Category = row.Category,
                Score = row.Score,
                CreatedAt = row.CreatedAt,
                JoinKey = row.JoinKey,
                ExpensivePayload = row.ExpensivePayload
            }).ToArray()
        };
    }

    private static object[] ReadColumn(Table table, int columnIndex)
    {
        return Enumerable.Range(0, table.Count)
            .Select(index => table[index][columnIndex])
            .ToArray();
    }

    private static string GetSinglePhysicalFilterLine(string physicalPlanText)
    {
        return physicalPlanText
            .Split('\n')
            .Select(static line => line.Trim())
            .Single(static line => line.StartsWith("PhysicalFilter", System.StringComparison.Ordinal));
    }

    private static void AssertSameTable(Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count);
        Assert.AreEqual(expected.Columns.Count(), actual.Columns.Count());

        for (var rowIndex = 0; rowIndex < expected.Count; rowIndex++)
        {
            CollectionAssert.AreEqual(
                expected[rowIndex].Values,
                actual[rowIndex].Values,
                $"Row {rowIndex} differs.");
        }
    }
}
