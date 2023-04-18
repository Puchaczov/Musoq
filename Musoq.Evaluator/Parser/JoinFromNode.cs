using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinFromNode : Musoq.Parser.Nodes.From.JoinFromNode
{
    public JoinFromNode(FromNode joinFrom, FromNode from, Node expression, JoinType joinType) 
        : base(joinFrom, from, expression, joinType, typeof(RowSource))
    {
    }
}