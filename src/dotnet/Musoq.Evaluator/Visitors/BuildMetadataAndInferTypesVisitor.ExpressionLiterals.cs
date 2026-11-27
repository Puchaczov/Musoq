using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        PushSemanticNode(new StringNode(node.Value, node.Span));
    }

    public override void Visit(DecimalNode node) => VisitNumericLiteral(node);

    public override void Visit(IntegerNode node) => VisitNumericLiteral(node);

    public override void Visit(HexIntegerNode node) => VisitNumericLiteral(node);

    public override void Visit(BinaryIntegerNode node) => VisitNumericLiteral(node);

    public override void Visit(OctalIntegerNode node) => VisitNumericLiteral(node);

    private void VisitNumericLiteral(NumericLiteralNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(int).Assembly);
        // Source predicates retain their published text. Group and aggregate bindings distinguish typed operands.
        PushSemanticNode(node.CopyForBinding(node.HasSpan && _queryState.QueryPart != QueryPart.Where));
    }

    public override void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(bool).Assembly);
        PushSemanticNode(new BooleanNode(node.Value, node.Span));
    }

    public override void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        AddAssembly(typeof(string).Assembly);
        var word = node is AggregateIdentifierNode aggregateIdentifier
            ? new AggregateIdentifierNode(aggregateIdentifier.Value, aggregateIdentifier.DisplayName)
            : new WordNode(node.Value, node.Span);
        PushSemanticNode(word);
    }

    public override void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new NullNode(node.ReturnType, node.Span) { IntendedTypeName = node.IntendedTypeName });
    }
}
