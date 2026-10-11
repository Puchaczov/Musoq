using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class Core022GroupByAggregationTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0044
    // Specification: {"specs/musoq-core-language-spec.md":["2.9","4.5","6.5","10.1","10.2","10.3","13.1","13.2","14.1","14.2","14.3","14.5","14.8","19.7"]}
    // Historical fixture: {"provider":"Inline VALUES with an empty BasicSchemaProvider<BasicEntity>","cte_definition":"ScoreRows","values_alias":"v","grouping_key":"Bucket","aggregates":["Count(*) as RowsInGroup","Count(Score) as PresentScores","Sum(Score ?? 0) as FilledTotal"],"ordering":"FilledTotal DESC, Bucket ASC","values_rows":[{"Bucket":"A","Score":null},{"Bucket":"A","Score":2},{"Bucket":"B","Score":null},{"Bucket":"B","Score":null},{"Bucket":"C","Score":3},{"Bucket":"C","Score":null},{"Bucket":"D","Score":0},{"Bucket":"D","Score":null}]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Bucket","RowsInGroup","PresentScores","FilledTotal"],"group_count":4,"groups_before_order":[["A",2,1,2],["B",2,0,0],["C",2,1,3],["D",2,1,0]],"rows":[["C",2,1,3],["A",2,1,2],["B",2,0,0],["D",2,1,0]],"row_ordering":"FilledTotal DESC, then Bucket ASC; B precedes D on the zero-total tie"}
    // Observed failure: compile|MQ9001_InternalCompilerError|nullable-values-fallback-inside-cte-aggregate|before-execution
    // Regression: nullable aggregate input cast covers the entire coalesced operand. Ordinary Count/Sum use their typed kernel metadata.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0044")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0044_GroupedCountAndCoalescedSumHandleNulls()
    {
        const string query = "with ScoreRows as (\r\n    select v.Bucket as Bucket, v.Score as Score\r\n    from values {\r\n        (Bucket: 'A', Score: null),\r\n        (Bucket: 'A', Score: 2),\r\n        (Bucket: 'B', Score: null),\r\n        (Bucket: 'B', Score: null),\r\n        (Bucket: 'C', Score: 3),\r\n        (Bucket: 'C', Score: null),\r\n        (Bucket: 'D', Score: 0),\r\n        (Bucket: 'D', Score: null)\r\n    } v\r\n)\r\nselect r.Bucket as Bucket,\r\n       Count(*) as RowsInGroup,\r\n       Count(r.Score) as PresentScores,\r\n       Sum(r.Score ?? 0) as FilledTotal\r\nfrom ScoreRows r\r\ngroup by r.Bucket\r\norder by FilledTotal desc, Bucket asc";

        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new BasicSchemaProvider<BasicEntity>(new Dictionary<string, IEnumerable<BasicEntity>>()), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Bucket", typeof(System.String)),
            ("RowsInGroup", typeof(long)),
            ("PresentScores", typeof(long)),
            ("FilledTotal", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { "C", 2L, 1L, 3 },
            new object?[] { "A", 2L, 1L, 2 },
            new object?[] { "B", 2L, 0L, 0 },
            new object?[] { "D", 2L, 1L, 0 });
    }

    [TestMethod]
    [DataRow("Sum", typeof(int?), 2)]
    [DataRow("Min", typeof(int?), 0)]
    [DataRow("Max", typeof(int?), 2)]
    public void TypedAggregates_CoalescedInputsPreserveEntireOperand(string function, Type resultType, int expected)
    {
        var query = $"select {function}(v.Score ?? 0) as Result from values {{ (Score: null), (Score: 2) }} v";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Result", resultType));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { expected });
    }

    [TestMethod]
    public void TypedAverage_CoalescedInputs_ShouldPreserveTheOperandAndReturnDecimal()
    {
        const string query = "select Avg(v.Score ?? 0) as Result from values { (Score: null), (Score: 2) } v";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Result", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 1m });
    }

    [TestMethod]
    public void Count_CoalescedInputsIncludeOriginalNullRow()
    {
        const string query = "select Count(v.Score ?? 0) as Result from values { (Score: null), (Score: 2) } v";
        using var compiled = CreateAndRunVirtualMachine(query, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("Result", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 2L });
    }
}
