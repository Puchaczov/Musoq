using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaMethodFromNode : Musoq.Parser.Nodes.From.SchemaMethodFromNode
{
    public SchemaMethodFromNode(string alias, string schema, string method)
        : base(alias, schema, method, typeof(RowSource))
    {
    }
}