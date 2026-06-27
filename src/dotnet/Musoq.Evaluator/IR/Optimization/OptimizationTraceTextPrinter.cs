using System;
using System.Linq;
using System.Text;

namespace Musoq.Evaluator.IR.Optimization;

internal static class OptimizationTraceTextPrinter
{
    public static string Print(OptimizationTrace? trace)
    {
        return Print(trace == null ? [] : [trace]);
    }

    public static string Print(params OptimizationTrace[] traces)
    {
        if (traces.Length == 0)
            return "OptimizerTrace [not produced]";

        var builder = new StringBuilder();
        builder.AppendLine("OptimizerTrace");

        if (traces.All(static trace => trace.Entries.Count == 0))
        {
            builder.AppendLine("  none");
            return builder.ToString().TrimEnd();
        }

        foreach (var entry in traces.SelectMany(static trace => trace.Entries))
        {
            builder
                .Append("  ")
                .Append(entry.Stage)
                .Append(" [")
                .Append(entry.PassName)
                .Append("] iteration ")
                .Append(entry.Iteration)
                .Append(" -> ")
                .Append(entry.Outcome)
                .Append(" changed: ")
                .Append(entry.IsChanged ? "yes" : "no")
                .Append(" - ")
                .AppendLine(entry.Reason);
        }

        return builder.ToString().TrimEnd();
    }

    public static string Append(string? existingText, OptimizationTrace? trace)
    {
        if (trace == null || trace.Entries.Count == 0)
            return string.IsNullOrWhiteSpace(existingText) ? Print(trace) : existingText;

        var traceText = Print(trace);
        if (string.IsNullOrWhiteSpace(existingText) ||
            string.Equals(existingText, "OptimizerTrace [not produced]", StringComparison.Ordinal))
        {
            return traceText;
        }

        if (string.Equals(existingText, "OptimizerTrace\n  none", StringComparison.Ordinal) ||
            string.Equals(existingText, "OptimizerTrace\r\n  none", StringComparison.Ordinal))
        {
            return traceText;
        }

        var traceLines = traceText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Skip(1);

        return existingText + Environment.NewLine + string.Join(Environment.NewLine, traceLines);
    }
}
