using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema.Exceptions;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    private static readonly string[] LocationCaseIds = ["A01", "A02", "A03", "A04", "A05", "A06"];
    private static readonly string[] SourceDomainCaseIds = ["B01", "B02", "B03", "B04", "B05", "B06"];
    private static readonly string[] TableOriginCaseIds = ["C01", "C02", "C03", "C04", "C05", "C06"];
    private static readonly string[] FallbackCaseIds = ["D01", "D02", "D03", "D04", "D05", "D06"];

    [TestMethod]
    public void CandidateMatrix_ShouldCoverFrozenContract()
    {
        var ids = LocationCaseIds
            .Concat(SourceDomainCaseIds)
            .Concat(TableOriginCaseIds)
            .Concat(FallbackCaseIds)
            .ToArray();

        Assert.HasCount(24, ids);
        Assert.AreEqual(24, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.HasCount(6, LocationCaseIds);
        Assert.HasCount(6, SourceDomainCaseIds);
        Assert.HasCount(6, TableOriginCaseIds);
        Assert.HasCount(6, FallbackCaseIds);
    }

    [TestMethod]
    public void LocationSemantics_ShouldDistinguishUnknownZeroAndInsertion()
    {
        var unknown = Diagnostic.ErrorUnknownLocation(
            DiagnosticCode.MQ3001_UnknownColumn,
            "unknown location",
            DiagnosticSourceKind.Query);
        AssertUnknownLocation(unknown, "A01");

        var zero = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "known insertion at zero",
            new SourceLocation(0, 1, 1),
            new SourceLocation(0, 1, 1),
            sourceKind: DiagnosticSourceKind.Query);
        AssertKnownInsertion(zero, 0, "A02");

        var nonzero = Diagnostic.Error(
            DiagnosticCode.MQ2001_UnexpectedToken,
            "known insertion",
            new SourceLocation(7, 1, 8),
            new SourceLocation(7, 1, 8),
            sourceKind: DiagnosticSourceKind.Query);
        AssertKnownInsertion(nonzero, 7, "A03");

        var sourceText = new SourceText("select 1", "query.musoq");
        var nullSpan = new NullSpanDiagnosticException().ToDiagnosticOrGeneric(sourceText);
        AssertUnknownLocation(nullSpan, "A04");

        var emptySpan = new EmptySpanDiagnosticException().ToDiagnosticOrGeneric(sourceText);
        AssertKnownInsertion(emptySpan, 0, "A05");

        var generatedContext = new DiagnosticContext(sourceText);
        TargetDiagnosticReporter.Report(
            [new TargetDiagnostic("MT-A06", TargetDiagnosticSeverity.Error, "generated failure")],
            generatedContext);
        var generated = generatedContext.Diagnostics.Single();
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, generated.SourceKind, "A06");
        AssertUnknownLocation(generated, "A06");
    }

    [TestMethod]
    public void SourceDomains_ShouldRetainSixOrigins()
    {
        var query = Diagnostic.Error(
            DiagnosticCode.MQ3001_UnknownColumn,
            "query root",
            new TextSpan(0, 1),
            DiagnosticSourceKind.Query);
        AssertDomain(query, DiagnosticPhase.Bind, DiagnosticSourceKind.Query, "B01");

        var schema = new Diagnostic(
            DiagnosticCode.MQ4008_DuplicateSchemaField,
            DiagnosticSeverity.Error,
            "schema root",
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Schema,
            sourceKind: DiagnosticSourceKind.Schema);
        AssertDomain(schema, DiagnosticPhase.Schema, DiagnosticSourceKind.Schema, "B02");

        var dataSource = DataSourceLifecycleException.ForRead(
            "#source",
            "items",
            "rows",
            "REC126-B03",
            new InvalidOperationException("provider detail"))
            .ToDiagnostic();
        AssertDomain(dataSource, DiagnosticPhase.DataSource, DiagnosticSourceKind.DataSource, "B03");

        var runtime = ScriptParameterBindingException.MissingRequired("token").ToDiagnostic();
        AssertDomain(runtime, DiagnosticPhase.Runtime, DiagnosticSourceKind.Runtime, "B04");

        var generatedContext = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        TargetDiagnosticReporter.Report(
            [new TargetDiagnostic(
                "MT-B05",
                TargetDiagnosticSeverity.Error,
                "generated root",
                new TargetSourceRange(12, 2, 4, 5, 4, 7),
                "generated-query.g.cs")],
            generatedContext);
        var generated = generatedContext.Diagnostics.Single();
        AssertDomain(generated, DiagnosticPhase.CodeGeneration, DiagnosticSourceKind.GeneratedSource, "B05");
        Assert.AreEqual("generated-query.g.cs", generated.Location.FilePath, "B05");

        var internalDiagnostic = InternalDiagnosticException.ForExecution(
            new InvalidOperationException("internal detail"))
            .ToDiagnostic();
        AssertDomain(internalDiagnostic, DiagnosticPhase.Internal, DiagnosticSourceKind.Internal, "B06");
        Assert.IsFalse(string.IsNullOrWhiteSpace(internalDiagnostic.CorrelationId), "B06");
    }

    [TestMethod]
    public void TableOrigins_ShouldRetainPreciseProviderSpans()
    {
        var modifierQuery = CreateContractQuery("Name: string encoding 'windows-1250'");
        var modifierResult = CompileWithContractDiagnostics(
            modifierQuery,
            ContractDiagnosticMode.WarnUnsupportedEncoding);
        var modifierWarning = modifierResult.Warnings.Single();
        Assert.AreEqual(
            CreateExpectedSpan(modifierQuery, "encoding 'windows-1250'"),
            modifierWarning.Span,
            "C01");
        Assert.AreEqual(DiagnosticSourceKind.Query, modifierWarning.SourceKind, "C01");

        var columnQuery = CreateContractQuery("Amount: decimal", "Amount");
        var columnResult = CompileWithContractDiagnostics(
            columnQuery,
            ContractDiagnosticMode.RequireAmountString);
        var columnError = columnResult.Errors.Single();
        Assert.AreEqual(CreateExpectedSpan(columnQuery, "Amount: decimal"), columnError.Span, "C02");
        Assert.AreEqual(DiagnosticSourceKind.Query, columnError.SourceKind, "C02");

        var planQuery = CreateContractQuery("Name: string encoding 'windows-1250'");
        var planResult = CompileWithContractDiagnostics(
            planQuery,
            ContractDiagnosticMode.RequireUtf8Encoding);
        var planError = planResult.Errors.Single();
        Assert.AreEqual(
            CreateExpectedSpan(planQuery, "encoding 'windows-1250'"),
            planError.Span,
            "C03");

        const string twoSources =
            "table LeftShape { Name: string encoding 'windows-1250' };" +
            "table RightShape { Name: string encoding 'windows-1250' };" +
            "couple #contract.items with table LeftShape as Left;" +
            "couple #contract.items with table RightShape as Right;" +
            "select l.Name, r.Name from Left() l cross join Right() r";
        var twoSourceResult = CompileWithContractDiagnostics(
            twoSources,
            ContractDiagnosticMode.WarnUnsupportedEncoding);
        var twoSourceStarts = twoSourceResult.Warnings
            .Select(static warning => warning.Span.Start)
            .OrderBy(static start => start)
            .ToArray();
        var expectedTwoSourceStarts = new[]
        {
            twoSources.IndexOf("encoding 'windows-1250'", StringComparison.Ordinal),
            twoSources.LastIndexOf("encoding 'windows-1250'", StringComparison.Ordinal)
        };
        CollectionAssert.AreEqual(expectedTwoSourceStarts, twoSourceStarts, "C04");

        var cteQuery =
            "table LegacyRecord { Id: int, Name: string encoding 'windows-1250' };" +
            "couple #contract.items with table LegacyRecord as Records;" +
            "with named as (select r.Name as Name from Records() r)" +
            "select Name from named";
        var cteResult = CompileWithContractDiagnostics(
            cteQuery,
            ContractDiagnosticMode.WarnUnsupportedEncoding);
        var cteWarning = cteResult.Warnings.Single();
        Assert.AreEqual(
            CreateExpectedSpan(cteQuery, "encoding 'windows-1250'"),
            cteWarning.Span,
            "C05");
        Assert.IsFalse(string.IsNullOrWhiteSpace(cteWarning.ContextSnippet), "C05");

        var bothOriginQuery = CreateContractQuery("Name: string encoding 'windows-1250'");
        var bothOriginResult = CompileWithContractDiagnostics(
            bothOriginQuery,
            ContractDiagnosticMode.BothOrigin);
        var bothOriginWarning = bothOriginResult.Warnings.Single();
        Assert.AreEqual(
            CreateExpectedSpan(bothOriginQuery, "encoding 'windows-1250'"),
            bothOriginWarning.Span,
            "C06");
        Assert.Contains("column=Name", bothOriginWarning.Message, "C06");
        Assert.Contains("modifier=encoding", bothOriginWarning.Message, "C06");
    }

    [TestMethod]
    public void FallbackConflict_ShouldKeepUnknownLocationsUnknown()
    {
        var sourceText = new SourceText("select 1", "query.musoq");

        var explicitEmpty = new DiagnosticContext(sourceText);
        explicitEmpty.ReportWarning(
            DiagnosticCode.MQ5013_SourceContractWarning,
            "explicit zero-length warning",
            TextSpan.Empty);
        var explicitWarning = explicitEmpty.Warnings.Single();
        Assert.IsTrue(explicitWarning.Location.IsValid, "D01");
        Assert.AreEqual(0, explicitWarning.Location.Offset, "D01");
        Assert.AreEqual(0, explicitWarning.Span.Length, "D01");

        var missingNode = new DiagnosticContext(sourceText);
        missingNode.ReportError(
            DiagnosticCode.MQ3071_SourceContractError,
            "unknown error location",
            (Node?)null);
        var missingDiagnostic = missingNode.Errors.Single();
        AssertUnknownLocation(missingDiagnostic, "D02");

        var dataSource = DataSourceLifecycleException.ForOpen(
            "#source",
            "items",
            "rows",
            "REC126-D03",
            new InvalidOperationException("open detail"));
        var dataSourceEnvelope = MusoqErrorEnvelope.FromException(dataSource);
        AssertUnknownLocation(dataSourceEnvelope, "D03");

        var generatedContext = new DiagnosticContext(sourceText);
        TargetDiagnosticReporter.Report(
            [new TargetDiagnostic("MT-D04", TargetDiagnosticSeverity.Error, "generated no range")],
            generatedContext);
        var generatedEnvelope = MusoqErrorEnvelope.FromDiagnostic(generatedContext.Diagnostics.Single());
        using var generatedJson = JsonDocument.Parse(MusoqErrorEnvelopeFormatter.FormatJson(generatedEnvelope));
        Assert.IsFalse(generatedJson.RootElement.TryGetProperty("location", out _), "D04");
        Assert.AreEqual("generated-source", generatedJson.RootElement.GetProperty("source").GetString(), "D04");

        var genericWarningResult = CompileWithContractDiagnostics(
            "select 1 from #contract.items()",
            ContractDiagnosticMode.GenericWarning);
        AssertUnknownLocation(genericWarningResult.Warnings.Single(), "D05");

        var genericErrorResult = CompileWithContractDiagnostics(
            "select 1 from #contract.items()",
            ContractDiagnosticMode.GenericError);
        AssertUnknownLocation(genericErrorResult.Errors.Single(), "D06");
    }

    private BuildResult CompileWithContractDiagnostics(
        string query,
        ContractDiagnosticMode mode)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new ContractDiagnosticSchemaProvider(mode),
            _loggerResolver);
    }

    private static void AssertDomain(
        Diagnostic diagnostic,
        DiagnosticPhase expectedPhase,
        DiagnosticSourceKind expectedSource,
        string caseId)
    {
        Assert.AreEqual(expectedPhase, diagnostic.Phase, caseId);
        Assert.AreEqual(expectedSource, diagnostic.SourceKind, caseId);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);
        Assert.AreEqual(expectedPhase, envelope.Phase, caseId);
        Assert.AreEqual(expectedSource, envelope.SourceKind, caseId);
    }

    private static void AssertKnownInsertion(Diagnostic diagnostic, int offset, string caseId)
    {
        Assert.IsTrue(diagnostic.Location.IsValid, caseId);
        Assert.IsTrue(diagnostic.EndLocation.IsValid, caseId);
        Assert.AreEqual(offset, diagnostic.Location.Offset, caseId);
        Assert.AreEqual(offset, diagnostic.EndLocation.Offset, caseId);
        Assert.AreEqual(0, diagnostic.Span.Length, caseId);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic);
        Assert.AreEqual(offset, envelope.Offset, caseId);
        Assert.AreEqual(0, envelope.Length, caseId);
    }

    private static void AssertUnknownLocation(Diagnostic diagnostic, string caseId)
    {
        Assert.AreEqual(SourceLocation.None, diagnostic.Location, caseId);
        Assert.AreEqual(SourceLocation.None, diagnostic.EndLocation, caseId);
        AssertUnknownLocation(MusoqErrorEnvelope.FromDiagnostic(diagnostic), caseId);
    }

    private static void AssertUnknownLocation(MusoqErrorEnvelope envelope, string caseId)
    {
        Assert.IsNull(envelope.Offset, caseId);
        Assert.IsNull(envelope.EndOffset, caseId);
        Assert.IsNull(envelope.Line, caseId);
        Assert.IsNull(envelope.Column, caseId);
        Assert.IsNull(envelope.EndLine, caseId);
        Assert.IsNull(envelope.EndColumn, caseId);
        Assert.IsNull(envelope.Length, caseId);
        Assert.IsNull(envelope.Snippet, caseId);
    }

    private sealed class NullSpanDiagnosticException : Exception, IDiagnosticException
    {
        public DiagnosticCode Code => DiagnosticCode.MQ3001_UnknownColumn;

        public TextSpan? Span => null;

        public Diagnostic ToDiagnostic(SourceText? sourceText = null)
        {
            return Diagnostic.Error(
                Code,
                "legacy converter fabricated a location",
                TextSpan.Empty,
                DiagnosticSourceKind.Query);
        }
    }

    private sealed class EmptySpanDiagnosticException : Exception, IDiagnosticException
    {
        public DiagnosticCode Code => DiagnosticCode.MQ3001_UnknownColumn;

        public TextSpan? Span => TextSpan.Empty;

        public Diagnostic ToDiagnostic(SourceText? sourceText = null)
        {
            return Diagnostic.Error(
                Code,
                "zero-length insertion is known",
                TextSpan.Empty,
                DiagnosticSourceKind.Query);
        }
    }
}
