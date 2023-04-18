using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ExpressionFromNode : Musoq.Parser.Nodes.From.ExpressionFromNode
{
    public ExpressionFromNode(FromNode fromNode) 
        : base(fromNode, typeof(RowSource))
    {
    }
}