using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(StarNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new StarNode(left, right), node,
            BinaryOperatorKind.Multiply, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(FSlashNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new FSlashNode(left, right), node,
            BinaryOperatorKind.Divide, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(ModuloNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new ModuloNode(left, right), node,
            BinaryOperatorKind.Modulo, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(AddNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = PopSemanticNode("Visit(AddNode) right");
        var left = PopSemanticNode("Visit(AddNode) left");

        var leftIsStringLiteral = left is WordNode;
        var rightIsStringLiteral = right is WordNode;

        if (leftIsStringLiteral || rightIsStringLiteral)
        {
            PushSemanticNode(left);
            PushSemanticNode(right);
            VisitBinaryOperatorWithSafePop((l, r) => new AddNode(l, r), VisitorOperationNames.VisitAddNode);
        }
        else
        {
            PushSemanticNode(left);
            PushSemanticNode(right);
            VisitBinaryOperatorWithTypeConversion((l, r) => new AddNode(l, r), node, BinaryOperatorKind.Add,
                BinaryOperationContext.ArithmeticOperation);
        }
    }

    public override void Visit(HyphenNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new HyphenNode(left, right), node,
            BinaryOperatorKind.Subtract, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(AndNode node)
    {
        var nodes = PopSemanticNodes(2, VisitorOperationNames.VisitAndNode);
        var left = nodes[0];
        var right = nodes[1];

        ValidateBooleanOperand(left, "AND", node);
        ValidateBooleanOperand(right, "AND", node);

        PushSemanticNode(new AndNode(left, right));
    }

    public override void Visit(OrNode node)
    {
        var nodes = PopSemanticNodes(2, VisitorOperationNames.VisitOrNode);
        var left = nodes[0];
        var right = nodes[1];

        ValidateBooleanOperand(left, "OR", node);
        ValidateBooleanOperand(right, "OR", node);

        PushSemanticNode(new OrNode(left, right));
    }

    public override void Visit(BitwiseAndNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseAndNode(left, right), node,
            BinaryOperatorKind.BitwiseAnd, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(BitwiseOrNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseOrNode(left, right), node,
            BinaryOperatorKind.BitwiseOr, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(BitwiseXorNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new BitwiseXorNode(left, right), node,
            BinaryOperatorKind.BitwiseXor, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(LeftShiftNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new LeftShiftNode(left, right), node,
            BinaryOperatorKind.LeftShift, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(RightShiftNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new RightShiftNode(left, right), node,
            BinaryOperatorKind.RightShift, BinaryOperationContext.ArithmeticOperation);
    }

    public override void Visit(ShortCircuitingNodeLeft node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var childNode = PopSemanticNode(VisitorOperationNames.VisitShortCircuitingNodeLeft);
        PushSemanticNode(new ShortCircuitingNodeLeft(childNode, node.UsedFor));
    }

    public override void Visit(ShortCircuitingNodeRight node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var childNode = PopSemanticNode(VisitorOperationNames.VisitShortCircuitingNodeRight);
        PushSemanticNode(new ShortCircuitingNodeRight(childNode, node.UsedFor));
    }
}
