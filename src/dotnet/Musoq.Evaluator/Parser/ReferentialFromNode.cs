using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ReferentialFromNode : Musoq.Parser.Nodes.From.ReferentialFromNode
{
    public ReferentialFromNode(string name, string alias)
        : base(name, alias, typeof(RowSource))
    {
    }
}
