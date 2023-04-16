using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinSourcesTableFromNode : Musoq.Parser.Nodes.From.JoinSourcesTableFromNode
{
    public JoinSourcesTableFromNode(FromNode first, FromNode second, Node expression, JoinType joinType) 
        : base(first, second ,expression, joinType, typeof(RowSource))
    {
    }
}