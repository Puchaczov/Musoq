using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(InterpretCallNode node)
    {
    }

    public virtual void Visit(ParseCallNode node)
    {
    }

    public virtual void Visit(InterpretAtCallNode node)
    {
    }

    public virtual void Visit(TryInterpretCallNode node)
    {
    }

    public virtual void Visit(TryParseCallNode node)
    {
    }

    public virtual void Visit(PartialInterpretCallNode node)
    {
    }

    public virtual void Visit(BinarySchemaNode node)
    {
    }

    public virtual void Visit(TextSchemaNode node)
    {
    }

    public virtual void Visit(FieldDefinitionNode node)
    {
    }

    public virtual void Visit(TextFieldDefinitionNode node)
    {
    }

    public virtual void Visit(ComputedFieldNode node)
    {
    }

    public virtual void Visit(FieldConstraintNode node)
    {
    }

    public virtual void Visit(FieldValueValidationNode node)
    {
    }

    public virtual void Visit(PrimitiveTypeNode node)
    {
    }

    public virtual void Visit(ByteArrayTypeNode node)
    {
    }

    public virtual void Visit(BinarySwitchTypeNode node)
    {
    }

    public virtual void Visit(StringTypeNode node)
    {
    }

    public virtual void Visit(SchemaReferenceTypeNode node)
    {
    }

    public virtual void Visit(ArrayTypeNode node)
    {
    }

    public virtual void Visit(BitsTypeNode node)
    {
    }

    public virtual void Visit(AlignmentNode node)
    {
    }

    public virtual void Visit(RepeatUntilTypeNode node)
    {
    }

    public virtual void Visit(InlineSchemaTypeNode node)
    {
    }

    public virtual void Visit(SubstreamTypeNode node)
    {
    }

}
