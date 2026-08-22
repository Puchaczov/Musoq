using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    public void QualifiedStarOnJoin_ShouldDiscoverOnlyTheQualifiedSourceCompletely()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select a.* from #wildcard.rows() a inner join #wildcard.rows() b on a.Id = b.Id",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", "source-column", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request => request.SequenceEqual(new[] { "Id" })),
            "The non-wildcard source should retain its explicit join-column request.");
    }

    [TestMethod]
    public void BareStarOnJoin_ShouldDiscoverEveryCurrentSourceCompletely()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * from #wildcard.rows() a inner join #wildcard.rows() b on a.Id = b.Id",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)),
            ("b.Id", typeof(int)),
            ("b.Name", typeof(string)),
            ("b.Other", typeof(string)),
            ("b.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [1, "Ada", "source-column", 10, 1, "Ada", "source-column", 10]);
        Assert.IsGreaterThanOrEqualTo(2, recorder.MetadataRequests.Count(static request => request.Length == 0));
    }

    [TestMethod]
    public void CteStarBoundary_ShouldRestoreOuterProjectionScope()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "with source_rows as (" +
                "select * from #wildcard.rows() inner_source where inner_source.Id > 0) " +
                "select * exclude (Other) from source_rows outer_source where outer_source.Name = 'Ada'",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("outer_source.Id", typeof(int)),
            ("outer_source.Name", typeof(string)),
            ("outer_source.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void DerivedQueryBoundary_ShouldKeepInnerWildcardDiscoveryLocal()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select derived.* exclude (Other) from (" +
                "select * from #wildcard.rows() inner_source where inner_source.Id > 0) derived",
                recorder)
            .Run();

        var names = table.Columns.Select(static column => column.ColumnName).ToArray();

        Assert.AreEqual(1, table.Count);
        CollectionAssert.AreEqual(new[] { "derived.Id", "derived.Name", "derived.Score" }, names);
        CollectionAssert.AreEqual(new object?[] { 1, "Ada", 10 }, table[0].Values);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void SiblingCtesReusingAliases_ShouldKeepWildcardOwnershipInEachCte()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "with first_rows as (" +
                "select Name from #wildcard.rows() a where a.Id > 0), " +
                "second_rows as (" +
                "select * from #wildcard.rows() a where a.Score > 0) " +
                "select * from first_rows first_alias",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(table, ("first_alias.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Ada"]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request =>
                request.Length == 2 &&
                request.Contains("Id", StringComparer.OrdinalIgnoreCase) &&
                request.Contains("Name", StringComparer.OrdinalIgnoreCase)),
            "The sibling CTE without a wildcard should retain only its explicit Id and Name requests.");
    }

    [TestMethod]
    public void SetBranches_ShouldDiscoverEachBranchWithoutSharingAliases()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * from #wildcard.rows() branch_a where branch_a.Id > 0 " +
                "union all " +
                "select * from #wildcard.rows() branch_a where branch_a.Score > 0",
                recorder)
            .Run();

        Assert.AreEqual(2, table.Count);
        CollectionAssert.AreEqual(
            new[] { "branch_a.Id", "branch_a.Name", "branch_a.Other", "branch_a.Score" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [1, "Ada", "source-column", 10],
            [1, "Ada", "source-column", 10]);
        Assert.IsGreaterThanOrEqualTo(2, recorder.MetadataRequests.Count(static request => request.Length == 0));
    }

    [TestMethod]
    public void CorrelatedSubquery_ShouldKeepOuterWildcardAndInnerExplicitColumnsSeparate()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * exclude (Other) from #wildcard.rows() outer_source " +
                "where outer_source.Id in (" +
                "select inner_source.Id from #wildcard.rows() inner_source " +
                "where inner_source.Name = 'Ada')",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("outer_source.Id", typeof(int)),
            ("outer_source.Name", typeof(string)),
            ("outer_source.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request => request.SequenceEqual(new[] { "Id", "Name" })),
            "The correlated subquery source should keep its explicit Id and Name requests.");
    }

    [TestMethod]
    public void ReusedAliasesAcrossNestedScopes_ShouldResolveTheInnermostSource()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select * exclude (Other) from #wildcard.rows() a " +
                "where a.Id in (" +
                "select a.Id from #wildcard.rows() a where a.Name = 'Ada')",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request =>
                request.Length == 2 &&
                request.Contains("Id", StringComparer.OrdinalIgnoreCase) &&
                request.Contains("Name", StringComparer.OrdinalIgnoreCase)),
            "The nested reused alias should retain its own explicit Id and Name requests.");
    }

    [TestMethod]
    public void QualifiedStarAcrossApply_ShouldNotBroadenTheAppliedSource()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var table = Compile(
                "select a.* from #wildcard.rows() a cross apply #wildcard.rows() b where b.Id > 0",
                recorder)
            .Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Id", typeof(int)),
            ("a.Name", typeof(string)),
            ("a.Other", typeof(string)),
            ("a.Score", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "Ada", "source-column", 10]);
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request => request.SequenceEqual(new[] { "Id" })),
            "The applied source should retain its explicit predicate-column request.");
    }

    [TestMethod]
    public void CorrelatedInnerQualifiedStar_ShouldNotBroadenTheOuterDatasource()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        _ = Compile(
            "select outer_source.Name from #wildcard.rows() outer_source " +
            "where exists (select outer_source.* from #wildcard.rows() inner_source " +
            "where inner_source.Id > 0)",
            recorder);

        Assert.IsTrue(
            recorder.MetadataRequests.Any(static request => request.SequenceEqual(new[] { "Name" })),
            "A qualified star in the inner correlated query must not broaden the outer source.");
    }
}
