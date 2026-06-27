using System;

namespace Musoq.Schema.Diagnostics;

public static class DiagnosticChunkMetricNames
{
    public const string Prefix = "chunked";
    public const string ChunksProduced = "chunksProduced";
    public const string ChunksConsumed = "chunksConsumed";
    public const string RowsProduced = "rowsProduced";
    public const string RowsConsumed = "rowsConsumed";
    public const string ProducerWaitOnFullCount = "producerWaitOnFullCount";
    public const string ProducerWaitOnFullTicks = "producerWaitOnFullTicks";
    public const string ConsumerWaitOnEmptyCount = "consumerWaitOnEmptyCount";
    public const string ConsumerWaitOnEmptyTicks = "consumerWaitOnEmptyTicks";
    public const string QueueDepth = "queueDepth";
    public const string PeakBacklogInChunks = "peakBacklogInChunks";
    public const string ProducerExceptions = "producerExceptions";
    public const string ProducerAbandonedCount = "producerAbandonedCount";
    public const string ProducerAbandonedWaitTicks = "producerAbandonedWaitTicks";

    public static string ForSource(string sourceName, string metricName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        return $"{Prefix}.{sourceName}.{metricName}";
    }

    public static bool IsForSourceMetric(string fullMetricName, string metricName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullMetricName);
        ArgumentException.ThrowIfNullOrWhiteSpace(metricName);

        if (!fullMetricName.StartsWith(Prefix + ".", StringComparison.Ordinal))
            return false;

        var metricStart = fullMetricName.LastIndexOf('.');
        return metricStart >= 0 &&
               fullMetricName.AsSpan(metricStart + 1).Equals(metricName.AsSpan(), StringComparison.Ordinal);
    }
}
