using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PartialTextCursorMetadataTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow(
        "text Child { Value: chars[1], End: literal '!' }; text Rec { Prefix: chars[1], Child: Child }",
        "abc",
        2,
        false,
        "{\"Prefix\":\"a\"}"
    )]
    [DataRow(
        "text Child { Value: chars[1], End: literal '!' }; text Rec { Prefix: chars[1], Child: optional Child, Tail: rest }",
        "abc",
        3,
        true,
        "{\"Prefix\":\"a\",\"Child\":null,\"Tail\":\"bc\"}"
    )]
    [DataRow("text Rec { Values: repeat chars[2] }", "abc", 2, false, "{}")]
    [DataRow("text Rec { Prefix: until '|', Next: until '|', Tail: rest }", "a|bc", 2, false, "{\"Prefix\":\"a\"}")]
    [DataRow("text Rec { Prefix: until '|', Tail: rest }", "|", 1, true, "{\"Prefix\":\"\",\"Tail\":\"\"}")]
    public void PartialParse_ShouldExposeCompletionAndAtomicNestedResults(
        string schema,
        string input,
        int position,
        bool success,
        string json
    )
    {
        var provider = new TextSchemaProvider(
            new Dictionary<string, IEnumerable<TextEntity>> { ["#test"] = [new() { Text = input }] }
        );
        using var query = CompileGeneratedQuery(
            $"{schema}; select p.BytesConsumed, p.BitOffset, p.IsSuccess, ToJson(p.ParsedFields) as Json "
                + "from #test.files() f cross apply PartialParse<Rec>(f.Text) p",
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions
        );
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [position, 0, success, json]);
    }
}
