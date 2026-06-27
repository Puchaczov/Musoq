using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Planning;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Converter.Build;

internal static class SourceContractDiagnosticReporter
{
    public static void Report(
        PlanningResult planningResult,
        DiagnosticContext diagnosticContext)
    {
        ArgumentNullException.ThrowIfNull(planningResult);
        ArgumentNullException.ThrowIfNull(diagnosticContext);

        var reported = new HashSet<SourceContractDiagnosticKey>();
        foreach (var candidate in CollectDiagnostics(planningResult))
        {
            if (candidate.Diagnostic.Severity == SourceContractDiagnosticSeverity.Info)
                continue;

            var key = new SourceContractDiagnosticKey(
                candidate.SourceContextId,
                candidate.Diagnostic.Severity,
                candidate.Diagnostic.Code,
                candidate.Diagnostic.ColumnName,
                candidate.Diagnostic.ModifierKey,
                candidate.Diagnostic.Message);

            if (!reported.Add(key))
                continue;

            var message = CreateMessage(candidate.SourceContextId, candidate.SourcePlan, candidate.Diagnostic);
            var span = ResolveSpan(planningResult, candidate);
            if (candidate.Diagnostic.Severity == SourceContractDiagnosticSeverity.Error)
            {
                diagnosticContext.ReportError(
                    DiagnosticCode.MQ3071_SourceContractError,
                    message,
                    span);
                continue;
            }

            diagnosticContext.ReportWarning(
                DiagnosticCode.MQ5013_SourceContractWarning,
                message,
                span);
        }
    }

    private static TextSpan ResolveSpan(
        PlanningResult planningResult,
        SourceContractDiagnosticCandidate candidate)
    {
        if (!planningResult.Properties.SourceContractDiagnosticLocationsBySourceId.TryGetValue(
                candidate.SourceContextId,
                out var locations))
        {
            return TextSpan.Empty;
        }

        if (locations.TryGetModifierSpan(
                candidate.Diagnostic.ColumnName,
                candidate.Diagnostic.ModifierKey,
                out var modifierSpan))
        {
            return modifierSpan;
        }

        if (locations.TryGetColumnSpan(candidate.Diagnostic.ColumnName, out var columnSpan))
            return columnSpan;

        return TextSpan.Empty;
    }

    private static IEnumerable<SourceContractDiagnosticCandidate> CollectDiagnostics(PlanningResult planningResult)
    {
        foreach (var sourcePlan in planningResult.Properties.SourcePlanResultsBySourceId
                     .OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            foreach (var diagnostic in sourcePlan.Value.ContractDiagnostics
                         .OrderBy(static diagnostic => diagnostic.Origin, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.ColumnName, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.ModifierKey, StringComparer.Ordinal)
                         .ThenBy(static diagnostic => diagnostic.Message, StringComparer.Ordinal))
            {
                yield return new SourceContractDiagnosticCandidate(sourcePlan.Key, sourcePlan.Value, diagnostic);
            }
        }
    }

    private static string CreateMessage(
        string sourceContextId,
        SourcePlanResult sourcePlan,
        SourceContractDiagnostic diagnostic)
    {
        var details = new[]
            {
                string.IsNullOrWhiteSpace(diagnostic.Origin) ? null : $"origin={diagnostic.Origin}",
                string.IsNullOrWhiteSpace(diagnostic.Code) ? null : $"sourceCode={diagnostic.Code}",
                string.IsNullOrWhiteSpace(diagnostic.ColumnName) ? null : $"column={diagnostic.ColumnName}",
                string.IsNullOrWhiteSpace(diagnostic.ModifierKey) ? null : $"modifier={diagnostic.ModifierKey}"
            }
            .Where(static part => part != null)
            .ToArray();
        var detailText = details.Length == 0
            ? string.Empty
            : $" ({string.Join(", ", details)})";
        var severity = diagnostic.Severity.ToString().ToLowerInvariant();

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Source contract {severity} for {FormatSourceTarget(sourceContextId, sourcePlan.ExecutionPlan.Identity)}: {diagnostic.Message}{detailText}");
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

    private sealed record SourceContractDiagnosticCandidate(
        string SourceContextId,
        SourcePlanResult SourcePlan,
        SourceContractDiagnostic Diagnostic);

    private readonly record struct SourceContractDiagnosticKey(
        string SourceContextId,
        SourceContractDiagnosticSeverity Severity,
        string? Code,
        string? ColumnName,
        string? ModifierKey,
        string Message);
}
