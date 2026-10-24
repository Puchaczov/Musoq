using Musoq.Evaluator.Helpers;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private IrNodes.CteRefNode CreateInMemoryCteRef(string alias, string? cteName = null,
        IntermediateSourceBinding? intermediate = null)
    {
        var schema = intermediate.HasValue ? BuildOutputSchema(alias, alias.ToScoreTable())
            : BuildCteReferenceSchema(cteName ?? alias, alias);
        return new IrNodes.CteRefNode(intermediate?.StorageName ?? cteName ?? alias, alias, schema)
        { IntermediateProducerId = intermediate?.ProducerId };
    }

}
