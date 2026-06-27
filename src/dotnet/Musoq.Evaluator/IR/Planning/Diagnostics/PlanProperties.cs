using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Logical.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial record PlanProperties
{
    public IReadOnlyDictionary<string, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsBySourceId { get; init; } =
        new Dictionary<string, SourceContractDiagnosticLocationMap>(StringComparer.Ordinal);
}
