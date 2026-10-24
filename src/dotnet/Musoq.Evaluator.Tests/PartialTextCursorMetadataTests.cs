using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PartialTextCursorMetadataTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("text Child { Value: chars[1], End: literal '!' }; text Rec { Prefix: chars[1], Child: Child }", 2, false, "{\"Prefix\":\"a\"}")]
    [DataRow("text Child { Value: chars[1], End: literal '!' }; text Rec { Prefix: chars[1], Child: optional Child, Tail: rest }", 3, true, "{\"Prefix\":\"a\",\"Child\":null,\"Tail\":\"bc\"}")]
    [DataRow("text Rec { Values: repeat chars[2] }", 2, false, "{}")]
    public void PartialParse_ShouldExposeCompletionAndAtomicNestedResults(string schema, int position, bool success, string json)
    {
        var provider = new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new() { Text = "abc" }]
        });
        using var query = CompileGeneratedQuery($"{schema}; select p.BytesConsumed, p.BitOffset, p.IsSuccess, ToJson(p.ParsedFields) as Json " +
            "from #test.files() f cross apply PartialParse<Rec>(f.Text) p", Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [position, 0, success, json]);
    }
}
