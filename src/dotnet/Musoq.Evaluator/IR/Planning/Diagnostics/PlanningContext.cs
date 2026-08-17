using System.Collections.Generic;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal sealed partial record PlanningContext
{
    public IReadOnlyDictionary<SchemaFromNode, SourceContractDiagnosticLocationMap> SourceContractDiagnosticLocationsBySource { get; init; } =
        new Dictionary<SchemaFromNode, SourceContractDiagnosticLocationMap>();
}
