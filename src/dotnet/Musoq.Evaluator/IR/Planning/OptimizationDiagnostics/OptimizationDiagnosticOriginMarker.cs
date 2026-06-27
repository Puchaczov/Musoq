using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Planning.OptimizationDiagnostics;

internal static class OptimizationDiagnosticOriginMarker
{
    public static SourcePlanResult Mark(SourcePlanResult result, string origin)
    {
        return result.Diagnostics.Count == 0
            ? result
            : result with { Diagnostics = Mark(result.Diagnostics, origin) };
    }

    public static SourcePlanResult Prepend(
        SourcePlanResult result,
        IReadOnlyList<OptimizationDiagnostic> diagnostics,
        string origin)
    {
        var marked = Mark(diagnostics, origin);
        return marked.Length == 0
            ? result
            : result with { Diagnostics = marked.Concat(result.Diagnostics).ToArray() };
    }

    private static OptimizationDiagnostic[] Mark(
        IReadOnlyList<OptimizationDiagnostic> diagnostics,
        string origin)
    {
        if (diagnostics.Count == 0)
            return [];

        var result = new OptimizationDiagnostic[diagnostics.Count];
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
