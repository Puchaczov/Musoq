using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Logical.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceContractDiagnosticLocationPlanner
{
    public static PlanProperties WithLocations(
        PlanProperties properties,
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans)
    {
        return properties with
        {
            SourceContractDiagnosticLocationsBySourceId = Create(context, scans)
        };
    }

    private static Dictionary<string, SourceContractDiagnosticLocationMap> Create(
        PlanningContext context,
        IReadOnlyList<SchemaScanNode> scans)
    {
        var result = new Dictionary<string, SourceContractDiagnosticLocationMap>(StringComparer.Ordinal);

        if (context.SourceContractDiagnosticLocationsBySource.Count == 0)
            return result;

        foreach (var scan in scans)
        {
            if (string.IsNullOrWhiteSpace(scan.SourceContextId))
                continue;

            foreach (var location in context.SourceContractDiagnosticLocationsBySource)
            {
                if (!string.Equals(location.Key.Id, scan.SourceContextId, StringComparison.Ordinal))
                    continue;

                result[scan.SourceContextId] = location.Value;
                break;
            }
        }

        return result;
    }
}
