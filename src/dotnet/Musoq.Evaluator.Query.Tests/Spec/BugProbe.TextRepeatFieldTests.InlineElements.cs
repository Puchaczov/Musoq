using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests.Spec;

public partial class BugProbeTextRepeatFieldTests
{
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P3-0043")]
    public void Current_P3_0043_AppliedTextElementsUseOrdinalOrdering()
    {
        const string query = "text LetterList { Letters: repeat chars[1] until end } select r.Name as Name, item.Value as Letter from #letters.entities() r cross apply Parse<LetterList>(r.Text) parsed cross apply parsed.Letters item order by item.Value asc";
        using var compiled = CompileInlineTextRepeat(query,
            new TextEntity { Name = "lowercase", Text = "cab" },
            new TextEntity { Name = "uppercase", Text = "A" });
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("Name", typeof(string)), ("Letter", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { "uppercase", "A" }, new object?[] { "lowercase", "a" },
            new object?[] { "lowercase", "b" }, new object?[] { "lowercase", "c" });
    }

    [TestMethod]
    [DataRow("chars[1]", "ab|Z", "a", "b")]
    [DataRow("literal 'x'", "xx|Z", "x", "x")]
    public void InlineTextRepeat_Delimiter_ShouldResumeAtTrailer(string element, string input, string first, string second)
    {
        var query = $"text Letters {{ Items: repeat {element} until '|', Tail: chars[1] }} select item.Value as Value, p.Tail as Tail from #letters.entities() r cross apply Parse<Letters>(r.Text) p cross apply p.Items item";
        using var compiled = CompileInlineTextRepeat(query, new TextEntity { Text = input });
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(string)), ("Tail", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { first, "Z" }, new object?[] { second, "Z" });
    }

    [TestMethod]
    public void InlineTextRepeat_EmptyInput_ShouldReturnTypedEmptyCollection()
    {
        const string query = "text Letters { Items: repeat chars[1] until end } select item.Value as Value from #letters.entities() r cross apply Parse<Letters>(r.Text) p cross apply p.Items item";
        using var compiled = CompileInlineTextRepeat(query, new TextEntity { Text = string.Empty });
        using var result = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(string)));
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    [DataRow("pattern 'a*'")]
    [DataRow("optional literal 'a'")]
    public void InlineTextRepeat_NonprogressElement_ShouldRejectInsteadOfLooping(string element)
    {
        var query = $"text Letters {{ Items: repeat {element} until end }} select p.Items as Items from #letters.entities() r cross apply Parse<Letters>(r.Text) p";
        using var compiled = CompileInlineTextRepeat(query, new TextEntity { Text = "b" });

        var failure = Assert.Throws<ParseException>(() =>
        {
            using var result = compiled.Run();
            _ = result.Count;
        });

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        StringAssert.Contains(failure.Details, "progress");
    }

    [TestMethod]
    public void InlineTextRepeat_IterationLimit_ShouldRetainFieldSpecificFailure()
    {
        const string query = "text Letters { Items: repeat chars[1] until end } select p.Items as Items from #letters.entities() r cross apply Parse<Letters>(r.Text) p";
        using var compiled = CompileInlineTextRepeat(query, new TextEntity { Text = new string('a', 10_001) });

        var failure = Assert.Throws<ParseException>(() =>
        {
            using var result = compiled.Run();
            _ = result.Count;
        });

        Assert.AreEqual(ParseErrorCode.MaxIterationsExceeded, failure.ErrorCode);
        Assert.AreEqual("Items", failure.FieldName);
        Assert.AreEqual(10_000, failure.Position);
    }

    [TestMethod]
    public void InlineTextRepeat_CaptureElement_ShouldPreserveCaptureValues()
    {
        const string query = "text Letters { Items: repeat pattern '(?<Value>[a-z])' capture (Value) until end } select p.Items as Items from #letters.entities() r cross apply Parse<Letters>(r.Text) p";
        using var compiled = CompileInlineTextRepeat(query, new TextEntity { Text = "ab" });
        using var result = compiled.Run();

        var itemsType = System.Linq.Enumerable.Single(result.Columns).ColumnType;
        Assert.IsTrue(itemsType.IsArray);
        Assert.AreEqual("CaptureResult_ItemsElement", itemsType.GetElementType()!.Name);
        Assert.AreEqual("Letters", itemsType.GetElementType()!.DeclaringType!.Name);
        TableMaterializationTestHelper.AssertColumns(result, ("Items", itemsType));
        Assert.AreEqual(1, result.Count);
        var items = (IReadOnlyList<object>)result[0].ReadValue(0);
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("a", ReadCaptureValue(items[0]));
        Assert.AreEqual("b", ReadCaptureValue(items[1]));
    }

    private static object? ReadCaptureValue(object capture)
    {
        var property = capture.GetType().GetProperty("Value")
            ?? throw new AssertFailedException("Expected a generated Value capture property.");
        return property.GetValue(capture);
    }

    private static CompiledQuery CompileInlineTextRepeat(string query, params TextEntity[] entities)
    {
        var sources = new Dictionary<string, IEnumerable<TextEntity>>(StringComparer.OrdinalIgnoreCase)
        {
            ["letters"] = entities,
            ["#letters"] = entities
        };

        return InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new TextSchemaProvider(sources), LoggerResolver, TestCompilationOptions);
    }
}
