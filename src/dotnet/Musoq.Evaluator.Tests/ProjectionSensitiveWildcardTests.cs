using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class ProjectionSensitiveWildcardTests
{
    [TestMethod]
    public void StarExclude_WithPredicate_ShouldDiscoverAndProjectCompleteSchema()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var query = Compile(
            "select * exclude (Other) from #wildcard.rows() a where a.Id > 0",
            recorder);

        var table = query.Run();
        var columnNames = table.Columns.Select(static column => column.ColumnName).ToArray();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, columnNames.Length);
        Assert.IsTrue(columnNames.Any(static name => name.EndsWith("Id", StringComparison.Ordinal)));
        Assert.IsTrue(columnNames.Any(static name => name.EndsWith("Name", StringComparison.Ordinal)));
        Assert.IsTrue(columnNames.Any(static name => name.EndsWith("Score", StringComparison.Ordinal)));
        Assert.IsFalse(columnNames.Any(static name => name.EndsWith("Other", StringComparison.Ordinal)));
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    [TestMethod]
    public void QualifiedStar_WithPredicate_ShouldDiscoverAndProjectQualifiedSchema()
    {
        var recorder = new ProjectionSensitiveWildcardRecorder();
        var query = Compile(
            "select a.* exclude (Other) from #wildcard.rows() a where a.Id > 0",
            recorder);

        var table = query.Run();
        var columnNames = table.Columns.Select(static column => column.ColumnName).ToArray();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(3, columnNames.Length);
        Assert.IsFalse(columnNames.Any(static name => name.EndsWith("Other", StringComparison.Ordinal)));
        Assert.IsTrue(recorder.MetadataRequests.Any(static request => request.Length == 0));
    }

    private static CompiledQuery Compile(string query, ProjectionSensitiveWildcardRecorder recorder)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ProjectionSensitiveWildcardSchemaProvider(recorder),
            new TestsLoggerResolver());
    }
}
