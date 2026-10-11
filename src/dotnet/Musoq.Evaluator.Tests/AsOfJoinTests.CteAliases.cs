using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0071
    // Specification: {"specs/musoq-core-language-spec.md":["3.6","5.3","8.11","8.11.2","8.11.10","13.1","14.1","14.3","14.6"]}
    // Historical fixture: {"kind":"inline_values_in_multiple_ctes","cte_sources":[{"name":"Events","rows":[{"EventId":"B","MatchTime":10},{"EventId":"A","MatchTime":10},{"EventId":"C","MatchTime":20},{"EventId":"D","MatchTime":35}]},{"name":"States","rows":[{"StateId":"s5","MatchTime":5,"Accepted":true},{"StateId":"s10","MatchTime":10,"Accepted":true},{"StateId":"s15","MatchTime":15,"Accepted":false},{"StateId":"s20","MatchTime":20,"Accepted":true},{"StateId":"s25","MatchTime":25,"Accepted":true},{"StateId":"s35","MatchTime":35,"Accepted":false}]}],"asof_condition":"e.MatchTime > s.MatchTime","post_asof_filter":"s.Accepted = true","output_order":"EventTime DESC, EventId ASC"}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["EventId","EventTime","StateId","StateTime"],"column_types":["System.String","System.Int32","System.String","System.Int32"],"row_count":3,"rows":[["D",35,"s25",25],["A",10,"s5",5],["B",10,"s5",5]],"row_ordering":"ORDER BY EventTime DESC, EventId ASC."}
    // Observed failure: compile|MQ9001_InternalCompilerError|multiple-CTE-ASOF-strict-backward-with-post-ASOF-filter|before-execution
    // Disposition: Failed root query; diary does not establish an independently confirmed product defect. | Existing ignored test: StrictBackwardAsofJoinComposesWithMultipleCtesAndPostJoinFilter.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0071")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void StrictBackwardAsofJoinComposesWithMultipleCtesAndPostJoinFilter()
    {
        const string query = """
            with
                Events as (
                    select v.EventId as EventId, v.MatchTime as MatchTime
                    from values {
                        (EventId: 'B', MatchTime: 10),
                        (EventId: 'A', MatchTime: 10),
                        (EventId: 'C', MatchTime: 20),
                        (EventId: 'D', MatchTime: 35)
                    } v
                ),
                States as (
                    select v.StateId as StateId, v.MatchTime as MatchTime, v.Accepted as Accepted
                    from values {
                        (StateId: 's5', MatchTime: 5, Accepted: true),
                        (StateId: 's10', MatchTime: 10, Accepted: true),
                        (StateId: 's15', MatchTime: 15, Accepted: false),
                        (StateId: 's20', MatchTime: 20, Accepted: true),
                        (StateId: 's25', MatchTime: 25, Accepted: true),
                        (StateId: 's35', MatchTime: 35, Accepted: false)
                    } v
                )
            select e.EventId as EventId,
                   e.MatchTime as EventTime,
                   s.StateId as StateId,
                   s.MatchTime as StateTime
            from Events e
            asof join States s on e.MatchTime > s.MatchTime
            where s.Accepted = true
            order by EventTime desc, EventId asc
            """;
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>();
        using var compiled = CreateAndRunVirtualMachine(query, sources);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertColumns(result,
            ("EventId", typeof(string)), ("EventTime", typeof(int)),
            ("StateId", typeof(string)), ("StateTime", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            new object?[] { "D", 35, "s25", 25 },
            new object?[] { "A", 10, "s5", 5 },
            new object?[] { "B", 10, "s5", 5 });
    }
}
