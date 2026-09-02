using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core017SemiAntiPresenceTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("semi join")]
    [DataRow("left semi join")]
    public void SemiJoin_Synonyms_ShouldReturnOnlyDistinctLeftRows(string joinOperator)
    {
        var query = $"select a.Id, a.Name from #A.Entities() a {joinOperator} #B.Entities() b on a.Id = b.Id and b.Population > 0";

        var table = Run(query, CreateSources(
            [
                new BasicEntity { Id = 1, Name = "A1" },
                new BasicEntity { Id = 2, Name = "A2" }
            ],
            [
                new BasicEntity { Id = 1, Name = "B1", Population = 10 },
                new BasicEntity { Id = 1, Name = "B1Duplicate", Population = 20 }
            ]));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [1, "A1"]);
    }

    [TestMethod]
    [DataRow("anti join")]
    [DataRow("anti semi join")]
    [DataRow("left anti semi join")]
    public void AntiJoin_Synonyms_ShouldReturnOnlyUnmatchedLeftRows(string joinOperator)
    {
        var query = $"select a.Id, a.Name from #A.Entities() a {joinOperator} #B.Entities() b on a.Id = b.Id";

        var table = Run(query, CreateSources(
            [
                new BasicEntity { Id = 1, Name = "A1" },
                new BasicEntity { Id = 2, Name = "A2" }
            ],
            [
                new BasicEntity { Id = 1, Name = "B1" },
                new BasicEntity { Id = 1, Name = "B1Duplicate" }
            ]));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [2, "A2"]);
    }

    [TestMethod]
    public void FullOuterJoin_RowPresence_ShouldDistinguishMissingRowsFromPresentNullValues()
    {
        const string query = """
            select
                case when a is present then 'LeftPresent' else 'LeftMissing' end as LeftState,
                case when b is present then 'RightPresent' else 'RightMissing' end as RightState,
                a.Id,
                b.Id,
                b.NullableValue
            from #A.Entities() a
            full join #B.Entities() b on a.Id = b.Id
            """;

        var table = Run(query, CreateSources(
            [new BasicEntity { Id = 1 }],
            [
                new BasicEntity { Id = 1, NullableValue = null },
                new BasicEntity { Id = 2, NullableValue = null }
            ]));

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("LeftState", typeof(string)),
            ("RightState", typeof(string)),
            ("a.Id", typeof(int?)),
            ("b.Id", typeof(int?)),
            ("b.NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { "LeftPresent", "RightPresent", 1, 1, null },
            new object?[] { "LeftMissing", "RightPresent", null, 2, null });
    }

    [TestMethod]
    [DataRow("semi join", "SEMI JOIN")]
    [DataRow("anti join", "ANTI JOIN")]
    public void SemiAndAntiJoin_WithoutOn_ShouldReportExactJoinConditionContract(
        string joinOperator,
        string displayOperator)
    {
        var query = $"select 1 from #A.Entities() a {joinOperator} #B.Entities() b where 1 = 1";
        var expectedStart = query.IndexOf("where", StringComparison.Ordinal);

        AssertParserDiagnosticContract(
            query,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            $"The {displayOperator} requires an ON condition.",
            expectedStart,
            "where".Length,
            "The JOIN condition is missing or is not a valid boolean expression.",
            "Core Spec - JOIN Clause",
            [
                "Add an ON clause with a comparison between the joined sources.",
                "Make sure the ON expression evaluates to a boolean value."
            ]);
    }

    [TestMethod]
    [DataRow("select a.Id from #A.Entities() a where a is missing", "a")]
    [DataRow("select a.Id from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id where a is missing", "a")]
    public void RowPresence_OnAlwaysPresentAlias_ShouldReportExactSemanticContract(string query, string alias)
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));

        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        var expectedMessage =
            $"Row presence predicates require an alias that can be absent because of LEFT, RIGHT, FULL, ASOF LEFT JOIN, or OUTER APPLY. Alias '{alias}' is always present in this scope.";
        var expectedStart = query.IndexOf($"{alias} is missing", StringComparison.Ordinal);
        var expectedSpan = new TextSpan(expectedStart, $"{alias} is missing".Length);

        Assert.AreEqual(DiagnosticCode.MQ3007_InvalidOperandTypes, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.HasCount(0, diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3007_InvalidOperandTypes, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(
            "The operator cannot be applied to the given operand types.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Operator Type Rules", envelope.DocsReference);
        var expectedFixes = new[]
        {
            "Convert operands to compatible types before comparing.",
            "For string date comparisons, parse to a numeric or date representation first."
        };
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(
            expectedFixes,
            envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    [TestMethod]
    public void SemiJoin_RightAliasOutsideOn_ShouldReportExactHiddenAliasContract()
    {
        const string query = "select b.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id";
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));

        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        var expectedStart = query.IndexOf("Name", StringComparison.Ordinal);
        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual("Unknown alias 'b'. Did you mean 'a'?", diagnostic.Message);
        Assert.AreEqual(new TextSpan(expectedStart, "Name".Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.HasCount(3, diagnostic.Arguments);
        Assert.AreEqual("b", diagnostic.Arguments["alias"]);
        Assert.AreEqual("a", diagnostic.Arguments["availableAliases"]);
        Assert.AreEqual("a", diagnostic.Arguments["suggestion"]);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3015_UnknownAlias, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedStart + "Name".Length, envelope.EndOffset);
        Assert.AreEqual("Name".Length, envelope.Length);
        Assert.AreEqual(
            "The query references an alias that is not visible in the current scope.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Aliasing", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[] { "Replace 'b' with 'a'" },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(1, envelope.Actions);
        var action = envelope.Actions[0];
        Assert.AreEqual(DiagnosticActionKind.QuickFix, action.Kind);
        Assert.AreEqual("Replace 'b' with 'a'", action.Title);
        Assert.IsNotNull(action.TextEdit);
        Assert.AreEqual(new TextSpan(expectedStart, "Name".Length), action.TextEdit!.Span);
        Assert.AreEqual("a", action.TextEdit.NewText);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
            new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] = [],
                    ["#B"] = []
                })).Analyze(query);
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources(
        IEnumerable<BasicEntity> left,
        IEnumerable<BasicEntity> right)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = left,
            ["#B"] = right
        };
    }

    private static void AssertParserDiagnosticContract(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        int expectedStart,
        int expectedLength,
        string expectedExplanation,
        string expectedDocsReference,
        string[] expectedFixes)
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(new TextSpan(expectedStart, expectedLength), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.HasCount(0, diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedStart + expectedLength, envelope.EndOffset);
        Assert.AreEqual(expectedLength, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
