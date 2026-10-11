using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core025WindowTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0045
    // Specification: {"specs/musoq-core-language-spec.md":["4.5","6.5","11.1","11.2","11.3","11.4","11.5.3","11.11.1","11.11.2","11.11.4","11.11.6"]}
    // Historical fixture: {"provider":"Inline VALUES with an empty BasicSchemaProvider<BasicEntity>","values_alias":"v","values_rows":[{"Ordinal":1,"Score":1,"Bucket":"N"},{"Ordinal":2,"Score":2,"Bucket":"N"},{"Ordinal":3,"Score":2,"Bucket":"N"},{"Ordinal":4,"Score":1,"Bucket":null},{"Ordinal":5,"Score":1,"Bucket":null},{"Ordinal":6,"Score":3,"Bucket":null}],"window":"PARTITION BY Bucket ORDER BY Score ASC; implicit RANGE frame"}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Bucket","Ordinal","Score","RunningTotal"],"column_types":["System.String","System.Int32","System.Int32","System.Decimal"],"row_count":6,"rows":[["N",1,1,1],["N",2,2,5],["N",3,2,5],[null,4,1,2],[null,5,1,2],[null,6,3,5]],"row_ordering":"No top-level ORDER BY is specified; compare as a multiset keyed by unique Ordinal"}
    // Observed failure: compile|MQ3027_InvalidQueryOutputType|windowed-Sum-over-inline-VALUES|before-execution
    // Restored regression: in-memory TransitionSchema window factories resolve through the P1-064 repair.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0045")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0045_ImplicitRangeIncludesNullablePartitionPeers()
    {
        const string query = "select v.Bucket as Bucket,\r\n       v.Ordinal as Ordinal,\r\n       v.Score as Score,\r\n       Sum(v.Score) over (\r\n           partition by v.Bucket\r\n           order by v.Score asc\r\n       ) as RunningTotal\r\nfrom values {\r\n    (Bucket: 'N', Ordinal: 1, Score: 1),\r\n    (Bucket: 'N', Ordinal: 2, Score: 2),\r\n    (Bucket: 'N', Ordinal: 3, Score: 2),\r\n    (Bucket: null, Ordinal: 4, Score: 1),\r\n    (Bucket: null, Ordinal: 5, Score: 1),\r\n    (Bucket: null, Ordinal: 6, Score: 3)\r\n} v";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Bucket", typeof(System.String)),
            ("Ordinal", typeof(System.Int32)),
            ("Score", typeof(System.Int32)),
            ("RunningTotal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "N", 1, 1, 1 },
            new object?[] { "N", 2, 2, 5 },
            new object?[] { "N", 3, 2, 5 },
            new object?[] { null, 4, 1, 2 },
            new object?[] { null, 5, 1, 2 },
            new object?[] { null, 6, 3, 5 });
    }

    // Candidate: SPECSTRESS-2026-09-29-P4-0057
    // Specification: {"specs/musoq-core-language-spec.md":["4.5","8.2","11.5.3","11.8","11.11.1","11.13"]}
    // Historical fixture: {"left_values":[{"Id":1,"Name":"A"},{"Id":2,"Name":"B"},{"Id":3,"Name":"C"}],"right_values":[{"Id":1,"Name":"A-1"},{"Id":1,"Name":"A-2"},{"Id":2,"Name":"B-1"}],"join":"LEFT JOIN on l.Id = r.Id","window":"Count(r.Name) OVER (PARTITION BY l.Id), no ORDER BY","qualify":"MatchCount <> 1","schema_provider_for_compiler":"SpecStressCoupledSchemaProvider (the query does not reference the provider; both sources are inline VALUES)"}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["LeftName","RightName","MatchCount"],"column_types":["System.String","System.String","System.Int32"],"row_count":3,"rows":[["A","A-1",2],["A","A-2",2],["C",null,0]],"row_ordering":"No top-level ORDER BY is specified; compare as a multiset"}
    // Observed failure: compile|MQ3027_InvalidQueryOutputType|windowed-count-over-inline-VALUES-left-join|before-execution
    // Restored regression: in-memory TransitionSchema window factories resolve through the P1-064 repair.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0057")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0057_QualifyCountsOnlyPresentJoinMatches()
    {
        const string query = "select l.Name as LeftName,\r\n       r.Name as RightName,\r\n       Count(r.Name) over (partition by l.Id) as MatchCount\r\nfrom values {\r\n    (Id: 1, Name: 'A'),\r\n    (Id: 2, Name: 'B'),\r\n    (Id: 3, Name: 'C')\r\n} l\r\nleft join values {\r\n    (Id: 1, Name: 'A-1'),\r\n    (Id: 1, Name: 'A-2'),\r\n    (Id: 2, Name: 'B-1')\r\n} r\r\n    on l.Id = r.Id\r\nqualify MatchCount <> 1";

        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("LeftName", typeof(System.String)),
            ("RightName", typeof(System.String)),
            ("MatchCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "A", "A-1", 2L },
            new object?[] { "A", "A-2", 2L },
            new object?[] { "C", null, 0L });
    }
}
