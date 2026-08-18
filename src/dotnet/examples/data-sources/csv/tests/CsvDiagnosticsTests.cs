using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvDiagnosticsTests : CsvExampleTestBase
{
    [TestMethod]
    public void Compile_WhenDelimiterIsInvalid_ShouldReportContractError()
    {
        var path = WriteTempCsv("Name||Amount\nAda||1\n");
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)}, true, 0, '||')";

        var result = CompileWithDiagnostics(query);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        StringAssert.Contains(error.Message, "CsvInvalidDelimiter");
    }

    [TestMethod]
    public void Compile_WhenSourceIndexIsInvalid_ShouldMapDiagnosticToModifier()
    {
        var path = WriteTempCsv("Ada\n");
        var query =
            "table CsvShape { Name: string source index '-1' };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)})";

        var result = CompileWithDiagnostics(query);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        StringAssert.Contains(error.Message, "CsvInvalidSourceIndex");
        StringAssert.Contains(error.Message, "column=Name");
        StringAssert.Contains(error.Message, "modifier=source.index");
        Assert.AreEqual(CreateExpectedSpan(query, "source index '-1'"), error.Span);
    }

    [TestMethod]
    public void Compile_WhenSourceNameIsMissingFromHeader_ShouldMapDiagnosticToModifier()
    {
        var path = WriteTempCsv("FullName\nAda\n");
        var query =
            "table CsvShape { Name: string source name 'MissingName' };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)}, true)";

        var result = CompileWithDiagnostics(query);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        StringAssert.Contains(error.Message, "CsvMissingSourceName");
        StringAssert.Contains(error.Message, "column=Name");
        StringAssert.Contains(error.Message, "modifier=source.name");
        Assert.AreEqual(CreateExpectedSpan(query, "source name 'MissingName'"), error.Span);
    }

    [TestMethod]
    public void Compile_WhenModifierIsUnsupported_ShouldReportContractWarning()
    {
        var path = WriteTempCsv("Payload\nabc\n");
        var query =
            "table CsvShape { Payload: string source codec 'base64' };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Payload from Rows({SqlString(path)}, true)";

        var result = CompileWithDiagnostics(query);

        Assert.IsTrue(result.Succeeded);
        var warning = result.Warnings.Single(item => item.Code == DiagnosticCode.MQ5013_SourceContractWarning);
        StringAssert.Contains(warning.Message, "CsvUnsupportedModifier");
        StringAssert.Contains(warning.Message, "column=Payload");
        StringAssert.Contains(warning.Message, "modifier=source.codec");
        Assert.AreEqual(CreateExpectedSpan(query, "source codec 'base64'"), warning.Span);
    }

    [TestMethod]
    public void Compile_WhenStaticCsvHasMalformedQuotes_ShouldReportContractError()
    {
        var path = WriteTempCsv("Name\n\"Ada\n");
        var query =
            "table CsvShape { Name: string };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)}, true)";

        var result = CompileWithDiagnostics(query);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        StringAssert.Contains(error.Message, "CsvMalformedQuotes");
    }

    [TestMethod]
    public void Compile_WhenStaticCsvCannotSatisfySourceIndex_ShouldReportShapeMismatch()
    {
        var path = WriteTempCsv("Ada\n");
        var query =
            "table CsvShape { Name: string source index '1' };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)})";

        var result = CompileWithDiagnostics(query);

        Assert.IsFalse(result.Succeeded);
        var error = result.Errors.Single(item => item.Code == DiagnosticCode.MQ3071_SourceContractError);
        StringAssert.Contains(error.Message, "CsvShapeMismatch");
        StringAssert.Contains(error.Message, "column=Name");
        StringAssert.Contains(error.Message, "modifier=source.index");
        Assert.AreEqual(CreateExpectedSpan(query, "source index '1'"), error.Span);
    }

    [TestMethod]
    public void Inspect_WhenCsvPlannerRuns_ShouldIncludePlanningText()
    {
        var path = WriteTempCsv("Name,Amount\nAda,1\n");
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)}, true) where Amount > 0";

        var inspection = Inspect(query);

        StringAssert.Contains(inspection.PlanningText, "source plan accepted:");
        StringAssert.Contains(inspection.PlanningText, "predicate=yes");
    }

    [TestMethod]
    public void Inspect_WhenCsvQueryIsGenerated_ShouldUseTypedQueryRowAccess()
    {
        var path = WriteTempCsv("Name\nAda\n");
        var query =
            "table CsvShape { Name: string };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name from Rows({SqlString(path)}, true)";

        var inspection = Inspect(query, new CsvSchemaProvider(enableQueryScopedRows: true));

        StringAssert.Contains(inspection.GeneratedCSharpCode, "GetQueryScopedRowSource<");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "IQuerySourceFieldReader");
        StringAssert.Contains(inspection.GeneratedCSharpCode, "Read<string>(0)");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetRowSource<Musoq.Examples.DataSources.Csv.CsvRow>", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetRowSourceChunks", StringComparison.Ordinal));
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains("GetColumnValue", StringComparison.Ordinal));
    }

    private static TextSpan CreateExpectedSpan(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.AreNotEqual(-1, start);
        return new TextSpan(start, text.Length);
    }
}
