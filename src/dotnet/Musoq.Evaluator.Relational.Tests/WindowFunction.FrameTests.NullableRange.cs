using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class WindowFunctionFrameTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0099
    // Specification: {"specs/musoq-core-language-spec.md":["4.5","6.5","11.1","11.2","11.3","11.4","11.5.3","11.8","11.11.1","11.11.4","11.12","11.12.2","11.12.3","14.4"]}
    // Historical fixture: {"provider":"Empty BasicSchemaProvider<BasicEntity>; query reads only inline VALUES rows.","cte":"framedRows","values_alias":"sample","values_rows":[{"Category":"A","Sequence":1,"OrderKey":null,"Amount":2},{"Category":"A","Sequence":2,"OrderKey":null,"Amount":3},{"Category":"A","Sequence":3,"OrderKey":1,"Amount":5},{"Category":"A","Sequence":4,"OrderKey":2,"Amount":7},{"Category":"B","Sequence":1,"OrderKey":null,"Amount":11},{"Category":"B","Sequence":2,"OrderKey":1,"Amount":13}],"windows":{"range":"PARTITION BY Category ORDER BY OrderKey ASC NULLS LAST RANGE BETWEEN 1 PRECEDING AND CURRENT ROW","rows":"PARTITION BY Category ORDER BY OrderKey ASC NULLS LAST, Sequence ASC ROWS BETWEEN 1 PRECEDING AND CURRENT ROW"},"output":"SELECT * FROM framedRows ORDER BY Category ASC, Sequence ASC"}
    // Historical expectation: {"diagnostic_build_succeeded":true,"compilation_succeeded":true,"execution_succeeded":true,"column_names":["Category","Sequence","OrderKey","Amount","RangeSum","RowsSum"],"column_types":{"Category":"System.String","Sequence":"System.Int32","OrderKey":"System.Nullable<System.Int32>","Amount":"System.Int32","RangeSum":"System.Decimal","RowsSum":"System.Decimal"},"row_count":6,"rows":["A",1,null,2,5,9,"A",2,null,3,5,5,"A",3,1,5,5,5,"A",4,2,7,12,12,"B",1,null,11,11,24,"B",2,1,13,13,13],"range":"For a NULL current bounded key, RANGE resolves to its NULL peer group; numeric keys use the one-key 1 PRECEDING frame.","rows_frame":"The explicit ROWS frame counts physical rows and clamps its starting 1 PRECEDING bound at each partition edge.","cte":"SELECT * expands the CTE output in projection order, including both window results.","ordering":"The final result is ordered by Category then Sequence; all six input rows are preserved."}
    // Observed failure: compile|MQ3027_InvalidOutputColumnType|bounded-SUM-OVER-RANGE-and-ROWS-inside-CTE|before-execution
    // Disposition: Unresolved/cap-deferred: Promotion cap 12 was reached before this candidate; after confirmation this distinct finding must remain deferred without further minimization.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0099")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0099_BoundedRangeNullKeysUsePeerGroup()
    {
        const string query = "with framedRows as (\r\n    select sample.Category as Category,\r\n           sample.Sequence as Sequence,\r\n           sample.OrderKey as OrderKey,\r\n           sample.Amount as Amount,\r\n           Sum(sample.Amount) over (\r\n               partition by sample.Category\r\n               order by sample.OrderKey asc nulls last\r\n               range between 1 preceding and current row\r\n           ) as RangeSum,\r\n           Sum(sample.Amount) over (\r\n               partition by sample.Category\r\n               order by sample.OrderKey asc nulls last, sample.Sequence asc\r\n               rows between 1 preceding and current row\r\n           ) as RowsSum\r\n    from values {\r\n        (Category: 'A', Sequence: 1i, OrderKey: null, Amount: 2i),\r\n        (Category: 'A', Sequence: 2i, OrderKey: null, Amount: 3i),\r\n        (Category: 'A', Sequence: 3i, OrderKey: 1i, Amount: 5i),\r\n        (Category: 'A', Sequence: 4i, OrderKey: 2i, Amount: 7i),\r\n        (Category: 'B', Sequence: 1i, OrderKey: null, Amount: 11i),\r\n        (Category: 'B', Sequence: 2i, OrderKey: 1i, Amount: 13i)\r\n    } sample\r\n)\r\nselect *\r\nfrom framedRows\r\norder by Category asc, Sequence asc";

        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Category", typeof(string)),
            ("Sequence", typeof(int)),
            ("OrderKey", typeof(int?)),
            ("Amount", typeof(int)),
            ("RangeSum", typeof(int?)),
            ("RowsSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { "A", 1, null, 2, 5, 9 },
            new object?[] { "A", 2, null, 3, 5, 5 },
            new object?[] { "A", 3, 1, 5, 5, 5 },
            new object?[] { "A", 4, 2, 7, 12, 12 },
            new object?[] { "B", 1, null, 11, 11, 24 },
            new object?[] { "B", 2, 1, 13, 13, 13 });
    }
}
