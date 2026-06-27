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
        var rewrittenWhereNode = new WhereNode(Nodes.Pop());

        var usedIdentifiers = _sourceBinding.UsedWhereNodes
            .Where(f => f.Key.QueryId == _sourceBinding.SchemaFromKey)
            .Select(f => f.Key)
            .ToArray();

        foreach (var aliasSchemaPair in tableSymbol.CompoundTables.Join(usedIdentifiers, t => t, f => f.Alias,
                     (t, f) => (Alias: t, Schema: f)))
            _sourceBinding.UsedWhereNodes[aliasSchemaPair.Schema] = rewrittenWhereNode;

        Nodes.Push(rewrittenWhereNode);
    }

    public override void Visit(GroupByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var having = Nodes.Peek() as HavingNode;

        if (having != null)
            Nodes.Pop();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
        {
            var field = Nodes.Pop() as FieldNode
                        ?? throw new VisitorException(
                            VisitorName,
                            "VisitGroupByNode",
                            "Expected GROUP BY field node on visitor stack.");
            EnsureGroupByFieldContainsNoAggregate(field);
            fields[i] = field;
        }

        Nodes.Push(new GroupByNode(fields, having, node.IsAll, node.Span));
    }

    public override void Visit(HavingNode node)
    {
        Nodes.Push(new HavingNode(Nodes.Pop()));
    }

    public override void Visit(QualifyNode node)
    {
        Nodes.Push(new QualifyNode(Nodes.Pop()));
    }

    public override void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SkipNode((IntegerNode)node.Expression));
    }

    public override void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new TakeNode((IntegerNode)node.Expression));
    }

    public override void Visit(OrderByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields));
    }
}
