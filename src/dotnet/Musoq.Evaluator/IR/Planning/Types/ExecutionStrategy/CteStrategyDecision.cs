using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteStrategyDecision(
    IReadOnlyDictionary<string, CteDefinitionStrategyDecision> DefinitionsByName)
{
    public bool CanFuseReadOnce(string name)
    {
        return DefinitionsByName.TryGetValue(name, out var definition) && definition.CanFuseReadOnce;
    }
}
