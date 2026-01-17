using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaFromNode(
    string schema,
    string method,
    ArgsListNode parameters,
    string alias,
    int inSourcePosition,
    bool hasExternallyProvidedTypes
)
    : Musoq.Parser.Nodes.From.SchemaFromNode(schema, method, parameters, alias, typeof(RowSource), inSourcePosition)
{
    private readonly string _positionalId = $"{alias}:{inSourcePosition}";

    public override string Id => _positionalId;

    public bool HasExternallyProvidedTypes { get; } = hasExternallyProvidedTypes;

    public override int GetHashCode()
    {
        return _positionalId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is SchemaFromNode schemaFromNode)
            return _positionalId == schemaFromNode._positionalId;

        return base.Equals(obj);
    }
}