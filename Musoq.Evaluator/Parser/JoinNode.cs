using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinNode : Musoq.Parser.Nodes.From.JoinNode
{
    public JoinNode(JoinFromNode joins) 
        : base(joins, typeof(RowSource))
    {
    }
}