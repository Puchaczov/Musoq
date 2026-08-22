using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests.Schema.Wildcard;

public sealed class ProjectionSensitiveWildcardSchemaProvider(
    ProjectionSensitiveWildcardRecorder recorder,
    bool queryScopedRowsEnabled = false) : ISchemaProvider
{
    public ISchema GetSchema(string schema)
    {
        if (!string.Equals(schema.TrimStart('#'), ProjectionSensitiveWildcardSchema.SchemaName,
                StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(schema);

        return new ProjectionSensitiveWildcardSchema(recorder, queryScopedRowsEnabled);
    }
}

public sealed class ProjectionSensitiveWildcardSchema(
    ProjectionSensitiveWildcardRecorder recorder,
    bool queryScopedRowsEnabled = false) : SchemaBase(SchemaName, CreateMethods()), IQueryScopedRowSourceSchema
{
    public const string SchemaName = "wildcard";
    public const string SourceName = "rows";

    public static ISchemaColumn[] FullColumns { get; } =
    [
        new SchemaColumn("Id", 0, typeof(int)),
        new SchemaColumn("Name", 1, typeof(string)),
        new SchemaColumn("Other", 2, typeof(string)),
        new SchemaColumn("Score", 3, typeof(int))
    ];

    private static object[] FullRow { get; } = [1, "Ada", "source-column", 10];

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        EnsureSourceName(name);
        var requestedColumns = metadataContext.AllColumns.ToArray();
        recorder.MetadataRequests.Add(requestedColumns.Select(static column => column.ColumnName).ToArray());
        recorder.MetadataContexts.Add(ProjectionSensitiveWildcardContextSnapshot.Create(metadataContext));

        var columns = requestedColumns.Length == 0
            ? FullColumns
            : FullColumns
                .Where(column => requestedColumns.Any(requested =>
                    string.Equals(requested.ColumnName, column.ColumnName, StringComparison.OrdinalIgnoreCase)))
                .Select((column, index) => (ISchemaColumn)new SchemaColumn(column.ColumnName, index, column.ColumnType))
                .ToArray();

        return new ProjectionSensitiveWildcardTable(columns);
    }

    public override SchemaMethodInfo[] GetRawConstructors(
        string methodName,
        SourceMetadataContext metadataContext)
    {
        return string.Equals(methodName, SourceName, StringComparison.OrdinalIgnoreCase)
            ? [new SchemaMethodInfo(methodName, SchemaConstructorInfo.Empty())]
            : [];
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        EnsureSourceName(name);
        return base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = queryScopedRowsEnabled
                ? SourceTransferCapabilities.QueryScopedRows
                : SourceTransferCapabilities.None
        };
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        EnsureSourceName(name);
        var requestedColumns = executionContext.AllColumns.ToArray();
        recorder.ExecutionRequests.Add(requestedColumns.Select(static column => column.ColumnName).ToArray());
        recorder.ExecutionContexts.Add(ProjectionSensitiveWildcardContextSnapshot.Create(executionContext));

        var row = FullRow.ToArray();

        return EnsureSourceType<T, object[]>(name, new ProjectionSensitiveWildcardRowSource([row]));
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        if (!queryScopedRowsEnabled)
            throw new InvalidOperationException("Query-scoped rows were not enabled for this fixture.");

        EnsureSourceName(name);
        recorder.QueryScopedExecutionContexts.Add(
            ProjectionSensitiveWildcardContextSnapshot.Create(request.ExecutionContext));
        recorder.QueryScopedShapes.Add(
            request.Shape.Fields
                .Select(static field => new ProjectionSensitiveWildcardShapeField(
                    field.Name,
                    field.SourceColumnIndex,
                    field.FieldType))
                .ToArray());

        return new ProjectionSensitiveWildcardQueryScopedRowSource<TRow, TMaterializer>(
            [FullRow.ToArray()],
            request.Shape.Fields);
    }

    private static void EnsureSourceName(string name)
    {
        if (!string.Equals(name, SourceName, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(name);
    }

    private static MethodsAggregator CreateMethods()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(manager);
    }
}

public sealed class ProjectionSensitiveWildcardRecorder
{
    public List<string[]> MetadataRequests { get; } = [];

    public List<string[]> ExecutionRequests { get; } = [];

    public List<ProjectionSensitiveWildcardContextSnapshot> MetadataContexts { get; } = [];

    public List<ProjectionSensitiveWildcardContextSnapshot> ExecutionContexts { get; } = [];

    public List<ProjectionSensitiveWildcardContextSnapshot> QueryScopedExecutionContexts { get; } = [];

    public List<ProjectionSensitiveWildcardShapeField[]> QueryScopedShapes { get; } = [];
}

public sealed record ProjectionSensitiveWildcardContextSnapshot(
    string QueryId,
    string[] Columns)
{
    public static ProjectionSensitiveWildcardContextSnapshot Create(SourceMetadataContext context)
    {
        return new ProjectionSensitiveWildcardContextSnapshot(
            context.QueryId,
            context.AllColumns.Select(static column => column.ColumnName).ToArray());
    }
}

public sealed record ProjectionSensitiveWildcardShapeField(
    string Name,
    int SourceColumnIndex,
    Type FieldType);

public sealed class ProjectionSensitiveWildcardTable(ISchemaColumn[] columns) : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } = columns;

    public SchemaTableMetadata Metadata { get; } = new(typeof(object[]));

    public ISchemaColumn? GetColumnByName(string name) =>
        Columns.FirstOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));

    public ISchemaColumn[] GetColumnsByName(string name) =>
        Columns.Where(column =>
                string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase))
            .ToArray();
}

public sealed class ProjectionSensitiveWildcardRowSource(IReadOnlyList<object[]> rows)
    : RowSourceBase<object[]>
{
    protected override void CollectChunks(IChunkWriter<object[]> writer) => writer.Write(rows);
}

public sealed class ProjectionSensitiveWildcardQueryScopedRowSource<TRow, TMaterializer>(
    IReadOnlyList<object[]> rows,
    IReadOnlyList<QueryRowField> fields) : RowSourceBase<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    protected override void CollectChunks(IChunkWriter<TRow> writer)
    {
        var materialized = new List<TRow>(rows.Count);
        foreach (var row in rows)
        {
            var reader = new ProjectionSensitiveWildcardQueryRowReader(row, fields);
            materialized.Add(TMaterializer.Materialize<ProjectionSensitiveWildcardQueryRowReader>(ref reader));
        }

        writer.Write(materialized);
    }
}

public ref struct ProjectionSensitiveWildcardQueryRowReader(
    object[] row,
    IReadOnlyList<QueryRowField> fields) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var value = row[fields[slot].SourceColumnIndex];
        return value is null ? default! : (T)value;
    }
}
