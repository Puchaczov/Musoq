using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class Core016JoinSemanticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void InnerJoin_ShorthandWithArithmeticPredicate_ShouldReturnOnlyMatches()
    {
        const string query = """
            select a.Name, b.Name
            from #A.Entities() a
            join #B.Entities() b on a.Id = b.Id + 1
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 2, Name = "A2" },
                new BasicEntity { Id = 9, Name = "A9" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1, Name = "B1" },
                new BasicEntity { Id = 2, Name = "B2" }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A2", "B1"]);
    }

    [TestMethod]
    public void MultipleInnerJoins_ShouldChainFromThePreviousJoinedSource()
    {
        const string query = """
            select a.Name, b.Name, c.Name
            from #A.Entities() a
            inner join #B.Entities() b on a.Id = b.Id
            inner join #C.Entities() c on b.Id = c.Id
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "A1" },
                new BasicEntity { Id = 2, Name = "A2" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1, Name = "B1" },
                new BasicEntity { Id = 3, Name = "B3" }
            ],
            ["#C"] =
            [
                new BasicEntity { Id = 1, Name = "C1" },
                new BasicEntity { Id = 4, Name = "C4" }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)),
            ("c.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", "B1", "C1"]);
    }

    [TestMethod]
    public void LeftJoin_ShouldNullExtendOnlyTheUnmatchedRightRows()
    {
        const string query = """
            select a.Id, b.Id, b.NullableValue
            from #A.Entities() a
            left join #B.Entities() b on a.Id = b.Id
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1, NullableValue = null }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("b.Id", typeof(int?)),
            ("b.NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 1, 1, null },
            new object?[] { 2, null, null });
    }

    [TestMethod]
    public void RightJoin_ShouldNullExtendOnlyTheUnmatchedLeftRows()
    {
        const string query = """
            select a.Id, b.Id, a.NullableValue
            from #A.Entities() a
            right outer join #B.Entities() b on a.Id = b.Id
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, NullableValue = null }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int?)),
            ("b.Id", typeof(int)),
            ("a.NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 1, 1, null },
            new object?[] { null, 2, null });
    }

    [TestMethod]
    [DataRow("full outer join")]
    [DataRow("full join")]
    public void FullJoin_Synonyms_ShouldReturnMatchedAndBothUnmatchedRows(string joinOperator)
    {
        var query = $"""
            select a.Id, b.Id
            from #A.Entities() a
            {joinOperator} #B.Entities() b on a.Id = b.Id
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 2 },
                new BasicEntity { Id = 3 }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int?)),
            ("b.Id", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 1, null },
            new object?[] { 2, 2 },
            new object?[] { null, 3 });
    }

    [TestMethod]
    public void CrossJoin_ShouldBuildCartesianProductBeforeWhereFiltering()
    {
        const string query = """
            select a.Id, b.Id
            from #A.Entities() a
            cross join #B.Entities() b
            where a.Id = 1 or b.Id = 20
            """;

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1 },
                new BasicEntity { Id = 2 }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 10 },
                new BasicEntity { Id = 20 }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("b.Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { 1, 10 },
            new object?[] { 1, 20 },
            new object?[] { 2, 20 });
    }

    [TestMethod]
    public void RequiredJoinAliases_ShouldReportExactFirstAndRightSourceContracts()
    {
        const string firstSourceQuery =
            "select 1 from #A.Entities() inner join #B.Entities() b on 1 = 1";
        const string rightSourceQuery =
            "select 1 from #A.Entities() a inner join #B.Entities() where 1 = 1";

        AssertRequiredAliasDiagnostic(
            firstSourceQuery,
            "The first source in a multi-source query requires an alias before INNER JOIN.",
            firstSourceQuery.IndexOf("#A.Entities()", StringComparison.Ordinal) + "#A.Entities()".Length,
            "INNER JOIN",
            "INNER JOIN",
            isFirstSource: true);
        AssertRequiredAliasDiagnostic(
            rightSourceQuery,
            "The INNER JOIN source requires an alias before WHERE.",
            rightSourceQuery.IndexOf("#B.Entities()", StringComparison.Ordinal) + "#B.Entities()".Length,
            "INNER JOIN",
            "WHERE",
            isFirstSource: false);
    }

    [TestMethod]
    public void CrossJoinWithOn_ShouldReportExactUnexpectedTokenContract()
    {
        const string query =
            "select 1 from #A.Entities() a cross join #B.Entities() b on a.Id = b.Id";

        AssertParserDiagnosticContract(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Cannot compose statement, On is not expected here",
            query.IndexOf("on", StringComparison.Ordinal),
            "on".Length,
            "The parser encountered a token that does not fit the expected SQL grammar at this position.",
            "Core Spec - Statement Structure",
            [
                "Check for missing keywords, commas, or parentheses near this location.",
                "Verify the query follows Musoq SQL syntax."
            ]);
    }

    [TestMethod]
    public void ConditionalJoinWithoutOn_ShouldReportExactJoinConditionContract()
    {
        const string query =
            "select 1 from #A.Entities() a inner join #B.Entities() b where 1 = 1";

        AssertParserDiagnosticContract(
            query,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            "The INNER JOIN requires an ON condition.",
            query.IndexOf("where", StringComparison.Ordinal),
            "where".Length,
            "The JOIN condition is missing or is not a valid boolean expression.",
            "Core Spec - JOIN Clause",
            [
                "Add an ON clause with a comparison between the joined sources.",
                "Make sure the ON expression evaluates to a boolean value."
            ]);
    }

    [TestMethod]
    public void NullableOptionalColumnIsNull_ShouldReportExactAmbiguityWarningContract()
    {
        const string query =
            "select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name is null";
        var result = Analyze(query);

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck);
        var expectedStart = query.IndexOf("Name", query.IndexOf("where", StringComparison.Ordinal), StringComparison.Ordinal);

        AssertWarningContract(
            warning,
            query,
            expectedStart,
            "Name".Length,
            "IS NULL on optional alias 'b.Name' cannot distinguish a missing outer-join row from a present NULL value",
            "A nullable column from an optional outer-join side can be NULL both when its row is missing and when a present row contains NULL.",
            [
                "Use the table alias IS PRESENT or IS MISSING predicate to test row existence.",
                "Check a source column that is non-nullable when a present row is required."
            ]);
    }

    [TestMethod]
    public void OptionalSideWhereFilter_ShouldReportExactNullRejectingWarningContract()
    {
        const string query =
            "select a.Name, b.Name from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Name = 'match'";
        var result = Analyze(query);

        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        var expectedStart = query.IndexOf("Name", query.IndexOf("where", StringComparison.Ordinal), StringComparison.Ordinal);

        AssertWarningContract(
            warning,
            query,
            expectedStart,
            "Name = 'match'".Length,
            "WHERE predicate rejects NULL-extended rows from optional alias 'b' and effectively turns the outer join into an inner join",
            "A WHERE predicate is false or UNKNOWN for the NULL-extended row of an outer join, so unmatched rows cannot survive.",
            [
                "Move the restriction into the JOIN ON clause when it is part of matching.",
                "Use an explicit row-presence predicate when removing unmatched rows is intentional."
            ]);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = [],
                ["#C"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static void AssertRequiredAliasDiagnostic(
        string query,
        string expectedMessage,
        int expectedSourceEnd,
        string expectedOperator,
        string expectedBoundary,
        bool isFirstSource)
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(new TextSpan(expectedSourceEnd, 0), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.HasCount(5, diagnostic.Arguments);
        Assert.AreEqual("required-source-alias", diagnostic.Arguments["aliasKind"]);
        Assert.AreEqual("schema", diagnostic.Arguments["sourceKind"]);
        Assert.AreEqual(expectedOperator, diagnostic.Arguments["operator"]);
        Assert.AreEqual(expectedBoundary, diagnostic.Arguments["boundary"]);
        Assert.AreEqual(isFirstSource ? "true" : "false", diagnostic.Arguments["isFirstSource"]);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedSourceEnd, envelope.Offset);
        Assert.AreEqual(expectedSourceEnd, envelope.EndOffset);
        Assert.AreEqual(0, envelope.Length);
        Assert.AreEqual(
            "A source in a multi-source query needs a stable alias so JOIN and APPLY expressions can address it reliably. Derived tables and VALUES sources always require an alias.",
            envelope.Explanation);
        Assert.AreEqual("Core Spec - Aliasing", envelope.DocsReference);
        CollectionAssert.AreEqual(
            new[]
            {
                "Add an alias immediately after the source, for example: FROM #schema.items() items.",
                "Use AS for clarity, or bracket an alias that is also a SQL keyword."
            },
            envelope.SuggestedFixes.ToArray());
        Assert.HasCount(2, envelope.Actions);
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
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

    private static void AssertWarningContract(
        Diagnostic warning,
        string query,
        int expectedStart,
        int expectedLength,
        string expectedMessage,
        string expectedExplanation,
        string[] expectedFixes)
    {
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind);
        Assert.AreEqual(expectedMessage, warning.Message);
        Assert.AreEqual(new TextSpan(expectedStart, expectedLength), warning.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(warning.ContextSnippet));
        Assert.HasCount(0, warning.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(warning, query);
        Assert.AreEqual(warning.Code, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedStart + expectedLength, envelope.EndOffset);
        Assert.AreEqual(expectedLength, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual("Core Spec - Outer Joins", envelope.DocsReference);
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
