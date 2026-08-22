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
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)),
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
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)));
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
        CollectionAssert.AreEqual(new[] { "a.Id", "a.Name", "a.Score", "rn" }, columnNames);
        Assert.AreEqual(1, table[0].Values[0]);
        Assert.AreEqual("Ada", table[0].Values[1]);
        Assert.AreEqual(10, table[0].Values[2]);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(3).ColumnType);
        Assert.AreEqual(1L, table[0].Values[3]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }
}
