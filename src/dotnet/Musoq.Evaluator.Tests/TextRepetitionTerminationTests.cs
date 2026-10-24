using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TextRepetitionTerminationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    [DataRow("", "[]", "")]
    [DataRow("ENDtail", "[]", "tail")]
    [DataRow("ENDEND", "[]", "END")]
    [DataRow("abENDtail", "[\"ab\"]", "tail")]
    [DataRow("abcd", "[\"ab\",\"cd\"]", "")]
    [DataRow("abcdENDtail", "[\"ab\",\"cd\"]", "tail")]
    public void DelimiterOrEof_ShouldTerminateBeforeCheckingAnotherIteration(string input, string items, string tail)
    {
        using var table = RunText("text Rec { Items: repeat chars[2] until 'END', Tail: rest }",
            "ToJson(r.Items) as Items, r.Tail", input);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [items, tail]);
    }

    [TestMethod]
    public void DelimiterInsideElement_ShouldBeConsumedAsElementContent()
    {
        using var table = RunText("text Rec { Items: repeat chars[5] until 'END', Tail: rest }",
            "ToJson(r.Items) as Items, r.Tail", "xENDyENDZ");
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["[\"xENDy\"]", "Z"]);
    }

    [TestMethod]
    [DataRow("pattern 'a*'", "b", 0, "ISE0009")]
    [DataRow("optional literal 'x'", "b", 0, "ISE0009")]
    [DataRow("chars[1]", "abc", 2, "ISE0009")]
    [DataRow("chars[2]", "abc", 2, "ISE0001")]
    public void FailedElementOrGuard_ShouldAbortWithoutPublishingPrefix(string element, string input, int position, string code)
    {
        using var table = RunText($"text Rec {{ Items: repeat {element} until 'END' }}",
            "r.IsSuccess, r.BytesConsumed, ToJson(r.ParsedFields) as Json, r.ErrorMessage", input, "PartialParse");
        Assert.AreEqual(false, table[0].ReadValue(0));
        Assert.AreEqual(position, table[0].ReadValue(1));
        Assert.AreEqual("{}", table[0].ReadValue(2));
        StringAssert.Contains((string)table[0].ReadValue(3)!, code);
    }

    [TestMethod]
    public void NestedRepeats_ShouldUseIndependentConfiguredBudgets()
    {
        using var table = RunText("text Chunk { Parts: repeat chars[1] until ',' }; " +
            "text Rec { Chunks: repeat Chunk until ';', Tail: rest }",
            "r.Chunks.Length, r.Chunks[0].Parts.Length, r.Chunks[1].Parts.Length, r.Tail", "ab,cd,;Z");
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 2, 2, "Z"]);
    }

    [TestMethod]
    public void NestedRepeatFailure_ShouldRetainCursorAndDiscardOuterPrefix()
    {
        using var table = RunText("text Chunk { Parts: repeat chars[1] until ',' }; " +
            "text Rec { Chunks: repeat Chunk until ';' }",
            "r.IsSuccess, r.BytesConsumed, ToJson(r.ParsedFields) as Json, r.ErrorField", "ab,cde,;", "PartialParse");
        TableMaterializationTestHelper.AssertRowsInOrder(table, [false, 5, "{}", "Chunks.Parts"]);
    }

    private Table RunText(string schema, string projection, string input, string call = "Parse")
    {
        var provider = new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = [new() { Text = input }]
        });
        var options = new CompilationOptions(usePrimitiveTypeValidation: false).WithInterpretationLimits(new(2));
        using var query = CompileGeneratedQuery($"{schema}; select {projection} from #test.files() f cross apply {call}<Rec>(f.Text) r",
            Guid.NewGuid().ToString(), provider, LoggerResolver, options);
        return TableMaterializationTestHelper.Materialize(query.Run());
    }
}
