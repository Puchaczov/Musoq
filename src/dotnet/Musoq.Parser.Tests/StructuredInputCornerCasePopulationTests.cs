using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

/// <summary>
/// Deterministic parser/recovery population for the structured-input grammar.
/// The seed, population size, authored diagnostic expectations, and normalized
/// query hashes make a failure reproducible and reducible to one case.
/// </summary>
[TestClass]
public sealed class StructuredInputCornerCasePopulationTests
{
    private const int CaseCount = 2_000;
    private const int Seed = 0x5A17_20;
    private const int MinimumCoverageCells = 20;

    private static readonly string[] RequiredCoverageCells =
    [
        "valid|values|from-first",
        "valid|values|select-first",
        "valid|values|bracketed-unicode",
        "valid|values|grouped-expression",
        "valid|array|primitive",
        "valid|array|nested",
        "valid|array|trailing-comma",
        "valid|record|nested",
        "valid|let|inferred",
        "valid|let|annotated",
        "valid|param|param",
        "valid|param|params",
        "valid|desc|arguments",
        "valid|identifier|values",
        "invalid|legacy-values-row",
        "invalid|mixed-values-row",
        "invalid|array-doubled-comma",
        "invalid|array-missing-keyword",
        "invalid|record-missing-colon",
        "invalid|parameter-missing-colon",
        "invalid|desc-keyword-typo",
        "invalid|unterminated-string"
    ];

    [TestMethod]
    public void GeneratedPopulation_ShouldKeepAuthoredSyntaxExpectationsAndUniqueCoverage()
    {
        var random = new Random(Seed);
        var valid = 0;
        var invalid = 0;
        var normalizedHashes = new HashSet<string>(StringComparer.Ordinal);
        var coverageCells = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < CaseCount; index++)
        {
            var caseData = index % 5 == 0
                ? CreateMalformedCase(index, random)
                : CreateValidCase(index, random);

            Assert.IsTrue(
                normalizedHashes.Add(HashNormalizedQuery(caseData.Query)),
                $"Population case {caseData.CaseId} duplicated a normalized query.");
            coverageCells.Add(caseData.CoverageCell);

            if (caseData.ExpectedDiagnostic is { } expectation)
            {
                AssertMalformedCase(caseData, expectation);
                invalid++;
                continue;
            }

            try
            {
                var root = new Parser(new Lexer(caseData.Query, true)).ComposeAll();
                Assert.IsNotNull(root, $"Valid case {caseData.CaseId} returned no AST: {caseData.Query}");
                valid++;
            }
            catch (Exception exception)
            {
                var shrunk = ShrinkFailingQuery(caseData.Query, static candidate =>
                    !ParseWithDiagnostics(candidate).Success);
                WriteShrinkCandidate(caseData.CaseId, shrunk);
                Assert.Fail($"Valid case {caseData.CaseId} threw while parsing. Shrunk query: {shrunk}. {exception}");
            }
        }

