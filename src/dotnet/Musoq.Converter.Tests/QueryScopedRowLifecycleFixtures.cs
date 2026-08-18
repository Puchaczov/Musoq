using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Tests;

public enum QueryRowLifecycleReaderStyle
{
    Ordinal,
    Property,
    Path
}

public enum QueryRowLifecycleEvent
{
    Open,
    Begin,
    Row,
    Rejected,
    Materialize,
    Failure,
    Cancellation,
    End,
    Dispose
}

public sealed class QueryRowLifecycleRecorder
{
    private readonly ConcurrentQueue<QueryRowLifecycleEvent> _events = [];
    private int _fieldReads;
    private int _materializerCalls;
    private int _openCalls;
    private int _rawRowsEnumerated;
    private int _rowsProduced;
    private int _rowsRejected;
    private int _sourcesDisposed;

    public int OpenCalls => Volatile.Read(ref _openCalls);

    public int RawRowsEnumerated => Volatile.Read(ref _rawRowsEnumerated);

    public int RowsRejected => Volatile.Read(ref _rowsRejected);

    public int MaterializerCalls => Volatile.Read(ref _materializerCalls);

    public int FieldReads => Volatile.Read(ref _fieldReads);

    public int RowsProduced => Volatile.Read(ref _rowsProduced);

    public int SourcesDisposed => Volatile.Read(ref _sourcesDisposed);

    public IReadOnlyList<QueryRowLifecycleEvent> Events => _events.ToArray();

    internal void RecordOpen()
    {
        Interlocked.Increment(ref _openCalls);
        Record(QueryRowLifecycleEvent.Open);
    }

    internal int RecordRawRow()
    {
        var count = Interlocked.Increment(ref _rawRowsEnumerated);
        Record(QueryRowLifecycleEvent.Row);
        return count;
    }

    internal void RecordRejected()
    {
        Interlocked.Increment(ref _rowsRejected);
        Record(QueryRowLifecycleEvent.Rejected);
    }

    internal void RecordMaterializerCall()
    {
        Interlocked.Increment(ref _materializerCalls);
        Record(QueryRowLifecycleEvent.Materialize);
    }

    internal void RecordFieldRead()
    {
        Interlocked.Increment(ref _fieldReads);
    }

    internal void RecordRowProduced()
    {
        Interlocked.Increment(ref _rowsProduced);
    }

    internal void RecordDisposed()
    {
        Interlocked.Increment(ref _sourcesDisposed);
        Record(QueryRowLifecycleEvent.Dispose);
    }

    internal void Record(QueryRowLifecycleEvent lifecycleEvent)
    {
        _events.Enqueue(lifecycleEvent);
    }
}

public sealed record QueryRowLifecycleScenario
{
    public IReadOnlyList<QueryRowLifecycleRawRow> Rows { get; init; } = [];

    public QueryRowLifecycleReaderStyle ReaderStyle { get; init; }

    public int ChunkSize { get; init; } = 2;

    public bool AcceptPredicate { get; init; }

    public bool AcceptTake { get; init; }

    public Func<QueryRowLifecycleRawRow, bool>? PushedPredicate { get; init; }

    public Exception? OpenFailure { get; init; }

    public bool ReturnNullSource { get; init; }

    public Exception? RawReadFailure { get; init; }

    public int RawReadFailureIndex { get; init; } = -1;

    public Exception? FieldReadFailure { get; init; }

    public int FieldReadFailureSourceOrdinal { get; init; } = -1;

    public Action<int>? OnRawRowEnumerated { get; init; }

    public QueryRowLifecycleRecorder Recorder { get; } = new();
}

public sealed class QueryRowLifecycleRawRow
{
    public QueryRowLifecycleRawRow(
        int id,
        string label,
        string category,
        int? maybeNumber,
        string? maybeText,
        bool omitNullableFields = false)
    {
        Id = id;
        OrdinalValues = [id, label, category, maybeNumber, maybeText];

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["id"] = id,
            ["label"] = label,
            ["category"] = category
        };
        var paths = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["/row/@id"] = id,
            ["/row/label"] = label,
            ["/row/category"] = category
        };

        if (!omitNullableFields)
        {
            properties["maybeNumber"] = maybeNumber;
            properties["maybeText"] = maybeText;
            paths["/row/maybe-number"] = maybeNumber;
            paths["/row/maybe-text"] = maybeText;
        }

        Properties = properties;
        Paths = paths;
    }

    public int Id { get; }

    public IReadOnlyList<object?> OrdinalValues { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public IReadOnlyDictionary<string, object?> Paths { get; }
}

