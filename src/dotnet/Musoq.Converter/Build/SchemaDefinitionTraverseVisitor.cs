using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Traversal;

namespace Musoq.Converter.Build;

/// <summary>
///     Walks the AST and dispatches schema definition nodes to a <see cref="SchemaDefinitionVisitor"/>.
///     Traversal is delegated to the centralized <see cref="AstWalker"/> so node descent is shared
///     with the rest of the parser traversal infrastructure.
/// </summary>
public sealed class SchemaDefinitionTraverseVisitor
{
    private readonly SchemaDefinitionVisitor _visitor;

    public SchemaDefinitionTraverseVisitor(SchemaDefinitionVisitor visitor)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    public void Walk(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        new SchemaDefinitionWalker(_visitor).Walk(node);
    }

    private sealed class SchemaDefinitionWalker(SchemaDefinitionVisitor visitor) : AstWalker
    {
        protected override bool Enter(Node node)
        {
            switch (node)
            {
                case BinarySchemaNode binarySchema:
                    visitor.Visit(binarySchema);
                    return false;
                case TextSchemaNode textSchema:
                    visitor.Visit(textSchema);
                    return false;
                default:
                    return true;
            }
        }
    }
}
