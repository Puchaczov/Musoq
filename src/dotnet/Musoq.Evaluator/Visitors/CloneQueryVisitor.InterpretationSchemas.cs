using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(ArrayIndexNode node)
    {
        var index = Nodes.Pop();
        var array = Nodes.Pop();
        Nodes.Push(new ArrayIndexNode(array, index));
    }

    public override void Visit(FieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var constraint = node.Constraint != null ? (FieldConstraintNode)Nodes.Pop() : null;
        var whenCondition = node.WhenCondition != null ? Nodes.Pop() : null;
        var atOffset = node.AtOffset != null ? Nodes.Pop() : null;

        Nodes.Push(new FieldDefinitionNode(
            node.Name,
            node.TypeAnnotation,
            constraint,
            atOffset,
            whenCondition,
            node.ValueValidation));
    }

    public override void Visit(ComputedFieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ComputedFieldNode(node.Name, Nodes.Pop()));
    }

    public override void Visit(FieldValueValidationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new FieldValueValidationNode(node.Kind, node.Values, node.IsByteList));
    }

    public override void Visit(TextFieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(node.FieldType == TextFieldType.Switch
            ? new TextFieldDefinitionNode(node.Name, node.SwitchCases)
            : new TextFieldDefinitionNode(
                node.Name,
                node.FieldType,
                node.PrimaryValue,
                node.SecondaryValue,
                node.Modifiers,
                node.EscapeCharacter,
                node.CaptureGroups));
    }

    public override void Visit(FieldConstraintNode node)
    {
        Nodes.Push(new FieldConstraintNode(Nodes.Pop()));
    }

    public override void Visit(PrimitiveTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new PrimitiveTypeNode(node.TypeName, node.Endianness));
    }

    public override void Visit(ByteArrayTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ByteArrayTypeNode(node.SizeExpression));
    }

    public override void Visit(BinarySwitchTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BinarySwitchTypeNode(node.Selector, node.Cases));
    }

    public override void Visit(StringTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new StringTypeNode(node.SizeExpression, node.Encoding, node.Modifiers, node.AsTextSchemaName));
    }

    public override void Visit(SchemaReferenceTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SchemaReferenceTypeNode(node.SchemaName, node.TypeArguments.ToArray()));
    }

    public override void Visit(ArrayTypeNode node)
    {
        var sizeExpression = Nodes.Pop();
        var elementType = (TypeAnnotationNode)Nodes.Pop();
        Nodes.Push(new ArrayTypeNode(elementType, sizeExpression));
    }

    public override void Visit(BitsTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BitsTypeNode(node.BitCount));
    }

    public override void Visit(AlignmentNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AlignmentNode(node.AlignmentBits));
    }

    public override void Visit(RepeatUntilTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(node.StopKind == RepeatUntilStopKind.EndOfInput
            ? RepeatUntilTypeNode.EndOfInput(node.ElementType, node.FieldName)
            : new RepeatUntilTypeNode(node.ElementType, node.Condition!, node.FieldName));
    }

    public override void Visit(SubstreamTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SubstreamTypeNode(node.SizeExpression, node.Mode, node.Target));
    }

    public override void Visit(InlineSchemaTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new SchemaFieldNode[node.Fields.Length];
        for (var index = fields.Length - 1; index >= 0; index--)
            fields[index] = (SchemaFieldNode)Nodes.Pop();

        Nodes.Push(new InlineSchemaTypeNode(fields));
    }
}
