using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0040
    // Specification: ["4.5","5.3","10.1-10.3","10.7","10.10.1","10.10.3-10.10.5","12.1","12.3","12.9","14.3","14.7","24.2-24.4","24.6"]
    // Historical fixture: {"provider":"Inline VALUES with an empty BasicSchemaProvider<BasicEntity>","cte_definition":"Combined","first_branch_alias":"v","second_branch_alias":"w","set_operator":"UNION ALL","grouping":"Bucket","aggregate_filters":["IncludeRow = true","IncludeRow = false"],"having":"Count(*) >= 2","ordering":["RowsInGroup desc","Bucket asc"],"branches":[[{"Bucket":"A","IncludeRow":true,"Score":10},{"Bucket":"A","IncludeRow":false,"Score":1},{"Bucket":"B","IncludeRow":true,"Score":null},{"Bucket":"C","IncludeRow":true,"Score":7},{"Bucket":"D","IncludeRow":null,"Score":9},{"Bucket":"E","IncludeRow":true,"Score":8}],[{"Bucket":"A","IncludeRow":true,"Score":10},{"Bucket":"B","IncludeRow":false,"Score":5},{"Bucket":"C","IncludeRow":false,"Score":2},{"Bucket":"D","IncludeRow":false,"Score":99}]]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Bucket","RowsInGroup","IncludedScoreCount","ExcludedScoreCount"],"groups_before_having":[["A",3,2,1],["B",2,0,1],["C",2,1,1],["D",2,0,1],["E",1,1,0]],"groups_after_having":[["A",3,2,1],["B",2,0,1],["C",2,1,1],["D",2,0,1]],"row_count":4,"rows":[["A",3,2,1],["B",2,0,1],["C",2,1,1],["D",2,0,1]],"row_ordering":"RowsInGroup DESC, then Bucket ASC"}
    // Observed failure: oracle_assertion_failure|The focused test stopped at Assert.IsTrue(diagnosticResult.Succeeded); diagnostic details were not included in the assertion output.
    // Regression: reconcile nullable/non-nullable inputs; Count assertions use its Int64 contract.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0040")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void UnionAllAcceptsCompatibleNullableAndNonNullableColumns()
    {
        const string query = """
            with Combined as (
                select v.Bucket as Bucket, v.IncludeRow as IncludeRow, v.Score as Score
                from values {
                    (Bucket: 'A', IncludeRow: true, Score: 10),
                    (Bucket: 'A', IncludeRow: false, Score: 1),
                    (Bucket: 'B', IncludeRow: true, Score: null),
                    (Bucket: 'C', IncludeRow: true, Score: 7),
                    (Bucket: 'D', IncludeRow: null, Score: 9),
                    (Bucket: 'E', IncludeRow: true, Score: 8)
                } v
                union all
                select w.Bucket as Bucket, w.IncludeRow as IncludeRow, w.Score as Score
                from values {
                    (Bucket: 'A', IncludeRow: true, Score: 10),
                    (Bucket: 'B', IncludeRow: false, Score: 5),
                    (Bucket: 'C', IncludeRow: false, Score: 2),
                    (Bucket: 'D', IncludeRow: false, Score: 99)
                } w
            )
            select c.Bucket as Bucket,
                   Count(*) as RowsInGroup,
                   Count(c.Score) FILTER (WHERE c.IncludeRow = true) as IncludedScoreCount,
                   Count(c.Score) FILTER (WHERE c.IncludeRow = false) as ExcludedScoreCount
            from Combined c
            group by c.Bucket
            having Count(*) >= 2
            order by RowsInGroup desc, Bucket asc
            """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>();
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertColumns(result,
            ("Bucket", typeof(string)), ("RowsInGroup", typeof(long)),
            ("IncludedScoreCount", typeof(long)), ("ExcludedScoreCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            new object?[] { "A", 3L, 2L, 1L },
            new object?[] { "B", 2L, 0L, 1L },
            new object?[] { "C", 2L, 1L, 1L },
            new object?[] { "D", 2L, 0L, 1L });
    }
}
