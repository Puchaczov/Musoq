using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class OuterApplyTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0086
    // Specification: {"specs/musoq-core-language-spec.md":["5.3","6.2","6.4-6.5","9","9.2","9.7"]}
    // Historical fixture: {"kind":"VALUES_outer_apply_correlated_derived_table","schema_provider":"GenericSchemaProvider with an empty schema map; query uses only VALUES sources","left_source":"VALUES alias leftRows; rows A/10, B/20, C/30","right_source":"VALUES alias rightRows inside the APPLY-derived SELECT; rows A/1, A/2, B/3","derived_alias":"matches","apply_kind":"OUTER APPLY","correlation":"rightRows.Key = leftRows.Key","projected_correlation_key":"rightRows.Key as MatchKey","left_rows":[{"Key":"A","LeftScore":10},{"Key":"B","LeftScore":20},{"Key":"C","LeftScore":30}],"right_rows":[{"Key":"A","Score":1},{"Key":"A","Score":2},{"Key":"B","Score":3}]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["LeftKey","LeftScore","MatchKey","RightScore"],"row_count":4,"rows":[{"LeftKey":"A","LeftScore":10,"MatchKey":"A","RightScore":1},{"LeftKey":"A","LeftScore":10,"MatchKey":"A","RightScore":2},{"LeftKey":"B","LeftScore":20,"MatchKey":"B","RightScore":3},{"LeftKey":"C","LeftScore":30,"MatchKey":null,"RightScore":null}],"row_ordering":"No ORDER BY; compare rows independent of order.","correlation":"Each right VALUES row is filtered using leftRows.Key from the containing query block.","correlation_key":"MatchKey is projected from rightRows.Key and remains visible through the matches derived-table alias.","outer_apply":"A produces two right matches; B produces one; unmatched C remains with NULL MatchKey and RightScore."}
    // Observed failure: compile|MQ9001_InternalCompilerError|correlated-OUTER-APPLY-derived-table-with-VALUES-sources|before-execution
    // Disposition: Failed root query; diary does not establish an independently confirmed product defect.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0086")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0086_CorrelatedOuterApplyPreservesUnmatchedRow()
    {
        const string query = "select leftRows.Key as LeftKey,\r\n       leftRows.LeftScore as LeftScore,\r\n       matches.MatchKey as MatchKey,\r\n       matches.RightScore as RightScore\r\nfrom values {\r\n    (Key: 'A', LeftScore: 10),\r\n    (Key: 'B', LeftScore: 20),\r\n    (Key: 'C', LeftScore: 30)\r\n} leftRows\r\nouter apply (\r\n    select rightRows.Key as MatchKey,\r\n           rightRows.Score as RightScore\r\n    from values {\r\n        (Key: 'A', Score: 1),\r\n        (Key: 'A', Score: 2),\r\n        (Key: 'B', Score: 3)\r\n    } rightRows\r\n    where rightRows.Key = leftRows.Key\r\n) matches";

        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("LeftKey", typeof(string)),
            ("LeftScore", typeof(int)),
            ("MatchKey", typeof(string)),
            ("RightScore", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "A", 10, "A", 1 },
            new object?[] { "A", 10, "A", 2 },
            new object?[] { "B", 20, "B", 3 },
            new object?[] { "C", 30, null, null });
    }
}
