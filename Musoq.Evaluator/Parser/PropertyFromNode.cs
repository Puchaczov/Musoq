using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class PropertyFromNode : Musoq.Parser.Nodes.From.PropertyFromNode
{
    public PropertyFromNode(string alias, string sourceAlias, string propertyName) 
        : base(alias, sourceAlias, propertyName, typeof(RowSource))
    {
    }
}