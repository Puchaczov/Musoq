using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class CloneTraverseVisitor(IExpressionVisitor visitor) : RawTraverseVisitor<IExpressionVisitor>(visitor)
{
    public override void Visit(BinarySchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(TextSchemaNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(FieldDefinitionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.TypeAnnotation.Accept(this);
        node.AtOffset?.Accept(this);
        node.WhenCondition?.Accept(this);
        node.Constraint?.Accept(this);
        node.ValueValidation?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(FieldValueValidationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var value in node.Values)
            value.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(ByteArrayTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.SizeExpression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(BinarySwitchTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var switchCase in node.Cases)
        {
            switchCase.CaseValue?.Accept(this);
            switchCase.BranchType.Accept(this);
        }

        node.Accept(Visitor);
    }

    public override void Visit(StringTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.SizeExpression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(RepeatUntilTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.ElementType.Accept(this);
        node.Condition?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(SubstreamTypeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.SizeExpression.Accept(this);
        node.Target?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }
}
