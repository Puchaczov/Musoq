using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticTable034TableContractTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void TableContract_ValidDefinition_ShouldPreserveOrderNullabilityAndQualifiedTypes()
    {
        const string query =
            "table Contract { Id: int?, Label: string?, Payload: System.String };" +
            "couple #test.whatever with table Contract as Source;" +
            "select Id, Label, Payload from Source()";
        var row = new Dictionary<string, object?>
        {
            ["Id"] = 7,
            ["Label"] = null,
            ["Payload"] = "payload"
        };

        var table = CreateAndRunVirtualMachine(query, new dynamic[] { row })
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int?)),
            ("Label", typeof(string)),
            ("Payload", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7, null, "payload"]);
    }

    [TestMethod]
    public void TableContract_ColumnNames_ShouldRemainCaseSensitive()
    {
        const string query =
            "table CaseColumns { Name: string, name: int };" +
            "couple #test.whatever with table CaseColumns as Source;" +
            "select source.Name, source.name from Source() source";
        var row = new Dictionary<string, object?>
        {
            ["Name"] = "upper",
            ["name"] = 42
        };

        var table = CreateAndRunVirtualMachine(query, new dynamic[] { row })
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("source.Name", typeof(string)),
            ("source.name", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["upper", 42]);
    }

    [TestMethod]
    public void TableContract_TableNames_ShouldRemainCaseSensitive()
    {
        const string query =
            "table Names { UpperValue: string };" +
            "table names { LowerValue: int };" +
            "couple #test.whatever with table Names as UpperSource;" +
            "couple #test.whatever with table names as LowerSource;" +
            "select upper.UpperValue, lower.LowerValue from UpperSource() upper " +
            "cross join LowerSource() lower";
        var row = new Dictionary<string, object?>
        {
            ["UpperValue"] = "upper",
            ["LowerValue"] = 42
        };

        var table = CreateAndRunVirtualMachine(query, new dynamic[] { row })
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("upper.UpperValue", typeof(string)),
            ("lower.LowerValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["upper", 42]);
    }

    [TestMethod]
    public void TableContract_DuplicateTable_ShouldReportTheSecondDefinitionSpan()
    {
        const string query =
            "table T { First: string };" +
            "table T { Second: int };" +
            "select 1 from #test.whatever()";

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            SpanOf(query, "table T { Second: int }"),
            "duplicate TABLE definition");
    }

    [TestMethod]
    public void TableContract_DuplicateColumn_ShouldReportTheSecondColumnNameSpan()
    {
        const string query =
            "table Dupes { Name: string, Name: int };" +
            "select 1 from #test.whatever()";
        var secondColumnStart = query.IndexOf("Name: int", StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, secondColumnStart);

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            new TextSpan(secondColumnStart, "Name".Length),
            "duplicate TABLE column");
    }

    [TestMethod]
    public void TableContract_EmptyDefinition_ShouldReportInvalidSchema()
    {
        const string query = "table Empty {};select 1 from #test.whatever()";

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            SpanOf(query, "table Empty {}"),
            "empty TABLE definition");
    }

    [TestMethod]
    public void TableContract_UnknownType_ShouldReportAtTheColumnDeclaration()
    {
        const string query = "table Contract { Value: banana };select 1 from #test.whatever()";

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ3005_TypeMismatch,
            SpanOf(query, "Value: banana"),
            "unknown TABLE column type");
    }

    [TestMethod]
    public void TableContract_MismatchedTableName_ShouldReportUndefinedTable()
    {
        const string query =
            "table Names { Value: string };" +
            "couple #test.whatever with table names as Source;" +
            "select Value from Source()";

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ3023_TableNotDefined,
            SpanOf(query, "couple #test.whatever with table names as Source"),
            "case-mismatched TABLE reference");
    }

    [TestMethod]
    public void TableContract_ForwardCouple_ShouldReportInvalidStatementOrder()
    {
        const string query =
            "couple #test.whatever with table Later as Source;" +
            "table Later { Value: string };" +
            "select 1 from #test.whatever()";

        AssertTableDiagnostic(
            query,
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            SpanOf(query, "couple #test.whatever with table Later as Source"),
            "forward TABLE reference");
    }

    private static void AssertTableDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        TextSpan expectedSpan,
        string context)
    {
        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);

        Assert.AreEqual(expectedSpan, diagnostic.Span, context);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation), context);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference), context);
        Assert.IsNotEmpty(envelope.SuggestedFixes, context);
        Assert.IsNotEmpty(envelope.Actions, context);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
            new UnknownSchemaProvider(Array.Empty<dynamic>()),
            compilationOptions: TestCompilationOptions)
            .Analyze(query);
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }
}
