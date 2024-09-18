using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class AccessMethodFromNode : Musoq.Parser.Nodes.From.AccessMethodFromNode
{
    public AccessMethodFromNode(string alias, string sourceAlias, Musoq.Parser.Nodes.AccessMethodNode accessMethod) 
        : base(alias, sourceAlias, accessMethod, typeof(RowSource))
    {
    }
}