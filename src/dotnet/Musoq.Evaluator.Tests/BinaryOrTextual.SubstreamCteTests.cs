using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ComponentBinaryEntity = Musoq.Evaluator.Tests.Components.BinaryEntity;
using ComponentBinarySchemaProvider = Musoq.Evaluator.Tests.Components.BinarySchemaProvider;

namespace Musoq.Evaluator.Tests;

public partial class BinaryOrTextualSubstreamTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0068
    // Specification: {"specs/musoq-core-language-spec.md":["3.4","5.3","6.2","6.3","9.1","14.1","14.3"],"specs/musoq-binary-text-spec.md":["3.2","3.3","4.1","4.2.1","4.3","4.8","4.13","4.13.1","4.13.2","4.13.3","7.1","7.2","7.3","8.1"]}
    // Historical fixture: {"provider":"Musoq.Evaluator.Tests.Components.BinarySchemaProvider","schema_registration_key":"test","source_method":"entities","row_type":"Musoq.Evaluator.Tests.Components.BinaryEntity","input_rows":[{"Name":"filtered","Content":[160,2,17,34,127]},{"Name":"kept","Content":[176,3,68,68,85,110]}],"binary_schemas":["binary PayloadBody { Value: byte, Mirror: byte at 1 }","binary Packet { Prefix: byte, Length: byte, Payload: substream[Length] as PayloadBody lax, Trailer: byte }"],"query_source":"test.entities() e CROSS APPLY Interpret<Packet>(e.Content) p","cte":"Parsed exports four explicit columns for the outer query."}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["Prefix","PayloadValue","PayloadMirror","Trailer"],"column_types_asserted":false,"column_type_oracle_note":"No CLR metadata type is asserted because P4-0011 already confirmed a separate nested-byte type-metadata defect.","row_count":2,"rows":[[160,17,34,127],[176,68,68,110]],"row_ordering":"No top-level ORDER BY; compare as an exact row multiset."}
    // Observed failure: spec-mismatch|BINARY-TEXT-APPENDIX-TYPE-MAPPING|nested-substream-byte-field-System.Object|MQ3027-CTE-output
    // Disposition: Failed root query; diary does not establish an independently confirmed product defect.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0068")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0068_LaxSubstreamCteExposesNestedFields()
    {
        const string query = "binary PayloadBody {\r\n    Value: byte,\r\n    Mirror: byte at 1\r\n};\r\nbinary Packet {\r\n    Prefix: byte,\r\n    Length: byte,\r\n    Payload: substream[Length] as PayloadBody lax,\r\n    Trailer: byte\r\n};\r\nwith Parsed as (\r\n    select p.Prefix as Prefix,\r\n           p.Payload.Value as PayloadValue,\r\n           p.Payload.Mirror as PayloadMirror,\r\n           p.Trailer as Trailer\r\n    from test.entities() e\r\n    cross apply Interpret<Packet>(e.Content) p\r\n)\r\nselect p.Prefix as Prefix,\r\n       p.PayloadValue as PayloadValue,\r\n       p.PayloadMirror as PayloadMirror,\r\n       p.Trailer as Trailer\r\nfrom Parsed p";
        var entities = new[]
        {
            new ComponentBinaryEntity { Name = "filtered", Content = [160, 2, 17, 34, 127] },
            new ComponentBinaryEntity { Name = "kept", Content = [176, 3, 68, 68, 85, 110] }
        };
        var sources = new Dictionary<string, IEnumerable<ComponentBinaryEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = CompileGeneratedQuery(query, Guid.NewGuid().ToString(), new ComponentBinarySchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Prefix", typeof(byte)),
            ("PayloadValue", typeof(byte)),
            ("PayloadMirror", typeof(byte)),
            ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { (byte)160, (byte)17, (byte)34, (byte)127 },
            new object?[] { (byte)176, (byte)68, (byte)68, (byte)110 });
    }
}
