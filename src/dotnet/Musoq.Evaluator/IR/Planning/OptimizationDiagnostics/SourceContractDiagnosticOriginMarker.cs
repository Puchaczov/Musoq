using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning.OptimizationDiagnostics;

internal static class SourceContractDiagnosticOriginMarker
{
    public static SourcePlanResult Mark(SourcePlanResult result, string origin)
    {
        return result.ContractDiagnostics.Count == 0
            ? result
            : result with { ContractDiagnostics = Mark(result.ContractDiagnostics, origin) };
    }

    public static SourcePlanResult Prepend(
        SourcePlanResult result,
        IReadOnlyList<SourceContractDiagnostic> diagnostics,
        string origin)
    {
        var marked = Mark(diagnostics, origin);
        return marked.Length == 0
            ? result
            : result with { ContractDiagnostics = marked.Concat(result.ContractDiagnostics).ToArray() };
    }

    private static SourceContractDiagnostic[] Mark(
        IReadOnlyList<SourceContractDiagnostic> diagnostics,
        string origin)
    {
        if (diagnostics.Count == 0)
            return [];

        var result = new SourceContractDiagnostic[diagnostics.Count];
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            result[index] = string.IsNullOrWhiteSpace(diagnostic.Origin)
                ? diagnostic with { Origin = origin }
                : diagnostic;
        }

        return result;
    }
}
