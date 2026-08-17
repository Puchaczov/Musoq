using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteStrategyDecision(
    IReadOnlyDictionary<string, CteDefinitionStrategyDecision> DefinitionsByName)
{
    public bool CanFuseReadOnce(string name)
    {
        return DefinitionsByName.TryGetValue(name, out var definition) && definition.CanFuseReadOnce;
    }
}
