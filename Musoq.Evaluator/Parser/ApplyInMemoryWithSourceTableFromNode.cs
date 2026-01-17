using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyInMemoryWithSourceTableFromNode : Musoq.Parser.Nodes.From.ApplyInMemoryWithSourceTableFromNode
{
    public ApplyInMemoryWithSourceTableFromNode(string inMemoryTableAlias, FromNode sourceTable, ApplyType applyType)
        : base(inMemoryTableAlias, sourceTable, applyType, typeof(RowSource))
    {
    }
}