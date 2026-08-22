using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    public void Like_WithPredicate_ShouldFilterCompleteDynamicSchema()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * like 'S%' from #wildcard.rows() a where a.Id > 0",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(table, ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void NotLike_WithOrderByOnExcludedColumn_ShouldRetainCompleteSchemaForBinding()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * not like 'O%' from #wildcard.rows() a order by a.Other",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void Exclude_WithPredicateOnExcludedColumn_ShouldKeepPredicateBindable()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * exclude (Other) from #wildcard.rows() a where a.Other = 'source-column'",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void Replace_ShouldPreserveOutputOrderTypeAndValue()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * replace (Score + 1 as Score) from #wildcard.rows() a where a.Id > 0",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", "source-column", 11]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void Rename_ShouldPreserveOutputOrderTypeAndValue()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * rename (Name as DisplayName) from #wildcard.rows() a where a.Id > 0",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("DisplayName", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", "source-column", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void CompleteModifierChain_ShouldApplyInDeclaredOrder()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * like '%' exclude (Other) replace (Score + 1 as Score) rename (Name as DisplayName) " +
                "from #wildcard.rows() a where a.Other = 'source-column' order by a.Score desc",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("DisplayName", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 11]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }
}
