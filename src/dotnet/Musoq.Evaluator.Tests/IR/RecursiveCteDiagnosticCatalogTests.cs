using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RecursiveCteDiagnosticCatalogTests
{
    public static IEnumerable<object[]> Cases => RecursiveCteUnsupportedCaseCatalog.Cases
        .Select(static item => new object[] { item });

    [TestMethod]
    public void Catalog_ShouldContainFiftyOneUniqueUnsupportedCases()
    {
        Assert.HasCount(51, RecursiveCteUnsupportedCaseCatalog.Cases);
        Assert.HasCount(
            51,
            RecursiveCteUnsupportedCaseCatalog.Cases
                .Select(static item => item.Name)
                .Distinct(StringComparer.Ordinal));
    }

    [TestMethod]
    public void Catalog_ShouldGuardEveryV1RestrictionFamily()
    {
        var names = RecursiveCteUnsupportedCaseCatalog.Cases
            .Select(static item => item.Name)
            .ToHashSet(StringComparer.Ordinal);
        string[] requiredCases =
        [
            "MissingRecursiveKeyword", "NoTopLevelUnion", "SelfReferenceInAnchor",
            "MultipleSelfReferences", "NestedSelfReference", "ForwardReference", "MutualRecursion",
            "UnionAllWithKeys", "DistinctRecursiveMember", "AggregateRecursiveMember",
            "GroupedRecursiveMember", "HavingRecursiveMember", "WindowRecursiveMember",
            "QualifyRecursiveMember", "OrderedRecursiveMember", "SkipRecursiveMember",
            "TakeRecursiveMember", "OuterJoinRecursiveMember", "RightOuterJoinRecursiveMember",
            "FullOuterJoinRecursiveMember", "SemiJoinRecursiveMember", "AntiJoinRecursiveMember",
            "AsOfJoinRecursiveMember", "PivotRecursiveMember", "UnpivotRecursiveMember",
            "NestedSetOperation", "RecursiveOutputColumnCountMismatch",
            "RecursiveOutputTypeMismatch", "RecursiveColumnListCountMismatch",
            "RecursiveDuplicateColumnName", "UnknownRecursiveUnionKey",
            "SearchClauseParserRecovery", "CycleClauseParserRecovery"
        ];

        foreach (var requiredCase in requiredCases)
            Assert.IsTrue(names.Contains(requiredCase), requiredCase);
    }

    [TestMethod]
    public void ParserRecoveryCases_ShouldReturnStableCompleteDiagnosticSetsWithoutThrowing()
    {
        var schemaProvider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [] } });

        foreach (var testCase in RecursiveCteUnsupportedCaseCatalog.Cases.Where(static item => item.ParserRecovery))
        {
            var actual = new QueryAnalyzer(schemaProvider).Analyze(testCase.Query).Diagnostics.ToArray();
            var expected = testCase.ExpectedDiagnostics!;

            Assert.HasCount(expected.Count, actual, testCase.Name);
            for (var index = 0; index < expected.Count; index++)
            {
                var expectedDiagnostic = expected[index];
                var actualDiagnostic = actual[index];
                Assert.AreEqual(expectedDiagnostic.Code, actualDiagnostic.Code, testCase.Name);
                StringAssert.Contains(actualDiagnostic.Message, expectedDiagnostic.MessageFragment, testCase.Name);
                Assert.IsGreaterThan(0, actualDiagnostic.Span.Length, testCase.Name);
                var span = testCase.Query.Substring(actualDiagnostic.Span.Start, actualDiagnostic.Span.Length);
                Assert.IsTrue(
                    span.Contains(expectedDiagnostic.SpanFragment, StringComparison.OrdinalIgnoreCase),
                    $"{testCase.Name}: diagnostic {index} span '{span}' does not contain " +
                    $"'{expectedDiagnostic.SpanFragment}'.");
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    [FeatureEvidence("recursive-ctes", FeatureEvidenceKind.RuntimeNegativeDiagnostic)]
    public void UnsupportedCase_ShouldStopBeforePlanningWithDeclaredDiagnostic(
        RecursiveCteUnsupportedCase testCase)
    {
        if (testCase.ParserRecovery)
        {
            var recovery = new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [] } }))
                .Analyze(testCase.Query);
            Assert.IsNotEmpty(recovery.Diagnostics, testCase.Name);
            Assert.AreEqual(testCase.DiagnosticCode, recovery.Diagnostics[0].Code, testCase.Name);
            StringAssert.Contains(recovery.Diagnostics[0].Message, testCase.MessageFragment, testCase.Name);
            AssertStructuredQueryDiagnostic(recovery.Diagnostics[0], testCase);
            return;
        }

        var buildItems = PlanOnlyBuildItems.Create(testCase.Query);
        var errors = buildItems.DiagnosticContext.Errors.ToArray();

        Assert.IsNotEmpty(errors, testCase.Name);
        Assert.AreEqual(testCase.DiagnosticCode, errors[0].Code, testCase.Name);
        StringAssert.Contains(errors[0].Message, testCase.MessageFragment, testCase.Name);
        Assert.IsNull(buildItems.LogicalPlan, testCase.Name);
        Assert.IsNull(buildItems.PhysicalPlan, testCase.Name);

        var span = errors[0].Span;
        Assert.IsGreaterThan(0, span.Length, testCase.Name);
        var offendingText = testCase.Query.Substring(span.Start, span.Length);
        Assert.IsTrue(
            offendingText.Contains(testCase.SpanFragment, StringComparison.OrdinalIgnoreCase),
            $"{testCase.Name}: diagnostic span '{offendingText}' does not contain '{testCase.SpanFragment}'.");
        AssertStructuredQueryDiagnostic(errors[0], testCase);
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void UnsupportedCase_QueryAnalyzerShouldReturnDeclaredPrimaryDiagnostic(
        RecursiveCteUnsupportedCase testCase)
    {
        var schemaProvider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>> { { "#A", [] } });
        var result = new QueryAnalyzer(schemaProvider).Analyze(testCase.Query);

        Assert.IsNotEmpty(result.Diagnostics, testCase.Name);
        Assert.AreEqual(testCase.DiagnosticCode, result.Diagnostics[0].Code, testCase.Name);
        StringAssert.Contains(result.Diagnostics[0].Message, testCase.MessageFragment, testCase.Name);
    }

    [TestMethod]
    public void MultilineUnsupportedRecursiveMember_ShouldRetainQueryLineAndContextSnippet()
    {
        const string query =
            "with recursive counter (Value) as (\n" +
            "    select Value from values {{ Value: 1 }} seed\n" +
            "    union all\n" +
            "    select distinct c.Value + 1 from counter c\n" +
            "    where c.Value < 3)\n" +
            "select Value from counter";

        var errors = PlanOnlyBuildItems.Create(query).DiagnosticContext.Errors.ToArray();

        Assert.HasCount(1, errors);
        var diagnostic = errors[0];
        var operatorOffset = query.IndexOf("select distinct", StringComparison.Ordinal);
        var expectedLocation = new SourceText(query).GetLocation(operatorOffset);

        Assert.AreEqual(DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, diagnostic.Code);
        Assert.AreEqual(expectedLocation.Line, diagnostic.Location.Line);
        Assert.IsGreaterThan(0, diagnostic.Location.Offset);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        StringAssert.Contains(diagnostic.ContextSnippet!, "select distinct");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedLocation.Line, envelope.Line);
        Assert.AreEqual(diagnostic.Location.Offset, envelope.Offset);
        StringAssert.Contains(envelope.Snippet!, "select distinct");
    }

    private static void AssertStructuredQueryDiagnostic(
        Diagnostic diagnostic,
        RecursiveCteUnsupportedCase testCase)
    {
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity, testCase.Name);
        Assert.AreEqual(
            DiagnosticPhaseMapping.FromCode(testCase.DiagnosticCode),
            diagnostic.Phase,
            testCase.Name);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind, testCase.Name);
        Assert.IsTrue(diagnostic.Location.IsValid, testCase.Name);
        Assert.IsTrue(diagnostic.EndLocation.IsValid, testCase.Name);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet), testCase.Name);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, testCase.Query);
        Assert.AreEqual(testCase.DiagnosticCode, envelope.Code, testCase.Name);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity, testCase.Name);
        Assert.AreEqual(diagnostic.Phase, envelope.Phase, testCase.Name);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind, testCase.Name);
        Assert.IsNotNull(envelope.Explanation, testCase.Name);
        Assert.IsNotEmpty(envelope.SuggestedFixes, testCase.Name);
        Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions, testCase.Name);
        Assert.IsTrue(
            envelope.Actions.All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null),
            testCase.Name);
    }
}
