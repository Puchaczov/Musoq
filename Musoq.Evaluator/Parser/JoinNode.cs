using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinNode : Musoq.Parser.Nodes.From.JoinNode
{
    public JoinNode(JoinFromNode join)
        : base(join, typeof(RowSource))
    {
    }
}