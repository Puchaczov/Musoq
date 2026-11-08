using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeTextEscapedModifierTests
{
    // Decode escaped closing delimiters and doubled escapes exactly once.
    // Repeat and rest advance by the original input length, preserving both trailers.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0015")]
    public void Current_P4_0015_EscapedBetweenRepeatResumesAtTrailer()
    {
        AssertOriginalEscapedRepeatResult("a>b");
    }

    [TestMethod]
    public void EscapedBetweenRepeat_DecodingPreservesAllRowsAndTrailers()
    {
        AssertOriginalEscapedRepeatResult("a>b");
    }

    private static void AssertOriginalEscapedRepeatResult(string escapedDelimiterValue)
    {
        const string query = "text Entry {\r\n    Value: between '<' '>' escaped\r\n}\r\ntext Packet {\r\n    Entries: repeat Entry until '!',\r\n    Trailer: rest\r\n}\r\nselect e.Name as PacketName, item.Value as ParsedValue, p.Trailer as Trailer\r\nfrom #test.entities() e\r\ncross apply Parse<Packet>(e.Text) p\r\ncross apply p.Entries item";
        var entities = new[]
        {
            new TextEntity { Name = "escaped", Text = "<a\\>b><c\\\\d>!tail" },
            new TextEntity { Name = "single", Text = "<x>!done" }
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
            ("ParsedValue", typeof(System.String)),
            ("Trailer", typeof(System.String)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "escaped", escapedDelimiterValue, "tail" },
            new object?[] { "escaped", "c\\d", "tail" },
            new object?[] { "single", "x", "done" });
    }
}
