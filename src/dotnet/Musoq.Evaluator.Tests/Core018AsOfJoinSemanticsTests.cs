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
public sealed class Core018AsOfJoinSemanticsTests : BasicEntityTestBase
{
    [TestMethod]
    public void AsOfJoin_NullEqualityAndInequalityKeys_ShouldNotMatch()
    {
        const string query =
            "select a.Name, b.Name from #A.Entities() a asof left join #B.Entities() b " +
            "on a.Country = b.Country and a.NullableValue >= b.NullableValue";

        var table = Run(query, new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = "A-null-country", Country = null, NullableValue = 100 },
                new BasicEntity { Name = "A-null-probe", Country = "US", NullableValue = null },
                new BasicEntity { Name = "A-match", Country = "US", NullableValue = 100 }
            ],
            ["#B"] =
            [
                new BasicEntity { Name = "B-null-country", Country = null, NullableValue = 90 },
                new BasicEntity { Name = "B-null-key", Country = "US", NullableValue = null },
                new BasicEntity { Name = "B-match", Country = "US", NullableValue = 90 }
            ]
        });

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            new object?[] { "A-null-country", null },
            new object?[] { "A-null-probe", null },
            new object?[] { "A-match", "B-match" });
    }

    [TestMethod]
    public void AsOfJoin_TieBreak_ShouldOrderOnlyTheNearestKeyAndHonorNullPlacement()
    {
        const string ascendingQuery =
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population tie break by b.NullableValue asc";
        const string descendingQuery =
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population tie break by b.NullableValue desc";
        const string explicitLastQuery =
            "select a.Name, b.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population tie break by b.NullableValue asc nulls last";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity { Name = "A1", Population = 100 }],
            ["#B"] =
            [
                new BasicEntity { Name = "B-far", Population = 80, NullableValue = 100 },
                new BasicEntity { Name = "B-nearest-null", Population = 90, NullableValue = null },
                new BasicEntity { Name = "B-nearest-value", Population = 90, NullableValue = 5 }
            ]
        };

        AssertTieBreakResult(ascendingQuery, sources, "B-nearest-null");
        AssertTieBreakResult(descendingQuery, sources, "B-nearest-value");
        AssertTieBreakResult(explicitLastQuery, sources, "B-nearest-value");
    }

    [TestMethod]
    public void AsOfJoin_MissingInequality_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b on a.Name = b.Name";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3036_AsOfJoinMissingInequality,
            "ASOF JOIN requires at least one inequality condition (>=, >, <=, <).",
            "Name = b.Name",
            "An ASOF JOIN requires exactly one inequality condition to identify the nearest match.",
            [
                "Add one inequality predicate between the left and right sources.",
                "Keep equality predicates for partitioning and one inequality for the as-of ordering."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_MultipleInequalities_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population and a.Money > b.Money";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities,
            "ASOF JOIN supports exactly one inequality condition. Found 2.",
            "Population >= b.Population and a.Money > b.Money",
            "An ASOF JOIN contains more than one inequality condition.",
            [
                "Keep only the inequality that defines the as-of ordering.",
                "Move additional range checks to WHERE when they are row filters."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_OrCondition_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population or a.Name = b.Name";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3038_AsOfJoinOrNotSupported,
            "ASOF JOIN ON clause does not support OR.",
            "Population >= b.Population or a.Name = b.Name",
            "The ASOF JOIN ON clause does not support OR conditions.",
            [
                "Rewrite the join condition using AND predicates.",
                "Split OR alternatives into separate queries when needed."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_OneSidedInequality_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b on a.Population >= a.Money";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
            "ASOF JOIN inequality must reference columns from both sides.",
            "Population >= a.Money",
            "The ASOF JOIN inequality must compare values from the left and right sources.",
            [
                "Reference one column from each side of the join in the inequality.",
                "Use source aliases to make each side explicit."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_NonOrderableInequality_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b on a.Array >= b.Array";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
            "ASOF JOIN inequality column type 'Int32[]' is not orderable.",
            "Array >= b.Array",
            "The ASOF JOIN inequality uses a column type that cannot be ordered.",
            [
                "Use a numeric, date, or other comparable column for the as-of inequality.",
                "Convert the column to an orderable type before joining."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_TieBreakReferencingLeftSide_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population tie break by a.Name";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
            "ASOF JOIN TIE BREAK BY expression must reference only right-side columns.",
            "Name",
            "The ASOF JOIN inequality must compare values from the left and right sources.",
            [
                "Reference one column from each side of the join in the inequality.",
                "Use source aliases to make each side explicit."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_NonOrderableTieBreak_ShouldReportExactSemanticContract()
    {
        const string query =
            "select a.Name from #A.Entities() a asof join #B.Entities() b " +
            "on a.Population >= b.Population tie break by b.Array";

        AssertSemanticDiagnosticContract(
            query,
            DiagnosticCode.MQ3040_AsOfJoinInequalityColumnNotOrderable,
            "ASOF JOIN inequality column type 'Int32[]' is not orderable.",
            "Array",
            "The ASOF JOIN inequality uses a column type that cannot be ordered.",
            [
                "Use a numeric, date, or other comparable column for the as-of inequality.",
                "Convert the column to an orderable type before joining."
            ]);
    }

    [TestMethod]
    public void AsOfJoin_TieBreakOutsideAsOfJoin_ShouldReportExactParserContract()
    {
        const string query =
            "select 1 from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id tie break by b.Id";

        AssertParserDiagnosticContract(
            query,
            DiagnosticCode.MQ2039_TieBreakRequiresAsOfJoin,
            "TIE BREAK BY is only supported for ASOF JOIN.",
            "tie",
            "TIE BREAK BY is defined only for ASOF JOIN and ASOF LEFT JOIN.",
            "Core Spec - ASOF JOIN",
            ["Move TIE BREAK BY to an ASOF join or remove the clause."]);
    }

    [TestMethod]
    public void AsOfRightJoin_ShouldReportExactUnsupportedSyntaxContract()
    {
        const string query =
            "select 1 from #A.Entities() a asof right join #B.Entities() b on a.Id >= b.Id";

        AssertParserDiagnosticContract(
            query,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "Cannot compose statement, Identifier is not expected here. Did you mean 'ASC'?",
            "asof",
            "'asof' is not recognized here. The query likely contains a mistyped Musoq keyword.",
            "Core Spec - Statement Structure",
            [
                "Replace 'asof' with 'ASC'",
                "Check for missing keywords, commas, or parentheses near this location.",
                "Verify the query follows Musoq SQL syntax."
            ],
            expectKeywordQuickFix: true);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        var vm = CreateAndRunVirtualMachine(query, sources);
        return TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
    }

    private void AssertTieBreakResult(
        string query,
        IDictionary<string, IEnumerable<BasicEntity>> sources,
        string expectedRightName)
    {
        var table = Run(query, sources);
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["A1", expectedRightName]);
    }

    private static void AssertSemanticDiagnosticContract(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        string expectedExpression,
        string expectedExplanation,
        string[] expectedFixes)
    {
        var diagnostic = GetSingleDiagnostic(query, expectedCode, DiagnosticPhase.Bind);
        var expectedStart = query.LastIndexOf(expectedExpression, StringComparison.Ordinal);
        var expectedSpan = new TextSpan(expectedStart, expectedExpression.Length);

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual("Core Spec - ASOF JOIN", envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(
            expectedFixes,
            envelope.Actions.Select(static action => action.Title).ToArray());
        Assert.IsTrue(envelope.Actions.All(static action =>
            action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
    }

    private static void AssertParserDiagnosticContract(
        string query,
        DiagnosticCode expectedCode,
        string expectedMessage,
        string expectedExpression,
        string expectedExplanation,
        string expectedDocsReference,
        string[] expectedFixes,
        bool expectKeywordQuickFix = false)
    {
        var diagnostic = GetSingleDiagnostic(query, expectedCode, DiagnosticPhase.Parse);
        var expectedStart = query.LastIndexOf(expectedExpression, StringComparison.Ordinal);
        var expectedSpan = new TextSpan(expectedStart, expectedExpression.Length);

        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedMessage, diagnostic.Message);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsEmpty(diagnostic.Arguments);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual(expectedSpan.End, envelope.EndOffset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.AreEqual(expectedExplanation, envelope.Explanation);
        Assert.AreEqual(expectedDocsReference, envelope.DocsReference);
        CollectionAssert.AreEqual(expectedFixes, envelope.SuggestedFixes.ToArray());
        Assert.HasCount(expectedFixes.Length, envelope.Actions);
        CollectionAssert.AreEqual(
            expectedFixes,
            envelope.Actions.Select(static action => action.Title).ToArray());
        if (expectKeywordQuickFix)
        {
            var quickFix = envelope.Actions[0];
            Assert.AreEqual(DiagnosticActionKind.QuickFix, quickFix.Kind);
            Assert.IsNotNull(quickFix.TextEdit);
            Assert.AreEqual(expectedSpan, quickFix.TextEdit.Span);
            Assert.AreEqual("ASC", quickFix.TextEdit.NewText);
            Assert.IsTrue(envelope.Actions.Skip(1).All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        }
        else
        {
            Assert.IsTrue(envelope.Actions.All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null));
        }
    }

    private static Diagnostic GetSingleDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        DiagnosticPhase expectedPhase)
    {
        var result = Analyze(query);
        Assert.IsFalse(result.IsSuccess, FormatDiagnostics(result));
        var errors = result.Errors.ToArray();
        Assert.HasCount(1, errors, FormatDiagnostics(result));

        var diagnostic = errors[0];
        Assert.AreEqual(expectedCode, diagnostic.Code, FormatDiagnostics(result));
        Assert.AreEqual(expectedPhase, diagnostic.Phase, FormatDiagnostics(result));
        return diagnostic;
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = [],
                ["#B"] = []
            });

        return new QueryAnalyzer(
            provider,
            compilationOptions: new CompilationOptions(usePrimitiveTypeValidation: false))
            .Analyze(query);
    }

    private static string FormatDiagnostics(QueryAnalysisResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(static diagnostic =>
            $"{diagnostic.Code}: {diagnostic.Message}"));
    }
}
