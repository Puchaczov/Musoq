using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class BlackBoxConversationReproductionTests
{
    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, 2)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 2)]
    [DataRow(QueryInstrumentationMode.Full, 2)]
    [DataRow(QueryInstrumentationMode.Disabled, 3)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 3)]
    [DataRow(QueryInstrumentationMode.Full, 3)]
    public void MinimalAggregateUnion_ShouldReturnEveryBranch(QueryInstrumentationMode mode, int branches)
    {
        const string branch = "select 'stage' as Stage, Count(*) as Records from #test.files() f";
        var sql = string.Join("\nunion all\n", Enumerable.Repeat(branch, branches));

        using var table = RunQuery(
            sql,
            mode,
            [
                [],
            ]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            Enumerable.Repeat(new object?[] { "stage", 1L }, branches).ToArray()
        );
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, 2)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 2)]
    [DataRow(QueryInstrumentationMode.Full, 2)]
    [DataRow(QueryInstrumentationMode.Disabled, 3)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 3)]
    [DataRow(QueryInstrumentationMode.Full, 3)]
    public void MinimalAggregateUnionOverCte_ShouldReturnEveryBranch(QueryInstrumentationMode mode, int branches)
    {
        const string cte = "with input as (select f.Name from #test.files() f) ";
        const string first = "select 'stage' as Stage, Count(*) as Records from input rows";
        const string next = "select 'stage', Count(*) from input rows";
        var sql = cte + string.Join("\nunion all\n", new[] { first }.Concat(Enumerable.Repeat(next, branches - 1)));

        using var table = RunQuery(
            sql,
            mode,
            [
                [],
            ]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            Enumerable.Repeat(new object?[] { "stage", 1L }, branches).ToArray()
        );
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void RecoveryCountsCombinedWithUnionAll_ShouldReturnEveryStage(QueryInstrumentationMode mode)
    {
        var sql =
            RecoveryCtes
            + """

                select 'valid' as Stage, Count(*) as Records from physical p where p.Valid = 1
                union all
                select 'latest', Count(*) from ranked r where r.Rank = 1
                union all
                select 'superseded', Count(*) from ranked r where r.Rank > 1
                union all
                select 'voided', Count(*) from ranked r where r.Rank = 1 and (r.Flags & 1) <> 0
                union all
                select 'effective', Count(*) from effective e
                """;

        using var table = RunQuery(sql, mode, RecoveryRecords);

        TableMaterializationTestHelper.AssertColumns(table, ("Stage", typeof(string)), ("Records", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["valid", 5L],
            ["latest", 3L],
            ["superseded", 2L],
            ["voided", 1L],
            ["effective", 2L]
        );
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void EffectiveCountAlone_ShouldFilterInvalidRevisionsBeforeRanking(QueryInstrumentationMode mode)
    {
        var sql = RecoveryCtes + "select Count(*) as Records from effective e";

        using var table = RunQuery(sql, mode, RecoveryRecords);

        TableMaterializationTestHelper.AssertColumns(table, ("Records", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void GroupedRecoverySummary_ShouldSeparateSupersededVoidedAndEffectiveEvents(QueryInstrumentationMode mode)
    {
        var sql =
            RecoveryCtes
            + """

                select case when r.Rank > 1 then 'superseded'
                            when (r.Flags & 1) <> 0 then 'voided' else 'effective' end as Stage,
                       Count(*) as Records
                from ranked r
                group by case when r.Rank > 1 then 'superseded'
                              when (r.Flags & 1) <> 0 then 'voided' else 'effective' end
                """;

        using var table = RunQuery(sql, mode, RecoveryRecords);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["effective", 2L],
            ["superseded", 2L],
            ["voided", 1L]
        );
    }
}
