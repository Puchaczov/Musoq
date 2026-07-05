using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(WhereNode node)
    {
        var hasProcessedQueryId = _sourceBinding.CurrentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId);
        var identifier = hasProcessedQueryId
            ? _sourceBinding.CurrentScope[MetaAttributes.ProcessedQueryId]
            : _sourceBinding.Identifier;

        var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);
        var rewrittenWhereNode = new WhereNode(PopSemanticNode());

        var usedIdentifiers = _sourceBinding.UsedWhereNodes
            .Where(f => f.Key.QueryId == _sourceBinding.SchemaFromKey)
            .Select(f => f.Key)
            .ToArray();

        foreach (var aliasSchemaPair in tableSymbol.CompoundTables.Join(usedIdentifiers, t => t, f => f.Alias,
                     (t, f) => (Alias: t, Schema: f)))
            _sourceBinding.UsedWhereNodes[aliasSchemaPair.Schema] = rewrittenWhereNode;

        PushSemanticNode(rewrittenWhereNode);
    }

    public override void Visit(GroupByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var having = PeekSemanticNode() as HavingNode;

        if (having != null)
            PopSemanticNode();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
        {
            var field = PopSemanticNode() as FieldNode
                        ?? throw new VisitorException(
                            VisitorName,
                            "VisitGroupByNode",
                            "Expected GROUP BY field node on visitor stack.");
            EnsureGroupByFieldContainsNoAggregate(field);
            fields[i] = field;
        }

        PushSemanticNode(new GroupByNode(fields, having, node.IsAll, node.Span));
    }

    public override void Visit(HavingNode node)
    {
        PushSemanticNode(new HavingNode(PopSemanticNode()));
    }

    public override void Visit(QualifyNode node)
    {
        PushSemanticNode(new QualifyNode(PopSemanticNode()));
    }

    public override void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new SkipNode((IntegerNode)node.Expression));
    }

    public override void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new TakeNode((IntegerNode)node.Expression));
    }

    public override void Visit(OrderByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)PopSemanticNode();

        PushSemanticNode(new OrderByNode(fields));
    }
}
