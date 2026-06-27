using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.Diagnostics;

public static class QueryProfileTextPrinter
{
    public static string Print(QueryProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var builder = new StringBuilder();

        builder.AppendLine("Musoq query profile");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Total elapsed: {FormatDuration(snapshot.TotalElapsed)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Total source rows: {snapshot.Sources.Sum(source => source.RowsRead)}");

        if (snapshot.Sources.Count == 0)
        {
            builder.AppendLine("Sources: none");
        }
        else
        {
            builder.AppendLine("Sources:");

            foreach (var source in snapshot.Sources)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  Source: {source.Name}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Rows read: {source.RowsRead}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    First row latency: {FormatDuration(source.FirstRowLatency)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Last row time: {FormatDuration(source.LastRowTime)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    MoveNext wait{CreateEstimatedSuffix(source)}: {FormatDuration(source.MoveNextWaitTime)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Consumer gap{CreateEstimatedSuffix(source)}: {FormatDuration(source.ConsumerGapTime)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Rows produced: {source.RowsProduced}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Bytes read: {source.BytesRead}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Exceptions: {source.ExceptionCount}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Diagnosis: {source.Diagnosis}");

                if (source.ExceptionType != null)
                    builder.AppendLine(CultureInfo.InvariantCulture, $"    Last exception: {source.ExceptionType}");

                foreach (var metric in source.Metrics.OrderBy(static metric => metric.Key, StringComparer.Ordinal))
                    builder.AppendLine(CultureInfo.InvariantCulture, $"    Metric {metric.Key}: {metric.Value}");

                foreach (var operation in source.Operations)
                {
                    builder.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"    Operation {operation.Operation}/{operation.Name}: count={operation.Count}, elapsed={FormatDuration(operation.ElapsedTime)}");
                }
            }
        }

        if (snapshot.Operators.Count == 0)
        {
            builder.AppendLine("Operators: none");
        }
        else
        {
            builder.AppendLine("Operators:");

            foreach (var operation in snapshot.Operators)
            {
                if (operation.HasActualStats)
                {
                    builder.AppendLine(
                        CultureInfo.InvariantCulture,
                        $"  {operation.Id} {operation.Name}: input rows={operation.InputRows}, output rows={operation.OutputRows}, elapsed={FormatDuration(operation)}");

                    if (operation.ExceptionCount > 0)
                        builder.AppendLine(CultureInfo.InvariantCulture, $"    Exceptions: {operation.ExceptionCount}");
                }
                else
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"  {operation.Id} {operation.Name}: stats unavailable");
                }
            }
        }

        return builder.ToString();
    }

    private static string FormatDuration(TimeSpan? value) =>
        value.HasValue ? FormatDuration(value.Value) : "n/a";

    private static string FormatDuration(TimeSpan value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value.TotalMilliseconds:0.###} ms");

    private static string FormatDuration(OperatorProfileSnapshot operation) =>
        operation.HasElapsedTime ? FormatDuration(operation.ElapsedTime) : "n/a";

    private static string CreateEstimatedSuffix(SourceProfileSnapshot source) =>
        source.IsTimingEstimated ? " (estimated)" : string.Empty;
}
