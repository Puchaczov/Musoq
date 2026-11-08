using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core026WindowTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0047
    // Specification: {"specs/musoq-core-language-spec.md":["6.1","11.1","11.2","11.4","11.5.3","11.11.1","11.11.2","11.11.6","11.12","11.12.1","11.12.3"]}
    // Historical fixture: {"provider":"BasicSchemaProvider<BasicEntity>","schema_key":"#A","source":"#A.Entities()","rows":[{"Id":1},{"Id":2},{"Id":2},{"Id":3}],"window_expressions":["Sum(Id) OVER (ORDER BY Id ASC ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)","Sum(Id) OVER (ORDER BY Id ASC RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)"]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Score","RowsTotal","RangeTotal"],"column_types":["System.Int32","System.Decimal","System.Decimal"],"row_count":4,"rows":[[1,1,1],[2,3,5],[2,5,5],[3,8,8]],"row_ordering":"No top-level ORDER BY is specified; compare as an exact row multiset without assigning one ROWS peer total to a particular tied row"}
    // Observed failure: compile|MQ9001_InternalCompilerError|explicit-rows-range-window-sums-over-typed-table|before-execution
    // Regression: ROWS key producer must materialize the order keys reused by RANGE. No top-level ordering is specified.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0047")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0047_RowsAndRangeDifferForDuplicateKeys()
    {
        const string query = "select v.Id as Score,\r\n       Sum(v.Id) over (\r\n           order by v.Id asc\r\n           rows between unbounded preceding and current row\r\n       ) as RowsTotal,\r\n       Sum(v.Id) over (\r\n           order by v.Id asc\r\n           range between unbounded preceding and current row\r\n       ) as RangeTotal\r\nfrom #A.Entities() v";
        var entities = new[]
        {
            new BasicEntity { Id = 1 },
            new BasicEntity { Id = 2 },
            new BasicEntity { Id = 2 },
            new BasicEntity { Id = 3 }
        };
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = entities,
            ["#A"] = entities
        };
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Score", typeof(System.Int32)),
            ("RowsTotal", typeof(int?)),
            ("RangeTotal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 1, 1, 1 },
            new object?[] { 2, 3, 5 },
            new object?[] { 2, 5, 5 },
            new object?[] { 3, 8, 8 });
    }

    [TestMethod]
    [DataRow("Rank", 4L)]
    [DataRow("DenseRank", 3L)]
    public void RankingBeforeRangeAggregate_ShouldPreserveSharedOrderKeys(string ranking, long finalRank)
    {
        var query = $"select v.Id as Score, {ranking}() over (order by v.Id) as Position, Sum(v.Id) over (order by v.Id range between unbounded preceding and current row) as Total from #A.Entities() v";
        using var compiled = CreateAndRunVirtualMachine(query, SharedWindowSources());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Score", typeof(int)), ("Position", typeof(long)), ("Total", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 1, 1L, 1 }, new object?[] { 2, 2L, 5 },
            new object?[] { 2, 2L, 5 }, new object?[] { 3, finalRank, 8 });
    }

    [TestMethod]
    public void DescendingRowsBeforeRangeAggregate_ShouldPreservePeerTotals()
    {
        const string query = "select v.Id as Score, Sum(v.Id) over (order by v.Id desc rows between unbounded preceding and current row) as RowsTotal, Sum(v.Id) over (order by v.Id desc range between unbounded preceding and current row) as RangeTotal from #A.Entities() v";
        using var compiled = CreateAndRunVirtualMachine(query, SharedWindowSources());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Score", typeof(int)), ("RowsTotal", typeof(int?)), ("RangeTotal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 3, 3, 3 }, new object?[] { 2, 5, 7 },
            new object?[] { 2, 7, 7 }, new object?[] { 1, 8, 8 });
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> SharedWindowSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Id = 1 }, new BasicEntity { Id = 2 }, new BasicEntity { Id = 2 }, new BasicEntity { Id = 3 }]
        };
    }
}
