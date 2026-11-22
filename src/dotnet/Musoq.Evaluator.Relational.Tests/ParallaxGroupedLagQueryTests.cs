using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("ParallaxRepro")]
public sealed class ParallaxGroupedLagQueryTests
{
    private const string GroupedLag = """
        SELECT b.Tick::long - a.Tick::long AS LagTicks, Count(*) AS N
        FROM values { (Tick: 0L), (Tick: 3L), (Tick: 6L) } a
        INNER JOIN values { (Tick: 0L), (Tick: 3L), (Tick: 6L) } b ON b.Tick > a.Tick
        GROUP BY b.Tick::long - a.Tick::long
        """;

    [TestMethod]
    [DataRow("ORDER BY LagTicks", 3L, 2L, 6L, 1L)]
    [DataRow("ORDER BY lagticks DESC NULLS LAST", 6L, 1L, 3L, 2L)]
    [DataRow("ORDER BY b.Tick::long - a.Tick::long", 3L, 2L, 6L, 1L)]
    public void GroupedLag_WithSameQueryOrdering_ReturnsOrderedCounts(
        string ordering,
        long firstLag,
        long firstCount,
        long secondLag,
        long secondCount
    )
    {
        using var compiled = InstanceCreator.CompileForExecution(
            GroupedLag + "\n" + ordering,
            nameof(GroupedLag_WithSameQueryOrdering_ReturnsOrderedCounts),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(result, [firstLag, firstCount], [secondLag, secondCount]);
    }

    [TestMethod]
    [DataRow("TAKE 1", 3L, 2L)]
    [DataRow("SKIP 1 TAKE 1", 6L, 1L)]
    public void GroupedLag_WithSameQueryPaging_ReturnsRequestedGroup(string paging, long lag, long count)
    {
        using var compiled = InstanceCreator.CompileForExecution(
            GroupedLag + "\nORDER BY LagTicks " + paging,
            nameof(GroupedLag_WithSameQueryPaging_ReturnsRequestedGroup),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(result, [lag, count]);
    }

    [TestMethod]
    public void GroupedLag_WithAliasOrdering_ReadsTheProjectedField()
    {
        var inspection = InstanceCreator.CompileForInspection(
            GroupedLag + "\nORDER BY LagTicks",
            nameof(GroupedLag_WithAliasOrdering_ReadsTheProjectedField),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );

        StringAssert.Contains(inspection.ExecutionPlanText, "by LagTicks ASC");
        Assert.DoesNotContain("__sortKey", inspection.ExecutionPlanText);
    }

    [TestMethod]
    public void GroupedLag_WithHiddenComputedOrderKey_ReturnsOnlyTheRequestedColumn()
    {
        var sql =
            GroupedLag.Replace(
                "b.Tick::long - a.Tick::long AS LagTicks, ",
                string.Empty,
                System.StringComparison.Ordinal
            ) + "\nORDER BY (b.Tick::long - a.Tick::long) * 2";
        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(GroupedLag_WithHiddenComputedOrderKey_ReturnsOnlyTheRequestedColumn),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("N", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, [2L], [1L]);
    }

    [TestMethod]
    [DataRow("Value", "10", 1L, "2", 2L)]
    [DataRow("p.Value", "2", 2L, "10", 1L)]
    public void GroupedOrdering_WhenAliasMatchesInputName_UsesTheBoundValue(
        string sortKey,
        string first,
        long firstCount,
        string second,
        long secondCount
    )
    {
        var sql = $$"""
            SELECT p.Value::string AS Value, Count(*) AS N
            FROM values { (Value: 2L), (Value: 10L), (Value: 2L) } p
            GROUP BY p.Value ORDER BY {{sortKey}}
            """;
        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(GroupedOrdering_WhenAliasMatchesInputName_UsesTheBoundValue),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(result, [first, firstCount], [second, secondCount]);
    }

    [TestMethod]
    public void GroupedLag_WithoutOrderBy_ReturnsCounts()
    {
        using var compiled = InstanceCreator.CompileForExecution(
            GroupedLag,
            nameof(GroupedLag_WithoutOrderBy_ReturnsCounts),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsUnordered(result, [3L, 2L], [6L, 1L]);
    }

    [TestMethod]
    public void GroupedLag_WithOuterOrderByAlias_ReturnsOrderedCounts()
    {
        var sql = $"""
            WITH lag_counts AS (
                {GroupedLag}
            )
            SELECT g.LagTicks, g.N FROM lag_counts g ORDER BY g.LagTicks
            """;
        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(GroupedLag_WithOuterOrderByAlias_ReturnsOrderedCounts),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(result, [3L, 2L], [6L, 1L]);
    }
}
