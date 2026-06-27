using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(Node node)
    {
    }

    public void Visit(DescNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Type == DescForType.Query)
        {
            Nodes.Push(new DescNode(Nodes.Pop()));
            return;
        }

        var from = (SchemaFromNode)Nodes.Pop();
        Nodes.Push(new DescNode(from, node.Type, node.Column));
    }

    public void Visit(StarNode node)
    {
        BinaryOperationVisitorHelper.ProcessStarOperation(Nodes, node.Span);
    }

    public void Visit(FSlashNode node)
    {
        BinaryOperationVisitorHelper.ProcessFSlashOperation(Nodes, node.Span);
    }

    public void Visit(ModuloNode node)
    {
        BinaryOperationVisitorHelper.ProcessModuloOperation(Nodes, node.Span);
    }

    public void Visit(AddNode node)
    {
        BinaryOperationVisitorHelper.ProcessAddOperation(Nodes, node.Span);
    }

    public void Visit(HyphenNode node)
    {
        BinaryOperationVisitorHelper.ProcessHyphenOperation(Nodes, node.Span);
    }

    public void Visit(BitwiseAndNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseAndOperation(Nodes, node.Span);
    }

    public void Visit(BitwiseOrNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseOrOperation(Nodes, node.Span);
    }

    public void Visit(BitwiseXorNode node)
    {
        BinaryOperationVisitorHelper.ProcessBitwiseXorOperation(Nodes, node.Span);
    }

    public void Visit(LeftShiftNode node)
    {
        BinaryOperationVisitorHelper.ProcessLeftShiftOperation(Nodes, node.Span);
    }

    public void Visit(RightShiftNode node)
    {
        BinaryOperationVisitorHelper.ProcessRightShiftOperation(Nodes, node.Span);
    }

    public void Visit(CoalesceNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new CoalesceNode(left, right, node.ReturnType));
    }

    public void Visit(ArrayIndexNode node)
    {
        var index = Nodes.Pop();
        var array = Nodes.Pop();
        Nodes.Push(new ArrayIndexNode(array, index));
    }

    public void Visit(AndNode node)
    {
        LogicalOperationVisitorHelper.ProcessAndOperation(Nodes, QueryRewriteUtilities.RewriteNullableBoolExpressions);
    }

    public void Visit(OrNode node)
    {
        LogicalOperationVisitorHelper.ProcessOrOperation(Nodes, QueryRewriteUtilities.RewriteNullableBoolExpressions);
    }

    public void Visit(EqualityNode node)
    {
        ComparisonOperationVisitorHelper.ProcessEqualityOperation(Nodes);
    }

    public void Visit(IsDistinctFromNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new IsDistinctFromNode(left, right, node.IsNegated));
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
    }

    public void Visit(GreaterOrEqualNode node)
    {
        ComparisonOperationVisitorHelper.ProcessGreaterOrEqualOperation(Nodes);
    }

    public void Visit(LessOrEqualNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLessOrEqualOperation(Nodes);
    }

    public void Visit(GreaterNode node)
    {
        ComparisonOperationVisitorHelper.ProcessGreaterOperation(Nodes);
    }

    public void Visit(LessNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLessOperation(Nodes);
    }

    public void Visit(DiffNode node)
    {
        ComparisonOperationVisitorHelper.ProcessDiffOperation(Nodes);
    }

    public void Visit(NotNode node)
    {
        LogicalOperationVisitorHelper.ProcessNotOperation(Nodes);
    }

    public void Visit(LikeNode node)
    {
        ComparisonOperationVisitorHelper.ProcessLikeOperation(Nodes);
    }

    public void Visit(RLikeNode node)
    {
        ComparisonOperationVisitorHelper.ProcessRLikeOperation(Nodes);
    }

    public void Visit(InNode node)
    {
        LogicalOperationVisitorHelper.ProcessInOperation(Nodes);
    }

    public void Visit(CollectionInNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new CollectionInNode(left, right));
    }

    public void Visit(InQueryNode node)
    {
        throw new NotSupportedException("InQueryNode should have been rewritten to CTE before this visitor runs.");
    }

    public void Visit(ExistsQueryNode node) => throw new InvalidOperationException("ExistsQueryNode should have been rewritten to CTE before this visitor runs.");

    public void Visit(ScalarSubqueryNode node) => throw new InvalidOperationException("ScalarSubqueryNode should have been rewritten to CTE before this visitor runs.");

    /// <summary>
    ///     Desugars BETWEEN into: expression >= min AND expression <= max
    /// </summary>
    public void Visit(BetweenNode node)
    {
        var max = Nodes.Pop();
        var min = Nodes.Pop();
        var expression = Nodes.Pop();


        var greaterOrEqual = new GreaterOrEqualNode(expression, min);
        var lessOrEqual = new LessOrEqualNode(expression, max);
        var andNode = new AndNode(greaterOrEqual, lessOrEqual);

        Nodes.Push(andNode);
    }

    public void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new StringNode(node.Value, node.Span));
    }

    public void Visit(DecimalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new DecimalNode(node.Value, node.Span));
    }

    public void Visit(IntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IntegerNode(node.ObjValue, node.Span));
    }

    public void Visit(HexIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new HexIntegerNode(node.ObjValue, node.Span));
    }

    public void Visit(BinaryIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BinaryIntegerNode(node.ObjValue, node.Span));
    }

    public void Visit(OctalIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new OctalIntegerNode(node.ObjValue, node.Span));
    }

    public void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BooleanNode(node.Value, node.Span));
    }

    public void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new WordNode(node.Value, node.Span));
    }

    public void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new NullNode(node.ReturnType, node.Span));
    }

    public void Visit(ContainsNode node)
    {
        LogicalOperationVisitorHelper.ProcessContainsOperation(Nodes);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public void Visit(IsNullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        LogicalOperationVisitorHelper.ProcessIsNullOperation(Nodes, node.IsNegated);
    }

    public void Visit(RowPresenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RowPresenceNode(Nodes.Pop(), node.IsPresent));
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        VisitAccessMethod(node);
    }

    public void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span, node.IntendedTypeName));
    }

    public void Visit(AllColumnsNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        StarReplaceItemNode[]? rewrittenReplaceItems = null;
        if (node.ReplaceItems is { Length: > 0 })
        {
            rewrittenReplaceItems = new StarReplaceItemNode[node.ReplaceItems.Length];
            for (var i = node.ReplaceItems.Length - 1; i >= 0; i--)
            {
                var rewrittenExpr = Nodes.Pop();
                rewrittenReplaceItems[i] = new StarReplaceItemNode(rewrittenExpr, node.ReplaceItems[i].ColumnName);
            }
        }

        Nodes.Push(new AllColumnsNode(
            node.Alias,
            node.LikePattern,
            node.IsNotLike,
            node.ExcludeColumns,
            rewrittenReplaceItems ?? node.ReplaceItems,
            node.RenameItems));
    }

    public void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IdentifierNode(node.Name));
    }

    public void Visit(ParameterBlockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parameters = new ParameterDeclarationNode[node.Parameters.Length];

        for (var i = node.Parameters.Length - 1; i >= 0; --i)
            parameters[i] = (ParameterDeclarationNode)Nodes.Pop();

        Nodes.Push(new ParameterBlockNode(parameters, node.Span));
    }

    public void Visit(ParameterDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var defaultValue = node.HasDefaultValue ? Nodes.Pop() : null;
        Nodes.Push(new ParameterDeclarationNode(node.Name, node.TypeName, node.IsNullable, defaultValue, node.Span));
    }

    public void Visit(ParameterReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ParameterReferenceNode(node.Name, node.ReturnType, node.Span));
    }

    public void Visit(AccessObjectArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsColumnAccess)
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.ColumnType, node.TableAlias, node.IntendedTypeName));
        else
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
    }

    public void Visit(AccessObjectKeyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessObjectKeyNode(node.Token, node.PropertyInfo));
    }

    public void Visit(PropertyValueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo));
    }

    public void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var exp = Nodes.Pop();
        var root = Nodes.Pop();

        Nodes.Push(new DotNode(root, exp, node.IsTheMostInner, node.Name, exp.ReturnType, node.IntendedTypeName));
    }

    public void Visit(AccessCallChainNode node)
    {
    }
}