        Assert.AreEqual(1_600, valid);
        Assert.AreEqual(400, invalid);
        Assert.AreEqual(CaseCount, normalizedHashes.Count, "Every generated case must have a distinct normalized query hash.");
        Assert.IsGreaterThanOrEqualTo(MinimumCoverageCells, coverageCells.Count, "The population must exercise independent grammar coverage cells.");
        foreach (var requiredCell in RequiredCoverageCells)
            Assert.IsTrue(coverageCells.Contains(requiredCell), $"Required coverage cell '{requiredCell}' was not exercised.");
    }

    [TestMethod]
    public void MetamorphicSyntaxPopulation_ShouldParseCommentsGroupingAndContextualNames()
    {
        var random = new Random(Seed ^ 0x11);
        var hashes = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < 128; index++)
        {
            var suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var fieldCase = index % 2 == 0 ? "Id" : "id";
            var query = $"from values /* {index} */ {{ ({fieldCase}: 'todo-{suffix}', Value: (({index} + 1) * 2)), (Value: ({index} + 2), {fieldCase}: 'fixme-{suffix}') }} valuesSource select valuesSource.{fieldCase}";
            var root = new Parser(new Lexer(query, true)).ComposeAll();
            Assert.IsNotNull(root, query);
            Assert.IsTrue(hashes.Add(HashNormalizedQuery(query)), $"Metamorphic VALUES case {index} was not unique.");

            var arrayQuery = $"select n.Value from #inputs.numbers(values: array /* {random.Next()} */ {{ ({index} + 1), {index + 2}, }}) n";
            var arrayRoot = new Parser(new Lexer(arrayQuery, true)).ComposeAll();
            Assert.IsNotNull(arrayRoot, arrayQuery);
            Assert.IsTrue(hashes.Add(HashNormalizedQuery(arrayQuery)), $"Metamorphic array case {index} was not unique.");
        }

        Assert.AreEqual(256, hashes.Count);
    }

    private static GeneratedCase CreateValidCase(int index, Random random)
    {
        var id = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var comment = index % 7 == 0 ? "/* boundary */" : string.Empty;
        var idField = (index % 3) switch
        {
            0 => "Id",
            1 => "id",
            _ => "ID"
        };

        return (index % 14) switch
        {
            0 => new GeneratedCase(
                $"valid-{index}",
                $"from values {comment} {{ ({idField}: 'todo-{id}', Value: (({index} + 1) * 2)), (Value: {index + 2}, {idField}: 'fixme-{id}',) }} rows select rows.{idField}",
                "valid|values|from-first"),
            1 => new GeneratedCase(
                $"valid-{index}",
                $"select rows.[Label] from values {comment} {{ ([Label]: 'quoted ({id})', [Value]: ({index} + 1) * 3), ([Value]: {index}, [Label]: 'quoted {{ {id} }}') }} rows",
                "valid|values|select-first"),
            2 => new GeneratedCase(
                $"valid-{index}",
                $"select rows.[ΔField{id}] from values {{ ([ΔField{id}]: 'unicode-{id}') }} rows",
                "valid|values|bracketed-unicode"),
            3 => new GeneratedCase(
                $"valid-{index}",
                $"select rows.Value from values {{ (Value: (({index} + 1) * 2)) }} rows",
                "valid|values|grouped-expression"),
            4 => new GeneratedCase(
                $"valid-{index}",
                $"select n.Value from #inputs.numbers(values: array {comment} {{ {index}, {index + 1}, {index + 1}, }}) n",
                "valid|array|primitive"),
            5 => new GeneratedCase(
                $"valid-{index}",
                $"select m.Value from #inputs.matrix(values: array {{ array {{ {index}, {index + 1} }}, array {{ {index + 2} }}, array {{ }}, }}) m",
                "valid|array|nested"),
            6 => new GeneratedCase(
                $"valid-{index}",
                $"select n.Value from #inputs.numbers(values: array {{ {index}, {index + 1}, }}) n",
                "valid|array|trailing-comma"),
            7 => new GeneratedCase(
                $"valid-{index}",
                $"select c.Enabled from #inputs.configure(options: (Enabled: true, Codes: array {{ {index}, {index + 1} }}, Window: (Before: {index % 4}, After: {index % 5}))) c",
                "valid|record|nested"),
            8 => new GeneratedCase(
                $"valid-{index}",
                $"let todo = (Id: 'todo-{id}', Pattern: 'TODO'); let items = array {{ $todo, (Pattern: 'FIXME', Id: 'fixme-{random.Next(1, 100_000)}') }}; select 1 from #system.dual()",
                "valid|let|inferred"),
            9 => new GeneratedCase(
                $"valid-{index}",
                $"let items: (Id: string, Pattern: string, Mode: string = 'literal')[] = array {{ (Id: 'todo-{id}', Pattern: 'TODO') }}; select 1 from #system.dual()",
                "valid|let|annotated"),
            10 => new GeneratedCase(
                $"valid-{index}",
                $"param(values: int[] = array {{ {index}, {index + 1} }}) select 1 from #system.dual()",
                "valid|param|param"),
            11 => new GeneratedCase(
                $"valid-{index}",
                $"params(values: int?[] = array {{ {index}, null, }}) select 1 from #system.dual()",
                "valid|param|params"),
            12 => new GeneratedCase(
                $"valid-{index}",
                $"desc arguments #inputs.match('TODO-{id}', patterns: array {{ (Id: 'todo', Pattern: 'TODO') }})",
                "valid|desc|arguments"),
            _ => new GeneratedCase(
                $"valid-{index}",
                $"select * from values valuesSource{index}",
                "valid|identifier|values")
        };
    }

    private static GeneratedCase CreateMalformedCase(int index, Random random)
    {
        var value = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var malformed = (index % 8) switch
        {
            0 => new
            {
                Query = $"select * from values {{ {{ Id: 'legacy-{value}' }} }} v",
                Code = DiagnosticCode.MQ2001_UnexpectedToken,
                Message = "Expected token is LeftParenthesis",
                Needle = "{ Id",
                Cell = "legacy-values-row"
            },
            1 => new
            {
                Query = $"select * from values {{ (Id: 'a-{value}'), {{ Id: 'b-{value}' }} }} v",
                Code = DiagnosticCode.MQ2001_UnexpectedToken,
                Message = "Expected token is LeftParenthesis",
                Needle = "{ Id",
                Cell = "mixed-values-row"
            },
            2 => new
            {
                Query = $"select * from #inputs.numbers(values: array {{ {value},, {value} + 1 }}) n",
                Code = DiagnosticCode.MQ2001_UnexpectedToken,
                Message = "cannot be used here",
                Needle = ",,",
                Cell = "array-doubled-comma"
            },
            3 => new
            {
                Query = $"select * from #inputs.numbers(values: {{ {value}, {value} + 1 }}) n",
                Code = DiagnosticCode.MQ2001_UnexpectedToken,
                Message = "cannot be used here",
                Needle = "{ ",
                Cell = "array-missing-keyword"
            },
            4 => new
            {
                Query = $"select * from #inputs.configure(options: (Enabled true, Codes: array {{ 1 }} /* {value} */)) c",
                Code = DiagnosticCode.MQ2010_MissingClosingParenthesis,
                Message = "missing its closing parenthesis",
                Needle = "options",
                Cell = "record-missing-colon"
            },
            5 => new
            {
                Query = $"params(items int) /* {value} */ select 1 from #system.dual()",
                Code = DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
                Message = "Invalid script parameter declaration",
                Needle = "items",
                Cell = "parameter-missing-colon"
            },
            6 => new
            {
                Query = $"desc argumentz #inputs.match('{random.Next()}')",
                Code = DiagnosticCode.MQ2001_UnexpectedToken,
                Message = "Cannot compose statement",
                Needle = "argumentz",
                Cell = "desc-keyword-typo"
            },
            _ => new
            {
                Query = $"select * from values {{ (Id: 'unterminated-{value}) }} v",
                Code = DiagnosticCode.MQ1002_UnterminatedString,
                Message = "Unterminated string literal",
                Needle = "'unterminated",
                Cell = "unterminated-string"
            }
        };

        return new GeneratedCase(
            $"invalid-{index}",
            malformed.Query,
            $"invalid|{malformed.Cell}",
            new DiagnosticExpectation(
                malformed.Code,
                DiagnosticPhase.Parse,
                DiagnosticSourceKind.Query,
                malformed.Message,
                malformed.Query.IndexOf(malformed.Needle, StringComparison.Ordinal),
                MinimumSuggestedFixes: 1));
    }

    private static void AssertMalformedCase(GeneratedCase caseData, DiagnosticExpectation expectation)
    {
        try
        {
            var result = ParseWithDiagnostics(caseData.Query);
            Assert.IsFalse(result.Success, $"Malformed case {caseData.CaseId} parsed: {caseData.Query}");
            Assert.IsNotEmpty(result.Diagnostics, $"Malformed case {caseData.CaseId} produced no diagnostic.");
            Assert.IsTrue(result.Diagnostics.Count <= 6, $"Recovery cascaded for case {caseData.CaseId}.");

            var diagnostic = result.Diagnostics.First(static item => item.IsError);
            Assert.AreEqual(expectation.Code, diagnostic.Code, caseData.CaseId);
            Assert.AreEqual(expectation.Phase, diagnostic.Phase, caseData.CaseId);
            Assert.AreEqual(expectation.SourceKind, diagnostic.SourceKind, caseData.CaseId);
            StringAssert.Contains(diagnostic.Message, expectation.MessageFragment, caseData.CaseId);
            Assert.IsTrue(
                diagnostic.Span.Start >= expectation.MinimumSpanStart &&
                diagnostic.Span.End <= caseData.Query.Length,
                $"{caseData.CaseId} produced out-of-range span {diagnostic.Span.Start}:{diagnostic.Span.Length}.");
            Assert.IsTrue(diagnostic.Arguments is not null, $"{caseData.CaseId} lost diagnostic facts.");
            Assert.IsGreaterThanOrEqualTo(expectation.MinimumSuggestedFixes, diagnostic.SuggestedFixes.Count, caseData.CaseId);
            foreach (var action in diagnostic.SuggestedFixes)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(action.Title), $"{caseData.CaseId} has an untitled diagnostic action.");
                if (action.TextEdit is { } edit)
                {
                    Assert.IsTrue(edit.Span.Start >= 0 && edit.Span.End <= caseData.Query.Length, $"{caseData.CaseId} has an invalid edit span.");
                    Assert.IsNotNull(edit.NewText, $"{caseData.CaseId} has a null edit payload.");
                }
            }
        }
        catch (AssertFailedException exception)
        {
            var shrunk = ShrinkFailingQuery(caseData.Query, static candidate =>
                !ParseWithDiagnostics(candidate).Success);
            WriteShrinkCandidate(caseData.CaseId, shrunk);
            Assert.Fail($"{exception.Message} Shrunk query: {shrunk}");
        }
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static string HashNormalizedQuery(string query)
    {
        var normalized = NormalizeQuery(query);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string NormalizeQuery(string query)
    {
        var builder = new StringBuilder(query.Length);
        var inString = false;
        var pendingSpace = false;
        for (var index = 0; index < query.Length; index++)
        {
            var character = query[index];
            if (character == '\'' && (index == 0 || query[index - 1] != '\\'))
            {
                if (pendingSpace && builder.Length > 0)
                    builder.Append(' ');
                pendingSpace = false;
                inString = !inString;
                builder.Append(character);
                continue;
            }

            if (!inString && char.IsWhiteSpace(character))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
                builder.Append(' ');
            pendingSpace = false;
            builder.Append(character);
        }

        return builder.ToString().Trim();
    }

    private static string ShrinkFailingQuery(string query, Func<string, bool> stillFails)
    {
        var current = query;
        for (var chunk = Math.Max(1, current.Length / 2); chunk >= 1; chunk /= 2)
        {
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var start = 0; start + chunk <= current.Length; start++)
                {
                    var candidate = current.Remove(start, chunk);
                    if (!stillFails(candidate))
                        continue;

                    current = candidate;
                    changed = true;
                    break;
                }
            }
        }

        return current;
    }

    private static void WriteShrinkCandidate(string caseId, string query)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "StructuredInputShrinks");
        Directory.CreateDirectory(directory);
        var safeId = string.Concat(caseId.Select(static character => char.IsLetterOrDigit(character) ? character : '_'));
        File.WriteAllText(Path.Combine(directory, safeId + ".sql"), query, Encoding.UTF8);
    }

    private readonly record struct GeneratedCase(
        string CaseId,
        string Query,
        string CoverageCell,
        DiagnosticExpectation? ExpectedDiagnostic = null);

    private readonly record struct DiagnosticExpectation(
        DiagnosticCode Code,
        DiagnosticPhase Phase,
        DiagnosticSourceKind SourceKind,
        string MessageFragment,
        int MinimumSpanStart,
        int MinimumSuggestedFixes);
}
