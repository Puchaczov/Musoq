using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void SourceExecutionContext_Diagnostics_DefaultsToNoOp()
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

        using (ctx.Diagnostics.Measure("setup", SourceDiagnosticOperation.Setup))
        {
        }

        ctx.Diagnostics.AddRowsProduced(10);
        ctx.Diagnostics.AddBytesRead(256);
        ctx.Diagnostics.AddMetric("pages", 2);

        Assert.AreSame(SourceDiagnostics.None, ctx.Diagnostics);
        Assert.IsFalse(ctx.Diagnostics.IsEnabled);
    }

    [TestMethod]
    public void SourceExecutionContext_Diagnostics_WhenProvided_UsesSink()
    {
        ISchemaColumn[] columns = [new SchemaColumn("col1", 0, typeof(string))];
        var sink = new CapturingSourceDiagnosticsSink();
        var diagnostics = new SourceDiagnostics(sink);
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            sourceDiagnostics: diagnostics
        );

        using (ctx.Diagnostics.Measure("fetch", SourceDiagnosticOperation.Fetch))
        {
            Assert.AreEqual(1, sink.BeginMeasureCount);
        }

        ctx.Diagnostics.AddRowsProduced(7);
        ctx.Diagnostics.AddBytesRead(1024);
        ctx.Diagnostics.AddMetric("pages", 3);

        Assert.AreSame(diagnostics, ctx.Diagnostics);
        Assert.IsTrue(ctx.Diagnostics.IsEnabled);
        Assert.AreEqual(1, sink.EndMeasureCount);
        Assert.AreEqual(7, sink.RowsProduced);
        Assert.AreEqual(1024, sink.BytesRead);
        Assert.AreEqual(3, sink.Metrics["pages"]);
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_WhenSourceUsesCurrentPattern_ReceivesNoOpDiagnostics()
    {
        var schema = new DiagnosticsCompatibilitySchema();
        ISchemaColumn[] columns = [new SchemaColumn("Value", 0, typeof(int))];
        var ctx = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance
        );

        var source = schema.GetRowSource<DiagnosticsCompatibilityEntity>("items", ctx);
        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.HasCount(1, rows);
        Assert.AreEqual(42, rows[0].Value);
        Assert.IsNotNull(DiagnosticsCompatibilitySource.CapturedDiagnostics);
        Assert.IsFalse(DiagnosticsCompatibilitySource.CapturedDiagnostics!.IsEnabled);
    }

    private sealed class CapturingSourceDiagnosticsSink : ISourceDiagnosticsSink
    {
        public int BeginMeasureCount { get; private set; }

        public int EndMeasureCount { get; private set; }

        public long RowsProduced { get; private set; }

        public long BytesRead { get; private set; }

        public Dictionary<string, long> Metrics { get; } = new(StringComparer.Ordinal);

        public IDisposable Measure(string name, SourceDiagnosticOperation operation)
        {
            BeginMeasureCount++;
            return new CallbackDisposable(() => EndMeasureCount++);
        }

        public void AddRowsProduced(long count)
        {
            RowsProduced += count;
        }

        public void AddBytesRead(long bytes)
        {
            BytesRead += bytes;
        }

        public void AddMetric(string name, long value)
        {
            Metrics[name] = Metrics.GetValueOrDefault(name) + value;
        }
    }

    private sealed class CallbackDisposable(Action callback) : IDisposable
    {
        public void Dispose()
        {
            callback();
        }
    }

    private sealed class DiagnosticsCompatibilitySchema : SchemaBase
    {
        public DiagnosticsCompatibilitySchema()
            : base("diagnostics", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<DiagnosticsCompatibilityTable>("items");
            AddSource<DiagnosticsCompatibilitySource>("items");
        }
    }

    private sealed class DiagnosticsCompatibilityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = [new SchemaColumn("Value", 0, typeof(int))];

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; } = new(typeof(DiagnosticsCompatibilityEntity));
    }

    private sealed class DiagnosticsCompatibilitySource(SourceExecutionContext context)
        : RowSource<DiagnosticsCompatibilityEntity>
    {
        public static SourceDiagnostics? CapturedDiagnostics { get; private set; }

        public override IEnumerable<IReadOnlyList<DiagnosticsCompatibilityEntity>> Chunks
        {
            get
            {
                CapturedDiagnostics = context.Diagnostics;
                yield return [new DiagnosticsCompatibilityEntity(42)];
            }
        }
    }

    private sealed record DiagnosticsCompatibilityEntity(int Value);
}
