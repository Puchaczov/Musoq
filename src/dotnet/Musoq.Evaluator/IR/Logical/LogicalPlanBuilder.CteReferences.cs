using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private OutputSchema BuildCteReferenceSchema(string cteName, string alias)
    {
        var definition = _cteDefinitions.LastOrDefault(definition =>
            string.Equals(definition.Name, cteName, StringComparison.OrdinalIgnoreCase));
        if (definition == null)
            return BuildOutputSchema(alias, cteName, cteName.ToScoreTable());

        return definition.Plan.OutputSchema;
    }
}
