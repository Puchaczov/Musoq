using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal static class BinarySchemaNestedReference
{
    internal static SchemaReferenceTypeNode? Resolve(TypeAnnotationNode type) => type switch
    {
        SchemaReferenceTypeNode direct => direct,
        SubstreamTypeNode { Target: SchemaReferenceTypeNode nested } => nested,
        _ => null
    };
}
