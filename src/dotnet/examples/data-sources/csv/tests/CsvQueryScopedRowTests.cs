using Musoq.Tests.Common;
using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvQueryScopedRowTests : CsvExampleTestBase
{
    [TestMethod]
    public void Default_WhenCoupledShapeIsTyped_ShouldMaterializeQueryScopedRows()
    {
        var path = WriteTempCsv("Name,Amount\nAda,12\n");
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder);
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name, Amount from Rows({SqlString(path)}, true)";

        var inspection = Inspect(query, provider);
        var table = Run(query, provider);

        StringAssert.Contains(
            inspection.GeneratedCSharpCode,
            "QuerySourceInvocation<");
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, recorder.QueryRowSourceCalls.Count);
        Assert.AreEqual(0, recorder.RowSourceCalls.Count);
    }

    [TestMethod]
    public void RuntimePathParameter_WhenDefaulted_ShouldMatchCompileTimeLetPath()
    {
        var path = WriteTempCsv(
            "Customer,Amount\n" +
            "Ada,10\n" +
            "Ada,25\n" +
            "Grace,12\n");
        const string queryBody =
            "select c.Customer as Customer, Count(*) as Orders, Sum(c.Amount::int) as Total " +
            "from #csv.file($path, true, 0) c " +
            "group by c.Customer order by c.Customer";
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder);
        var parameterizedQuery = $"param(path: string = {SqlString(path)}) {queryBody}";
        using var parameterized = Compile(parameterizedQuery, provider);
        using var parameterizedTable = parameterized.Run();

        var letQuery = $"let path: string = {SqlString(path)} {queryBody}";
        using var letCompiled = Compile(letQuery);
        using var letTable = letCompiled.Run();

        Assert.AreEqual(2, parameterizedTable.Count);
        Assert.AreEqual(parameterizedTable.Count, letTable.Count);
        Assert.AreEqual("Ada", parameterizedTable[0].ReadValue(0));
        Assert.AreEqual(2L, Convert.ToInt64(parameterizedTable[0].ReadValue(1)));
        Assert.AreEqual(35L, Convert.ToInt64(parameterizedTable[0].ReadValue(2)));
        Assert.AreEqual("Grace", parameterizedTable[1].ReadValue(0));
        Assert.AreEqual(1L, Convert.ToInt64(parameterizedTable[1].ReadValue(1)));
        Assert.AreEqual(12L, Convert.ToInt64(parameterizedTable[1].ReadValue(2)));
        CollectionAssert.AreEqual(parameterizedTable[0].ReadValues(), letTable[0].ReadValues());
        CollectionAssert.AreEqual(parameterizedTable[1].ReadValues(), letTable[1].ReadValues());

        Assert.AreEqual(1, recorder.QueryRowSourceCalls.Count);
        Assert.AreEqual(0, recorder.RowSourceCalls.Count);
        var executionParameters = recorder.QueryRowSourceCalls.Single().Parameters;

        Assert.AreEqual(path.Replace('\\', '/'), executionParameters.Path);
        Assert.AreEqual(true, executionParameters.HasHeader);
        Assert.AreEqual(0, executionParameters.SkipRows);
    }

    [TestMethod]
    public void ExplicitLegacyOptOut_WhenCoupledShapeIsTyped_ShouldRequireNativeTransfer()
    {
        var path = WriteTempCsv("Name,Amount\nAda,12\nBob,3\n");
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder, enableQueryScopedRows: false);
        var query = "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name, Amount from Rows({SqlString(path)}, true) where Amount > 5 order by Name";
        var exception = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() => Run(query, provider));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, exception.PrimaryEnvelope.Code);
        Assert.Contains("native typed positional accessor", exception.Message);
        Assert.AreEqual(0, recorder.QueryRowSourceCalls.Count);
        Assert.AreEqual(0, recorder.RowSourceCalls.Count);
    }

    [TestMethod]
    public void OptIn_WhenDirectCsvHasHeader_ShouldExposeHeaderNamesAsNullableStrings()
    {
        var path = WriteTempCsv("Full Name,Status\nAda,Open\n");
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder, enableQueryScopedRows: true);
        var query =
            $"select d.Status from #csv.file({SqlString(path)}, true) d where d.Status = 'Open'";

        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Open", table[0].ReadValue(0));
    }

    [TestMethod]
    public void OptIn_WhenHeaderContainsSpecialNames_ShouldBindBracketQuotedColumns()
    {
        var path = WriteTempCsv("Full Name,naĂŻve-value\nAda,ready\n");
        var provider = new CsvSchemaProvider(enableQueryScopedRows: true);
        var query =
            $"select d.[Full Name], d.[naĂŻve-value] from #csv.file({SqlString(path)}, true) d";

        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0].ReadValue(0));
        Assert.AreEqual("ready", table[0].ReadValue(1));
    }

    [TestMethod]
    public void OptIn_WhenDirectCsvIsHeaderless_ShouldExposeColumnOrdinals()
    {
        var path = WriteTempCsv("Ada,Open\n");
        var provider = new CsvSchemaProvider(enableQueryScopedRows: true);
        var query =
            $"select d.Column0, d.Column1 from #csv.file({SqlString(path)}) d";

        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0].ReadValue(0));
        Assert.AreEqual("Open", table[0].ReadValue(1));
    }

}
