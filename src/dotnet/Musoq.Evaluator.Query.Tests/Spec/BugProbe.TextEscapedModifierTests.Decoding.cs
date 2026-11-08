using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeTextEscapedModifierTests
{
    [TestMethod]
    [DataRow("between '<' '>' escaped", @"<a\>b>tail", "a>b")]
    [DataRow("between '<' '>' escaped", @"<a\\>tail", @"a\")]
    [DataRow("between '<' '>' escaped", @"<a\\\>b>tail", @"a\>b")]
    [DataRow("between '<' '>' escaped", @"<a\qb>tail", @"a\qb")]
    [DataRow("between '<<' '>>' escaped", @"<<a\>>b>>tail", "a>>b")]
    [DataRow("between '<<' '>>' escaped", @"<<a\>>>tail", @"a\>")]
    [DataRow("between '<' '>' escaped '~'", "<a~>b>tail", "a>b")]
    [DataRow(@"between '\'' '\'' escaped '\''", "'it''s'tail", "it's")]
    [DataRow("between '<' '>' escaped trim upper", @"<  a\>b  >tail", "A>B")]
    public void TextBetween_EscapedContentDecodesBeforeModifiersAndPreservesRest(
        string fieldDefinition, string input, string expected)
    {
        var query = $"text Packet {{ Value: {fieldDefinition}, Trailer: rest }} " +
                    "select p.Value as Value, p.Trailer as Trailer " +
                    "from #test.entities() e cross apply Parse<Packet>(e.Text) p";
        var sources = new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new TextEntity { Name = "input", Text = input }]
        };
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(string)), ("Trailer", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { expected, "tail" });
    }
}
