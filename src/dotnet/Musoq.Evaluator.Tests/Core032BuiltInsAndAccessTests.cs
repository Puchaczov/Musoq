using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core032BuiltInsAndAccessTests : BasicEntityTestBase
{
    [TestMethod]
    public void RequiredBuiltIns_WithNullArguments_ShouldPropagateNull()
    {
        const string query =
            "select Trim(Name), ToUpper(Name), Abs(NullableValue), Concat(Name, 'text') " +
            "from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
                query,
                CreateSingleSource(new BasicEntity { Name = null, NullableValue = null }))
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(
            table[0].Values.All(static value => value is null),
            string.Join(", ", table[0].Values.Select(static value => value is null ? "<null>" : $"<{value}>")));
    }

    [TestMethod]
    public void StringOperations_ShouldSeparateOrdinalComparisonFromInsensitiveMatching()
    {
        const string query =
            "select Name, Name = 'hello' as EqualValue, Name like 'hello' as LikeValue, " +
            "Contains(Name, 'HEL') as ContainsValue, Replace(Name, 'HELLO', 'Hi') as Replaced, " +
            "IndexOf(Name, 'HEL') as Position from #A.Entities() order by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("Hello"), new BasicEntity("hello")]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { "Hello", false, true, true, "Hi", 0 },
            table[0].Values);
        CollectionAssert.AreEqual(
            new object?[] { "hello", true, true, true, "Hi", 0 },
            table[1].Values);
    }

    [TestMethod]
    public void TypedNestedIndexing_ShouldUseSafeNegativeAndOutOfBoundsSemantics()
    {
        const string query =
            "select a.Children[-1].Name, a.Children[100].Name, a.Array[-100] from #A.Entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateSingleSource(new BasicEntity("root")))
            .Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(
            new object?[] { "child2", null, 2 },
            table[0].Values);
    }

    [TestMethod]
    public void NestedRuntimeIndexing_ShouldResolveCollectionsStringsAndDictionariesSafely()
    {
        var value = new NestedEntity(
            [new NestedItem("first"), new NestedItem("second"), new NestedItem("last")],
            "abc",
            new Dictionary<string, NestedItem> { ["key"] = new("mapped") },
            [10, 20, 30]);

        Assert.AreEqual("last", EvaluationHelper.GetNestedValue(value, "Items[-1].Name"));
        Assert.AreEqual("last", EvaluationHelper.GetNestedValue(value, "Items[-4].Name"));
        Assert.AreEqual(0, EvaluationHelper.GetNestedValue(value, "Numbers[100]"));
        Assert.AreEqual('c', EvaluationHelper.GetNestedValue(value, "Text[-1]"));
        Assert.AreEqual('\0', EvaluationHelper.GetNestedValue(value, "Text[100]"));
        Assert.AreEqual("mapped", EvaluationHelper.GetNestedValue(value, "Named['key'].Name"));
        Assert.IsNull(EvaluationHelper.GetNestedValue(value, "Named['missing'].Name"));
    }

    [TestMethod]
    public void UnknownBuiltIn_ShouldReportStructuredQueryDiagnostic()
    {
        const string query = "select MissingFunction(Name) from #A.Entities()";
        var result = new QueryAnalyzer(CreateSchemaProvider()).Analyze(query);

        var diagnostic = result.Errors.Single();
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(DiagnosticCode.MQ3086_UnknownCallable, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf("MissingFunction", StringComparison.Ordinal), envelope.Offset);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.AreEqual("MissingFunction", envelope.Arguments["callable"]);
    }

    [TestMethod]
    public void IndexingNonArray_ShouldReportStructuredTypeDiagnostic()
    {
        const string query = "select Population[0] from #A.Entities()";
        var result = new QueryAnalyzer(CreateSchemaProvider()).Analyze(query);

        var diagnostic = result.Errors.Single();
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(DiagnosticCode.MQ3017_ObjectNotArray, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf("Population", StringComparison.Ordinal), envelope.Offset);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsTrue(envelope.Actions.Count > 0);
    }

    [TestMethod]
    public void TemporalStringCoercion_ShouldEmitAmbiguityAdvisoryWithSourceMetadata()
    {
        const string query = "select Name from #A.Entities() where Time = '01/02/2026'";
        var result = new QueryAnalyzer(CreateSchemaProvider()).Analyze(query);
        var diagnostic = result.Warnings.Single(item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(query.IndexOf("'01/02/2026'", StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual("01/02/2026".Length + 2, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsTrue(envelope.Actions.Count > 0);
    }

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
    {
        return new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [new BasicEntity("Ada")]
            });
    }

    private sealed class NestedEntity(
        IReadOnlyList<NestedItem> items,
        string text,
        IReadOnlyDictionary<string, NestedItem> named,
        IReadOnlyList<int> numbers)
    {
        public IReadOnlyList<NestedItem> Items { get; } = items;
        public string Text { get; } = text;
        public IReadOnlyDictionary<string, NestedItem> Named { get; } = named;
        public IReadOnlyList<int> Numbers { get; } = numbers;
    }

    private sealed class NestedItem(string name)
    {
        public string Name { get; } = name;
    }
}
