using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.Diagnostics;

public static class ExplainAnalyzeTextPrinter
{
    public static string Print(string annotatedExecutionPlanText, QueryProfileSnapshot profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var builder = new StringBuilder();
        builder.AppendLine("Musoq explain analyze");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Total elapsed: {FormatDuration(profile.TotalElapsed)}");
        builder.AppendLine();
        builder.AppendLine("Execution plan:");

        foreach (var line in annotatedExecutionPlanText.SplitLines())
            builder.AppendLine(AnnotateLine(line, profile));

        builder.AppendLine();
        builder.AppendLine("Source boundary stats:");
        if (profile.Sources.Count == 0)
        {
            builder.AppendLine("  none");
        }
        else
        {
            foreach (var source in profile.Sources)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  Source: {source.Name}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Rows read: {source.RowsRead}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    MoveNext wait{CreateEstimatedSuffix(source)}: {FormatDuration(source.MoveNextWaitTime)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Consumer gap{CreateEstimatedSuffix(source)}: {FormatDuration(source.ConsumerGapTime)}");
                builder.AppendLine(CultureInfo.InvariantCulture, $"    Diagnosis: {source.Diagnosis}");
            }
        }

        return builder.ToString();
    }

    private static string AnnotateLine(string line, QueryProfileSnapshot profile)
    {
        if (string.IsNullOrWhiteSpace(line))
            return line;

        var trimmed = line.TrimStart();
        var operatorIdEnd = trimmed.IndexOf(']', StringComparison.Ordinal);
        if (!trimmed.StartsWith("[op", StringComparison.Ordinal) || operatorIdEnd < 0)
            return line;

        var operatorId = trimmed[1..operatorIdEnd];
        var snapshot = profile.Operators.FirstOrDefault(operation => operation.Id == operatorId);
        if (snapshot == null)
            return line;

        if (!snapshot.HasActualStats)
            return $"{line}  actual rows=n/a, elapsed=n/a";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{line}  actual rows={snapshot.OutputRows}, elapsed={FormatDuration(snapshot)}");
    }

    private static string FormatDuration(TimeSpan value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value.TotalMilliseconds:0.###} ms");

    private static string FormatDuration(OperatorProfileSnapshot operation) =>
        operation.HasElapsedTime ? FormatDuration(operation.ElapsedTime) : "n/a";

    private static string CreateEstimatedSuffix(SourceProfileSnapshot source) =>
        source.IsTimingEstimated ? " (estimated)" : string.Empty;

    private static string[] SplitLines(this string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
}
