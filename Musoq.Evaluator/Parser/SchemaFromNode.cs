using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaFromNode : Musoq.Parser.Nodes.From.SchemaFromNode
{
    public SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias) 
        : base(schema, method, parameters, alias, typeof(RowSource))
    {
    }
}