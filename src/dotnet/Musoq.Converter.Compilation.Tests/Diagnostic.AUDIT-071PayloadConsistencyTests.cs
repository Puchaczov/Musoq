using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Schema.Exceptions;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticAudit071PayloadConsistencyTests
{
    [TestMethod]
    public void QueryDiagnostics_ShouldPreserveLocationsAndStructuredFactsAcrossEnvelopeConversion()
    {
        const string parseQuery = "SELECT FROM #system.dual()";
        var parseResult = InstanceCreator.CompileWithDiagnostics(
            parseQuery,
            "audit-071-parse",
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var parseDiagnostic = parseResult.Diagnostics.Single();
        var parseEnvelope = parseResult.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ2005_InvalidSelectList, parseDiagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, parseEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, parseEnvelope.SourceKind);
        AssertEnvelopeLocationMatches(parseDiagnostic, parseEnvelope);
        StringAssert.Contains(parseEnvelope.Snippet!, "SELECT FROM");

        const string bindQuery = "SELECT nonexistent FROM #system.dual()";
        var bindResult = InstanceCreator.CompileWithDiagnostics(
            bindQuery,
            "audit-071-bind",
            new SystemSchemaProvider(),
            new TestsLoggerResolver());

        var bindDiagnostic = bindResult.Diagnostics.Single();
        var bindEnvelope = bindResult.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, bindDiagnostic.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, bindEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, bindEnvelope.SourceKind);
        Assert.AreEqual("nonexistent", bindDiagnostic.Arguments["column"]);
        Assert.AreEqual(bindDiagnostic.Arguments["column"], bindEnvelope.Arguments["column"]);
        AssertEnvelopeLocationMatches(bindDiagnostic, bindEnvelope);
        StringAssert.Contains(bindEnvelope.Snippet!, "nonexistent");
    }

    [TestMethod]
    public void SchemaDataSourceAndRuntimeDiagnostics_ShouldPreserveDomainsAndAvoidFabricatedLocations()
    {
        const string schema = "schema {\r\n  Value: bits(7)\r\n}";
        var schemaSource = new SourceText(schema, "payload.schema");
        var schemaSpan = new TextSpan(schema.IndexOf("bits(7)", StringComparison.Ordinal), "bits(7)".Length);
        var schemaException = new SyntaxException(
            "The binary schema field is invalid.",
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            schemaSpan,
            [new KeyValuePair<string, string>("schema", "payload")],
            [DiagnosticAction.Suggestion("Use a supported bit width.")]);
        var schemaDiagnostic = schemaException.ToDiagnostic(schemaSource);
        var schemaEnvelope = MusoqErrorEnvelope.FromDiagnostic(schemaDiagnostic, "select 1");

        Assert.AreEqual(DiagnosticPhase.Schema, schemaDiagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, schemaDiagnostic.SourceKind);
        Assert.AreEqual(DiagnosticPhase.Schema, schemaEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, schemaEnvelope.SourceKind);
        AssertEnvelopeLocationMatches(schemaDiagnostic, schemaEnvelope);
        Assert.AreEqual("payload", schemaEnvelope.Arguments["schema"]);
        Assert.IsTrue(schemaEnvelope.SuggestedFixes.Contains("Use a supported bit width."));
        var schemaAction = schemaEnvelope.Actions.Single(action => action.Title == "Use a supported bit width.");
        Assert.AreEqual(DiagnosticActionKind.Suggestion, schemaAction.Kind);
        Assert.IsNull(schemaAction.TextEdit);
        StringAssert.Contains(schemaEnvelope.Snippet!, "bits(7)");
        Assert.IsFalse(schemaEnvelope.Snippet!.Contains("select 1", StringComparison.OrdinalIgnoreCase));

        const string providerSecret = "audit-071-provider-secret";
        var dataSourceException = DataSourceLifecycleException.ForRead(
            "orders",
            "provider",
            "ordersAlias",
            "source-context-071",
            new InvalidOperationException(providerSecret));
        var dataSourceDiagnostic = dataSourceException.ToDiagnostic();
        var dataSourceEnvelope = MusoqErrorEnvelope.FromDiagnostic(dataSourceDiagnostic, "select 1");

        Assert.AreEqual(DiagnosticCode.MQ7011_DataSourceReadFailed, dataSourceEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.DataSource, dataSourceEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, dataSourceEnvelope.SourceKind);
        Assert.AreEqual("orders", dataSourceEnvelope.Arguments["schema"]);
        Assert.AreEqual("provider", dataSourceEnvelope.Arguments["source"]);
        Assert.AreEqual("ordersAlias", dataSourceEnvelope.Arguments["alias"]);
        Assert.AreEqual("source-context-071", dataSourceEnvelope.Arguments["sourceContextId"]);
        Assert.AreEqual("read", dataSourceEnvelope.Arguments["operation"]);
        AssertUnknownLocation(dataSourceEnvelope);
        Assert.IsFalse(dataSourceEnvelope.Message.Contains(providerSecret, StringComparison.Ordinal));

        var runtimeDiagnostic = ScriptParameterBindingException.Unknown("unlistedParameter").ToDiagnostic();
        var runtimeEnvelope = MusoqErrorEnvelope.FromDiagnostic(runtimeDiagnostic, "select 1");

        Assert.AreEqual(DiagnosticCode.MQ7006_UnknownScriptParameter, runtimeEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Runtime, runtimeEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Runtime, runtimeEnvelope.SourceKind);
        AssertUnknownLocation(runtimeEnvelope);
    }

    [TestMethod]
    public void GeneratedAndInternalDiagnostics_ShouldRetainNonQueryIdentityAndCorrelation()
    {
        var generatedContext = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        TargetDiagnosticReporter.Report(
            [
                new TargetDiagnostic(
                    "MT0710",
                    TargetDiagnosticSeverity.Error,
                    "generated source failed",
                    new TargetSourceRange(23, 4, 4, 11, 4, 15),
                    "CompiledQuery.g.cs",
                    "generated line")
            ],
            generatedContext);

        var generatedDiagnostic = generatedContext.Diagnostics.Single();
        var generatedEnvelope = MusoqErrorEnvelope.FromDiagnostic(generatedDiagnostic, "select 1");

        Assert.AreEqual(DiagnosticPhase.CodeGeneration, generatedEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.GeneratedSource, generatedEnvelope.SourceKind);
        Assert.AreEqual("MT0710", generatedEnvelope.Arguments["targetCode"]);
        Assert.AreEqual(23, generatedEnvelope.Offset);
        Assert.AreEqual(27, generatedEnvelope.EndOffset);
        Assert.AreEqual(4, generatedEnvelope.Length);
        Assert.AreEqual(4, generatedEnvelope.Line);
        Assert.AreEqual(11, generatedEnvelope.Column);
        Assert.AreEqual(4, generatedEnvelope.EndLine);
        Assert.AreEqual(15, generatedEnvelope.EndColumn);
        Assert.AreEqual("generated line", generatedEnvelope.Snippet);
        Assert.IsFalse(generatedEnvelope.Snippet!.Contains("select 1", StringComparison.Ordinal));

        var generatedWithoutRangeContext = new DiagnosticContext(new SourceText("select 1", "query.musoq"));
        TargetDiagnosticReporter.Report(
            [new TargetDiagnostic("MT0711", TargetDiagnosticSeverity.Error, "generated source has no range")],
            generatedWithoutRangeContext);
        var generatedWithoutRange = MusoqErrorEnvelope.FromDiagnostic(
            generatedWithoutRangeContext.Diagnostics.Single(),
            "select 1");
        AssertUnknownLocation(generatedWithoutRange);

        const string internalSecret = "audit-071-internal-secret";
        var internalException = InternalDiagnosticException.ForExecution(new InvalidOperationException(internalSecret));
        var internalEnvelope = MusoqErrorEnvelope.FromDiagnostic(internalException.ToDiagnostic(), "select 1");
        var internalJson = MusoqErrorEnvelopeFormatter.FormatJson(internalEnvelope);

        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, internalEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.Internal, internalEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Internal, internalEnvelope.SourceKind);
        Assert.AreEqual(internalException.CorrelationId, internalEnvelope.CorrelationId);
        Assert.AreEqual(internalException.CorrelationId, internalEnvelope.Arguments["correlationId"]);
        AssertUnknownLocation(internalEnvelope);
        Assert.IsFalse(internalJson.Contains(internalSecret, StringComparison.Ordinal));

        using var document = JsonDocument.Parse(internalJson);
        Assert.AreEqual(internalException.CorrelationId, document.RootElement.GetProperty("correlationId").GetString());
    }

    [TestMethod]
    public void StructuredPayload_ShouldPreserveRelatedLocationsActionsAndCorrelationInJson()
    {
        const string query = "select Name\nfrom #source.rows()";
        var querySource = new SourceText(query, "query.musoq");
        var primarySpan = new TextSpan(query.IndexOf("Name", StringComparison.Ordinal), "Name".Length);
        var (primaryStart, primaryEnd) = querySource.GetLocations(primarySpan);

        const string schema = "Name: string";
        var schemaSource = new SourceText(schema, "payload.schema");
        var relatedSpan = new TextSpan(0, "Name".Length);
        var (relatedStart, relatedEnd) = schemaSource.GetLocations(relatedSpan);
        var related = new DiagnosticRelatedLocation(
            relatedStart,
            relatedEnd,
            "Schema declaration is here",
            DiagnosticSourceKind.Schema);
        var action = DiagnosticAction.QuickFix("Replace with Value", primarySpan, "Value");
        var diagnostic = new Diagnostic(
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticSeverity.Error,
            "Unknown column 'Name'.",
            primaryStart,
            primaryEnd,
            querySource.GetContextSnippet(primarySpan),
            suggestedFixes: [action],
            phase: DiagnosticPhase.Bind,
            sourceKind: DiagnosticSourceKind.Query,
            arguments:
            [
                new KeyValuePair<string, string>("column", "Name"),
                new KeyValuePair<string, string>("availableColumns", "Value")
            ],
            relatedLocations: [related],
            correlationId: "audit-071-correlation");

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(primaryStart.Offset, envelope.Offset);
        Assert.AreEqual(primaryEnd.Offset, envelope.EndOffset);
        Assert.AreEqual(primarySpan.Length, envelope.Length);
        Assert.AreEqual("Name", envelope.Arguments["column"]);
        Assert.AreEqual(DiagnosticSourceKind.Schema, envelope.RelatedLocations.Single().SourceKind);
        Assert.AreEqual(0, envelope.RelatedLocations.Single().Location.Offset);
        Assert.AreEqual(4, envelope.RelatedLocations.Single().EndLocation.Offset);
        Assert.AreEqual("audit-071-correlation", envelope.CorrelationId);
        Assert.AreEqual("Value", envelope.Actions.Single().TextEdit!.NewText);

        using var document = JsonDocument.Parse(MusoqErrorEnvelopeFormatter.FormatJson(envelope));
        var root = document.RootElement;
        Assert.AreEqual("MQ3001", root.GetProperty("code").GetString());
        Assert.AreEqual("bind", root.GetProperty("phase").GetString());
        Assert.AreEqual("query", root.GetProperty("source").GetString());
        Assert.AreEqual("Name", root.GetProperty("arguments").GetProperty("column").GetString());
        Assert.AreEqual("schema", root.GetProperty("related")[0].GetProperty("source").GetString());
        Assert.AreEqual(0, root.GetProperty("related")[0].GetProperty("offset").GetInt32());
        Assert.AreEqual(4, root.GetProperty("related")[0].GetProperty("endOffset").GetInt32());
        Assert.AreEqual(primarySpan.Start, root.GetProperty("actions")[0].GetProperty("edit").GetProperty("start").GetInt32());
        Assert.AreEqual(4, root.GetProperty("actions")[0].GetProperty("edit").GetProperty("length").GetInt32());
        Assert.AreEqual("Value", root.GetProperty("actions")[0].GetProperty("edit").GetProperty("newText").GetString());
        Assert.AreEqual("audit-071-correlation", root.GetProperty("correlationId").GetString());
    }

    private static void AssertEnvelopeLocationMatches(Diagnostic diagnostic, MusoqErrorEnvelope envelope)
    {
        Assert.IsTrue(diagnostic.Location.IsValid);
        Assert.AreEqual(diagnostic.Location.Offset, envelope.Offset);
        Assert.AreEqual(diagnostic.Location.Line, envelope.Line);
        Assert.AreEqual(diagnostic.Location.Column, envelope.Column);
        Assert.IsTrue(diagnostic.EndLocation.IsValid);
        Assert.AreEqual(diagnostic.EndLocation.Offset, envelope.EndOffset);
        Assert.AreEqual(diagnostic.EndLocation.Line, envelope.EndLine);
        Assert.AreEqual(diagnostic.EndLocation.Column, envelope.EndColumn);
        Assert.AreEqual(diagnostic.Span.Length, envelope.Length);
    }

    private static void AssertUnknownLocation(MusoqErrorEnvelope envelope)
    {
        Assert.IsNull(envelope.Offset);
        Assert.IsNull(envelope.EndOffset);
        Assert.IsNull(envelope.Line);
        Assert.IsNull(envelope.Column);
        Assert.IsNull(envelope.EndLine);
        Assert.IsNull(envelope.EndColumn);
        Assert.IsNull(envelope.Length);
        Assert.IsNull(envelope.Snippet);
    }
}
