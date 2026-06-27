using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(CreateTableNode node)
    {
    }

    public void Visit(CoupleNode node)
    {
    }

    public void Visit(StatementsArrayNode node)
    {
    }

    public void Visit(StatementNode node)
    {
    }

    public void Visit(BinarySchemaNode node)
    {
    }

    public void Visit(TextSchemaNode node)
    {
    }

    public void Visit(FieldDefinitionNode node)
    {
    }

    public void Visit(ComputedFieldNode node)
    {
    }

    public void Visit(TextFieldDefinitionNode node)
    {
    }

    public void Visit(FieldConstraintNode node)
    {
    }

    public void Visit(FieldValueValidationNode node)
    {
    }

    public void Visit(PrimitiveTypeNode node)
    {
    }

    public void Visit(ByteArrayTypeNode node)
    {
    }

    public void Visit(BinarySwitchTypeNode node)
    {
    }

    public void Visit(StringTypeNode node)
    {
    }

    public void Visit(SchemaReferenceTypeNode node)
    {
    }

    public void Visit(ArrayTypeNode node)
    {
    }

    public void Visit(BitsTypeNode node)
    {
    }

    public void Visit(AlignmentNode node)
    {
    }

    public void Visit(RepeatUntilTypeNode node)
    {
    }

    public void Visit(SubstreamTypeNode node)
    {
    }

    public void Visit(InlineSchemaTypeNode node)
    {
    }

    public void Visit(WindowFunctionNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(WindowSpecificationNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(WindowFrameNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(WindowFrameBoundNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(WindowDefinitionNode node)
    {
        Nodes.Push(node);
    }

    public void Visit(WindowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)Nodes.Pop();

        Nodes.Push(new WindowNode(definitions));
    }
}
