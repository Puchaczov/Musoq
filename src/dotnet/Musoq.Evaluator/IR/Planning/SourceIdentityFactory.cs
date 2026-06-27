using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceIdentityFactory
{
    public static SourceIdentity Create(SchemaFromNode source)
    {
        return new SourceIdentity(source.Schema, source.Method, source.Id, source.Alias);
    }
}