public sealed class QueryRowLifecycleSchemaProvider(QueryRowLifecycleScenario scenario) : ISchemaProvider
{
    private readonly QueryRowLifecycleSchema _schema = new(scenario);

    public ISchema GetSchema(string schema)
    {
        if (string.Equals(schema, "lifecycle", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(schema, "#lifecycle", StringComparison.OrdinalIgnoreCase))
        {
            return _schema;
        }

        throw new NotSupportedException(schema);
    }
}

public sealed class QueryRowLifecycleSchema : SchemaBase, IQueryScopedRowSourceSchema
{
    private readonly QueryRowLifecycleScenario _scenario;

    public QueryRowLifecycleSchema(QueryRowLifecycleScenario scenario)
        : base("lifecycle", CreateLibrary())
    {
        _scenario = scenario;
        AddTable<QueryRowLifecycleTable>("rows");
        AddSource<QueryRowLifecycleLegacySource>("rows");
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        return base.DescribeSource(name, context, parameters) with
        {
            TransferCapabilities = SourceTransferCapabilities.QueryScopedRows
        };
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        var acceptedPredicate = _scenario.AcceptPredicate ? request.Predicate : null;
        var acceptedTake = _scenario.AcceptTake ? request.Take : null;
        var executionPlan = new SourceExecutionPlan
        {
            Identity = request.Identity,
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = acceptedPredicate,
            AcceptedTake = acceptedTake
        };

        return new SourcePlanResult
        {
            ExecutionPlan = executionPlan,
            AcceptedColumns = request.RequiredColumns,
            AcceptedPredicate = acceptedPredicate,
            ResidualPredicate = acceptedPredicate == null ? request.Predicate : null,
            ResidualOrderBy = request.OrderBy,
            ResidualSkip = request.Skip,
            AcceptedTake = acceptedTake,
            ResidualTake = acceptedTake.HasValue ? null : request.Take
        };
    }

    public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
        string name,
        QueryScopedRowSourceRequest request,
        params object?[] parameters)
        where TMaterializer : struct, IQueryRowMaterializer<TRow>
    {
        _scenario.Recorder.RecordOpen();
        if (_scenario.OpenFailure != null)
            throw _scenario.OpenFailure;
        if (_scenario.ReturnNullSource)
            return null!;

        return new QueryRowLifecycleSource<TRow, TMaterializer>(_scenario, request);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methods = new MethodsManager();
        methods.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methods);
    }
}

public sealed class QueryRowLifecycleTable : ISchemaTable
{
    public ISchemaColumn[] Columns { get; } =
    [
        new SchemaColumn(nameof(QueryRowLifecycleDeclaredRow.Id), 0, typeof(int)),
        new SchemaColumn(nameof(QueryRowLifecycleDeclaredRow.Label), 1, typeof(string)),
        new SchemaColumn(nameof(QueryRowLifecycleDeclaredRow.Category), 2, typeof(string)),
        new SchemaColumn(nameof(QueryRowLifecycleDeclaredRow.MaybeNumber), 3, typeof(int?)),
        new SchemaColumn(nameof(QueryRowLifecycleDeclaredRow.MaybeText), 4, typeof(string))
    ];

    public SchemaTableMetadata Metadata { get; } = new(typeof(QueryRowLifecycleDeclaredRow));

    public ISchemaColumn? GetColumnByName(string name)
    {
        return Columns.SingleOrDefault(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
    }

    public ISchemaColumn[] GetColumnsByName(string name)
    {
        return Columns.Where(column =>
            string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
    }
}

public sealed record QueryRowLifecycleDeclaredRow(
    int Id,
    string Label,
    string Category,
    int? MaybeNumber,
    string? MaybeText);

public sealed class QueryRowLifecycleLegacySource : RowSource<QueryRowLifecycleDeclaredRow>
{
    public override IEnumerable<IReadOnlyList<QueryRowLifecycleDeclaredRow>> Chunks => [];
}
