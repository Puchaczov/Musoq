using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        LoadQueryScope();
        var aliasPrecollector = Visitor as BuildMetadataAndInferTypesVisitor;
        aliasPrecollector?.PrecollectCurrentQuerySelectAliases(node.Select);
        aliasPrecollector?.PrecollectCurrentQueryWindowDefinitions(node.Window);

        SetQueryPart(QueryPart.From);
        node.From.Accept(this);

        SetQueryPart(QueryPart.Where);
        node.Where?.Accept(this);

        SetQueryPart(QueryPart.GroupBy);
        if (node.GroupBy?.IsAll == true && Visitor is BuildMetadataAndInferTypesVisitor buildVisitor)
            buildVisitor.MarkCurrentQueryGroupByAll();
        node.GroupBy?.Accept(this);

        SetQueryPart(QueryPart.Select);
        node.Select.Accept(this);

        node.Skip?.Accept(this);
        node.Take?.Accept(this);

        node.Window?.Accept(this);

        SetQueryPart(QueryPart.Qualify);
        node.Qualify?.Accept(this);

        SetQueryPart(QueryPart.OrderBy);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);
        RestoreScope();
        SetQueryPart(QueryPart.None);
        aliasPrecollector?.EndCurrentQueryWindowDefinitionScope();
        aliasPrecollector?.EndCurrentQuerySelectAliasScope();
        EndQueryScope();
    }

    public override void Visit(UnionNode node)
    {
        LoadScope("Union");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(UnionAllNode node)
    {
        LoadScope("UnionAll");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(ExceptNode node)
    {
        LoadScope("Except");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(IntersectNode node)
    {
        LoadScope("Intersect");
        TraverseSetOperatorWithScope(node);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        LoadScope("CTE Inner Expression");
        Visitor.InnerCteBegins();
        TraverseChildren(node);
        Visitor.InnerCteEnds();
        node.Accept(Visitor);
        RestoreScope();
    }

    public virtual void SetQueryPart(QueryPart part)
    {
        Visitor.SetQueryPart(part);
    }

    public virtual void QueryBegins()
    {
        Visitor.QueryBegins();
    }

    public virtual void QueryEnds()
    {
        Visitor.QueryEnds();
    }
}
