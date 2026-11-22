using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    [TestCategory("MirageRepro")]
    [DataRow("source")]
    [DataRow("alternate")]
    public void UnionBranches_ShouldUseTheirOwnProjectedColumns(string secondAlias)
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var sql = $$"""
            select 'first' as Side, source.Name as Value from #wildcard.rows() source
            union all
            select 'second' as Side, {{secondAlias}}.Other as Value from #wildcard.rows() {{secondAlias}}
            order by Side
            """;
        using var query = Compile(sql, recorder);

        using var rows = query.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(rows, ["first", "Ada"], ["second", "source-column"]);
        CollectionAssert.AreEquivalent(
            new[] { "Name", "Other" },
            recorder
                .MetadataContexts.Where(static context => context.QueryId.Contains(':'))
                .Select(static context => string.Join(",", context.Columns))
                .ToArray()
        );
    }

    [TestMethod]
    [TestCategory("MirageRepro")]
    [DataRow("source", "source")]
    [DataRow("named", "scored")]
    public void GroupedCtes_ShouldKeepKeysWhenSiblingScansProjectDifferentColumns(string secondAlias, string thirdAlias)
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var sql = $$"""
            with grouped as (
                select source.Id as Id, count(*) as Total
                from #wildcard.rows() source group by source.Id
            ), named as (
                select count(*) as Total from #wildcard.rows() {{secondAlias}}
                where {{secondAlias}}.Name = 'Ada'
            ), scored as (
                select sum({{thirdAlias}}.Score) as Score from #wildcard.rows() {{thirdAlias}}
            )
            select g.Id, g.Total as GroupedRows, n.Total as NamedRows, s.Score
            from grouped g cross join named n cross join scored s
            """;
        using var query = Compile(sql, recorder);

        using var rows = query.Run();

        TableMaterializationTestHelper.AssertRowsInOrder(rows, [1, 1L, 1L, 10]);
        CollectionAssert.AreEquivalent(
            new[] { "Id", "Name", "Score" },
            recorder
                .MetadataContexts.Where(static context => context.QueryId.Contains(':'))
                .Select(static context => string.Join(",", context.Columns))
                .ToArray()
        );
    }
}
