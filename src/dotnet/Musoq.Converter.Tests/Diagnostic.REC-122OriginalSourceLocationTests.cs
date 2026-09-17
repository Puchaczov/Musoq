using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Targets.Abstractions;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC122OriginalSourceLocationTests
{
    [TestMethod]
    public void CloneLocationMatrix_ShouldResolveEverySpanToOriginalText()
    {
        Assert.HasCount(12, CloneCases);
        var failures = new List<string>();

        foreach (var testCase in CloneCases)
        {
            try
            {
                var original = Parse(testCase.Query);
                var visitor = new CloneQueryVisitor();
                original.Accept(new CloneTraverseVisitor(visitor));

                AssertEquivalentSpan(testCase, original, visitor.Root);
            }
            catch (System.Exception exception)
            {
                failures.Add($"{testCase.Id}: {exception.Message}");
            }
        }

        Assert.IsEmpty(failures, string.Join(System.Environment.NewLine, failures));
    }

    [TestMethod]
    public void NormalizationLocationMatrix_ShouldResolveEverySpanToOriginalText()
    {
        Assert.HasCount(12, NormalizationCases);
        var failures = new List<string>();

        foreach (var testCase in NormalizationCases)
        {
            try
            {
                var original = Parse(testCase.Query);
                var normalized = new PreLogicalNormalizer().Normalize(original).NormalizedRoot;

                AssertEquivalentSpan(testCase, original, normalized);
            }
            catch (System.Exception exception)
            {
                failures.Add($"{testCase.Id}: {exception.Message}");
            }
        }

        Assert.IsEmpty(failures, string.Join(System.Environment.NewLine, failures));
    }

    [TestMethod]
    public void RewriteLocationMatrix_ShouldResolveEverySpanToOriginalText()
    {
        Assert.HasCount(12, RewriteCases);
        var failures = new List<string>();

        foreach (var testCase in RewriteCases)
        {
            try
            {
                var items = InstanceCreator.CreateForAnalyze(
                    testCase.Query,
                    $"REC122_{testCase.Id}",
                    new SystemSchemaProvider(),
                    new TestsLoggerResolver(),
                    new CompilationOptions());

                Assert.IsFalse(items.DiagnosticContext.HasErrors, testCase.Id);
                var phase = items.SemanticArtifacts.Phase;
                Assert.IsNotNull(phase.RewrittenQuery, testCase.Id);
                AssertEquivalentSpan(testCase, phase.MetadataQuery, phase.RewrittenQuery!);
            }
            catch (System.Exception exception)
            {
                failures.Add($"{testCase.Id}: {exception.Message}");
            }
        }

        Assert.IsEmpty(failures, string.Join(System.Environment.NewLine, failures));
    }

    [TestMethod]
    public void GeneratedBoundaryMatrix_ShouldNeverInventQueryLocations()
    {
        Assert.HasCount(12, GeneratedCases);

        foreach (var testCase in GeneratedCases)
        {
            var context = new DiagnosticContext(new SourceText(testCase.Query, "query.musoq"));
            TargetDiagnosticReporter.Report(
                [new TargetDiagnostic(
                    $"MT{testCase.Id[1..]}",
                    TargetDiagnosticSeverity.Error,
                    "generated source failed",
                    new TargetSourceRange(testCase.GeneratedOffset, testCase.GeneratedLength, 4, 7, 4, 7 + testCase.GeneratedLength),
                    "CompiledQuery.g.cs",
                    "generated line")],
                context);

            var diagnostic = context.Diagnostics.Single();
            Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, diagnostic.SourceKind, testCase.Id);
            Assert.AreEqual(testCase.GeneratedOffset, diagnostic.Location.Offset, testCase.Id);
            Assert.AreEqual(testCase.GeneratedOffset + testCase.GeneratedLength, diagnostic.EndLocation.Offset, testCase.Id);
            Assert.IsEmpty(diagnostic.RelatedLocations, testCase.Id);
            Assert.IsFalse(diagnostic.Location.Offset < 0, testCase.Id);
            Assert.AreNotEqual(testCase.Query.IndexOf(testCase.QueryMarker, System.StringComparison.Ordinal), diagnostic.Location.Offset, testCase.Id);
        }
    }

    [TestMethod]
    public void WrongOriginMapMutation_ShouldFailTheLocationAssertion()
    {
        var query = "select d.Dummy from #system.dual() d where d.Dummy = 'x'";
        var parsed = Parse(query);
        var original = FindRequired(parsed, nameof(EqualityNode), "d.Dummy = 'x'");

        var correct = new SourceOrigin(original.Span.Start, original.Span.Length);
        var wrongOrClamped = new SourceOrigin(0, query.Length);

        Assert.IsTrue(IsExactOrigin(query, original, correct));
        Assert.IsFalse(IsExactOrigin(query, original, wrongOrClamped));
    }

    private static void AssertEquivalentSpan(LocationCase testCase, Node originalRoot, Node transformedRoot)
    {
        var original = Find(originalRoot, testCase.NodeType, testCase.Marker);
        var transformed = FindRequired(transformedRoot, testCase.NodeType, testCase.Marker);

        var expected = original?.HasSpan == true
            ? original.Span
            : SpanForText(testCase.Query, testCase.ExpectedText ?? testCase.Marker, testCase.Id);
        Assert.IsTrue(transformed.HasSpan, $"{testCase.Id}: transformed node lost its span");
        Assert.AreEqual(expected, transformed.Span, testCase.Id);
        Assert.AreEqual(
            testCase.Query.Substring(transformed.Span.Start, transformed.Span.Length),
            testCase.Query.Substring(expected.Start, expected.Length),
            testCase.Id);
        Assert.IsFalse(
            transformed.Span.Length == testCase.Query.Length && transformed.Span.Start == 0,
            $"{testCase.Id}: transformation widened the source span to the whole query");
    }

    private static bool IsExactOrigin(string query, Node source, SourceOrigin origin)
    {
        return origin.Start == source.Span.Start &&
               origin.Length == source.Span.Length &&
               origin.Start >= 0 &&
               origin.Start + origin.Length <= query.Length &&
               query.Substring(origin.Start, origin.Length) == query.Substring(source.Span.Start, source.Span.Length);
    }

    private static RootNode Parse(string query)
    {
        var lexer = new Lexer(query, true);
        return new global::Musoq.Parser.Parser(lexer).ComposeAll();
    }

    private static Node FindRequired(Node root, string nodeType, string marker)
    {
        return Find(root, nodeType, marker) ?? throw new AssertFailedException(
            $"Could not find {nodeType} containing '{marker}' under {root.GetType().Name}.");
    }

    private static Node? Find(Node root, string nodeType, string marker)
    {
        return Descendants(root)
            .FirstOrDefault(node =>
                node.GetType().Name == nodeType &&
                (string.IsNullOrEmpty(marker) || node.ToString().Contains(marker, System.StringComparison.Ordinal)));
    }

    private static TextSpan SpanForText(string query, string text, string id)
    {
        var start = query.IndexOf(text, System.StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"{id}: expected source text '{text}' was not found");
        return new TextSpan(start, text.Length);
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        var seen = new HashSet<Node>();
        var pending = new Stack<Node>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!seen.Add(node))
                continue;

            yield return node;

            foreach (var property in node.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                    continue;

                object? value;
                try
                {
                    value = property.GetValue(node);
                }
                catch (TargetInvocationException)
                {
                    continue;
                }

                if (value is Node child)
                {
                    pending.Push(child);
                    continue;
                }

                if (value is not IEnumerable enumerable || value is string)
                    continue;

                foreach (var item in enumerable)
                    if (item is Node childItem)
                        pending.Push(childItem);
            }
        }
    }

    private sealed record LocationCase(string Id, string Query, string NodeType, string Marker, string? ExpectedText = null);

    private sealed record GeneratedCase(string Id, string Query, string QueryMarker, int GeneratedOffset, int GeneratedLength);

    private readonly record struct SourceOrigin(int Start, int Length);

    private static IReadOnlyList<LocationCase> CloneCases { get; } =
    [
        new("C01", "select d.Dummy from #system.dual() d", nameof(QueryNode), "d.Dummy"),
        new("C02", "select d.Dummy from #system.dual() d where d.Dummy = 'x'", nameof(WhereNode), "where", "d.Dummy = 'x'"),
        new("C03", "select a.Dummy from #system.dual() a inner join #system.dual() b on a.Dummy = b.Dummy", nameof(JoinFromNode), "a.Dummy = b.Dummy"),
        new("C04", "select a.Dummy from #system.dual() a cross apply #system.dual() b", nameof(ApplyFromNode), "cross apply"),
        new("C05", "select case when 1 = 1 then 1 else 0 end from #system.dual() d", nameof(CaseNode), "case", "case when 1 = 1 then 1 else 0 end"),
        new("C06", "with c as (select d.Dummy from #system.dual() d) select Dummy from c", nameof(CteExpressionNode), "with"),
        new("C07", "select d.Dummy from #system.dual() d order by d.Dummy", nameof(OrderByNode), "order by"),
        new("C08", "select d.Dummy from #system.dual() d where d.Dummy is null", nameof(WhereNode), "where", "d.Dummy is null"),
        new("C09", "select d.Dummy from #system.dual() d where d.Dummy in ('a', 'b')", nameof(WhereNode), "where", "d.Dummy in ('a', 'b')"),
        new("C10", "select * from #system.dual() d", nameof(AllColumnsNode), "*", "*"),
        new("C11", "select d.Dummy from #system.dual() d where d.Dummy = 'x'", nameof(EqualityNode), "d.Dummy = 'x'"),
        new("C12", "select d.Dummy from #system.dual() d where d.Dummy between 'a' and 'z'", nameof(BetweenNode), "between")
    ];

    private static IReadOnlyList<LocationCase> NormalizationCases { get; } =
    [
        new("N01", "from #system.dual() d select d.Dummy", nameof(QueryNode), "d.Dummy"),
        new("N02", "select distinct d.Dummy from #system.dual() d", nameof(GroupByNode), "d.Dummy", "Dummy"),
        new("N03", "with c as (select d.Dummy from #system.dual() d) select Dummy from c", nameof(CteInnerExpressionNode), "c as"),
        new("N04", "select d.Dummy from #system.dual() d where d.Dummy = 'x'", nameof(QueryNode), "d.Dummy"),
        new("N05", "select d.Dummy from #system.dual() d order by d.Dummy", nameof(OrderByNode), "order by"),
        new("N06", "select d.Dummy from #system.dual() d skip 1 take 2", nameof(SkipNode), "skip"),
        new("N07", "select d.Dummy from #system.dual() d skip 1 take 2", nameof(TakeNode), "take"),
        new("N08", "select case when 1 = 1 then 1 else 0 end from #system.dual() d", nameof(CaseNode), "case"),
        new("N09", "select d.Dummy from #system.dual() d where d.Dummy in ('a', 'b')", nameof(InNode), "in"),
        new("N10", "select d.Dummy from #system.dual() d where d.Dummy between 'a' and 'z'", nameof(BetweenNode), "between"),
        new("N11", "select d.Dummy from #system.dual() d where d.Dummy = 'x' and d.Dummy = 'y'", nameof(AndNode), "and"),
        new("N12", "binary Header { Value: byte }; select d.Dummy from #system.dual() d", nameof(BinarySchemaNode), "binary Header")
    ];

    private static IReadOnlyList<LocationCase> RewriteCases { get; } =
    [
        new("R01", "select d.Dummy from #system.dual() d where d.Dummy = 'x'", nameof(EqualityNode), "d.Dummy = 'x'"),
        new("R02", "select d.Dummy from #system.dual() d where d.Dummy is null", nameof(IsNullNode), "is null"),
        new("R03", "select d.Dummy from #system.dual() d where d.Dummy between 'a' and 'z'", nameof(AndNode), "and", "Dummy between 'a' and 'z'"),
        new("R04", "select d.Dummy from #system.dual() d where d.Dummy in ('a', 'b')", nameof(OrNode), "or", "Dummy in ('a', 'b')"),
        new("R05", "select d.Dummy from #system.dual() d where d.Dummy = 'x' and d.Dummy = 'y'", nameof(AndNode), "and"),
        new("R06", "select d.Dummy from #system.dual() d where d.Dummy = 'x' or d.Dummy = 'y'", nameof(OrNode), "or"),
        new("R07", "select d.Dummy from #system.dual() d order by d.Dummy", nameof(OrderByNode), "order by"),
        new("R08", "select distinct d.Dummy from #system.dual() d", nameof(GroupByNode), "d.Dummy"),
        new("R09", "with c as (select d.Dummy from #system.dual() d) select Dummy from c", nameof(CteExpressionNode), "with"),
        new("R10", "select * from #system.dual() d", nameof(FieldNode), "d.Dummy", "*"),
        new("R11", "text Header { Value: rest }; select h.Value from #system.dual() d cross apply Parse<Header>(d.Dummy) h", nameof(InterpretFromNode), "Parse", "Parse<Header>"),
        new("R12", "select d.Dummy from #system.dual() d", nameof(SelectNode), "select")
    ];

    private static IReadOnlyList<GeneratedCase> GeneratedCases { get; } =
    [
        new("G01", "select d.Dummy from #system.dual() d", "d.Dummy", 101, 1),
        new("G02", "from #system.dual() d select d.Dummy", "d.Dummy", 102, 2),
        new("G03", "select distinct d.Dummy from #system.dual() d", "distinct", 103, 3),
        new("G04", "select * from #system.dual() d", "*", 104, 4),
        new("G05", "with c as (select d.Dummy from #system.dual() d) select Dummy from c", "c as", 105, 5),
        new("G06", "select d.Dummy from #system.dual() d where d.Dummy = 'x'", "where", 106, 6),
        new("G07", "select d.Dummy from #system.dual() d order by d.Dummy", "order by", 107, 7),
        new("G08", "select d.Dummy from #system.dual() d skip 1 take 2", "skip", 108, 8),
        new("G09", "select case when 1 = 1 then 1 else 0 end from #system.dual() d", "case", 109, 9),
        new("G10", "select d.Dummy from #system.dual() d where d.Dummy between 'a' and 'z'", "between", 110, 10),
        new("G11", "select a.Dummy from #system.dual() a inner join #system.dual() b on a.Dummy = b.Dummy", "inner join", 111, 11),
        new("G12", "binary Header { Value: byte }; select d.Dummy from #system.dual() d", "binary Header", 112, 12)
    ];
}
