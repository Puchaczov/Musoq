using System;
using System.Collections;
using System.Collections.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Converter.Tests;

public sealed class QueryRowLifecycleSource<TRow, TMaterializer>(
    QueryRowLifecycleScenario scenario,
    QueryScopedRowSourceRequest request) : RowSource<TRow>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    public override IEnumerable<IReadOnlyList<TRow>> Chunks =>
        new QueryRowLifecycleChunks<TRow, TMaterializer>(scenario, request);
}

internal sealed class QueryRowLifecycleChunks<TRow, TMaterializer>(
    QueryRowLifecycleScenario scenario,
    QueryScopedRowSourceRequest request) : IEnumerable<IReadOnlyList<TRow>>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    public IEnumerator<IReadOnlyList<TRow>> GetEnumerator()
    {
        return new QueryRowLifecycleChunkEnumerator<TRow, TMaterializer>(scenario, request);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class QueryRowLifecycleChunkEnumerator<TRow, TMaterializer> : IEnumerator<IReadOnlyList<TRow>>
    where TMaterializer : struct, IQueryRowMaterializer<TRow>
{
    private readonly QueryRowLifecycleScenario _scenario;
    private readonly QueryScopedRowSourceRequest _request;
    private int _acceptedRows;
    private bool _begun;
    private bool _completed;
    private bool _disposed;
    private int _rawIndex = -1;

    public QueryRowLifecycleChunkEnumerator(
        QueryRowLifecycleScenario scenario,
        QueryScopedRowSourceRequest request)
    {
        _scenario = scenario;
        _request = request;
    }

    public IReadOnlyList<TRow> Current { get; private set; } = [];

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_disposed || _completed)
            return false;

        EnsureBegun();
        var chunk = new List<TRow>(Math.Max(1, _scenario.ChunkSize));

        try
        {
            while (chunk.Count < Math.Max(1, _scenario.ChunkSize))
            {
                if (HasReachedAcceptedTake())
                {
                    _completed = true;
                    break;
                }

                var nextIndex = _rawIndex + 1;
                if (nextIndex >= _scenario.Rows.Count)
                {
                    _completed = true;
                    break;
                }

                _rawIndex = nextIndex;
                var rawCount = _scenario.Recorder.RecordRawRow();
                _scenario.OnRawRowEnumerated?.Invoke(rawCount);
                _request.ExecutionContext.ReportDataSourceRowsRead(
                    "query-row-lifecycle",
                    rawCount,
                    _scenario.Rows.Count);
                _request.ExecutionContext.EndWorkToken.ThrowIfCancellationRequested();

                if (_rawIndex == _scenario.RawReadFailureIndex && _scenario.RawReadFailure != null)
                    throw _scenario.RawReadFailure;

                var raw = _scenario.Rows[_rawIndex];
                if (_request.ExecutionContext.Plan.AcceptedPredicate != null &&
                    _scenario.PushedPredicate != null &&
                    !_scenario.PushedPredicate(raw))
                {
                    _scenario.Recorder.RecordRejected();
                    continue;
                }

                _scenario.Recorder.RecordMaterializerCall();
                using var materialize = _request.ExecutionContext.Diagnostics.Measure(
                    "query-row.materialize",
                    SourceDiagnosticOperation.Transform);
                var row = Materialize(raw);
                chunk.Add(row);
                _acceptedRows++;
                _scenario.Recorder.RecordRowProduced();
                _request.ExecutionContext.Diagnostics.AddRowsProduced(1);
            }

            if (chunk.Count == 0)
                return false;

            Current = chunk;
            return true;
        }
        catch (OperationCanceledException)
        {
            _scenario.Recorder.Record(QueryRowLifecycleEvent.Cancellation);
            throw;
        }
        catch (Exception)
        {
            _scenario.Recorder.Record(QueryRowLifecycleEvent.Failure);
            throw;
        }
    }

    public void Reset() => throw new NotSupportedException();

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_begun)
        {
            _request.ExecutionContext.ReportDataSourceEnd(
                "query-row-lifecycle",
                _scenario.Recorder.RawRowsEnumerated);
            _scenario.Recorder.Record(QueryRowLifecycleEvent.End);
        }

        _scenario.Recorder.RecordDisposed();
    }

    private void EnsureBegun()
    {
        if (_begun)
            return;

        _begun = true;
        _scenario.Recorder.Record(QueryRowLifecycleEvent.Begin);
        _request.ExecutionContext.ReportDataSourceBegin("query-row-lifecycle");
        _request.ExecutionContext.ReportDataSourceRowsKnown("query-row-lifecycle", _scenario.Rows.Count);
    }

    private bool HasReachedAcceptedTake()
    {
        var take = _request.ExecutionContext.Plan.AcceptedTake;
        return take.HasValue && _acceptedRows >= take.Value;
    }

    private TRow Materialize(QueryRowLifecycleRawRow raw)
    {
        switch (_scenario.ReaderStyle)
        {
            case QueryRowLifecycleReaderStyle.Ordinal:
            {
                var reader = new QueryRowOrdinalReader(raw, _request.Shape.Fields, _scenario);
                return TMaterializer.Materialize<QueryRowOrdinalReader>(ref reader);
            }
            case QueryRowLifecycleReaderStyle.Property:
            {
                var reader = new QueryRowPropertyReader(raw, _request.Shape.Fields, _scenario);
                return TMaterializer.Materialize<QueryRowPropertyReader>(ref reader);
            }
            case QueryRowLifecycleReaderStyle.Path:
            {
                var reader = new QueryRowPathReader(raw, _request.Shape.Fields, _scenario);
                return TMaterializer.Materialize<QueryRowPathReader>(ref reader);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(_scenario.ReaderStyle));
        }
    }
}

