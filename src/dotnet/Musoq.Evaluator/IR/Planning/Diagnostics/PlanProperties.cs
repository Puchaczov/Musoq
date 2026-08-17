using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial record PlanProperties
{
    public IReadOnlyDictionary<string, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsBySourceId { get; init; } =
        new Dictionary<string, SourceContractDiagnosticLocationMap>(StringComparer.Ordinal);
}
