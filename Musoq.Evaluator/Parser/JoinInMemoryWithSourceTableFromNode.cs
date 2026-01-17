using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class JoinInMemoryWithSourceTableFromNode : Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode
{
    public JoinInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, Node expression,
        JoinType joinType)
        : base(inMemoryTableAlias, sourceTable, expression, joinType, typeof(RowSource))
    {
    }
}