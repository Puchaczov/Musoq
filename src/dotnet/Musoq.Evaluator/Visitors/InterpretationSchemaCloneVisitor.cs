using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(FieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var valueValidation = node.ValueValidation != null ? (FieldValueValidationNode)Nodes.Pop() : null;
        var constraint = node.Constraint != null ? (FieldConstraintNode)Nodes.Pop() : null;
        var whenCondition = node.WhenCondition != null ? Nodes.Pop() : null;
        var atOffset = node.AtOffset != null ? Nodes.Pop() : null;
        var typeAnnotation = (TypeAnnotationNode)Nodes.Pop();

        Nodes.Push(new FieldDefinitionNode(
            node.Name,
            typeAnnotation,
            constraint,
            atOffset,
            whenCondition,
            valueValidation));
    }

    public override void Visit(ComputedFieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ComputedFieldNode(node.Name, Nodes.Pop()));
    }

    public override void Visit(FieldValueValidationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var values = new Node[node.Values.Count];
        for (var index = values.Length - 1; index >= 0; index--)
            values[index] = Nodes.Pop();

        Nodes.Push(new FieldValueValidationNode(node.Kind, values, node.IsByteList));
    }

    public override void Visit(TextFieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(node.FieldType == TextFieldType.Switch
            ? new TextFieldDefinitionNode(
                node.Name,
                node.SwitchCases
                    .Select(static switchCase => new TextSwitchCaseNode(switchCase.Pattern, switchCase.TypeName))
                    .ToArray())
            : new TextFieldDefinitionNode(
                node.Name,
                node.FieldType,
                node.PrimaryValue,
                node.SecondaryValue,
                node.Modifiers,
                node.EscapeCharacter,
                (string[])node.CaptureGroups.Clone()));
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
        Nodes.Push(new ByteArrayTypeNode(Nodes.Pop()));
    }

    public override void Visit(BinarySwitchTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var cases = new BinarySwitchCaseNode[node.Cases.Length];
        for (var index = cases.Length - 1; index >= 0; index--)
        {
            var branchType = (TypeAnnotationNode)Nodes.Pop();
            var caseValue = node.Cases[index].CaseValue != null ? Nodes.Pop() : null;
            cases[index] = new BinarySwitchCaseNode(caseValue, node.Cases[index].BranchAlias, branchType);
        }

        Nodes.Push(new BinarySwitchTypeNode(node.Selector, cases));
    }

    public override void Visit(StringTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new StringTypeNode(Nodes.Pop(), node.Encoding, node.Modifiers, node.AsTextSchemaName));
    }

    public override void Visit(SchemaReferenceTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SchemaReferenceTypeNode(node.SchemaName, (string[])node.TypeArguments.Clone()));
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
        var condition = node.Condition != null ? Nodes.Pop() : null;
        var elementType = (TypeAnnotationNode)Nodes.Pop();
        Nodes.Push(node.StopKind == RepeatUntilStopKind.EndOfInput
            ? RepeatUntilTypeNode.EndOfInput(elementType, node.FieldName)
            : new RepeatUntilTypeNode(elementType, condition!, node.FieldName));
    }

    public override void Visit(SubstreamTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var target = node.Target != null ? (TypeAnnotationNode)Nodes.Pop() : null;
        Nodes.Push(new SubstreamTypeNode(Nodes.Pop(), node.Mode, target));
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
