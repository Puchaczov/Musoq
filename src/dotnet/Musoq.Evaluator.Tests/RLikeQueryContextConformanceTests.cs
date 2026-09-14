using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RLikeQueryContextConformanceTests : BasicEntityTestBase
{
    [TestMethod]
    public void ProjectionWhereAndCase_ShouldReturnExactResults()
    {
        const string query =
            "select Id, Name rlike r'\\AA' as StartsWithA, " +
            "case when Name rlike City then 'hit' else 'miss' end as Status " +
            "from #A.Entities() where Name rlike r'a\\z' or Id = 3 order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", @"\AAlpha\z"),
            Row(2, "Beta", @"\AAl"),
            Row(3, "Echo", "never"))).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("StartsWithA", typeof(bool)),
            ("Status", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, true, "hit"], [2, false, "miss"], [3, false, "miss"]);
    }

    [TestMethod]
    public void MultipleDirectRLikeExpressionsAcrossProjectionWhereAndCase_ShouldUseIndependentPatternScopes()
    {
        const string query =
            "select Id, Name rlike 'ph' as ContainsPh, " +
            "case when Name rlike r'\\AAl' then 'prefix' else 'other' end as Kind " +
            "from #A.Entities() where Name rlike r'a\\z' order by Id";
        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(Row(1, "Alpha"), Row(2, "Beta"), Row(3, "Echo")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, true, "prefix"],
            [2, false, "other"]);
    }

    [TestMethod]
    public void BooleanComposition_ShouldPreserveDynamicAndConstantMatches()
    {
        const string query =
            "select Id, Name from #A.Entities() " +
            "where (Name rlike City and Id < 3) or Name rlike r'\\AGamma\\z' order by Id";
        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(
            Row(1, "Alpha", @"\AAl"), Row(2, "Beta", @"\AZ"), Row(3, "Gamma", null)))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int)), ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Alpha"], [3, "Gamma"]);
    }

    [TestMethod]
    public void InnerAndLeftJoinOn_ShouldMatchColumnPatternsAndNullExtend()
    {
        const string innerQuery =
            "select a.Id, b.Id from #A.Entities() a inner join #B.Entities() b " +
            "on a.Name rlike b.City order by a.Id, b.Id";
        const string leftQuery =
            "select a.Id, b.Id from #A.Entities() a left join #B.Entities() b " +
            "on a.Name rlike b.City order by a.Id, b.Id";
        var sources = TwoSources(
            [Row(1, "Alpha"), Row(2, "Beta"), Row(3, "Alpine")],
            [Row(10, "prefix", @"\AAl"), Row(11, "exact", @"\ABeta\z")]);

        var inner = CreateAndRunVirtualMachine(innerQuery, sources).Run(TestContext.CancellationToken);
        var left = CreateAndRunVirtualMachine(leftQuery, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(inner, [1, 10], [2, 11], [3, 10]);
        TableMaterializationTestHelper.AssertRowsInOrder(left, [1, 10], [2, 11], [3, 10]);
    }

    [TestMethod]
    public void LeftJoinOn_WhenUnmatched_ShouldProduceNullableRightColumn()
    {
        const string query =
            "select a.Id, b.Id from #A.Entities() a left join #B.Entities() b " +
            "on a.Name rlike b.City order by a.Id";
        var table = CreateAndRunVirtualMachine(
            query,
            TwoSources([Row(1, "Alpha"), Row(2, "Beta")], [Row(10, "prefix", @"\AAl")]))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.Id", typeof(int)), ("b.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 10], [2, null]);
    }

    [TestMethod]
    public void AggregateFilterAndHaving_ShouldApplyRegexPredicates()
    {
        const string filterQuery =
            "select Count(Name) filter (where Name rlike r'\\AAlph.\\z') as Matches from #A.Entities()";
        const string havingQuery =
            "select City, Count(Name) as Rows from #A.Entities() group by City " +
            "having City rlike r'\\AŁ' order by City";

        var filter = CreateAndRunVirtualMachine(filterQuery, CreateSingleSource(
            Row(1, "Alpha"), Row(2, "Alphi"), Row(3, "Alpine"))).Run(TestContext.CancellationToken);
        var having = CreateAndRunVirtualMachine(havingQuery, CreateSingleSource(
            Row(1, "A", "Łódź"), Row(2, "B", "Łódź"), Row(3, "C", "Warsaw")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(filter, [2L]);
        TableMaterializationTestHelper.AssertRowsInOrder(having, ["Łódź", 2L]);
    }

    [TestMethod]
    public void QualifyAndWindowFilter_ShouldApplyAfterWindowEvaluation()
    {
        const string qualifyQuery =
            "select Name, RowNumber() over (order by Name) as rn from #A.Entities() " +
            "qualify RowNumber() over (order by Name) >= 1 and Name rlike r'\\AA' order by Name";
        const string windowQuery =
            "select Id, Count(Name) filter (where Name rlike r'\\AA') over (order by Id) as Matches " +
            "from #A.Entities() order by Id";
        var rows = CreateSingleSource(Row(1, "Alice"), Row(2, "Bob"), Row(3, "Ada"));

        var qualify = CreateAndRunVirtualMachine(qualifyQuery, rows).Run(TestContext.CancellationToken);
        var window = CreateAndRunVirtualMachine(windowQuery, rows).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(qualify, ["Ada", 1L], ["Alice", 2L]);
        TableMaterializationTestHelper.AssertRowsInOrder(window, [1, 1], [2, 1], [3, 2]);
    }

    [TestMethod]
    public void CteAndRecursiveCte_ShouldPreserveRLikePredicates()
    {
        const string cteQuery =
            "with candidates as (select Id, Name, City from #A.Entities() where Name rlike City) " +
            "select Id, Name from candidates where Name rlike r'\\AA' order by Id";
        const string recursiveQuery =
            "with recursive words (Value, Depth) as (" +
            "select 'A', 0 from values {(Seed: 1)} seed union all " +
            "select w.Value + 'x', w.Depth + 1 from words w " +
            "where w.Value rlike r'\\AA' and w.Depth < 2) " +
            "select Value, Depth from words order by Depth";

        var cte = CreateAndRunVirtualMachine(cteQuery, CreateSingleSource(
            Row(1, "Alpha", @"\AAl"), Row(2, "Beta", @"\AB"), Row(3, "Gamma", @"\AZ")))
            .Run(TestContext.CancellationToken);
        var recursive = CreateAndRunVirtualMachine(recursiveQuery, CreateSingleSource())
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(cte, [1, "Alpha"]);
        TableMaterializationTestHelper.AssertRowsInOrder(recursive, ["A", 0], ["Ax", 1], ["Axx", 2]);
    }

    [TestMethod]
    public void SetDistinctOrderAndPage_ShouldPreserveOperatorOrder()
    {
        const string unionQuery =
            "select Id, Name from #A.Entities() where Name rlike r'\\AA' union all (Id, Name) " +
            "select Id, Name from #B.Entities() where Name rlike r'ta\\z' order by Id";
        const string pageQuery =
            "select distinct Name from #A.Entities() where Name rlike r'\\AA' order by Name skip 1 take 1";
        var union = CreateAndRunVirtualMachine(
            unionQuery,
            TwoSources([Row(1, "Alpha"), Row(2, "Beta")], [Row(3, "Delta"), Row(4, "Echo")]))
            .Run(TestContext.CancellationToken);
        var page = CreateAndRunVirtualMachine(pageQuery, CreateSingleSource(
            Row(1, "Ada"), Row(2, "Alice"), Row(3, "Ada"), Row(4, "Bob")))
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(union, [1, "Alpha"], [3, "Delta"]);
        TableMaterializationTestHelper.AssertRowsInOrder(page, ["Alice"]);
    }

    [TestMethod]
    public void PredicateQuantifierAndParallelBranches_ShouldReturnExactRows()
    {
        const string quantifierQuery =
            "select Id from #A.Entities() where any(Name, City) rlike r'\\AA' order by Id";
        const string parallelQuery =
            "with leftRows as (select Id, Name from #A.Entities() where Name rlike City), " +
            "rightRows as (select Id, Name from #B.Entities() where Name rlike City) " +
            "select Id, Name from leftRows union all (Id, Name) select Id, Name from rightRows order by Id";
        var quantifier = CreateAndRunVirtualMachine(quantifierQuery, CreateSingleSource(
            Row(1, "Alpha", "Warsaw"), Row(2, "Beta", "Athens"), Row(3, "Gamma", "Rome")))
            .Run(TestContext.CancellationToken);
        var parallel = CreateAndRunVirtualMachine(
            parallelQuery,
            TwoSources(
                [Row(1, "Alpha", @"\AAl"), Row(2, "Beta", @"\AZ")],
                [Row(3, "Gamma", @"\AGa"), Row(4, "Delta", @"\AQ")]),
            new CompilationOptions(ParallelizationMode.Full)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(quantifier, [1], [2]);
        TableMaterializationTestHelper.AssertRowsInOrder(parallel, [1, "Alpha"], [3, "Gamma"]);
    }

    [TestMethod]
    public void ContextCatalog_ShouldCoverEveryContextAndMandatoryStrategyPair()
    {
        var coverage = RLikeQueryConformanceCatalog.ContextCoverage;
        var missingContexts = Enum.GetValues<RLikeQueryContext>().Except(coverage.Select(item => item.Context)).ToArray();
        var requiredPairs = new[]
        {
            (RLikeQueryContext.CrossApply, RLikeExecutionStrategy.Dynamic),
            (RLikeQueryContext.OuterApply, RLikeExecutionStrategy.Prepared),
            (RLikeQueryContext.AggregateFilter, RLikeExecutionStrategy.Prepared),
            (RLikeQueryContext.Parallel, RLikeExecutionStrategy.Dynamic)
        };
        var missingPairs = requiredPairs
            .Where(pair => !coverage.Any(item => item.Context == pair.Item1 && item.Strategy == pair.Item2))
            .ToArray();

        Assert.IsEmpty(missingContexts, $"Missing RLIKE contexts: {string.Join(", ", missingContexts)}");
        Assert.IsEmpty(missingPairs, $"Missing RLIKE strategy/context pairs: {string.Join(", ", missingPairs)}");
    }

    private static BasicEntity Row(int id, string? name, string? city = null) =>
        new() { Id = id, Name = name, City = city };

    private static Dictionary<string, IEnumerable<BasicEntity>> TwoSources(
        BasicEntity[] first,
        BasicEntity[] second) => new() { ["#A"] = first, ["#B"] = second };
}
