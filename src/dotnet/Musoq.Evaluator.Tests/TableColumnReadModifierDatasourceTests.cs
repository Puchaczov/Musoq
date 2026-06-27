using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.ReadModifiers;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class TableColumnReadModifierDatasourceTests : BasicEntityTestBase
{
    [TestMethod]
    public void ReadModifiers_DifferentColumnsCanUseDifferentEncodings()
    {
        const string query =
            "table RecordsShape { Utf8Name: string encoding 'utf-8', Utf16Name: string encoding 'utf-16le' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Utf8Name, Utf16Name from Records()";
        var rows = new[]
        {
            Row(
                ("Utf8Name", Encoding.UTF8.GetBytes("Za\u017c\u00f3\u0142\u0107")),
                ("Utf16Name", Encoding.Unicode.GetBytes("\u0141\u00f3d\u017a")))
        };

        var table = Run(query, rows);

        Assert.AreEqual("Za\u017c\u00f3\u0142\u0107", table[0][0]);
        Assert.AreEqual("\u0141\u00f3d\u017a", table[0][1]);
    }

    [TestMethod]
    public void ReadModifiers_TrimAffectsStringsAndParsedNumericValues()
    {
        const string query =
            "table RecordsShape { Name: string trim, Amount: decimal trim };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name, Amount from Records()";
        var rows = new[]
        {
            Row(("Name", "  Ada  "), ("Amount", "  12.50  "))
        };

        var table = Run(query, rows);

        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual(12.50m, table[0][1]);
    }

    [TestMethod]
    public void ReadModifiers_CultureParsesDecimalForWhereOrderByAndProjection()
    {
        const string query =
            "table RecordsShape { Name: string, Amount: decimal culture 'pl-PL' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name, Amount from Records() where Amount > 2 order by Amount";
        var rows = new[]
        {
            Row(("Name", "Low"), ("Amount", "1,20")),
            Row(("Name", "High"), ("Amount", "10,50")),
            Row(("Name", "Mid"), ("Amount", "2,40"))
        };

        var table = Run(query, rows);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Mid", table[0][0]);
        Assert.AreEqual(2.40m, table[0][1]);
        Assert.AreEqual("High", table[1][0]);
        Assert.AreEqual(10.50m, table[1][1]);
    }

    [TestMethod]
    public void ReadModifiers_FormatParsesDateValuesForOrderingAndProjection()
    {
        const string query =
            "table RecordsShape { Label: string, EventDate: datetime format 'dd.MM.yyyy' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Label, EventDate from Records() order by EventDate";
        var rows = new[]
        {
            Row(("Label", "Newer"), ("EventDate", "02.01.2024")),
            Row(("Label", "Older"), ("EventDate", "31.12.2023"))
        };

        var table = Run(query, rows);

        Assert.AreEqual("Older", table[0][0]);
        Assert.AreEqual(new DateTime(2023, 12, 31), table[0][1]);
        Assert.AreEqual("Newer", table[1][0]);
        Assert.AreEqual(new DateTime(2024, 1, 2), table[1][1]);
    }

    [TestMethod]
    public void ReadModifiers_SourceCodecBase64DecodesPayloadText()
    {
        const string query =
            "table RecordsShape { Payload: string source codec 'base64' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Payload from Records()";
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("payload text"));
        var rows = new[] { Row(("Payload", payload)) };

        var table = Run(query, rows);

        Assert.AreEqual("payload text", table[0][0]);
    }

    [TestMethod]
    public void ReadModifiers_MissingModifiersUseDatasourceDefaultsAndEmitNoContractDiagnostics()
    {
        const string query =
            "table RecordsShape { Name: string };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[] { Row(("Name", Encoding.UTF8.GetBytes("Default text"))) };
        var provider = new ReadModifiersSchemaProvider(rows);

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        var table = result.CompiledQuery!.Run();

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsFalse(result.Errors.Any(static item => item.Code == DiagnosticCode.MQ3071_SourceContractError));
        Assert.AreEqual("Default text", table[0][0]);
    }

    [TestMethod]
    public void ReadModifiers_LenientUnsupportedEncodingReportsWarningAndCompileSucceeds()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'windows-1250' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[] { Row(("Name", Encoding.UTF8.GetBytes("Lenient text"))) };

        var result = CompileWithDiagnostics(
            query,
            rows,
            ReadModifiersValidationMode.LenientUnsupportedModifiers);

        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);
        Assert.IsTrue(result.Succeeded);
        Assert.Contains("Encoding modifier 'windows-1250' is ignored by #readmods.records().", warning.Message);
        Assert.Contains("#readmods.records()", warning.Message);
        Assert.Contains("sourceCode=UnsupportedEncoding", warning.Message);
        Assert.Contains("column=Name", warning.Message);
        Assert.Contains("modifier=encoding", warning.Message);
        Assert.AreEqual(CreateExpectedSpan(query, "encoding 'windows-1250'"), warning.Span);
        Assert.AreEqual("Lenient text", result.CompiledQuery!.Run()[0][0]);
    }

    [TestMethod]
    public void ReadModifiers_StrictUtf8EncodingReportsErrorForDifferentEncoding()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'utf-16le' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[] { Row(("Name", Encoding.Unicode.GetBytes("Strict text"))) };

        var result = CompileWithDiagnostics(
            query,
            rows,
            ReadModifiersValidationMode.StrictUtf8Encoding);

        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Only utf-8 encoding is supported, but column 'Name' requested 'utf-16le'.", error.Message);
        Assert.Contains("sourceCode=UnsupportedEncoding", error.Message);
        Assert.Contains("column=Name", error.Message);
        Assert.Contains("modifier=encoding", error.Message);
        Assert.AreEqual(CreateExpectedSpan(query, "encoding 'utf-16le'"), error.Span);
    }

    [TestMethod]
    public void ReadModifiers_SourceKindConflictReportsColumnError()
    {
        const string query =
            "table RecordsShape { Amount: decimal };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Amount from Records()";
        var rows = new[] { Row(("Amount", "12.50")) };
        var sourceKinds = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = typeof(string)
        };

        var result = CompileWithDiagnostics(
            query,
            rows,
            ReadModifiersValidationMode.ValidateSourceKinds,
            sourceKinds);

        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        Assert.IsFalse(result.Succeeded);
        Assert.Contains("Source column 'Amount' is String, but the table contract declares Decimal.", error.Message);
        Assert.Contains("sourceCode=ColumnKindMismatch", error.Message);
        Assert.Contains("column=Amount", error.Message);
        Assert.AreEqual(CreateExpectedSpan(query, "Amount: decimal"), error.Span);
    }

    [TestMethod]
    public void ReadModifiers_InfoDiagnosticsAppearOnlyInPlanningText()
    {
        const string query =
            "table RecordsShape { Name: string };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[] { Row(("Name", "Info text")) };
        var provider = new ReadModifiersSchemaProvider(rows, ReadModifiersValidationMode.ReportInfo);

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);
        var result = CompileWithDiagnostics(query, rows, ReadModifiersValidationMode.ReportInfo);

        Assert.IsFalse(result.Warnings.Any(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning));
        Assert.IsFalse(result.Errors.Any(item => item.Code == DiagnosticCode.MQ3071_SourceContractError));
        Assert.Contains("source contract diagnostic [DescribeSource]: Info ReadModifiersInfo", inspection.PlanningText);
        Assert.Contains("source contract diagnostic [TryPlanSource]: Info ReadModifiersInfo", inspection.PlanningText);
    }

    [TestMethod]
    public void ReadModifiers_DuplicateDescriptorAndPlanDiagnosticsAreDeduplicated()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'windows-1250' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[] { Row(("Name", Encoding.UTF8.GetBytes("Duplicate text"))) };

        var result = CompileWithDiagnostics(
            query,
            rows,
            ReadModifiersValidationMode.LenientUnsupportedModifiers);

        var warnings = result.Warnings
            .Where(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning)
            .ToArray();
        Assert.AreEqual(1, warnings.Length);
        Assert.Contains("origin=DescribeSource", warnings[0].Message);
    }

    [TestMethod]
    public void ReadModifiers_TwoAliasesUsingSameTableMapDiagnosticsToTableModifierSpan()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'windows-1250' };" +
            "couple #readmods.records with table RecordsShape as FirstRecords;" +
            "couple #readmods.records with table RecordsShape as SecondRecords;" +
            "select a.Name, b.Name from FirstRecords() a inner join SecondRecords() b on 1 = 1";
        var rows = new[] { Row(("Name", Encoding.UTF8.GetBytes("Alias text"))) };

        var result = CompileWithDiagnostics(
            query,
            rows,
            ReadModifiersValidationMode.LenientUnsupportedModifiers);

        var warnings = result.Warnings
            .Where(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning)
            .OrderBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();
        var expectedSpan = CreateExpectedSpan(query, "encoding 'windows-1250'");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(2, warnings.Length);
        Assert.IsTrue(warnings.Any(item => item.Message.Contains(" as a ", StringComparison.Ordinal)));
        Assert.IsTrue(warnings.Any(item => item.Message.Contains(" as b ", StringComparison.Ordinal)));
        Assert.IsTrue(warnings.All(item => item.Span == expectedSpan));
    }

    [TestMethod]
    public void ReadModifiers_SourceApisSeeSameModifierMap()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'utf-8' trim, Payload: string source codec 'base64' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name, Payload from Records()";
        var rows = new[]
        {
            Row(("Name", Encoding.UTF8.GetBytes("  API text  ")), ("Payload", "cGF5bG9hZA=="))
        };
        var provider = new ReadModifiersSchemaProvider(rows);

        var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run();

        Assert.AreEqual("API text", table[0][0]);
        Assert.AreEqual("payload", table[0][1]);
        AssertColumnModifier(provider.Recorder.GetTableColumns.Last(), "Name", ColumnReadModifiers.Encoding, "utf-8");
        AssertColumnModifier(provider.Recorder.GetTableColumns.Last(), "Name", ColumnReadModifiers.Trim, "true");
        AssertColumnModifier(provider.Recorder.DescriptorColumns.Single(), "Payload", "source.codec", "base64");
        AssertColumnRefModifier(provider.Recorder.PlanRequests.Single().RequiredColumns, "Name", ColumnReadModifiers.Encoding, "utf-8");
        AssertColumnRefModifier(provider.Recorder.PlanRequests.Single().RequiredColumns, "Payload", "source.codec", "base64");
        AssertColumnModifier(provider.Recorder.ExecutionColumns.Single(), "Name", ColumnReadModifiers.Trim, "true");
        AssertColumnModifier(provider.Recorder.ExecutionColumns.Single(), "Payload", "source.codec", "base64");
    }

    [TestMethod]
    public void ReadModifiers_RequiredColumnPruningKeepsOnlyRequestedColumnModifiers()
    {
        const string query =
            "table RecordsShape { Name: string encoding 'utf-8', Payload: string source codec 'base64', Amount: decimal culture 'pl-PL' };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "select Name from Records()";
        var rows = new[]
        {
            Row(("Name", Encoding.UTF8.GetBytes("Name only")), ("Payload", "cGF5bG9hZA=="), ("Amount", "1,20"))
        };
        var provider = new ReadModifiersSchemaProvider(rows);

        var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run();
        var request = provider.Recorder.PlanRequests.Single();

        Assert.AreEqual("Name only", table[0][0]);
        Assert.AreEqual(1, request.RequiredColumns.Count);
        Assert.AreEqual("Name", request.RequiredColumns[0].Name);
        Assert.AreEqual("utf-8", request.RequiredColumns[0].ReadModifiers[ColumnReadModifiers.Encoding]);
        Assert.IsFalse(request.RequiredColumns.Any(static column => column.Name == "Payload"));
        Assert.IsFalse(request.RequiredColumns.Any(static column => column.Name == "Amount"));
    }

    [TestMethod]
    public void ReadModifiers_CteJoinGroupingSortingAndPredicatesKeepModifiersBeforeSourceExecution()
    {
        const string query =
            "table RecordsShape { Id: int, Category: string encoding 'utf-8' trim, Amount: decimal culture 'pl-PL' trim };" +
            "couple #readmods.records with table RecordsShape as Records;" +
            "with filtered as (select Id, Category, Amount from Records() where Amount > 1) " +
            "select f.Category, Count(r.Id) from filtered f inner join Records() r on f.Category = r.Category " +
            "group by f.Category order by f.Category";
        var rows = new[]
        {
            Row(("Id", 1), ("Category", Encoding.UTF8.GetBytes(" A ")), ("Amount", " 1,20 ")),
            Row(("Id", 2), ("Category", Encoding.UTF8.GetBytes(" A ")), ("Amount", " 2,40 ")),
            Row(("Id", 3), ("Category", Encoding.UTF8.GetBytes(" B ")), ("Amount", " 3,50 "))
        };
        var provider = new ReadModifiersSchemaProvider(rows);

        var table = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run();
        var requiredColumns = provider.Recorder.PlanRequests.SelectMany(static request => request.RequiredColumns).ToArray();

        Assert.AreEqual("A", table[0][0]);
        Assert.AreEqual(4L, table[0][1]);
        Assert.AreEqual("B", table[1][0]);
        Assert.AreEqual(1L, table[1][1]);
        Assert.IsTrue(requiredColumns.Any(column =>
            column.Name == "Amount" &&
            column.ReadModifiers.TryGetValue(ColumnReadModifiers.Culture, out var culture) &&
            culture == "pl-PL"));
        Assert.IsTrue(requiredColumns
            .Where(static column => column.Name == "Category")
            .All(static column => column.ReadModifiers.ContainsKey(ColumnReadModifiers.Encoding)));
        Assert.IsTrue(provider.Recorder.ExecutionColumns
            .SelectMany(static columns => columns)
            .Where(static column => column.ColumnName == "Category")
            .All(static column => column.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim)));
    }

    [TestMethod]
    public void ReadModifiers_GeneratedMetadataKeysIncludeReadModifiers()
    {
        const string query =
            "table Utf8Shape { Name: string encoding 'utf-8' };" +
            "table Utf16Shape { Name: string encoding 'utf-16le' };" +
            "couple #readmods.records with table Utf8Shape as Utf8Records;" +
            "couple #readmods.records with table Utf16Shape as Utf16Records;" +
            "select a.Name, b.Name from Utf8Records() a inner join Utf16Records() b on 1 = 1";
        var provider = new ReadModifiersSchemaProvider([Row(("Name", Encoding.UTF8.GetBytes("Name")))]);

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            TestCompilationOptions);

        Assert.IsTrue(CountOccurrences(inspection.GeneratedCSharpCode, "\"utf-8\"") >= 1);
        Assert.IsTrue(CountOccurrences(inspection.GeneratedCSharpCode, "\"utf-16le\"") >= 1);
    }

    [TestMethod]
    public void ReadModifiers_PublicMetadataCopiesOriginalModifierDictionaries()
    {
        var modifiers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ColumnReadModifiers.Encoding] = "utf-8"
        };

        var schemaColumn = new SchemaColumn("Name", 0, typeof(string), modifiers);
        var sourceColumnRef = new SourceColumnRef("Name", modifiers);
        modifiers[ColumnReadModifiers.Encoding] = "utf-16le";
        modifiers[ColumnReadModifiers.Trim] = "true";

        Assert.AreEqual("utf-8", schemaColumn.ReadModifiers[ColumnReadModifiers.Encoding]);
        Assert.AreEqual("utf-8", sourceColumnRef.ReadModifiers[ColumnReadModifiers.Encoding]);
        Assert.IsFalse(schemaColumn.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim));
        Assert.IsFalse(sourceColumnRef.ReadModifiers.ContainsKey(ColumnReadModifiers.Trim));
    }

    private Musoq.Evaluator.Tables.Table Run(
        string query,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        return CreateAndRunVirtualMachine(
                query,
                schemaProvider: new ReadModifiersSchemaProvider(rows))
            .Run();
    }

    private BuildResult CompileWithDiagnostics(
        string query,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        ReadModifiersValidationMode validationMode,
        IReadOnlyDictionary<string, Type>? sourceKinds = null)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new ReadModifiersSchemaProvider(rows, validationMode, sourceKinds),
            LoggerResolver,
            TestCompilationOptions);
    }

    private static void AssertColumnModifier(
        IReadOnlyCollection<ISchemaColumn> columns,
        string columnName,
        string key,
        string value)
    {
        Assert.AreEqual(value, columns.Single(column => column.ColumnName == columnName).ReadModifiers[key]);
    }

    private static void AssertColumnRefModifier(
        IReadOnlyList<SourceColumnRef> columns,
        string columnName,
        string key,
        string value)
    {
        Assert.AreEqual(value, columns.Single(column => column.Name == columnName).ReadModifiers[key]);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var start = 0;

        while ((start = text.IndexOf(value, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += value.Length;
        }

        return count;
    }

    private static TextSpan CreateExpectedSpan(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, start);
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
