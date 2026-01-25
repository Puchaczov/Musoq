using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class RewriteWhereExpressionToPassItToDataSourceVisitor : CloneQueryVisitor
{
    private readonly Node _equalityNode;
    private readonly SchemaFromNode _schemaFromNode;

    public RewriteWhereExpressionToPassItToDataSourceVisitor(SchemaFromNode schemaFromNode)
    {
        _schemaFromNode = schemaFromNode;
        _equalityNode = new EqualityNode(new IntegerNode("1", "s"), new IntegerNode("1", "s"));
    }

    public WhereNode WhereNode => (WhereNode)Nodes.Peek();

    public override void Visit(GreaterNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new GreaterNode(left, right));
    }

    public override void Visit(GreaterOrEqualNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new GreaterOrEqualNode(left, right));
    }

    public override void Visit(LessNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new LessNode(left, right));
    }

    public override void Visit(LessOrEqualNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new LessOrEqualNode(left, right));
    }

    public override void Visit(EqualityNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForBinaryNode(node)) Nodes.Push(new EqualityNode(left, right));
    }

    public override void Visit(DiffNode node)
    {
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
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (!VisitForArgsListNode(node.ToCompareExpression)) Nodes.Push(new ContainsNode(left, (ArgsListNode)right));
    }

    public override void Visit(InNode node)
    {
        var clonedNode = Nodes.Pop();

        if (!VisitForArgsListNode((ArgsListNode)node.Right)) Nodes.Push(clonedNode);
    }

    private bool VisitForBinaryNode(BinaryNode node)
    {
        var isComplexVisitor = new IsComplexVisitor(_schemaFromNode.Alias);
        var isComplexTraverseVisitor = new IsComplexTraverseVisitor(isComplexVisitor);

        node.Left.Accept(isComplexTraverseVisitor);
        var leftIsComplex = isComplexVisitor.IsComplex;

        isComplexVisitor.Reset();

        node.Right.Accept(isComplexTraverseVisitor);
        var rightIsComplex = isComplexVisitor.IsComplex;

        if (leftIsComplex || rightIsComplex)
        {
            Nodes.Push(_equalityNode);
            return true;
        }

        return false;
    }

    private bool VisitForArgsListNode(ArgsListNode node)
    {
        var isComplexVisitor = new IsComplexVisitor(_schemaFromNode.Alias);
        var isComplexTraverseVisitor = new IsComplexTraverseVisitor(isComplexVisitor);
        var isComplex = false;
        foreach (var argument in node.Args)
        {
            argument.Accept(isComplexTraverseVisitor);
            isComplex |= isComplexVisitor.IsComplex;
            isComplexVisitor.Reset();
        }

        if (isComplex)
        {
            Nodes.Push(_equalityNode);
            return true;
        }

        return false;
    }

    private class IsComplexTraverseVisitor : CloneTraverseVisitor
    {
        public IsComplexTraverseVisitor(IExpressionVisitor visitor)
            : base(visitor)
        {
        }
    }

    private class IsComplexVisitor : CloneQueryVisitor
    {
        private readonly string _rootAlias;

        public IsComplexVisitor(string rootAlias)
        {
            _rootAlias = rootAlias;
        }

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
            if (node.Alias != _rootAlias)
                IsComplex = true;

            base.Visit(node);
        }

        public override void Visit(AccessObjectArrayNode node)
        {
            base.Visit(node);
        }
    }
}
