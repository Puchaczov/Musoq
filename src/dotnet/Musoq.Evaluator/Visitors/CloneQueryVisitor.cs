using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor : DefensiveVisitorBase
{
    protected Stack<Node> Nodes { get; } = new();

    /// <summary>
    ///     Gets the name of this visitor for error reporting.
    /// </summary>
    protected override string VisitorName => nameof(CloneQueryVisitor);

    public RootNode Root => SafeCast<RootNode>(SafePeek(Nodes, VisitorOperationNames.GettingRoot),
        VisitorOperationNames.GettingRoot);

    public override void Visit(Node node)
    {
    }

    public override void Visit(StarNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitStarNode, (l, r) => new StarNode(l, r));

    public override void Visit(FSlashNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitFSlashNode, (l, r) => new FSlashNode(l, r));

    public override void Visit(ModuloNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitModuloNode, (l, r) => new ModuloNode(l, r));

    public override void Visit(AddNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitAddNode, (l, r) => new AddNode(l, r));

    public override void Visit(HyphenNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitHyphenNode, (l, r) => new HyphenNode(l, r));

    public override void Visit(AndNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitAndNode, (l, r) => new AndNode(l, r));

    public override void Visit(OrNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitOrNode, (l, r) => new OrNode(l, r));

    public override void Visit(BitwiseAndNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitBitwiseAndNode, (l, r) => new BitwiseAndNode(l, r));

    public override void Visit(BitwiseOrNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitBitwiseOrNode, (l, r) => new BitwiseOrNode(l, r));

    public override void Visit(BitwiseXorNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitBitwiseXorNode, (l, r) => new BitwiseXorNode(l, r));

    public override void Visit(LeftShiftNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitLeftShiftNode, (l, r) => new LeftShiftNode(l, r));

    public override void Visit(RightShiftNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), VisitorOperationNames.VisitRightShiftNode, (l, r) => new RightShiftNode(l, r));

    public override void Visit(CoalesceNode node) =>
        CloneBinaryNodeWithSpan(node ?? throw new ArgumentNullException(nameof(node)), nameof(Visit), (l, r) => new CoalesceNode(l, r, node.ReturnType));

    public override void Visit(ShortCircuitingNodeLeft node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
    }

    public override void Visit(ShortCircuitingNodeRight node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
    }

    public override void Visit(EqualityNode node) =>
        CloneBinaryNode((l, r) => new EqualityNode(l, r));
    public override void Visit(IsDistinctFromNode node) =>
        CloneBinaryNode((l, r) => new IsDistinctFromNode(l, r, node.IsNegated));

    public override void Visit(GreaterOrEqualNode node) =>
        CloneBinaryNode((l, r) => new GreaterOrEqualNode(l, r));

    public override void Visit(LessOrEqualNode node) =>
        CloneBinaryNode((l, r) => new LessOrEqualNode(l, r));

    public override void Visit(GreaterNode node) =>
        CloneBinaryNode((l, r) => new GreaterNode(l, r));

    public override void Visit(LessNode node) =>
        CloneBinaryNode((l, r) => new LessNode(l, r));

    public override void Visit(DiffNode node) =>
        CloneBinaryNode((l, r) => new DiffNode(l, r));

    public override void Visit(NotNode node)
    {
        Nodes.Push(new NotNode(Nodes.Pop()));
    }

    public override void Visit(LikeNode node) =>
        CloneBinaryNode((l, r) => new LikeNode(l, r));

    public override void Visit(RLikeNode node) =>
        CloneBinaryNode((l, r) => new RLikeNode(l, r));

    public override void Visit(InNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new InNode(left, (ArgsListNode)right));
    }

    public override void Visit(BetweenNode node)
    {
        var max = Nodes.Pop();
        var min = Nodes.Pop();
        var expression = Nodes.Pop();
        Nodes.Push(new BetweenNode(expression, min, max));
    }

    public override void Visit(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.HasExplicitFieldName));
    }

    public override void Visit(FieldOrderedNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder, node.FieldName, node.HasExplicitFieldName, node.Order, node.NullOrdering));
    }

    public override void Visit(SelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct));
    }

    public override void Visit(GroupSelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new GroupSelectNode(fields));
    }
}
