using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class InMemoryGroupedFromNode : Musoq.Parser.Nodes.From.InMemoryGroupedFromNode
{
    public InMemoryGroupedFromNode(string alias)
        : base(alias, typeof(RowSource))
    {
    }
}
