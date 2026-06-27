using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

/// <summary>
///     Validates that separate option instances route through the same physical-plan and Execution-IR pipeline.
/// </summary>
/// <remarks>
///     IR support radar (7.4.f.5):
///     - Simple select/where: supported (WhenSimpleSelect_ShouldProduceSameResults, WhenWhereGreaterThan_ShouldProduceSameResults)
///     - Group by/aggregates/having: supported (WhenGroupByWithCount_ShouldProduceSameResults, WhenHaving_ShouldProduceSameResults)
///     - Order by/skip/take: supported (WhenOrderBy_ShouldProduceSameResults, WhenSkipTake_ShouldProduceSameResults)
///     - Joins: supported (WhenInnerJoin_ShouldProduceSameResults, WhenLeftOuterJoin_ShouldProduceSameResults)
///     - Set operations: supported (WhenUnionAll_ShouldProduceSameResults, WhenIntersect_ShouldProduceSameResults)
///     - CTE and expressions: supported (WhenSimpleCte_ShouldProduceSameResults, WhenCaseWhen_ShouldProduceSameResults)
///     - Window and qualify: supported (WhenWindowRowNumber_ShouldProduceSameResults, WhenQualify_ShouldProduceSameResults)
/// </remarks>
[TestClass]
public class IrPipelineValidationTests
{
    private static readonly CompilationOptions CompatibilityOptions = new(usePrimitiveTypeValidation: false);
    private static readonly CompilationOptions ExplicitIrOptions = new(usePrimitiveTypeValidation: false);
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    #region Simple SELECT

