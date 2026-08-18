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
            "GetQueryScopedRowSource<");
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, recorder.QueryRowSourceCalls.Count);
        Assert.AreEqual(0, recorder.RowSourceCalls.Count);
    }

    [TestMethod]
    public void ExplicitLegacyOptOut_WhenCoupledShapeIsTyped_ShouldUseDeclaredCsvRows()
    {
        var path = WriteTempCsv("Name,Amount\nAda,12\nBob,3\n");
        var recorder = new CsvDataSourceApiRecorder();
        var provider = new CsvSchemaProvider(recorder, enableQueryScopedRows: false);
        var query =
            "table CsvShape { Name: string, Amount: int };" +
            "couple #csv.file with table CsvShape as Rows;" +
            $"select Name, Amount from Rows({SqlString(path)}, true) where Amount > 5 order by Name";

        var inspection = Inspect(query, provider);
        StringAssert.Contains(
            inspection.GeneratedCSharpCode,
            "GetRowSource<Musoq.Examples.DataSources.Csv.CsvRow>");
        Assert.IsFalse(inspection.GeneratedCSharpCode.Contains(
            "GetQueryScopedRowSource<",
            StringComparison.Ordinal));

        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual(12, table[0][1]);
        Assert.AreEqual(0, recorder.QueryRowSourceCalls.Count);
        Assert.AreEqual(1, recorder.RowSourceCalls.Count);
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
        Assert.AreEqual("Open", table[0][0]);
    }

    [TestMethod]
    public void OptIn_WhenHeaderContainsSpecialNames_ShouldBindBracketQuotedColumns()
    {
        var path = WriteTempCsv("Full Name,naïve-value\nAda,ready\n");
        var provider = new CsvSchemaProvider(enableQueryScopedRows: true);
        var query =
            $"select d.[Full Name], d.[naïve-value] from #csv.file({SqlString(path)}, true) d";

        var table = Run(query, provider);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("ready", table[0][1]);
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
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual("Open", table[0][1]);
    }

}
