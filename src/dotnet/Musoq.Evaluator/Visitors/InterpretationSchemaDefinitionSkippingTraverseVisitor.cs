using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public abstract class InterpretationSchemaDefinitionSkippingTraverseVisitor<TExpressionVisitor>(TExpressionVisitor visitor)
    : RawTraverseVisitor<TExpressionVisitor>(visitor)
    where TExpressionVisitor : class, IExpressionVisitor
{
    public override void Visit(BinarySchemaNode node)
    {
    }

    public override void Visit(TextSchemaNode node)
    {
    }

    public override void Visit(FieldDefinitionNode node)
    {
    }

    public override void Visit(ComputedFieldNode node)
    {
    }

    public override void Visit(TextFieldDefinitionNode node)
    {
    }

    public override void Visit(FieldConstraintNode node)
    {
    }

    public override void Visit(FieldValueValidationNode node)
    {
    }

    public override void Visit(PrimitiveTypeNode node)
    {
    }

    public override void Visit(ByteArrayTypeNode node)
    {
    }

    public override void Visit(BinarySwitchTypeNode node)
    {
    }

    public override void Visit(StringTypeNode node)
    {
    }

    public override void Visit(SchemaReferenceTypeNode node)
    {
    }

    public override void Visit(ArrayTypeNode node)
    {
    }

    public override void Visit(BitsTypeNode node)
    {
    }

    public override void Visit(AlignmentNode node)
    {
    }

    public override void Visit(RepeatUntilTypeNode node)
    {
    }

    public override void Visit(SubstreamTypeNode node)
    {
    }

    public override void Visit(InlineSchemaTypeNode node)
    {
    }
}