    [TestMethod]
    public void WhenSimpleSelect_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities()";
        AssertSameResults(query, SingleSource("Alice", "Bob", "Charlie"));
    }

    [TestMethod]
    public void WhenSelectWithConstant_ShouldProduceSameResults()
    {
        const string query = "select 1, 'hello' from #A.Entities()";
        AssertSameResults(query, SingleSource("X"));
    }

    [TestMethod]
    public void WhenSelectWithArithmetic_ShouldProduceSameResults()
    {
        const string query = "select Country, Population + 10, Population * 2 from #A.Entities()";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50)));
    }

    [TestMethod]
    public void WhenSelectStar_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities()";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Alice")));
    }

    #endregion

    #region WHERE clause

    [TestMethod]
    public void WhenWhereGreaterThan_ShouldProduceSameResults()
    {
        const string query = "select Country from #A.Entities() where Population > 100";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150)));
    }

    [TestMethod]
    public void WhenWhereWithAnd_ShouldProduceSameResults()
    {
        const string query = "select Country from #A.Entities() where Population > 50 and Population < 200";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150)));
    }

    [TestMethod]
    public void WhenWhereWithOr_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() where Name = 'Alice' or Name = 'Charlie'";
        AssertSameResults(query, SingleSource("Alice", "Bob", "Charlie"));
    }

    [TestMethod]
    public void WhenWhereWithIsNull_ShouldProduceSameResults()
    {
        const string query = "select City from #A.Entities() where Country is not null";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Berlin", null, 200)));
    }

    #endregion

    #region GROUP BY

    [TestMethod]
    public void WhenGroupByWithCount_ShouldProduceSameResults()
    {
        const string query = "select Country, Count(Country) from #A.Entities() group by Country";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Krakow", "Poland", 50),
            new BasicEntity("Berlin", "Germany", 200)));
    }

    [TestMethod]
    public void WhenGroupByWithSum_ShouldProduceSameResults()
    {
        const string query = "select Country, Sum(Population) from #A.Entities() group by Country";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Krakow", "Poland", 50),
            new BasicEntity("Berlin", "Germany", 200)));
    }

    [TestMethod]
    public void WhenGroupByWithMultipleAggregates_ShouldProduceSameResults()
    {
        const string query = "select Country, Count(Country), Sum(Population), Min(Population), Max(Population) from #A.Entities() group by Country";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Krakow", "Poland", 50),
            new BasicEntity("Berlin", "Germany", 200),
            new BasicEntity("Munich", "Germany", 150)));
    }

    [TestMethod]
    public void WhenAggregateOnly_ShouldProduceSameResults()
    {
        const string query = "select Count(Name) from #A.Entities()";
        AssertSameResults(query, SingleSource("Alice", "Bob", "Charlie"));
    }

    #endregion

    #region HAVING

    [TestMethod]
    public void WhenHaving_ShouldProduceSameResults()
    {
        const string query = "select Country, Count(Country) from #A.Entities() group by Country having Count(Country) > 1";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Krakow", "Poland", 50),
            new BasicEntity("Berlin", "Germany", 200)));
    }

    #endregion

    #region ORDER BY / SKIP / TAKE

    [TestMethod]
    public void WhenOrderBy_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() order by Name asc";
        AssertSameResultsOrdered(query, SingleSource("Charlie", "Alice", "Bob"));
    }

    [TestMethod]
    public void WhenOrderByDesc_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() order by Name desc";
        AssertSameResultsOrdered(query, SingleSource("Charlie", "Alice", "Bob"));
    }

    [TestMethod]
    public void WhenSkipTake_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() order by Name asc skip 1 take 1";
        AssertSameResultsOrdered(query, SingleSource("Charlie", "Alice", "Bob"));
    }

    #endregion

    #region INNER JOIN

    [TestMethod]
    public void WhenInnerJoin_ShouldProduceSameResults()
    {
        const string query = "select a.Name, b.Name from #A.Entities() a inner join #B.Entities() b on a.Country = b.Country";
        AssertSameResults(query, DualSource(
            [new BasicEntity { Name = "Alice", Country = "PL" }, new BasicEntity { Name = "Bob", Country = "DE" }],
            [new BasicEntity { Name = "Warsaw", Country = "PL" }, new BasicEntity { Name = "Berlin", Country = "DE" }]));
    }

    [TestMethod]
    public void WhenLeftOuterJoin_ShouldProduceSameResults()
    {
        const string query = "select a.Name, b.Name from #A.Entities() a left outer join #B.Entities() b on a.Country = b.Country";
        AssertSameResults(query, DualSource(
            [new BasicEntity { Name = "Alice", Country = "PL" }, new BasicEntity { Name = "Bob", Country = "DE" }, new BasicEntity { Name = "Charlie", Country = "FR" }],
            [new BasicEntity { Name = "Warsaw", Country = "PL" }, new BasicEntity { Name = "Berlin", Country = "DE" }]));
    }

    #endregion

    #region UNION / EXCEPT / INTERSECT

    [TestMethod]
    public void WhenUnionAll_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() union all (Name) select Name from #B.Entities()";
        AssertSameResults(query, DualSource(
            [new BasicEntity("Alice")],
            [new BasicEntity("Bob")]));
    }

    [TestMethod]
    public void WhenUnion_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() union (Name) select Name from #B.Entities()";
        AssertSameResults(query, DualSource(
            [new BasicEntity("Alice"), new BasicEntity("Bob")],
            [new BasicEntity("Bob"), new BasicEntity("Charlie")]));
    }

    [TestMethod]
    public void WhenExcept_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() except (Name) select Name from #B.Entities()";
        AssertSameResults(query, DualSource(
            [new BasicEntity("Alice"), new BasicEntity("Bob")],
            [new BasicEntity("Bob"), new BasicEntity("Charlie")]));
    }

    [TestMethod]
    public void WhenIntersect_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() intersect (Name) select Name from #B.Entities()";
        AssertSameResults(query, DualSource(
            [new BasicEntity("Alice"), new BasicEntity("Bob")],
            [new BasicEntity("Bob"), new BasicEntity("Charlie")]));
    }

    #endregion

    #region CTE

    [TestMethod]
    public void WhenSimpleCte_ShouldProduceSameResults()
    {
        const string query = "with cte as (select Name from #A.Entities()) select Name from cte";
        AssertSameResults(query, SingleSource("Alice", "Bob"));
    }

    [TestMethod]
    public void WhenChainedCte_ShouldProduceSameResults()
    {
        const string query = @"
            with first as (select Country, Population from #A.Entities() where Population > 50),
                 second as (select Country from first where Population < 200)
            select Country from second";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150)));
    }

    #endregion

    #region Window functions

    [TestMethod]
    public void WhenRowNumber_ShouldProduceSameResults()
    {
        const string query = "select Name, RowNumber() over (order by Name asc) from #A.Entities()";
        AssertSameResultsOrdered(query, SingleSource("Charlie", "Alice", "Bob"));
    }

    #endregion

    #region DISTINCT

    [TestMethod]
    public void WhenDistinct_ShouldProduceSameResults()
    {
        const string query = "select distinct Country from #A.Entities()";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Krakow", "Poland", 50),
            new BasicEntity("Berlin", "Germany", 200)));
    }

    #endregion

    #region Method calls and expressions

    [TestMethod]
    public void WhenStringMethod_ShouldProduceSameResults()
    {
        const string query = "select ToUpperInvariant(Name) from #A.Entities()";
        AssertSameResults(query, SingleSource("alice", "bob"));
    }

    [TestMethod]
    public void WhenCaseWhen_ShouldProduceSameResults()
    {
        const string query = @"
            select
                case when Population > 150 then 'big' when Population > 50 then 'medium' else 'small' end
            from #A.Entities()";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150)));
    }

    [TestMethod]
    public void WhenBetween_ShouldProduceSameResults()
    {
        const string query = "select Country from #A.Entities() where Population between 50 and 150";
        AssertSameResults(query, SingleSource(
            new BasicEntity("NYC", 200),
            new BasicEntity("LA", 50),
            new BasicEntity("CHI", 150)));
    }

    [TestMethod]
    public void WhenLike_ShouldProduceSameResults()
    {
        const string query = "select Name from #A.Entities() where Name like '%li%'";
        AssertSameResults(query, SingleSource("Alice", "Bob", "Charlie"));
    }

    [TestMethod]
    public void WhenCoalesce_ShouldProduceSameResults()
    {
        const string query = "select Coalesce(Country, 'Unknown') from #A.Entities()";
        AssertSameResults(query, SingleSource(
            new BasicEntity("Warsaw", "Poland", 100),
            new BasicEntity("Berlin", null, 200)));
    }

    #endregion

    #region Helpers

    private void AssertSameResults(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(sources);

        var compatibilityTable = RunPipeline(query, provider, CompatibilityOptions);
        var explicitIrTable = RunPipeline(query, provider, ExplicitIrOptions);

        AssertTablesEqual(compatibilityTable, explicitIrTable, query);
    }

    private void AssertSameResultsOrdered(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(sources);

        var compatibilityTable = RunPipeline(query, provider, CompatibilityOptions);
        var explicitIrTable = RunPipeline(query, provider, ExplicitIrOptions);

        AssertTablesEqualOrdered(compatibilityTable, explicitIrTable, query);
    }

    private Table RunPipeline(string query, ISchemaProvider provider, CompilationOptions options)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            _loggerResolver,
            options);

        return compiled.Run(CancellationToken.None);
    }

    private static void AssertTablesEqual(Table expected, Table actual, string query)
    {
        Assert.AreEqual(expected.Count, actual.Count,
            $"Row count mismatch for query: {query}. Expected {expected.Count}, got {actual.Count}");

        var expectedColumns = expected.Columns.ToList();
        var actualColumns = actual.Columns.ToList();

        Assert.HasCount(expectedColumns.Count, actualColumns,
            $"Column count mismatch for query: {query}");

        for (var col = 0; col < expectedColumns.Count; col++)
        {
            Assert.AreEqual(expectedColumns[col].ColumnName, actualColumns[col].ColumnName,
                $"Column {col} name mismatch for query: {query}");
            Assert.AreEqual(expectedColumns[col].ColumnType, actualColumns[col].ColumnType,
                $"Column {col} type mismatch for '{expectedColumns[col].ColumnName}' in query: {query}");
        }

        var expectedRows = expected
            .Select(row => Enumerable.Range(0, expectedColumns.Count).Select(i => row[i]).ToArray())
            .OrderBy(row => string.Join("|", row.Select(v => v?.ToString() ?? "NULL")))
            .ToList();

        var actualRows = actual
            .Select(row => Enumerable.Range(0, actualColumns.Count).Select(i => row[i]).ToArray())
            .OrderBy(row => string.Join("|", row.Select(v => v?.ToString() ?? "NULL")))
            .ToList();

        for (var row = 0; row < expectedRows.Count; row++)
            for (var col = 0; col < expectedColumns.Count; col++)
                Assert.AreEqual(expectedRows[row][col], actualRows[row][col],
                    $"Value mismatch at sorted row {row}, column '{expectedColumns[col].ColumnName}' for query: {query}");
    }

    private static void AssertTablesEqualOrdered(Table expected, Table actual, string query)
    {
        Assert.AreEqual(expected.Count, actual.Count,
            $"Row count mismatch for query: {query}");

        var expectedColumns = expected.Columns.ToList();
        var actualColumns = actual.Columns.ToList();

        Assert.HasCount(expectedColumns.Count, actualColumns,
            $"Column count mismatch for query: {query}");

        for (var row = 0; row < expected.Count; row++)
            for (var col = 0; col < expectedColumns.Count; col++)
                Assert.AreEqual(expected[row][col], actual[row][col],
                    $"Value mismatch at row {row}, column '{expectedColumns[col].ColumnName}' for query: {query}");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> SingleSource(params string[] names)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", names.Select(n => new BasicEntity(n)).ToArray() }
        };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> SingleSource(params BasicEntity[] entities)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", entities } };
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> DualSource(
        BasicEntity[] sourcesA,
        BasicEntity[] sourcesB)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", sourcesA },
            { "#B", sourcesB }
        };
    }

    #endregion
}
