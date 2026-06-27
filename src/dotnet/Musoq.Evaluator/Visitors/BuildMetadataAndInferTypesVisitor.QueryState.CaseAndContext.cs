using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CaseNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (var i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        if (_diagnostics.NullSuspiciousTypes.All(type => type != NullNode.NullType.Instance))
        {
            var anyWasNullable = _diagnostics.NullSuspiciousTypes.Any(type => type.GetUnderlyingNullable() != null);
            var greatestCommonSubtype = SemanticTypeInferenceService.FindGreatestCommonSubtype(_diagnostics.NullSuspiciousTypes);
            var caseNode = anyWasNullable
                ? new CaseNode(whenThenPairs.ToArray(), elseNode, greatestCommonSubtype)
                : new CaseNode(whenThenPairs.ToArray(), elseNode,
                    BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype));

            Nodes.Push(caseNode);
        }
        else
        {
            var greatestCommonSubtype = SemanticTypeInferenceService.FindGreatestCommonSubtype(_diagnostics.NullSuspiciousTypes);
            var nullableGreatestCommonSubtype =
                BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(greatestCommonSubtype);
            var caseNode = new CaseNode(whenThenPairs.ToArray(), elseNode, nullableGreatestCommonSubtype);

            var rewritePartsWithProperNullHandling =
                new RewritePartsWithProperNullHandlingVisitor(greatestCommonSubtype);
            var rewritePartsWithProperNullHandlingTraverser =
                new RewritePartsWithProperNullHandlingTraverseVisitor(rewritePartsWithProperNullHandling);

            caseNode.Accept(rewritePartsWithProperNullHandlingTraverser);

            Nodes.Push(rewritePartsWithProperNullHandling.RewrittenNode);
        }

        _diagnostics.NullSuspiciousTypes.Clear();
    }

    public override void Visit(WhenNode node)
    {
        var expression = Nodes.Pop();

        ValidateExpressionIsBoolean(expression, "CASE WHEN");

        var newNode = new WhenNode(expression);

        Nodes.Push(newNode);
    }

    public override void Visit(ThenNode node)
    {
        var newNode = new ThenNode(Nodes.Pop());

        _diagnostics.NullSuspiciousTypes.Add(newNode.ReturnType ?? typeof(object));

        Nodes.Push(newNode);
    }

    public override void Visit(ElseNode node)
    {
        var newNode = new ElseNode(Nodes.Pop());

        _diagnostics.NullSuspiciousTypes.Add(newNode.ReturnType ?? typeof(object));

        Nodes.Push(newNode);
    }

    public void SetQueryPart(QueryPart part)
    {
        _queryState.QueryPart = part;
    }

    public void QueryBegins()
    {
        GroupByAllQueryBegins();
        _sourceBinding.SchemaFromKey += 1;
    }

    public void QueryEnds()
    {
        GroupByAllQueryEnds();
        _sourceBinding.Identifier = string.Empty;
    }

    public void SetTheMostInnerIdentifierOfDotNode(IdentifierNode? node)
    {
        _resultShape.TheMostInnerIdentifier = node;
    }

    public void InnerCteBegins()
    {
    }

    public void InnerCteEnds()
    {
    }

    public bool IsCurrentContextColumn(string name)
    {
        if (string.IsNullOrEmpty(_sourceBinding.Identifier)) return false;

        if (!_sourceBinding.CurrentScope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(_sourceBinding.Identifier, out var tableSymbol))
            return false;

        return tableSymbol.GetColumnByAliasAndName(_sourceBinding.Identifier, name) != null;
    }
}
