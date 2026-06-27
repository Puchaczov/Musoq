using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{

    #region SourceExecutionContext Tests

    [TestMethod]
    public void SourceExecutionContext_Constructor_SetsProperties()
    {
        ISchemaColumn[] columns = [new SchemaColumn("col1", 0, typeof(string))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );

        Assert.AreEqual("queryId", ctx.QueryId);
        Assert.IsFalse(ctx.EndWorkToken.IsCancellationRequested);
    }

    [TestMethod]
    public void SourceExecutionContext_AllColumns_ReturnsColumns()
    {
        ISchemaColumn[] columns =
            [new SchemaColumn("Col1", 0, typeof(int)), new SchemaColumn("Col2", 1, typeof(string))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );

        Assert.HasCount(2, ctx.AllColumns);
    }

    [TestMethod]
    public void SourceExecutionContext_SourceRuntimeSettings_ReturnsDict()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var sourceRuntimeSettings = new Dictionary<string, string> { { "KEY", "VALUE" } };
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            sourceRuntimeSettings,
            NullLogger.Instance
        );

        Assert.AreEqual("VALUE", ctx.SourceRuntimeSettings["KEY"]);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceBegin_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );


        ctx.ReportDataSourceBegin("testSource");
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceRowsKnown_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );


        ctx.ReportDataSourceRowsKnown("testSource", 100);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceRowsRead_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );


        ctx.ReportDataSourceRowsRead("testSource", 50, 100);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceEnd_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );


        ctx.ReportDataSourceEnd("testSource", 100);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceBegin_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            (_, args) => received = args
        );

        ctx.ReportDataSourceBegin("testSource");

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.Begin, received.Phase);
        Assert.AreEqual("testSource", received.DataSourceName);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceRowsKnown_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            (_, args) => received = args
        );

        ctx.ReportDataSourceRowsKnown("testSource", 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.RowsKnown, received.Phase);
        Assert.AreEqual(100L, received.TotalRows);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceRowsRead_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            (_, args) => received = args
        );

        ctx.ReportDataSourceRowsRead("testSource", 50, 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.RowsRead, received.Phase);
        Assert.AreEqual(50L, received.RowsProcessed);
    }

    [TestMethod]
    public void SourceExecutionContext_ReportDataSourceEnd_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            (_, args) => received = args
        );

        ctx.ReportDataSourceEnd("testSource", 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.End, received.Phase);
    }

    #endregion

}
