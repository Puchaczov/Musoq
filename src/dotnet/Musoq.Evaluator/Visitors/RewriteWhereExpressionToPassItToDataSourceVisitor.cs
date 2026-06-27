using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Rewrites WHERE expressions to be safe for pushing to individual data sources.
///     This visitor handles predicate pushdown by:
///     1. Keeping predicates that only reference the current data source's alias
///     2. Replacing predicates that reference other data sources with "1 = 1" (always true)
///     3. Handling OR conditions specially - if any branch references another data source,
///     the entire OR must be replaced with "1 = 1" since we can't partially push OR predicates
///     4. Replacing complex expressions (method calls, property chains) with "1 = 1"
///     Example: For data source 'a' in query:
///     SELECT * FROM #A.data() a JOIN #B.data() b ON a.Id = b.Id
///     WHERE a.Status = 'open' AND b.Type = 'bug' OR a.Priority = 1
///     Result for 'a': WHERE 1 = 1 (because OR branch references b)
///     Result for 'b': WHERE 1 = 1 (because OR branch references a)
///     But for:
///     SELECT * FROM #A.data() a JOIN #B.data() b ON a.Id = b.Id
///     WHERE a.Status = 'open' AND b.Type = 'bug'
///     Result for 'a': WHERE a.Status = 'open' AND 1 = 1
///     Result for 'b': WHERE 1 = 1 AND b.Type = 'bug'
/// </summary>
public class RewriteWhereExpressionToPassItToDataSourceVisitor : CloneQueryVisitor
{
    private readonly Node _equalityNode;
    private readonly SchemaFromNode _schemaFromNode;
    private readonly IsComplexVisitor _isComplexVisitor;
    private readonly IsComplexTraverseVisitor _isComplexTraverseVisitor;

    public RewriteWhereExpressionToPassItToDataSourceVisitor(SchemaFromNode schemaFromNode)
    {
        ArgumentNullException.ThrowIfNull(schemaFromNode);
        _schemaFromNode = schemaFromNode;
        _equalityNode = new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s"));
        _isComplexVisitor = new IsComplexVisitor(schemaFromNode.Alias);
        _isComplexTraverseVisitor = new IsComplexTraverseVisitor(_isComplexVisitor);
    }

    public WhereNode WhereNode => (WhereNode)Nodes.Peek();

    /// <summary>
    ///     Handles OR nodes specially. If either branch of an OR contains references
    ///     to other data sources, the entire OR must be replaced with "1 = 1".
    ///     This is because: (a.Col = 1 OR b.Col = 2) cannot be partially pushed to
    ///     either data source - data source 'a' can't know about 'b', and if we only
    ///     push (a.Col = 1), we'd incorrectly filter out rows where b.Col = 2 is true.
    /// </summary>
    public override void Visit(OrNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();


        var leftHasOtherAlias = ContainsOtherAlias(node.Left);
        var rightHasOtherAlias = ContainsOtherAlias(node.Right);

        if (leftHasOtherAlias || rightHasOtherAlias)
            Nodes.Push(_equalityNode);
        else
            Nodes.Push(new OrNode(left, right));
    }

    public override void Visit(GreaterNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new GreaterNode(left, right));
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new GreaterOrEqualNode(left, right));
    }

    public override void Visit(LessNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new LessNode(left, right));
    }

    public override void Visit(LessOrEqualNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new LessOrEqualNode(left, right));
    }

    public override void Visit(EqualityNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new EqualityNode(left, right));
    }

    public override void Visit(IsDistinctFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new IsDistinctFromNode(left, right, node.IsNegated));
    }

    public override void Visit(DiffNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new DiffNode(left, right));
    }

    public override void Visit(LikeNode node)
    {
        Nodes.Pop();
        Nodes.Pop();

        Nodes.Push(_equalityNode);
    }

    public override void Visit(RLikeNode node)
    {
        Nodes.Pop();
        Nodes.Pop();

        Nodes.Push(_equalityNode);
    }

    public override void Visit(ContainsNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForArgsListNode(node.ToCompareExpression)) Nodes.Push(new ContainsNode(left, (ArgsListNode)right));
    }

    public override void Visit(InNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForArgsListNode((ArgsListNode)node.Right))
            Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public override void Visit(CollectionInNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node))
            Nodes.Push(new CollectionInNode(left, right));
    }

    public override void Visit(BetweenNode node)
    {
        var clonedNode = Nodes.Pop();
        Nodes.Push(clonedNode);
    }

    private bool ContainsOtherAlias(Node node)
    {
        _isComplexVisitor.Reset();
        node.Accept(_isComplexTraverseVisitor);
        return _isComplexVisitor.IsComplex;
    }

    private bool VisitForBinaryNode(BinaryNode node)
    {
        _isComplexVisitor.Reset();
        node.Left.Accept(_isComplexTraverseVisitor);
        var leftIsComplex = _isComplexVisitor.IsComplex;

        _isComplexVisitor.Reset();
        node.Right.Accept(_isComplexTraverseVisitor);
        var rightIsComplex = _isComplexVisitor.IsComplex;

        if (leftIsComplex || rightIsComplex)
        {
            Nodes.Push(_equalityNode);
            return true;
        }

        return false;
    }

    private bool VisitForArgsListNode(ArgsListNode node)
    {
        var isComplex = false;
        foreach (var argument in node.Args)
        {
            _isComplexVisitor.Reset();
            argument.Accept(_isComplexTraverseVisitor);
            isComplex |= _isComplexVisitor.IsComplex;
        }

        if (isComplex)
        {
            Nodes.Push(_equalityNode);
            return true;
        }

        return false;
    }

    private class IsComplexTraverseVisitor(IExpressionVisitor visitor) : CloneTraverseVisitor(visitor);

    private class IsComplexVisitor(string rootAlias) : CloneQueryVisitor
    {
        public bool IsComplex { get; private set; }

        public void Reset()
        {
            IsComplex = false;
        }

        public override void Visit(DotNode node)
        {
            base.Visit(node);
        }

        public override void Visit(AccessMethodNode node)
        {
            IsComplex = true;

            base.Visit(node);
        }

        public override void Visit(AccessCallChainNode node)
        {
            IsComplex = true;

            base.Visit(node);
        }

        public override void Visit(AccessColumnNode node)
        {
            if (node.Alias != rootAlias)
                IsComplex = true;

            base.Visit(node);
        }

        public override void Visit(AccessObjectArrayNode node)
        {
            base.Visit(node);
        }
    }
}
