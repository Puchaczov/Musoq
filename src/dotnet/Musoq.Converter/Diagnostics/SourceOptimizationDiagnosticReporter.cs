using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Build;

internal static class SourceOptimizationDiagnosticReporter
{
    public static void Report(
        PlanningResult planningResult,
        DiagnosticContext diagnosticContext)
    {
        ArgumentNullException.ThrowIfNull(planningResult);
        ArgumentNullException.ThrowIfNull(diagnosticContext);

        var reported = new HashSet<SourceOptimizationDiagnosticKey>();
        foreach (var candidate in CollectWarnings(planningResult))
        {
            var key = new SourceOptimizationDiagnosticKey(
                candidate.SourceContextId,
                candidate.Diagnostic.Optimization,
                candidate.Diagnostic.Target,
                candidate.Diagnostic.Message);

            if (!reported.Add(key))
                continue;

            diagnosticContext.ReportWarning(
                DiagnosticCode.MQ5013_SourceContractWarning,
                CreateMessage(candidate.SourceContextId, candidate.SourcePlan, candidate.Diagnostic),
                TextSpan.Empty);
        }
    }

    private static IEnumerable<SourceOptimizationDiagnosticCandidate> CollectWarnings(PlanningResult planningResult)
    {
        foreach (var sourcePlan in planningResult.Properties.SourcePlanResultsBySourceId
                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            foreach (var diagnostic in sourcePlan.Value.Diagnostics
                         .Where(static diagnostic => diagnostic.Severity == OptimizationDiagnosticSeverity.Warning)
                         .OrderBy(static diagnostic => diagnostic.Optimization, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.Target, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal))
            {
                yield return new SourceOptimizationDiagnosticCandidate(sourcePlan.Key, sourcePlan.Value, diagnostic);
            }
        }
    }

    private static string CreateMessage(
        string sourceContextId,
        SourcePlanResult sourcePlan,
        OptimizationDiagnostic diagnostic)
    {
        var parts = new[]
            {
                string.IsNullOrWhiteSpace(diagnostic.Optimization) ? null : $"optimization={diagnostic.Optimization}",
                string.IsNullOrWhiteSpace(diagnostic.Target) ? null : $"target={diagnostic.Target}",
                string.IsNullOrWhiteSpace(diagnostic.Origin) ? null : $"origin={diagnostic.Origin}"
            }
            .Where(static part => part != null)
            .ToArray();
        var details = parts.Length == 0
            ? string.Empty
            : $" ({string.Join(", ", parts)})";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Source optimization warning for {FormatSourceTarget(sourceContextId, sourcePlan.ExecutionPlan.Identity)}: {diagnostic.Message}{details}");
    }

    private static string FormatSourceTarget(string sourceContextId, SourceIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(identity.SchemaName) || string.IsNullOrWhiteSpace(identity.MethodName))
            return sourceContextId;

        var alias = string.IsNullOrWhiteSpace(identity.Alias)
            ? string.Empty
            : $" as {identity.Alias}";
        var id = string.IsNullOrWhiteSpace(identity.SourceContextId)
            ? sourceContextId
            : identity.SourceContextId;

        return $"#{identity.SchemaName}.{identity.MethodName}(){alias} [{id}]";
    }

    private sealed record SourceOptimizationDiagnosticCandidate(
        string SourceContextId,
        SourcePlanResult SourcePlan,
        OptimizationDiagnostic Diagnostic);

    private readonly record struct SourceOptimizationDiagnosticKey(
        string SourceContextId,
        string? Optimization,
        string? Target,
        string Message);
}
