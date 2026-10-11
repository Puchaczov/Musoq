using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv.Tests;

[TestClass]
public sealed class CsvSourceExecutionTests : CsvExampleTestBase
{
    [TestMethod]
    public void EmptyNativeQuery_ShouldConsumeNoRowsAndReportCompleteSourceProgress()
    {
        var events = new List<DataSourceEventArgs>();
        using var compiled = Compile("table Shape { Name: string }; couple #csv.file with table Shape as Rows; select Name from Rows()");
        compiled.DataSourceProgress += (_, args) => events.Add(args);
        using var table = compiled.Run();
        Assert.AreEqual(0, table.ToArray().Length);
        CollectionAssert.AreEqual(new[] { DataSourcePhase.Begin, DataSourcePhase.RowsKnown, DataSourcePhase.End },
            events.Select(item => item.Phase).ToArray());
    }
    [TestMethod]
    public void NullableNumericPredicate_ShouldExcludeMissingValuesAndPreserveTypedResults()
    {
        var path = WriteTempCsv("Name,Amount\nMissing,\nSmall,-2\nLarge,12.5\nZero,0\n");
        try
        {
            var query = "table Shape { Name: string, Amount: decimal? }; couple #csv.file with table Shape as Rows;" +
                $"select Name, Amount from Rows({SqlString(path)}, true) where Amount < 1 order by Name";
            using var compiled = Compile(query);
            using var table = compiled.Run();
            Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
            Assert.AreEqual(typeof(decimal?), table.Columns.ElementAt(1).ColumnType);
            CollectionAssert.AreEqual(new[] { ("Small", (decimal?)-2m), ("Zero", (decimal?)0m) },
                table.Select(row => (row.GetValue<string>(0), row.GetValue<decimal?>(1))).ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
