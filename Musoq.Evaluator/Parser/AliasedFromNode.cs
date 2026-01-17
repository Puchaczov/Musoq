using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class AliasedFromNode : Musoq.Parser.Nodes.From.AliasedFromNode
{
    public AliasedFromNode(string identifier, ArgsListNode args, string alias, int inSourcePosition)
        : base(identifier, args, alias, typeof(RowSource), inSourcePosition)
    {
    }
}