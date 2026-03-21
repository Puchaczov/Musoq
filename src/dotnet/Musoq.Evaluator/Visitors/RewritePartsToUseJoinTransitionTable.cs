using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsToUseJoinTransitionTable(string alias = "") : CloneQueryVisitor
{
    public SelectNode ChangedSelect { get; private set; }

    public GroupByNode ChangedGroupBy { get; private set; }

    public WhereNode ChangedWhere { get; private set; }

    public OrderByNode ChangedOrderBy { get; private set; }

    public WindowNode ChangedWindow { get; private set; }

    public Node RewrittenNode => Nodes.Pop();

    public override void Visit(AccessColumnNode node)
    {
        base.Visit(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), alias, node.ReturnType,
            node.Span, node.IntendedTypeName));
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        if (node.IsColumnAccess)
        {
            var newColumnName = NamingHelper.ToColumnName(node.TableAlias, node.Token.Name);
            var newToken = new NumericAccessToken(newColumnName, node.Token.Index.ToString(), TextSpan.Empty);
            Nodes.Push(new AccessObjectArrayNode(newToken, node.ColumnType, alias, node.IntendedTypeName));
        }
        else
        {
            base.Visit(node);
        }
    }

    public override void Visit(WindowFunctionNode node)
    {
        if (node.WindowSpecification == null)
        {
            Nodes.Push(node);
            return;
        }

        var newPartitionFields = RewriteFieldNodes(node.WindowSpecification.PartitionFields);
        var newOrderFields = RewriteOrderedFieldNodes(node.WindowSpecification.OrderByFields);
        var newFuncCall = RewriteFunctionCall(node.FunctionCall);

        var newSpec = new WindowSpecificationNode(newPartitionFields, newOrderFields);
        var newNode = new WindowFunctionNode(newFuncCall, newSpec);

        if (node.ReturnType != null)
            newNode.SetReturnType(node.ReturnType);

        Nodes.Push(newNode);
    }

    public override void Visit(SelectNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldNode)Nodes.Pop();

        ChangedSelect = new SelectNode(fields);
    }

    public override void Visit(GroupByNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        HavingNode having = null;
        if (node.Having != null)
            having = (HavingNode)Nodes.Pop();

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldNode)Nodes.Pop();

        ChangedGroupBy = new GroupByNode(fields, having);
    }

    public override void Visit(WhereNode node)
    {
        ChangedWhere = new WhereNode(Nodes.Pop());
    }

    public override void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldOrderedNode)Nodes.Pop();

        ChangedOrderBy = new OrderByNode(fields);
    }

    public override void Visit(WindowNode node)
    {
        var definitions = new WindowDefinitionNode[node.Definitions.Length];
        for (var i = node.Definitions.Length - 1; i >= 0; i--)
            definitions[i] = (WindowDefinitionNode)Nodes.Pop();

        ChangedWindow = new WindowNode(definitions);
    }

    private FieldNode[] RewriteFieldNodes(FieldNode[] fields)
    {
        var result = new FieldNode[fields.Length];

        for (var i = 0; i < fields.Length; i++)
        {
            var rewritten = RewriteExpressionNode(fields[i].Expression);
            result[i] = new FieldNode(rewritten, fields[i].FieldOrder, fields[i].FieldName);
        }

        return result;
    }

    private FieldOrderedNode[] RewriteOrderedFieldNodes(FieldOrderedNode[] fields)
    {
        var result = new FieldOrderedNode[fields.Length];

        for (var i = 0; i < fields.Length; i++)
        {
            var rewritten = RewriteExpressionNode(fields[i].Expression);
            result[i] = new FieldOrderedNode(rewritten, fields[i].FieldOrder, fields[i].FieldName, fields[i].Order);
        }

        return result;
    }

    private AccessMethodNode RewriteFunctionCall(AccessMethodNode funcCall)
    {
        if (funcCall.Arguments == null || funcCall.Arguments.Args.Length == 0)
            return funcCall;

        var newArgs = new Node[funcCall.Arguments.Args.Length];
        for (var i = 0; i < newArgs.Length; i++)
            newArgs[i] = RewriteExpressionNode(funcCall.Arguments.Args[i]);

        return new AccessMethodNode(
            funcCall.FunctionToken,
            new ArgsListNode(newArgs),
            funcCall.ExtraAggregateArguments,
            funcCall.CanSkipInjectSource,
            funcCall.Method,
            funcCall.Alias,
            funcCall.Span,
            funcCall.IsDistinct);
    }

    private Node RewriteExpressionNode(Node expression)
    {
        var rewriter = new RewritePartsToUseJoinTransitionTable(alias);
        var traverser = new CloneTraverseVisitor(rewriter);
        expression.Accept(traverser);
        return rewriter.RewrittenNode;
    }
}
