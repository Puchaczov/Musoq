using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(EqualityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new EqualityNode(left, right), node,
            BinaryOperatorKind.Equality);
    }

    public override void Visit(IsDistinctFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new IsDistinctFromNode(left, right, node.IsNegated),
            node,
            node.IsNegated ? BinaryOperatorKind.Equality : BinaryOperatorKind.Inequality);
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterOrEqualNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(LessOrEqualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessOrEqualNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(GreaterNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new GreaterNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(LessNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new LessNode(left, right), node,
            BinaryOperatorKind.Relational, BinaryOperationContext.RelationalComparison);
    }

    public override void Visit(DiffNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitBinaryOperatorWithTypeConversion((left, right) => new DiffNode(left, right), node,
            BinaryOperatorKind.Inequality);
    }
}
