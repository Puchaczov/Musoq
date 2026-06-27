using Musoq.Parser.Nodes;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract partial class NoOpExpressionVisitor : IExpressionVisitor
{
    public virtual void Visit(Node node)
    {
    }

    public virtual void Visit(DescNode node)
    {
    }

    public virtual void Visit(StarNode node)
    {
    }

    public virtual void Visit(FSlashNode node)
    {
    }

    public virtual void Visit(ModuloNode node)
    {
    }

    public virtual void Visit(AddNode node)
    {
    }

    public virtual void Visit(HyphenNode node)
    {
    }

    public virtual void Visit(AndNode node)
    {
    }

    public virtual void Visit(OrNode node)
    {
    }

    public virtual void Visit(ShortCircuitingNodeLeft node)
    {
    }

    public virtual void Visit(ShortCircuitingNodeRight node)
    {
    }

    public virtual void Visit(EqualityNode node)
    {
    }

    public virtual void Visit(GreaterOrEqualNode node)
    {
    }

    public virtual void Visit(LessOrEqualNode node)
    {
    }

    public virtual void Visit(GreaterNode node)
    {
    }

    public virtual void Visit(LessNode node)
    {
    }

    public virtual void Visit(DiffNode node)
    {
    }

    public virtual void Visit(NotNode node)
    {
    }

    public virtual void Visit(LikeNode node)
    {
    }

    public virtual void Visit(RLikeNode node)
    {
    }

    public virtual void Visit(InNode node)
    {
    }

    public virtual void Visit(BetweenNode node)
    {
    }

    public virtual void Visit(FieldNode node)
    {
    }

    public virtual void Visit(FieldOrderedNode node)
    {
    }

    public virtual void Visit(StringNode node)
    {
    }

    public virtual void Visit(DecimalNode node)
    {
    }

    public virtual void Visit(IntegerNode node)
    {
    }

    public virtual void Visit(HexIntegerNode node)
    {
    }

    public virtual void Visit(BinaryIntegerNode node)
    {
    }

    public virtual void Visit(OctalIntegerNode node)
    {
    }

    public virtual void Visit(BooleanNode node)
    {
    }

    public virtual void Visit(WordNode node)
    {
    }

    public virtual void Visit(NullNode node)
    {
    }

    public virtual void Visit(ParameterBlockNode node)
    {
    }

    public virtual void Visit(ParameterDeclarationNode node)
    {
    }

    public virtual void Visit(ParameterReferenceNode node)
    {
    }

    public virtual void Visit(ContainsNode node)
    {
    }

    public virtual void Visit(AccessMethodNode node)
    {
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
    }

    public virtual void Visit(IsNullNode node)
    {
    }

    public virtual void Visit(RowPresenceNode node)
    {
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public virtual void Visit(AccessColumnNode node)
    {
    }

    public virtual void Visit(AllColumnsNode node)
    {
    }

    public virtual void Visit(IdentifierNode node)
    {
    }

    public virtual void Visit(AccessObjectArrayNode node)
    {
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
    }

    public virtual void Visit(PropertyValueNode node)
    {
    }

    public virtual void Visit(DotNode node)
    {
    }

    public virtual void Visit(AccessCallChainNode node)
    {
    }

    public virtual void Visit(ArgsListNode node)
    {
    }

}
