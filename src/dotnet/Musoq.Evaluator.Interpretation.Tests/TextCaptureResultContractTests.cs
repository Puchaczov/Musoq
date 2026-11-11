using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TextCaptureResultContractTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void RequestedNames_ShouldControlCaseAndSerializationOrder()
    {
        using var result = Run("text Rec { Data: pattern '(?<name>a)(?<Name>b)' capture (Name, name) }", "p.Data.Name as UpperName, p.Data.name as LowerName, ToJson(p.Data)", "ab");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["b", "a", "{\"Name\":\"b\",\"name\":\"a\"}"]);
    }

    [TestMethod]
    [DataRow("(?<Last>[a-z])+", "Last", "abcd", "d")]
    [DataRow("x(?<Missing>y)?", "Missing", "x", "")]
    public void CaptureValues_ShouldObserveTheFinalOrNonparticipatingGroup(string pattern, string group, string input, string expected)
    {
        using var result = Run($"text Rec {{ Data: pattern '{pattern}' capture ({group}) }}", $"p.Data.{group}", input);
        Assert.AreEqual(typeof(string), result.Columns.Single().ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [expected]);
    }

    [TestMethod]
    public void FailedOptionalPattern_ShouldReturnNullAndRestoreTheCursor()
    {
        using var result = Run("text Rec { Data: optional pattern '(?<Name>x)' capture (Name), Tail: rest }", "p.Data.Name, ToJson(p.Data), p.Tail", "z");
        TableMaterializationTestHelper.AssertRowsInOrder(result, [null, null, "z"]);
    }

    [TestMethod]
    public void Modifiers_ShouldTransformCapturedStringsWithoutChangingCursorAdvancement()
    {
        using var result = Run("text Rec { Data: pattern 'X(?<Keep> [A-Za-z]+ )Y' capture (Keep) trim upper, Tail: rest }", "p.Data.Keep, p.Tail", "X AbC Y!");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["ABC", "!"]);
    }

    [TestMethod]
    public void RetainedCaptures_ShouldNotLimitFullMatchCursorAdvancement()
    {
        const string sql = "text Rec { Data: pattern 'prefix(?<Keep>[a-z]+)!' capture (Keep), Tail: chars[1] } select p.ParsedFields.Data.Keep, p.ParsedFields.Tail, p.BytesConsumed, ToJson(p.ParsedFields.Data) from #test.lines() e cross apply PartialParse<Rec>(e.Line) p";
        using var result = RunSql(sql, "prefixabc!tail");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["abc", "t", 11, "{\"Keep\":\"abc\"}"]);
    }

    [TestMethod]
    [DataRow(@"\A(?<Word>\p{L}+)\z")]
    [DataRow(@"\G(?<Word>\p{L}+)$")]
    [DataRow(@"\\b(?<Word>\p{L}+)")]
    public void CaptureAnchors_ShouldUseTheRemainingRegion(string pattern)
    {
        using var result = Run($"text Rec {{ _: literal 'X', Part: pattern '{pattern}' capture (Word) lower }}",
            "p.Part.Word", "XŁÓDŹ");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["łódź"]);
    }

    [TestMethod]
    public void CapturesAcrossNestedParses_ShouldKeepEachInputAndRegion()
    {
        const string sql = """
            text Child { Part: pattern '\A(?<Word>\p{L}+)\z' capture (Word) lower };
            text Rec { Left: pattern '\A(?<Word>\p{L}+)' capture (Word), _: literal ';', Right: Child };
            select p.Left.Word, p.Right.Part.Word from #test.lines() e cross apply Parse<Rec>(e.Line) p
            """;
        using var result = RunSql(sql, "a;ŁÓDŹ", "b;ΓΔ");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["a", "łódź"], ["b", "γδ"]);
    }

    [TestMethod]
    public void NonCaptureAlternation_ShouldKeepItsEstablishedMatchAndCursor()
    {
        using var result = Run("text Rec { _: literal 'x', Part: pattern 'n|foo', Tail: rest }",
            "p.Part, p.Tail", "x--foo!");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["foo", "oo!"]);
    }

    [TestMethod]
    public void GeneratedPatternStorage_ShouldAvoidUserFieldNames()
    {
        using var result = Run("text Rec { _musoqTextInput: literal 'X', _musoqPattern0: pattern 'a', Tail: rest }",
            "p.Tail", "Xa!");
        TableMaterializationTestHelper.AssertRowsInOrder(result, ["!"]);
    }

    [TestMethod]
    [DataRow("Name, Name")]
    [DataRow("name")]
    [DataRow("Missing")]
    public void InvalidRequestsInUnusedOptionalDeclarations_ShouldFailCompilation(string groups)
    {
        var error = Assert.Throws<MusoqQueryException>(() => RunSql($"text Unused {{ Data: optional pattern '(?<Name>x)' capture ({groups}) }} select 1 from #test.lines() e", "x"));
        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, error.PrimaryEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Schema, error.PrimaryEnvelope.Phase);
    }

    private Table Run(string schemas, string projection, string input) =>
        RunSql($"{schemas} select {projection} from #test.lines() e cross apply Parse<Rec>(e.Line) p", input);

    private Table RunSql(string sql, params string[] inputs)
    {
        var provider = new TextSchemaProvider(new Dictionary<string, IEnumerable<TextEntity>>
        {
            ["#test"] = inputs.Select(static input => new TextEntity { Text = input })
        });
        using var compiled = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        return TableMaterializationTestHelper.Materialize(compiled.Run());
    }
}