public ref struct QueryRowOrdinalReader(
    QueryRowLifecycleRawRow raw,
    IReadOnlyList<QueryRowField> fields,
    QueryRowLifecycleScenario scenario) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var field = QueryRowReaderValue.GetField(fields, slot, scenario);
        return QueryRowReaderValue.Cast<T>(raw.OrdinalValues[field.SourceColumnIndex]);
    }
}

public ref struct QueryRowPropertyReader(
    QueryRowLifecycleRawRow raw,
    IReadOnlyList<QueryRowField> fields,
    QueryRowLifecycleScenario scenario) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var field = QueryRowReaderValue.GetField(fields, slot, scenario);
        var key = QueryRowReaderValue.PropertyName(field.SourceColumnIndex);
        return raw.Properties.TryGetValue(key, out var value)
            ? QueryRowReaderValue.Cast<T>(value)
            : default!;
    }
}

public ref struct QueryRowPathReader(
    QueryRowLifecycleRawRow raw,
    IReadOnlyList<QueryRowField> fields,
    QueryRowLifecycleScenario scenario) : IQuerySourceFieldReader
{
    public T Read<T>(int slot)
    {
        var field = QueryRowReaderValue.GetField(fields, slot, scenario);
        var path = QueryRowReaderValue.Path(field.SourceColumnIndex);
        return raw.Paths.TryGetValue(path, out var value)
            ? QueryRowReaderValue.Cast<T>(value)
            : default!;
    }
}

internal static class QueryRowReaderValue
{
    public static QueryRowField GetField(
        IReadOnlyList<QueryRowField> fields,
        int slot,
        QueryRowLifecycleScenario scenario)
    {
        var field = fields[slot];
        scenario.Recorder.RecordFieldRead();
        if (field.SourceColumnIndex == scenario.FieldReadFailureSourceOrdinal &&
            scenario.FieldReadFailure != null)
        {
            throw scenario.FieldReadFailure;
        }

        return field;
    }

    public static T Cast<T>(object? value)
    {
        return value == null ? default! : (T)value;
    }

    public static string PropertyName(int sourceOrdinal)
    {
        return sourceOrdinal switch
        {
            0 => "id",
            1 => "label",
            2 => "category",
            3 => "maybeNumber",
            4 => "maybeText",
            _ => throw new ArgumentOutOfRangeException(nameof(sourceOrdinal))
        };
    }

    public static string Path(int sourceOrdinal)
    {
        return sourceOrdinal switch
        {
            0 => "/row/@id",
            1 => "/row/label",
            2 => "/row/category",
            3 => "/row/maybe-number",
            4 => "/row/maybe-text",
            _ => throw new ArgumentOutOfRangeException(nameof(sourceOrdinal))
        };
    }
}
