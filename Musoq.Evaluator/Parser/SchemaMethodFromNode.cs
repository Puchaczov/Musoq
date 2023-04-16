using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaMethodFromNode : Musoq.Parser.Nodes.From.SchemaMethodFromNode
{
    public SchemaMethodFromNode(string schema, string method) 
        : base(schema, method, typeof(RowSource))
    {
    }
}