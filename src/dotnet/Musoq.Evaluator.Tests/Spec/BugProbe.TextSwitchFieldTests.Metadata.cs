using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeTextSwitchFieldTests
{
    // Switch arms all declare Payload as String; ordered lookahead selects A before AB.
    // A failed TryParse remains a missing result with null projected fields.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0014")]
    public void Current_P4_0014_TextSwitchChoosesFirstMatchingArm()
    {
        AssertOriginalSwitchResult(typeof(string));
    }

    [TestMethod]
    public void TextSwitch_CommonStringMetadataPreservesFirstMatchAndMissingParse()
    {
        AssertOriginalSwitchResult(typeof(string));
    }

    private static void AssertOriginalSwitchResult(Type payloadType)
    {
        const string query = "text AfterA {\r\n    _: literal 'A',\r\n    Payload: pattern '[A-Za-z]+',\r\n    _: literal '|'\r\n}\r\ntext AfterAB {\r\n    _: literal 'AB',\r\n    Payload: pattern '\\d+',\r\n    _: literal '|'\r\n}\r\ntext AfterC {\r\n    _: literal 'C',\r\n    Payload: pattern '\\d+',\r\n    _: literal '|'\r\n}\r\ntext Packet {\r\n    Variant: switch {\r\n        pattern 'A' => AfterA,\r\n        pattern 'AB' => AfterAB,\r\n        pattern 'C' => AfterC\r\n    },\r\n    Trailer: rest\r\n}\r\nselect e.Name as PacketName, case when p is missing then 'unparsed' else 'parsed' end as ParseStatus, p.Variant.Payload as BranchPayload, p.Trailer as Trailer\r\nfrom #test.entities() e\r\nouter apply TryParse<Packet>(e.Text) p";
        var entities = new[]
        {
            new TextEntity { Name = "overlap", Text = "ABred|tail" },
            new TextEntity { Name = "fallback", Text = "C123|next" },
            new TextEntity { Name = "no-match", Text = "Zignored|none" }
        };
        var sources = new Dictionary<string, IEnumerable<TextEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["test"] = entities,
            ["#test"] = entities
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("PacketName", typeof(System.String)),
            ("ParseStatus", typeof(System.String)),
            ("BranchPayload", payloadType),
            ("Trailer", typeof(System.String)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "overlap", "parsed", "Bred", "tail" },
            new object?[] { "fallback", "parsed", "123", "next" },
            new object?[] { "no-match", "unparsed", null, null });
    }
}
