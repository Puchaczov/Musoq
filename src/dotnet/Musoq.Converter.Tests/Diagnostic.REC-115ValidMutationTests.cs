using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC115ValidMutationTests
{
    private const string SimpleSeed = "select d.Dummy from #system.dual() d";
    private const string BindingSeed = "select row.Left from values {( Left: 'a', Right: 'b' )} row";
    private readonly TestsLoggerResolver loggerResolver = new();

    [TestMethod]
    public void ValidMutationMatrix_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(8, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static family => family.Count() == 6));

        foreach (var candidate in CandidateCases)
        {
            var result = Analyze(candidate.Query);

            Assert.IsFalse(result.HasErrors, $"{candidate.Id}: {FormatDiagnostics(result.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(result.Errors, $"{candidate.Id}: {FormatDiagnostics(result.Diagnostics, candidate.Query)}");

            if (candidate.ExpectedWarning is null)
            {
                Assert.IsEmpty(result.Warnings, $"{candidate.Id}: unexpected advisory\n{FormatDiagnostics(result.Diagnostics, candidate.Query)}");
                continue;
            }

            var warning = result.Warnings.SingleOrDefault(item => item.Code == candidate.ExpectedWarning.Value);
            Assert.IsNotNull(warning, $"{candidate.Id}: expected {candidate.ExpectedWarning}\n{FormatDiagnostics(result.Diagnostics, candidate.Query)}");
            Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity, candidate.Id);
            Assert.IsTrue(warning.Location.IsValid, candidate.Id);
            Assert.IsTrue(warning.Span.Start >= 0 && warning.Span.End <= candidate.Query.Length, candidate.Id);
        }
    }

    [TestMethod]
    public void WarningAndConvenienceCompilation_ShouldRemainRunnable()
    {
        foreach (var candidate in CandidateCases.Where(static candidate => candidate.ExpectedWarning is not null))
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC115_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(build.Succeeded, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(build.Errors, candidate.Id);
            var expectedWarning = candidate.ExpectedWarning.GetValueOrDefault();
            Assert.IsTrue(build.Warnings.Any(item => item.Code == expectedWarning), candidate.Id);

            using var table = build.CompiledQuery!.Run();
            Assert.IsNotEmpty(table, candidate.Id);
        }
    }

    [TestMethod]
    public void EquivalentSyntaxAndFormatting_ShouldPreserveRuntimeValues()
    {
        foreach (var candidate in CandidateCases.Where(static candidate => candidate.ExecutionComparable))
        {
            using var seed = Execute(candidate.SeedQuery, candidate.Id + "_seed");
            using var mutated = Execute(candidate.Query, candidate.Id + "_mutated");

            Assert.AreEqual(seed.Count, mutated.Count, candidate.Id);
            Assert.HasCount(seed.Columns.Count(), mutated.Columns, candidate.Id);
            for (var rowIndex = 0; rowIndex < seed.Count; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < seed.Columns.Count(); columnIndex++)
                    Assert.AreEqual(seed[rowIndex][columnIndex], mutated[rowIndex][columnIndex], candidate.Id);
            }
        }
    }

    [TestMethod]
    public void BindingToAnotherRealColumn_ShouldBeValidButChangeTheSelectedValue()
    {
        foreach (var candidate in CandidateCases.Where(static candidate => candidate.Family == "BINDING"))
        {
            var result = Execute(candidate.Query, candidate.Id);
            Assert.AreEqual(1, result.Count, candidate.Id);
            Assert.AreEqual("b", result[0][0], candidate.Id);
        }
    }

    [TestMethod]
    public void FormattingAndCaseMutations_ShouldOnlyMoveWarningCoordinates()
    {
        var warningCodes = CandidateCases
            .Where(static candidate => candidate.Family == "FORMATTING")
            .Select(candidate => Analyze(candidate.Query).Warnings.Select(static warning => warning.Code).ToArray())
            .ToArray();

        Assert.IsTrue(warningCodes.All(static codes => codes.Length == 0));

        var escapeCodes = CandidateCases
            .Where(static candidate => candidate.Family == "ESCAPES")
            .Select(candidate => Analyze(candidate.Query).Warnings.Select(static warning => warning.Code).ToArray())
            .ToArray();

        Assert.IsTrue(escapeCodes.Take(2).All(static codes => codes.SequenceEqual([DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape])));
        Assert.IsTrue(escapeCodes.Skip(2).All(static codes => codes.Length == 0));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new SystemSchemaProvider()).Analyze(query);
    }

    private Table Execute(string query, string identity)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            $"REC115_{identity}_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            loggerResolver);
        return compiled.Run();
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static IReadOnlyList<ValidMutation> CandidateCases { get; } =
    [
        new("A01", "SEMICOLONS", SimpleSeed, SimpleSeed + ";", null, true),
        new("A02", "SEMICOLONS", SimpleSeed, "let value: string = 'single'; select d.Dummy from #system.dual() d where d.Dummy = $value;", null, true),
        new("A03", "SEMICOLONS", SimpleSeed, "enum State : int { Ready = 1, }; " + SimpleSeed + ";", null, true),
        new("A04", "SEMICOLONS", SimpleSeed, "flags enum Access : uint { None = 0ui, Read = 1ui, }; " + SimpleSeed + ";", null, true),
        new("A05", "SEMICOLONS", SimpleSeed, "enum State : int { Ready = 1, Alias = 1, }; " + SimpleSeed, null, true),
        new("A06", "SEMICOLONS", SimpleSeed, SimpleSeed + " -- final comment", null, true),

        new("B01", "ALIASES", SimpleSeed, "select d.Dummy Value from #system.dual() d", null, true),
        new("B02", "ALIASES", SimpleSeed, "select d.Dummy as [Value] from #system.dual() d", null, true),
        new("B03", "ALIASES", SimpleSeed, "select d.Dummy as [from] from #system.dual() d", null, true),
        new("B04", "ALIASES", SimpleSeed, "select d.Dummy as [Column With Spaces] from #system.dual() d", null, true),
        new("B05", "ALIASES", SimpleSeed, "select d.Dummy as value from #system.dual() d", null, true),
        new("B06", "ALIASES", SimpleSeed, "select d.Dummy [Output] from #system.dual() d", null, true),

        new("C01", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [case] from #system.dual() d", null, true),
        new("C02", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [group] from #system.dual() d", null, true),
        new("C03", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [order] from #system.dual() d", null, true),
        new("C04", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [where] from #system.dual() d", null, true),
        new("C05", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [select] from #system.dual() d", null, true),
        new("C06", "CONTEXTUAL", SimpleSeed, "select d.Dummy as [order key] from #system.dual() d", null, true),

        new("D01", "ESCAPES", SimpleSeed, @"select 'C:\new\test' from #system.dual() d", DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, false),
        new("D02", "ESCAPES", SimpleSeed, @"select 'C:\temp\folder' from #system.dual() d", DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, false),
        new("D03", "ESCAPES", SimpleSeed, @"select 'ordinary\q' from #system.dual() d", null, false),
        new("D04", "ESCAPES", SimpleSeed, @"select 'ordinary\z' from #system.dual() d", null, false),
        new("D05", "ESCAPES", SimpleSeed, @"select r'C:\new\test' from #system.dual() d", null, false),
        new("D06", "ESCAPES", SimpleSeed, @"select r'ordinary\q' from #system.dual() d", null, false),

        new("E01", "BINDING", BindingSeed, "select row.Right from values {( Left: 'a', Right: 'b' )} row", null, false),
        new("E02", "BINDING", BindingSeed, "select row.Right as Left from values {( Left: 'a', Right: 'b' )} row", null, false),
        new("E03", "BINDING", BindingSeed, "select row.Right from values {( Left: 'a', Right: 'b' )} row where row.Right = 'b'", null, false),
        new("E04", "BINDING", BindingSeed, "select row.Right, row.Left from values {( Left: 'a', Right: 'b' )} row", null, false),
        new("E05", "BINDING", BindingSeed, "with source as (select row.Right as Chosen from values {( Left: 'a', Right: 'b' )} row) select Chosen from source", null, false),
        new("E06", "BINDING", BindingSeed, "select row.Right from values {( Left: 'a', Right: 'b' )} row order by row.Right", null, false),

        new("F01", "FORMATTING", SimpleSeed, "select\n  d.Dummy\nfrom #system.dual() d", null, true),
        new("F02", "FORMATTING", SimpleSeed, "-- leading comment\nselect d.Dummy from #system.dual() d", null, true),
        new("F03", "FORMATTING", SimpleSeed, "select /* between */ d.Dummy from #system.dual() d", null, true),
        new("F04", "FORMATTING", SimpleSeed, "SELECT d.Dummy FROM #system.dual() d", null, true),
        new("F05", "FORMATTING", SimpleSeed, "select d.Dummy from #system.dual() d -- trailing comment", null, true),
        new("F06", "FORMATTING", SimpleSeed, "select d.Dummy from #system.dual() d where d.Dummy = 'single'", null, true),

        new("G01", "LITERAL_IDENTIFIER", SimpleSeed, "select 'don''t' from #system.dual() d", null, false),
        new("G02", "LITERAL_IDENTIFIER", SimpleSeed, "select 0x10 as [HexValue] from #system.dual() d", null, false),
        new("G03", "LITERAL_IDENTIFIER", SimpleSeed, "select 0b10 as [BinaryValue] from #system.dual() d", null, false),
        new("G04", "LITERAL_IDENTIFIER", SimpleSeed, "select -0 as [NegativeZero] from #system.dual() d", null, false),
        new("G05", "LITERAL_IDENTIFIER", SimpleSeed, "select 1.0 as [DecimalValue] from #system.dual() d", null, false),
        new("G06", "LITERAL_IDENTIFIER", SimpleSeed, "select d.Dummy as [MixedCaseOutput] from #system.dual() d", null, true),

        new("H01", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Value: byte, }; " + SimpleSeed, null, true),
        new("H02", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Value: byte, Marker: short le, }; " + SimpleSeed, null, true),
        new("H03", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Value: byte const 1, }; " + SimpleSeed, null, true),
        new("H04", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Length: byte, Payload: byte[Length], }; " + SimpleSeed, null, true),
        new("H05", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Name: string[4] ascii, }; " + SimpleSeed, null, true),
        new("H06", "BINARY_SCHEMA", SimpleSeed, "binary Packet { Flag: byte, }; " + SimpleSeed + ";", null, true)
    ];

    private sealed record ValidMutation(
        string Id,
        string Family,
        string SeedQuery,
        string Query,
        DiagnosticCode? ExpectedWarning,
        bool ExecutionComparable);
}
