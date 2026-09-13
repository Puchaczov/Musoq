using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Schema;
using MusoqParser = Musoq.Parser.Parser;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Recovery campaign REC-138: bounded local stress cases for parser depth,
///     malformed literal size, repeated diagnostic volume, and descriptor
///     suggestion rendering.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DiagnosticRec138BoundsTests
{
    private const int DiagnosticLimit = 100;
    private const int SuggestedCandidateLimit = 5;
    private const string FixtureId = "REC-138-local-bounds-fixture";

    [TestMethod]
    public void NestedMalformedQueries_ShouldReturnBoundedDiagnosticsWithoutStackOverflow()
    {
        foreach (var testCase in DepthCases)
        {
            var query = "select " + new string('(', testCase.Depth) + "1" +
                        new string(')', testCase.Depth - 1) + " from #system.dual()";
            var result = Parse(query);

            Assert.IsNotEmpty(result.Errors, $"{FixtureId}/{testCase.CaseId}");
            Assert.IsLessThanOrEqualTo(
                DiagnosticLimit,
                result.Diagnostics.Count,
                $"{FixtureId}/{testCase.CaseId} emitted unbounded diagnostics.");
            Assert.IsTrue(
                result.Diagnostics.All(static diagnostic => diagnostic.Code != DiagnosticCode.MQ9001_InternalCompilerError),
                $"{FixtureId}/{testCase.CaseId} leaked an internal compiler diagnostic: {result.FormatDiagnostics()}");
        }
    }

    [TestMethod]
    public void LongMalformedLiterals_ShouldKeepRenderedDiagnosticsBoundedAndHonorCancellation()
    {
        foreach (var testCase in LiteralCases)
        {
            var malformedLiteral = "0x" + new string('G', testCase.Length);
            var query = "select " + malformedLiteral + " from #system.dual()";
            var result = Parse(query);
            var diagnostic = result.Errors.FirstOrDefault(item => item.Code == DiagnosticCode.MQ1006_InvalidHexNumber);

            Assert.IsNotNull(diagnostic, $"{FixtureId}/{testCase.CaseId}: {result.FormatDiagnostics()}");
            Assert.IsLessThanOrEqualTo(
                DiagnosticLimit,
                result.Diagnostics.Count,
                $"{FixtureId}/{testCase.CaseId} emitted unbounded diagnostics.");
            Assert.IsLessThanOrEqualTo(
                DiagnosticSafety.MaxDisplayLength,
                DiagnosticSafety.SanitizeMessage(diagnostic!).Length,
                $"{FixtureId}/{testCase.CaseId} exceeded the display bound.");

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(
                () => new QueryAnalyzer(new UnexpectedSchemaProvider()).Analyze(query, cancellation.Token),
                $"{FixtureId}/{testCase.CaseId} did not preserve cancellation.");
        }
    }

    [TestMethod]
    public void RepeatedMalformedStatements_ShouldRespectDeclaredDiagnosticLimit()
    {
        foreach (var testCase in VolumeCases)
        {
            var query = string.Join(
                ";",
                Enumerable.Repeat("select from #system.dual()", testCase.StatementCount));
            var lexer = new Lexer(query, skipWhiteSpaces: true, recoverOnError: true);
            var diagnostics = new DiagnosticBag
            {
                SourceText = lexer.SourceText,
                MaxErrors = testCase.MaxErrors
            };
            var result = new MusoqParser(lexer, diagnostics).ParseWithDiagnostics();

            Assert.IsNotEmpty(result.Errors, $"{FixtureId}/{testCase.CaseId}");
            Assert.IsTrue(diagnostics.HasTooManyErrors, $"{FixtureId}/{testCase.CaseId}");
            Assert.IsLessThanOrEqualTo(testCase.MaxErrors, diagnostics.ErrorCount, testCase.CaseId);
            Assert.IsLessThanOrEqualTo(testCase.MaxErrors, result.ErrorCount, testCase.CaseId);
            Assert.IsTrue(
                result.Diagnostics.All(static diagnostic => diagnostic.Code != DiagnosticCode.MQ9001_InternalCompilerError),
                $"{FixtureId}/{testCase.CaseId} leaked an internal compiler diagnostic: {result.FormatDiagnostics()}");
        }
    }

    [TestMethod]
    public void NearMatchingDescriptorNames_ShouldRankDeterministicallyAndBoundRenderedCandidates()
    {
        foreach (var testCase in CandidateCases)
        {
            var input = "Column00000";
            var candidates = Enumerable.Range(1, testCase.CandidateCount)
                .Select(index => $"Column{index:D5}")
                .ToArray();
            var expected = candidates.Take(SuggestedCandidateLimit).ToArray();

            var first = ErrorCatalog.GetDidYouMeanCandidates(
                input,
                candidates,
                maxDistance: 1,
                maxCandidates: SuggestedCandidateLimit);
            var second = ErrorCatalog.GetDidYouMeanCandidates(
                input,
                candidates.Reverse(),
                maxDistance: 1,
                maxCandidates: SuggestedCandidateLimit);

            CollectionAssert.AreEqual(expected, first.ToArray(), $"{FixtureId}/{testCase.CaseId}");
            CollectionAssert.AreEqual(first.ToArray(), second.ToArray(), $"{FixtureId}/{testCase.CaseId}");
            Assert.IsLessThanOrEqualTo(SuggestedCandidateLimit, first.Count, testCase.CaseId);

            var sourceText = new SourceText($"select {input}", $"{FixtureId}/{testCase.CaseId}.sql");
            var context = new DiagnosticContext(sourceText, maxErrors: 1);
            context.ReportUnknownColumn(input, candidates, new TextSpan(7, input.Length));
            var diagnostic = context.Diagnostics.Single();

            Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, diagnostic.Code, testCase.CaseId);
            Assert.IsLessThanOrEqualTo(
                DiagnosticSafety.MaxDisplayLength,
                DiagnosticSafety.SanitizeMessage(diagnostic).Length,
                $"{FixtureId}/{testCase.CaseId} rendered an unbounded message.");
            Assert.IsTrue(
                diagnostic.Arguments["availableColumns"].Split(", ", StringSplitOptions.RemoveEmptyEntries).Length <=
                SuggestedCandidateLimit,
                $"{FixtureId}/{testCase.CaseId} rendered too many available candidates.");
            Assert.IsTrue(
                diagnostic.Arguments["candidateColumns"].Split(", ", StringSplitOptions.RemoveEmptyEntries).Length <=
                SuggestedCandidateLimit,
                $"{FixtureId}/{testCase.CaseId} rendered too many close candidates.");
            Assert.IsTrue(
                diagnostic.SuggestedFixes.All(static action => action.TextEdit is null),
                $"{FixtureId}/{testCase.CaseId} created an arbitrary text edit for a tie.");
        }
    }

    private static ParseResult Parse(string query)
    {
        var lexer = new Lexer(query, skipWhiteSpaces: true, recoverOnError: true);
        var diagnostics = new DiagnosticBag { SourceText = lexer.SourceText };
        return new MusoqParser(lexer, diagnostics).ParseWithDiagnostics();
    }

    private static readonly IReadOnlyList<DepthCase> DepthCases =
    [
        new("D01", 2),
        new("D02", 4),
        new("D03", 8),
        new("D04", 16),
        new("D05", 24),
        new("D06", 32),
        new("D07", 48),
        new("D08", 64),
        new("D09", 96),
        new("D10", 128),
        new("D11", 160),
        new("D12", 192)
    ];

    private static readonly IReadOnlyList<LiteralCase> LiteralCases =
    [
        new("L01", 8),
        new("L02", 16),
        new("L03", 32),
        new("L04", 64),
        new("L05", 128),
        new("L06", 256),
        new("L07", 512),
        new("L08", 1024),
        new("L09", 2048),
        new("L10", 4096),
        new("L11", 8192),
        new("L12", 16384)
    ];

    private static readonly IReadOnlyList<VolumeCase> VolumeCases =
    [
        new("V01", 16, 3),
        new("V02", 24, 4),
        new("V03", 32, 5),
        new("V04", 48, 6),
        new("V05", 64, 7),
        new("V06", 96, 8),
        new("V07", 128, 9),
        new("V08", 192, 10),
        new("V09", 256, 11),
        new("V10", 384, 12),
        new("V11", 512, 13),
        new("V12", 768, 14)
    ];

    private static readonly IReadOnlyList<CandidateCase> CandidateCases =
    [
        new("C01", 16),
        new("C02", 32),
        new("C03", 64),
        new("C04", 128),
        new("C05", 256),
        new("C06", 512),
        new("C07", 768),
        new("C08", 1024),
        new("C09", 1536),
        new("C10", 2048),
        new("C11", 3072),
        new("C12", 4096)
    ];

    private sealed class UnexpectedSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new InvalidOperationException("REC-138 provider should not be reached after cancellation.");
        }
    }

    private sealed record DepthCase(string CaseId, int Depth);

    private sealed record LiteralCase(string CaseId, int Length);

    private sealed record VolumeCase(string CaseId, int StatementCount, int MaxErrors);

    private sealed record CandidateCase(string CaseId, int CandidateCount);
}
