using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Diagnostics;

public static class SourceProfileDiagnosisClassifier
{
    public static SourceProfileDiagnosis Classify(SourceProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return Classify(
            snapshot.RowsRead,
            snapshot.MoveNextWaitTime,
            snapshot.ConsumerGapTime,
            snapshot.ExceptionCount,
            snapshot.Metrics);
    }

    public static SourceProfileDiagnosis Classify(
        long rowsRead,
        TimeSpan moveNextWaitTime,
        TimeSpan consumerGapTime,
        int exceptionCount = 0,
        IReadOnlyDictionary<string, long>? metrics = null)
    {
        if (rowsRead <= 0 || exceptionCount > 0)
            return SourceProfileDiagnosis.Unknown;

        if (TryClassifyChunkedBackpressure(metrics, out var diagnosis))
            return diagnosis;

        var waitTicks = Math.Max(0, moveNextWaitTime.Ticks);
        var gapTicks = Math.Max(0, consumerGapTime.Ticks);

        if (waitTicks == 0 && gapTicks == 0)
            return SourceProfileDiagnosis.Unknown;

        if (waitTicks >= gapTicks * 2)
            return SourceProfileDiagnosis.SourceBound;

        if (gapTicks >= waitTicks * 2)
            return SourceProfileDiagnosis.EvaluatorBound;

        return SourceProfileDiagnosis.Balanced;
    }

    private static bool TryClassifyChunkedBackpressure(
        IReadOnlyDictionary<string, long>? metrics,
        out SourceProfileDiagnosis diagnosis)
    {
        diagnosis = SourceProfileDiagnosis.Unknown;

        if (metrics == null || metrics.Count == 0)
            return false;

        var producerWaits = SumMetric(metrics, DiagnosticChunkMetricNames.ProducerWaitOnFullCount);
        var consumerWaits = SumMetric(metrics, DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount);

        if (producerWaits == 0 && consumerWaits == 0)
            return false;

        if (consumerWaits >= producerWaits * 2 && consumerWaits > 0)
        {
            diagnosis = SourceProfileDiagnosis.SourceBound;
            return true;
        }

        if (producerWaits >= consumerWaits * 2 && producerWaits > 0)
        {
            diagnosis = SourceProfileDiagnosis.EvaluatorBound;
            return true;
        }

        diagnosis = SourceProfileDiagnosis.Balanced;
        return true;
    }

    private static long SumMetric(IReadOnlyDictionary<string, long> metrics, string metricName)
    {
        return metrics
            .Where(metric => DiagnosticChunkMetricNames.IsForSourceMetric(metric.Key, metricName))
            .Sum(metric => Math.Max(0, metric.Value));
    }
}
