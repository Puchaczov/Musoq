using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyFromNode : Musoq.Parser.Nodes.From.ApplyFromNode
{
    public ApplyFromNode(FromNode source, FromNode with, ApplyType applyType)
        : base(source, with, applyType, typeof(RowSource))
    {
    }
}
