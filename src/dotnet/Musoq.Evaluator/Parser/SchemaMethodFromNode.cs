using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaMethodFromNode(string alias, string schema, string method)
    : Musoq.Parser.Nodes.From.SchemaMethodFromNode(alias, schema, method, typeof(RowSource<>));
