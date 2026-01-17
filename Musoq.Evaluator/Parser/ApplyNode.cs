using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class ApplyNode : Musoq.Parser.Nodes.From.ApplyNode
{
    public ApplyNode(ApplyFromNode applies)
        : base(applies, typeof(RowSource))
    {
    }
}