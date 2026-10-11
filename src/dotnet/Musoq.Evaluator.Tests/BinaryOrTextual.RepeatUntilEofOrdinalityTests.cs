using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentBinarySchemaProvider = Musoq.Evaluator.Tests.Components.BinarySchemaProvider;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualRepeatUntilEofSubstreamTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0072
    // Specification: {"specs/musoq-core-language-spec.md":["5.3","9.1","9.4","9.5","13.1"],"specs/musoq-binary-text-spec.md":["3.2","3.3","4.1","4.2.1","4.10","7.1","7.2","7.3"]}
    // Historical fixture: {"provider":"Musoq.Evaluator.Tests.Components.BinarySchemaProvider","schema_registration_key":"#test","source_method":"entities","row_type":"Musoq.Evaluator.Tests.Components.BinaryEntity","input_rows":[{"Name":"empty","ContentBytesHex":"00","Content":[0]},{"Name":"one","ContentBytesHex":"0207","Content":[2,7]},{"Name":"two","ContentBytesHex":"030809","Content":[3,8,9]}],"binary_schema":"binary Packet { Prefix: byte, Values: byte repeat until eof }","apply_chain":["CROSS APPLY Interpret<Packet>(e.Content) p","CROSS APPLY p.Values v WITH ORDINALITY"],"post_order":"e.Name ASC, v.Ordinal DESC"}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Name","Prefix","Item","Ordinal"],"column_types":["System.String","System.Byte","System.Byte","System.Int32"],"row_count":3,"rows":[["one",2,7,0],["two",3,9,1],["two",3,8,0]],"row_ordering":"ORDER BY e.Name ASC, v.Ordinal DESC."}
    // Observed failure: compile|MQ9001_InternalCompilerError|binary-repeat-until-eof-with-apply-ordinality-and-mixed-ordering|before-execution
    // Disposition: Failed root query; diary does not establish an independently confirmed product defect. | Existing ignored test: BinaryRepeatUntilEofSupportsApplyOrdinalityAndMixedOrdering.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0072")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void BinaryRepeatUntilEofSupportsApplyOrdinalityAndMixedOrdering()
    {
        const string query = """
            binary Packet {
                Prefix: byte,
                Values: byte repeat until eof
            };
            select e.Name as Name,
                   p.Prefix as Prefix,
                   v.Value as Item,
                   v.Ordinal as Ordinal
            from test.entities() e
            cross apply Interpret<Packet>(e.Content) p
            cross apply p.Values v with ordinality
            order by e.Name asc, v.Ordinal desc
            """;
        var entities = new[]
        {
            new ComponentBinaryEntity { Name = "empty", Content = [0] },
            new ComponentBinaryEntity { Name = "one", Content = [2, 7] },
            new ComponentBinaryEntity { Name = "two", Content = [3, 8, 9] }
        };
        var sources = new Dictionary<string, IEnumerable<ComponentBinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ComponentBinarySchemaProvider(sources),
            LoggerResolver,
            TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertColumns(result,
            ("Name", typeof(string)), ("Prefix", typeof(byte)),
            ("Item", typeof(byte)), ("Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            result,
            new object?[] { "one", (byte)2, (byte)7, 0 },
            new object?[] { "two", (byte)3, (byte)9, 1 },
            new object?[] { "two", (byte)3, (byte)8, 0 });
    }
}
