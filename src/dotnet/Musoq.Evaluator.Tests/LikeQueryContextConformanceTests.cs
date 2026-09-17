using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class LikeQueryContextConformanceTests : BasicEntityTestBase
{
    [TestMethod]
    public void Projection_ShouldReturnBooleanForEveryRow()
    {
        const string query = "select Id, Name like 'Al%' as Matched from #A.Entities() order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha"), Row(2, "Beta"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Matched", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, true], [2, false]);
    }

    [TestMethod]
    public void WhereAndBooleanComposition_ShouldPreserveLeftToRightResults()
    {
        const string query =
            "select Id, Name from #A.Entities() where (Name like City and Id < 3) or Name like 'Gamma' order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", "Al%"), Row(2, "Beta", "Z%"), Row(3, "Gamma", null))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"], [3, "Gamma"]);
    }

    [TestMethod]
    public void CaseWhen_ShouldUseDynamicPatternResult()
    {
        const string query =
            "select Id, case when Name like City then 'hit' else 'miss' end as Status from #A.Entities() order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", "Al%"), Row(2, "Beta", "Al%"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Status", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "hit"], [2, "miss"]);
    }

    [TestMethod]
    public void InnerJoinOn_ShouldMatchColumnPatterns()
    {
        const string query =
            "select a.Id, b.Id from #A.Entities() a inner join #B.Entities() b on a.Name like b.City order by a.Id, b.Id";
        var sources = TwoSources(
            [Row(1, "Alpha"), Row(2, "Beta"), Row(3, "Alpine")],
            [Row(10, "prefix", "Al%"), Row(11, "exact", "Beta")]);
        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Id", typeof(int)), ("b.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 10], [2, 11], [3, 10]);
    }

    [TestMethod]
    public void LeftJoinOn_ShouldNullExtendUnmatchedRows()
    {
        const string query =
            "select a.Id, b.Id from #A.Entities() a left join #B.Entities() b on a.Name like b.City order by a.Id, b.Id";
        var sources = TwoSources(
            [Row(1, "Alpha"), Row(2, "Beta")],
            [Row(10, "prefix", "Al%")]);
        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Id", typeof(int)), ("b.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 10], [2, null]);
    }

    [TestMethod]
    public void AggregateFilter_ShouldApplyPreparedWildcardPattern()
    {
        const string query = "select Count(Name) filter (where Name like 'A_ph_') as Matches from #A.Entities()";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha"), Row(2, "Alphi"), Row(3, "Alpine"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Matches", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L]);
    }

    [TestMethod]
    public void Having_ShouldApplyPreparedUnicodePatternToGroupKey()
    {
        const string query =
            "select City, Count(Name) as Rows from #A.Entities() group by City having City like 'Ł%' order by City";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "A", "Łódź"), Row(2, "B", "Łódź"), Row(3, "C", "Warsaw"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Rows", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Łódź", 2L]);
    }

    [TestMethod]
    public void DirectQualify_ShouldFilterBaseColumnAfterWindowEvaluation()
    {
        const string query =
            "select Name, RowNumber() over (order by Name) as rn from #A.Entities() " +
            "qualify RowNumber() over (order by Name) >= 1 and Name like 'A%' order by Name";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alice"), Row(2, "Bob"), Row(3, "Ada"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Ada", 1L], ["Alice", 2L]);
    }

    [TestMethod]
    public void WindowAggregateFilter_ShouldApplyLikeInsideWindow()
    {
        const string query =
            "select Id, Count(Name) filter (where Name like 'A%') over (order by Id) as Matches " +
            "from #A.Entities() order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alice"), Row(2, "Bob"), Row(3, "Ada"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Matches", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 1], [2, 1], [3, 2]);
    }

    [TestMethod]
    public void CteProducerAndConsumer_ShouldPreserveDynamicLike()
    {
        const string query =
            "with candidates as (select Id, Name, City from #A.Entities() where Name like City) " +
            "select Id, Name from candidates where Name like 'A%' order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", "Al%"), Row(2, "Beta", "B_ta"), Row(3, "Gamma", "Z%"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"]);
    }

    [TestMethod]
    public void DynamicLikeInStoredCte_ShouldCaptureQueryScopedCacheInBuildHelper()
    {
        const string query =
            "with candidates as (select Id, Name from #A.Entities() where Name like City) " +
            "select Id, Name from candidates order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", "Al%"), Row(2, "Beta", "Z%"), Row(3, "Gamma", "Ga%")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"], [3, "Gamma"]);
    }

    [TestMethod]
    public void RecursiveCte_ShouldEvaluateLikeInRecursiveMember()
    {
        const string query =
            "with recursive words (Value, Depth) as (" +
            "select 'A', 0 from values {(Seed: 1)} seed union all " +
            "select w.Value + 'x', w.Depth + 1 from words w where w.Value like 'A%' and w.Depth < 2) " +
            "select Value, Depth from words order by Depth";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(string)), ("Depth", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", 0], ["Ax", 1], ["Axx", 2]);
    }

    [TestMethod]
    public void UnionBranches_ShouldEvaluateIndependentLikePredicates()
    {
        const string query =
            "select Id, Name from #A.Entities() where Name like 'A%' union all (Id, Name) " +
            "select Id, Name from #B.Entities() where Name like '%ta' order by Id";
        var sources = TwoSources([Row(1, "Alpha"), Row(2, "Beta")], [Row(3, "Delta"), Row(4, "Echo")]);
        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"], [3, "Delta"]);
    }

    [TestMethod]
    public void DistinctOrderSkipTake_ShouldApplyAfterLike()
    {
        const string query =
            "select distinct Name from #A.Entities() where Name like 'A%' order by Name skip 1 take 1";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Ada"), Row(2, "Alice"), Row(3, "Ada"), Row(4, "Bob"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Alice"]);
    }

    [TestMethod]
    public void PredicateQuantifier_ShouldEvaluateEverySelectedOperand()
    {
        const string query =
            "select Id from #A.Entities() where any(Name, City) like 'A%' order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", "Warsaw"), Row(2, "Beta", "Athens"), Row(3, "Gamma", "Rome"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2]);
    }

    [TestMethod]
    public void ParallelCteBranches_ShouldReturnSameDynamicLikeRows()
    {
        const string query =
            "with leftRows as (select Id, Name from #A.Entities() where Name like City), " +
            "rightRows as (select Id, Name from #B.Entities() where Name like City) " +
            "select Id, Name from leftRows union all (Id, Name) select Id, Name from rightRows order by Id";
        var sources = TwoSources(
            [Row(1, "Alpha", "Al%"), Row(2, "Beta", "Z%")],
            [Row(3, "Gamma", "Ga%"), Row(4, "Delta", "Q%")]);
        var options = new CompilationOptions(ParallelizationMode.Full);
        var table = CreateAndRunVirtualMachine(query, sources, options).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"], [3, "Gamma"]);
    }

    [TestMethod]
    public void ContextCatalog_ShouldCoverEveryContextAndMandatoryStrategyPair()
    {
        var coverage = LikeQueryConformanceCatalog.ContextCoverage;
        var missingContexts = System.Enum.GetValues<LikeQueryContext>()
            .Except(coverage.Select(item => item.Context))
            .ToArray();
        var missingPairs = new[]
            {
                (LikeQueryContext.CrossApply, LikeExecutionStrategy.Dynamic),
                (LikeQueryContext.InnerJoin, LikeExecutionStrategy.Dynamic),
                (LikeQueryContext.AggregateFilter, LikeExecutionStrategy.Prepared),
                (LikeQueryContext.Having, LikeExecutionStrategy.Prepared),
                (LikeQueryContext.Parallel, LikeExecutionStrategy.Dynamic)
            }
            .Where(pair => !coverage.Any(item => item.Context == pair.Item1 && item.Strategy == pair.Item2))
            .ToArray();
        var missingSources = System.Enum.GetValues<LikePatternSource>()
            .Except(LikeQueryConformanceCatalog.Cases.Select(item => item.Source)
                .Concat(coverage.Where(item => item.PatternSource.HasValue).Select(item => item.PatternSource!.Value)))
            .ToArray();

        Assert.IsEmpty(missingContexts, $"Missing LIKE query contexts: {string.Join(", ", missingContexts)}");
        Assert.IsEmpty(missingPairs, $"Missing LIKE strategy/context pairs: {string.Join(", ", missingPairs)}");
        Assert.IsEmpty(missingSources, $"Missing LIKE pattern sources: {string.Join(", ", missingSources)}");
    }

    private static BasicEntity Row(int id, string? name, string? city = null) =>
        new() { Id = id, Name = name, City = city };

    private static Dictionary<string, IEnumerable<BasicEntity>> TwoSources(
        BasicEntity[] first,
        BasicEntity[] second) =>
        new() { ["#A"] = first, ["#B"] = second };
}
