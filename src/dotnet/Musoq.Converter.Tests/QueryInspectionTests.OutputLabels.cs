using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    [DataRow("select v.Value from values { (Value: 7) } v", "Value")]
    [DataRow("select v.* from values { (Value: 7) } v", "Value")]
    [DataRow("select v.Value as Renamed from values { (Value: 7) } v", "Renamed")]
    [DataRow("select v.Value as [v.Value] from values { (Value: 7) } v", "v.Value")]
    [DataRow("select * rename (Value as [v.Value]) from values { (Value: 7) } v", "v.Value")]
    [DataRow("with data as (select v.Value as [v.Value] from values { (Value: 7) } v) select * from data d", "v.Value")]
    public void OutputLabels_ShouldRespectBoundNamesAndExplicitAliases(string query, string expectedLabel)
    {
        using var compiled = CompileForExecution(query);
        using var result = compiled.Run();

        Assert.AreEqual(expectedLabel, Inspect(query).LogicalPlan.OutputSchema.Columns.Single().Name);
        Assert.AreEqual(expectedLabel, result.Columns.Single().ColumnName);
        Assert.AreEqual(7, result[0].ReadValue(0));
    }

    [TestMethod]
    [DataRow("select l.Id, r.Id from values { (Id: 7) } l cross join values { (Id: 'seven') } r")]
    [DataRow("with data as (select v.Unused as Unused, v.Number as Id, v.Text as Id from values { (Unused: 0, Number: 7, Text: 'seven') } v) select * exclude (Unused) from data d")]
    [DataRow("select * from (select v.Number as Id, v.Text as Id from values { (Number: 7, Text: 'seven') } v) d")]
    public void DuplicateOutputLabels_ShouldPreserveDifferentTypesAndPositions(string query)
    {
        using var compiled = CompileForExecution(query);
        using var result = compiled.Run();
        var columns = result.Columns.ToArray();

        CollectionAssert.AreEqual(new[] { "Id", "Id" }, columns.Select(column => column.ColumnName).ToArray());
        Assert.AreEqual(typeof(int), columns[0].ColumnType);
        Assert.AreEqual(typeof(string), columns[1].ColumnType);
        Assert.AreEqual(7, result[0].ReadValue(0));
        Assert.AreEqual("seven", result[0].ReadValue(1));
    }

    [TestMethod]
    [DataRow("with data as (select v.First as Id, v.Second as Id from values { (First: 7, Second: 'seven') } v) select d.Id from data d")]
    [DataRow("select Id from values { (Id: 7) } l cross join values { (Id: 'seven') } r")]
    public void DuplicateColumnReference_ShouldReportAmbiguity(string query)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => CompileForExecution(query));

        StringAssert.Contains(exception.Message, "MQ3002", exception.ToString());
    }

    [TestMethod]
    [DataRow("select l.Id, r.Id from values { (Id: 7) } l cross join values { (Id: 'z'), (Id: 'a') } r order by r.Id")]
    [DataRow("with leftRows as (select Id from values { (Id: 7) } v), rightRows as (select Id from values { (Id: 'z'), (Id: 'a') } v) select l.Id, r.Id from leftRows l cross join rightRows r order by r.Id")]
    public void DuplicateOutputLabels_OrderByRightSource_ShouldUseRightColumn(string query)
    {
        using var compiled = CompileForExecution(query);
        using var result = compiled.Run();

        Assert.AreEqual(7, result[0].ReadValue(0));
        Assert.AreEqual("a", result[0].ReadValue(1));
        Assert.AreEqual(7, result[1].ReadValue(0));
        Assert.AreEqual("z", result[1].ReadValue(1));
    }

    [TestMethod]
    public void UnprojectedOrderColumn_WithSameLabelAsProjectedColumn_ShouldUseBoundSource()
    {
        const string query = "select l.Id from values { (Id: 2), (Id: 1) } l cross join values { (Id: 2), (Id: 1) } r order by r.Id, l.Id";
        using var compiled = CompileForExecution(query);
        using var result = compiled.Run();

        CollectionAssert.AreEqual(new object[] { 1, 2, 1, 2 }, result.Rows.Select(row => row.ReadValue(0)).ToArray());
    }

}
