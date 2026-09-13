using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC118ProofAndTemporalTests
{
    private const string SystemSeed = "select d.Dummy from #system.dual() d";
    private const string ValuesSeed = "select row.Value from values { { Value: 1 } } row";
    private const string ValuesTwoSeed = "select row.Value from values { { Value: 2 } } row";
    private const string JoinSeed = "select a.Dummy from #system.dual() a";
    private readonly TestsLoggerResolver loggerResolver = new();

    [TestMethod]
    public void AdvisoryMatrix_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(4, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static family => family.Count() == 12));

        foreach (var candidate in CandidateCases)
        {
            var analysis = Analyze(candidate);

            Assert.IsFalse(analysis.HasErrors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(analysis.Errors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                analysis.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                analysis.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            foreach (var warning in analysis.Warnings)
            {
                Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity, candidate.Id);
                Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase, candidate.Id);
                Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind, candidate.Id);
                Assert.IsTrue(warning.Location.IsValid, candidate.Id);
                Assert.IsTrue(warning.Span.Start >= 0 && warning.Span.End <= candidate.Query.Length, candidate.Id);
            }

            var syntax = new QueryAnalyzer(new SystemSchemaProvider()).ValidateSyntax(candidate.Query);
            Assert.IsFalse(syntax.HasErrors, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(syntax.Warnings, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
        }
    }

    [TestMethod]
    public void WarningCompilation_ShouldRemainRunnableAndPreserveWarningContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC118_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver);

            Assert.IsTrue(build.Succeeded, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(build.Errors, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                build.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                build.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            ConfigureParameters(build.CompiledQuery!, candidate.Parameters);
            using var table = build.CompiledQuery!.Run();
            Assert.IsNotNull(table, candidate.Id);
        }
    }

    [TestMethod]
    public void WarningOnlyCompilation_ShouldPreserveRuntimeValuesAndParameterArguments()
    {
        const string query =
            "param (moment: datetime); select $moment from #system.dual() d where $moment = '01/02/2026'";
        var expectedMoment = new DateTime(2026, 1, 2);

        var diagnosticBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"REC118_runtime_diagnostic_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            loggerResolver);

        Assert.IsTrue(diagnosticBuild.Succeeded, FormatDiagnostics(diagnosticBuild.Diagnostics, query));
        Assert.IsEmpty(diagnosticBuild.Errors, FormatDiagnostics(diagnosticBuild.Diagnostics, query));
        Assert.AreEqual(1, diagnosticBuild.Warnings.Count(static warning =>
            warning.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        ConfigureParameters(diagnosticBuild.CompiledQuery!, ParameterKind.DateTime);
        Assert.AreEqual(expectedMoment, diagnosticBuild.CompiledQuery!.Parameters["moment"]);
        using var diagnosticTable = diagnosticBuild.CompiledQuery.Run();

        var convenienceQuery = InstanceCreator.CompileForExecution(
            query,
            $"REC118_runtime_convenience_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            loggerResolver);
        ConfigureParameters(convenienceQuery, ParameterKind.DateTime);
        Assert.AreEqual(expectedMoment, convenienceQuery.Parameters["moment"]);
        using var convenienceTable = convenienceQuery.Run();

        Assert.AreEqual(convenienceTable.Count, diagnosticTable.Count);
        Assert.HasCount(convenienceTable.Columns.Count(), diagnosticTable.Columns);
        for (var rowIndex = 0; rowIndex < convenienceTable.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < convenienceTable.Columns.Count(); columnIndex++)
                Assert.AreEqual(convenienceTable[rowIndex][columnIndex], diagnosticTable[rowIndex][columnIndex]);
        }
    }

    private static QueryAnalysisResult Analyze(AdvisoryCase candidate)
    {
        return new QueryAnalyzer(new SystemSchemaProvider()).Analyze(candidate.Query);
    }

    private static void ConfigureParameters(CompiledQuery query, ParameterKind parameters)
    {
        switch (parameters)
        {
            case ParameterKind.None:
                return;
            case ParameterKind.DateTime:
                query.Parameters["moment"] = new DateTime(2026, 1, 2);
                return;
            case ParameterKind.DateTimeOffset:
                query.Parameters["moment"] = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
                return;
            case ParameterKind.Double:
                query.Parameters["value"] = double.NaN;
                return;
            case ParameterKind.DoublePair:
                query.Parameters["left"] = double.NaN;
                query.Parameters["right"] = double.NaN;
                return;
            case ParameterKind.Int:
                query.Parameters["value"] = 1;
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(parameters), parameters, null);
        }
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static AdvisoryCase SystemCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics,
        ParameterKind parameters = ParameterKind.None)
    {
        return new AdvisoryCase(id, family, query, expectedDiagnostics, parameters);
    }

    private static AdvisoryCase ValuesCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics,
        ParameterKind parameters = ParameterKind.None)
    {
        return new AdvisoryCase(id, family, query, expectedDiagnostics, parameters);
    }

    private static AdvisoryCase JoinCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics)
    {
        return new AdvisoryCase(id, family, query, expectedDiagnostics, ParameterKind.None);
    }

    private static IReadOnlyList<AdvisoryCase> CandidateCases { get; } =
    [
        SystemCase("T01", "TAUTOLOGY", SystemSeed + " where true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("T02", "TAUTOLOGY", SystemSeed + " where 1 = 1", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("T03", "TAUTOLOGY", SystemSeed + " where false or true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("T04", "TAUTOLOGY", SystemSeed + " where true and true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        ValuesCase("T05", "TAUTOLOGY", "select row.Value from values { { Value: 1 } } row group by row.Value having true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        JoinCase("T06", "TAUTOLOGY", JoinSeed + " inner join #system.dual() b on true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        ValuesCase("T07", "TAUTOLOGY", ValuesSeed + " where row.Value = row.Value", []),
        ValuesCase("T08", "TAUTOLOGY", ValuesSeed + " where row.Value = ToInt32(1) and row.Value = ToInt32(2)", []),
        SystemCase("T09", "TAUTOLOGY", "param (value: double); " + SystemSeed + " where $value = $value", [], ParameterKind.Double),
        ValuesCase("T10", "TAUTOLOGY", ValuesSeed + " where row.Value is null or row.Value is not null", []),
        ValuesCase("T11", "TAUTOLOGY", ValuesTwoSeed + " where row.Value is null and row.Value is not null", []),
        SystemCase("T12", "TAUTOLOGY", "param (value: double); select $value from #system.dual() d where $value = $value", [], ParameterKind.Double),

        SystemCase("C01", "CONTRADICTION", SystemSeed + " where false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("C02", "CONTRADICTION", SystemSeed + " where 1 = 2", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("C03", "CONTRADICTION", SystemSeed + " where true and false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("C04", "CONTRADICTION", SystemSeed + " where false or false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        ValuesCase("C05", "CONTRADICTION", ValuesSeed + " where row.Value = 1 and row.Value = 2", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        ValuesCase("C06", "CONTRADICTION", ValuesSeed + " where row.Value > 10 and row.Value < 5", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("C07", "CONTRADICTION", "param (left: double, right: double); " + SystemSeed + " where $left = $right", [], ParameterKind.DoublePair),
        ValuesCase("C08", "CONTRADICTION", ValuesSeed + " where row.Value = ToInt32(1) and row.Value < ToInt32(0)", []),
        ValuesCase("C09", "CONTRADICTION", ValuesTwoSeed + " where row.Value is null and row.Value is not null", []),
        ValuesCase("C10", "CONTRADICTION", ValuesSeed + " where row.Value > ToInt32(0)", []),
        ValuesCase("C11", "CONTRADICTION", ValuesSeed + " where row.Value = row.Value + 1", []),
        SystemCase("C12", "CONTRADICTION", "param (value: int); " + SystemSeed + " where $value = 1 and $value = 2", [], ParameterKind.Int),

        SystemCase("A01", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '01/02/2026'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTime),
        SystemCase("A02", "TEMPORAL_AMBIGUITY", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = '01/02/2026'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTimeOffset),
        SystemCase("A03", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '01-02-2026'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTime),
        SystemCase("A04", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '01/02/26'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTime),
        SystemCase("A05", "TEMPORAL_AMBIGUITY", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = '12/11/2026'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTimeOffset),
        SystemCase("A06", "TEMPORAL_AMBIGUITY", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = '02-01-2026'", [DiagnosticCode.MQ5003_ImplicitTypeConversion], ParameterKind.DateTimeOffset),
        SystemCase("A07", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '2026-01-02'", [] , ParameterKind.DateTime),
        SystemCase("A08", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '13/02/2026'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTime),
        SystemCase("A09", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = 'January 2, 2026'", [], ParameterKind.DateTime),
        SystemCase("A10", "TEMPORAL_AMBIGUITY", "param (moment: datetime); " + SystemSeed + " where $moment = '01/13/2026'", [], ParameterKind.DateTime),
        SystemCase("A11", "TEMPORAL_AMBIGUITY", "select ToDateTimeWithFormat('01/02/2026', 'dd/MM/yyyy') from #system.dual() d", []),
        SystemCase("A12", "TEMPORAL_AMBIGUITY", "select '01/02/2026' from #system.dual() d", []),

        SystemCase("I01", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = '02/31/2026'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTime),
        SystemCase("I02", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = 'not-a-date'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTime),
        SystemCase("I03", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = '2026-99-99'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTime),
        SystemCase("I04", "TEMPORAL_IMPOSSIBLE", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = 'not-a-date'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTimeOffset),
        SystemCase("I05", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = '31/31/2026'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTime),
        SystemCase("I06", "TEMPORAL_IMPOSSIBLE", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = '2026-02-30T10:00:00+00:00'", [DiagnosticCode.MQ5025_ImpossibleImplicitConversion], ParameterKind.DateTimeOffset),
        SystemCase("I07", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = '2026-01-02'", [], ParameterKind.DateTime),
        SystemCase("I08", "TEMPORAL_IMPOSSIBLE", "select ToDateTimeWithFormat('not-a-date', 'MM/dd/yyyy') from #system.dual() d", []),
        SystemCase("I09", "TEMPORAL_IMPOSSIBLE", "select ToDateTime('2026-01-02') from #system.dual() d", []),
        SystemCase("I10", "TEMPORAL_IMPOSSIBLE", "param (moment: datetime); " + SystemSeed + " where $moment = $moment", [], ParameterKind.DateTime),
        SystemCase("I11", "TEMPORAL_IMPOSSIBLE", "param (moment: datetimeoffset); " + SystemSeed + " where $moment = $moment", [], ParameterKind.DateTimeOffset),
        SystemCase("I12", "TEMPORAL_IMPOSSIBLE", "select ToDateTimeWithFormat('2026-01-02', 'yyyy-MM-dd') from #system.dual() d", [])
    ];

    private enum ParameterKind
    {
        None,
        DateTime,
        DateTimeOffset,
        Double,
        DoublePair,
        Int
    }

    private sealed record AdvisoryCase(
        string Id,
        string Family,
        string Query,
        IReadOnlyList<DiagnosticCode> ExpectedDiagnostics,
        ParameterKind Parameters);
}
