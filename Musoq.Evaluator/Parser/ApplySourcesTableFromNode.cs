using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplySourcesTableFromNode : Musoq.Parser.Nodes.From.ApplySourcesTableFromNode
{
    public ApplySourcesTableFromNode(FromNode first, FromNode second, ApplyType applyType) 
        : base(first, second, applyType, typeof(RowSource))
    {
    }
}