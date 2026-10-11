using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    public void ExplicitSelectReference_ShouldCoexistWithWildcardDiscovery()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select *, a.Id as CopyId from #wildcard.rows() a where a.Name = 'Ada'",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Name", typeof(string)),
            ("Other", typeof(string)),
            ("Score", typeof(int)),
            ("CopyId", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, "Ada", "source-column", 10, 1]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void GroupByAndHavingReferenceExcludedColumn_ShouldRemainBindable()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * exclude (Score) from #wildcard.rows() a " +
                "group by a.Id, a.Name, a.Other having Sum(a.Score) > 0",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Name", typeof(string)),
            ("Other", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", "source-column"]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void WindowAndQualifyReferenceExcludedColumn_ShouldRemainBindable()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * exclude (Other), RowNumber() over (order by a.Score) as rn " +
                "from #wildcard.rows() a qualify RowNumber() over (order by a.Score) <= 1",
                recorder)
            .Run();

        var columnNames = table.Columns.Select(static column => column.ColumnName).ToArray();

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new[] { "Id", "Name", "Score", "rn" }, columnNames);
        Assert.AreEqual(1, table[0].ReadValues()[0]);
        Assert.AreEqual("Ada", table[0].ReadValues()[1]);
        Assert.AreEqual(10, table[0].ReadValues()[2]);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual(1L, table[0].ReadValues()[3]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }
}
