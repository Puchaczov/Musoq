using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinsNode : Musoq.Parser.Nodes.From.JoinsNode
{
    public JoinsNode(JoinFromNode joins) 
        : base(joins, typeof(RowSource))
    {
    }
}