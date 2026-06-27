using Musoq.Evaluator.IR.Bindings;
using ParserNullOrdering = Musoq.Parser.Nodes.NullOrdering;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private static NullOrdering ConvertNullOrdering(ParserNullOrdering nullOrdering)
    {
        return nullOrdering switch
        {
            ParserNullOrdering.First => NullOrdering.First,
            ParserNullOrdering.Last => NullOrdering.Last,
            _ => NullOrdering.Default
        };
    }
}
