using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.DataSources;

internal sealed class DiagnosticChunkMetrics : IChunkPipelineMetrics
{
    private readonly object _gate = new();
    private readonly SourceDiagnostics _diagnostics;
    private readonly string _sourceName;
    private long _queueDepth;
    private long _peakBacklogInChunks;
    private Exception? _producerException;

    public DiagnosticChunkMetrics(SourceDiagnostics diagnostics, string sourceName)
    {
        _diagnostics = diagnostics;
        _sourceName = sourceName;
    }

    public void RecordProduced(int rows, int queueDepth)
    {
        _diagnostics.AddRowsProduced(rows);
        AddMetric(DiagnosticChunkMetricNames.ChunksProduced, 1);
        AddMetric(DiagnosticChunkMetricNames.RowsProduced, rows);
        RecordQueueDepth(queueDepth);
    }

    public void RecordChunkConsumed(int rows, int queueDepth)
    {
        AddMetric(DiagnosticChunkMetricNames.ChunksConsumed, 1);
        RecordRowsConsumed(rows);
        RecordQueueDepth(queueDepth);
    }

    public void RecordRowsConsumed(int rows)
    {
        if (rows > 0)
            AddMetric(DiagnosticChunkMetricNames.RowsConsumed, rows);
    }

    public void RecordProducerWaitOnFull()
    {
        AddMetric(DiagnosticChunkMetricNames.ProducerWaitOnFullCount, 1);
    }

    public void RecordProducerWaitOnFullElapsed(TimeSpan elapsed)
    {
        AddMetric(DiagnosticChunkMetricNames.ProducerWaitOnFullTicks, Math.Max(0, elapsed.Ticks));
    }

    public IDisposable MeasureProducerWaitOnFull()
    {
        return _diagnostics.Measure("chunked.producer.waitOnFull", SourceDiagnosticOperation.Produce);
    }

    public void RecordConsumerWaitOnEmpty()
    {
        AddMetric(DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount, 1);
    }

    public void RecordConsumerWaitOnEmptyElapsed(TimeSpan elapsed)
    {
        AddMetric(DiagnosticChunkMetricNames.ConsumerWaitOnEmptyTicks, Math.Max(0, elapsed.Ticks));
    }

    public IDisposable MeasureConsumerWaitOnEmpty()
    {
        return _diagnostics.Measure("chunked.consumer.waitOnEmpty", SourceDiagnosticOperation.Read);
    }

    public void RecordQueueDepth(int queueDepth)
    {
        lock (_gate)
        {
            var depth = Math.Max(0, queueDepth);
            var depthDelta = depth - _queueDepth;
            if (depthDelta != 0)
            {
                AddMetric(DiagnosticChunkMetricNames.QueueDepth, depthDelta);
                _queueDepth = depth;
            }

            if (depth <= _peakBacklogInChunks)
                return;

            AddMetric(DiagnosticChunkMetricNames.PeakBacklogInChunks, depth - _peakBacklogInChunks);
            _peakBacklogInChunks = depth;
        }
    }

    public void RecordProducerException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (_gate)
        {
            _producerException = exception;
        }

        AddMetric(DiagnosticChunkMetricNames.ProducerExceptions, 1);
    }

    public void RecordProducerAbandoned(TimeSpan waitElapsed)
    {
        AddMetric(DiagnosticChunkMetricNames.ProducerAbandonedCount, 1);
        AddMetric(DiagnosticChunkMetricNames.ProducerAbandonedWaitTicks, Math.Max(0, waitElapsed.Ticks));
    }

    public void ThrowIfProducerFailed()
    {
        Exception? exception;

        lock (_gate)
        {
            exception = _producerException;
        }

        if (exception != null)
            throw exception;
    }

    private void AddMetric(string metricName, long value)
    {
        _diagnostics.AddMetric(DiagnosticChunkMetricNames.ForSource(_sourceName, metricName), value);
    }
}
