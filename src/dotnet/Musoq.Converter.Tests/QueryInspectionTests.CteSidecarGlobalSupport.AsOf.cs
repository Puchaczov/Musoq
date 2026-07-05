using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForAsOfBackwardLookupWithPartition_ShouldReturnNearestMatch()
    {
        var compiled = CompileForExecution(
            """
            with trades as (
                select 0 as Value from #system.dual()
                union all (Value) select 1 as Value from #system.dual()
                union all (Value) select 2 as Value from #system.dual()
            ),
            quotes as (
                select 0 as Value from #system.dual()
                union all (Value) select 1 as Value from #system.dual()
                union all (Value) select 2 as Value from #system.dual()
                union all (Value) select 3 as Value from #system.dual()
                union all (Value) select 4 as Value from #system.dual()
            )
            select
                case when t.Value = 2 then 'B' else 'A' end as Symbol,
                case when t.Value = 0 then 10 when t.Value = 1 then 15 else 12 end as TradeTime,
                case when q.Value = 0 then 8 when q.Value = 1 then 14 when q.Value = 2 then 16 when q.Value = 3 then 10 else 13 end as QuoteTime,
                case when q.Value = 0 then 101 when q.Value = 1 then 102 when q.Value = 2 then 103 when q.Value = 3 then 201 else 202 end as Price
            from trades t
            asof join quotes q
                on case when t.Value = 2 then 'B' else 'A' end = case when q.Value >= 3 then 'B' else 'A' end
                and case when t.Value = 0 then 10 when t.Value = 1 then 15 else 12 end >= case when q.Value = 0 then 8 when q.Value = 1 then 14 when q.Value = 2 then 16 when q.Value = 3 then 10 else 13 end
            order by Symbol, TradeTime
            """,
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(8, table[0][2]);
        Assert.AreEqual(101, table[0][3]);
        Assert.AreEqual("A", table[1][0]);
        Assert.AreEqual(15, table[1][1]);
        Assert.AreEqual(14, table[1][2]);
        Assert.AreEqual(102, table[1][3]);
        Assert.AreEqual("B", table[2][0]);
        Assert.AreEqual(12, table[2][1]);
        Assert.AreEqual(10, table[2][2]);
        Assert.AreEqual(201, table[2][3]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForAsOfForwardLookup_ShouldReturnNextMatch()
    {
        var compiled = CompileForExecution(
            """
            with events as (
                select 0 as Value from #system.dual()
                union all (Value) select 1 as Value from #system.dual()
                union all (Value) select 2 as Value from #system.dual()
            ),
            statuses as (
                select 0 as Value from #system.dual()
                union all (Value) select 1 as Value from #system.dual()
                union all (Value) select 2 as Value from #system.dual()
            )
            select
                case when c.Value = 0 then 10 when c.Value = 1 then 15 else 20 end as EventTime,
                case when s.Value = 0 then 12 when s.Value = 1 then 20 else 25 end as StatusTime,
                case when s.Value = 0 then 'warmup' when s.Value = 1 then 'ready' else 'done' end as Status
            from events c
            asof join statuses s
                on case when c.Value = 0 then 10 when c.Value = 1 then 15 else 20 end <= case when s.Value = 0 then 12 when s.Value = 1 then 20 else 25 end
            order by EventTime
            """,
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(10, table[0][0]);
        Assert.AreEqual(12, table[0][1]);
        Assert.AreEqual("warmup", table[0][2]);
        Assert.AreEqual(15, table[1][0]);
        Assert.AreEqual(20, table[1][1]);
        Assert.AreEqual("ready", table[1][2]);
        Assert.AreEqual(20, table[2][0]);
        Assert.AreEqual(20, table[2][1]);
        Assert.AreEqual("ready", table[2][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForAsOfLeftNoMatch_ShouldNullExtendRightSide()
    {
        var compiled = CompileForExecution(
            """
            with trades as (
                select 5 as TradeTime from #system.dual()
                union all (TradeTime) select 10 as TradeTime from #system.dual()
            ),
            quotes as (
                select 8 as QuoteTime, 101 as Price from #system.dual()
            )
            select t.TradeTime as TradeTime, q.QuoteTime as QuoteTime, q.Price as Price
            from trades t
            asof left join quotes q on t.TradeTime >= q.QuoteTime
            order by t.TradeTime
            """,
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(5, table[0][0]);
        Assert.IsNull(table[0][1]);
        Assert.IsNull(table[0][2]);
        Assert.AreEqual(10, table[1][0]);
        Assert.AreEqual(8, table[1][1]);
        Assert.AreEqual(101, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenCteSidecarIndexesAreEnabledForAsOfTieBreak_ShouldChooseDeterministicCandidate()
    {
        var compiled = CompileForExecution(
            """
            select a.Name, b.Name
            from #A.entities() a
            asof join #B.entities() b on a.Population >= b.Population
            tie break by b.Name desc
            """,
            CreateAsOfTieBreakSchemaProvider(),
            SidecarGlobalOptions);

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("A1", table[0][0]);
        Assert.AreEqual("B-Zulu", table[0][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteSidecarIndexesAreEnabledForCteBackedAsOfJoin_ShouldUseAsOfExecutionIr()
    {
        var result = Inspect(CreateCteBackedAsOfJoinQuery(), SidecarGlobalOptions);

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("AsOfProbe [r <- _cteRowResults.Slot0", result.ExecutionPlanText);
        AssertExecutionPlanDoesNotContain("Sidecar join pipeline failed", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<", result.GeneratedCSharpCode);
        Assert.Contains("resultAsOfIndex.Find", result.GeneratedCSharpCode);
    }

    private static EntitySetSchemaProvider CreateAsOfTieBreakSchemaProvider()
    {
        return new EntitySetSchemaProvider(
            new Dictionary<string, IReadOnlyList<EntitySetEntity>>(StringComparer.OrdinalIgnoreCase)
            {
                ["#A"] =
                [
                    new EntitySetEntity { Name = "A1", Country = "PL", Population = 100 }
                ],
                ["#B"] =
                [
                    new EntitySetEntity { Name = "B-Zulu", Country = "PL", Population = 90 },
                    new EntitySetEntity { Name = "B-Alpha", Country = "PL", Population = 90 },
                    new EntitySetEntity { Name = "B-Older", Country = "PL", Population = 50 }
                ]
            });
    }
}
