using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticTable035TableSourceContractTests : BasicEntityTestBase
{
    [TestMethod]
    public void TableContract_ReadModifiers_ShouldPreserveMapsAcrossMetadataPlanningAndExecution()
    {
        const string query =
            "table RecordsShape { " +
            "Name: string encoding 'utf-8' trim, " +
            "Amount: decimal culture 'pl-PL' format '#,##0.00', " +
            "Payload: string source codec 'base64', " +
            "Plain: string };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name, Amount, Payload, Plain from Records()";
        var rows = new[]
        {
            Row(
                ("Name", "  Name  "),
                ("Amount", "12,50"),
                ("Payload", "cGF5bG9hZA=="),
                ("Plain", "plain"))
        };
        var provider = new ReadModifiersSchemaProvider(rows);

        var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run();

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Name", 12.50m, "payload", "plain"]);

        Assert.IsNotEmpty(provider.Recorder.GetTableColumns);
        Assert.IsNotEmpty(provider.Recorder.DescriptorColumns);
        Assert.IsNotEmpty(provider.Recorder.ExecutionColumns);
        Assert.IsNotEmpty(provider.Recorder.PlanRequests);

        foreach (var columns in provider.Recorder.GetTableColumns
                     .Concat(provider.Recorder.DescriptorColumns)
                     .Concat(provider.Recorder.ExecutionColumns))
        {
            AssertModifierMap(
                columns.Single(column => column.ColumnName == "Name").ReadModifiers,
                (ColumnReadModifiers.Encoding, "utf-8"),
                (ColumnReadModifiers.Trim, "true"));
            AssertModifierMap(
                columns.Single(column => column.ColumnName == "Amount").ReadModifiers,
                (ColumnReadModifiers.Culture, "pl-PL"),
                (ColumnReadModifiers.Format, "#,##0.00"));
            AssertModifierMap(
                columns.Single(column => column.ColumnName == "Payload").ReadModifiers,
                ("source.codec", "base64"));
            Assert.IsEmpty(columns.Single(column => column.ColumnName == "Plain").ReadModifiers);
        }

        foreach (var column in provider.Recorder.PlanRequests
                     .SelectMany(static request => request.RequiredColumns))
        {
            if (column.Name == "Name")
                AssertModifierMap(column.ReadModifiers, (ColumnReadModifiers.Encoding, "utf-8"), (ColumnReadModifiers.Trim, "true"));
            else if (column.Name == "Amount")
                AssertModifierMap(column.ReadModifiers, (ColumnReadModifiers.Culture, "pl-PL"), (ColumnReadModifiers.Format, "#,##0.00"));
            else if (column.Name == "Payload")
                AssertModifierMap(column.ReadModifiers, ("source.codec", "base64"));
            else if (column.Name == "Plain")
                Assert.IsEmpty(column.ReadModifiers);
        }

        Assert.IsTrue(provider.Recorder.PlanRequests.All(static request => request.RequiredColumns.Count == 4));
    }

    [TestMethod]
    public void TableContract_SourceContractWarning_ShouldExposeStructuredModifierDiagnostic()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'windows-1250' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var provider = new ReadModifiersSchemaProvider(
            [Row(("Name", "warning"))],
            ReadModifiersValidationMode.LenientUnsupportedModifiers);

        var result = Compile(query, provider);

        Assert.IsTrue(result.Succeeded);
        var warning = result.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5013_SourceContractWarning, warning.Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind);
        Assert.AreEqual(SpanOf(query, "encoding 'windows-1250'"), warning.Span);
        Assert.IsTrue(warning.Location.IsValid);
        Assert.IsTrue(warning.EndLocation.IsValid);

        var envelope = result.ToAllEnvelopes().Single();
        Assert.AreEqual(DiagnosticCode.MQ5013_SourceContractWarning, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(warning.Span.Start, envelope.Offset);
        Assert.AreEqual(warning.Span.End, envelope.EndOffset);
        Assert.AreEqual(warning.Span.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.AreEqual("Table/Couple Spec - Source Contract Diagnostics", envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void TableContract_SourceContractError_ShouldExposeStructuredColumnDiagnostic()
    {
        const string query =
            "table RecordsShape { Amount: decimal };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Amount from Records()";
        var provider = new ReadModifiersSchemaProvider(
            [Row(("Amount", "12.50"))],
            ReadModifiersValidationMode.ValidateSourceKinds,
            new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["Amount"] = typeof(string)
            });

        var result = Compile(query, provider);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, error.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, error.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, error.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, error.SourceKind);
        Assert.AreEqual(SpanOf(query, "Amount: decimal"), error.Span);
        Assert.IsTrue(error.Location.IsValid);
        Assert.IsTrue(error.EndLocation.IsValid);

        var envelope = result.ToEnvelopes().Single();
        Assert.AreEqual(DiagnosticCode.MQ3071_SourceContractError, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(error.Span.Start, envelope.Offset);
        Assert.AreEqual(error.Span.End, envelope.EndOffset);
        Assert.AreEqual(error.Span.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.AreEqual("Table/Couple Spec - Source Contract Diagnostics", envelope.DocsReference);
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void TableContract_SourceContractInfo_ShouldRemainPlanningOnly()
    {
        const string query =
            "table RecordsShape { Name: string };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new ReadModifiersSchemaProvider(
                [Row(("Name", "info"))],
                ReadModifiersValidationMode.ReportInfo),
            LoggerResolver,
            TestCompilationOptions);
        var result = Compile(
            query,
            new ReadModifiersSchemaProvider(
                [Row(("Name", "info"))],
                ReadModifiersValidationMode.ReportInfo));

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code is DiagnosticCode.MQ5013_SourceContractWarning or DiagnosticCode.MQ3071_SourceContractError));
        Assert.Contains("source contract diagnostic [DescribeSource]: Info ReadModifiersInfo", inspection.PlanningText);
        Assert.Contains("source contract diagnostic [TryPlanSource]: Info ReadModifiersInfo", inspection.PlanningText);
    }

    private BuildResult Compile(string query, ReadModifiersSchemaProvider provider)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
    }

    private static void AssertModifierMap(
        IReadOnlyDictionary<string, string> modifiers,
        params (string Key, string Value)[] expected)
    {
        Assert.AreEqual(expected.Length, modifiers.Count);
        foreach (var (key, value) in expected)
        {
            Assert.IsTrue(modifiers.TryGetValue(key, out var actual), $"Missing modifier '{key}'.");
            Assert.AreEqual(value, actual);
        }
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }

    private static IReadOnlyDictionary<string, object?> Row(
        params (string Name, object? Value)[] values)
    {
        return values.ToDictionary(
            static value => value.Name,
            static value => value.Value,
            StringComparer.Ordinal);
    }
}
