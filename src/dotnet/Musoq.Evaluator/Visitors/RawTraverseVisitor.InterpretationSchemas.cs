using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;
public partial class RawTraverseVisitor<TExpressionVisitor>    where TExpressionVisitor : class, IExpressionVisitor

{
    public virtual void Visit(InterpretCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ParseCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(InterpretAtCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(TryInterpretCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(TryParseCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(PartialInterpretCallNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(BinarySchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(TextSchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldDefinitionNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ComputedFieldNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(TextFieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldConstraintNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(FieldValueValidationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(PrimitiveTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(ByteArrayTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(BinarySwitchTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(StringTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(SchemaReferenceTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(ArrayTypeNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(BitsTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AlignmentNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(RepeatUntilTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(SubstreamTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(InlineSchemaTypeNode node)
    {
        VisitChildrenThenNode(node);
    }
}
