using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
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
            var then = PopSemanticNode();
            var when = PopSemanticNode();
            whenThenPairs.Add((when, then));
        }

        var elseNode = PopSemanticNode();

        EnumTypeDescriptor? enumType = null;
        if (TryBindEnumCaseResults(
                whenThenPairs,
                elseNode,
                out var boundWhenThenPairs,
                out var boundElseNode,
                out var boundEnumType))
        {
            whenThenPairs = boundWhenThenPairs;
            elseNode = boundElseNode;
            enumType = boundEnumType;
            _diagnostics.NullSuspiciousTypes.Clear();
            foreach (var pair in whenThenPairs)
                _diagnostics.NullSuspiciousTypes.Add(pair.Then.ReturnType ?? typeof(object));
            _diagnostics.NullSuspiciousTypes.Add(elseNode.ReturnType ?? typeof(object));
        }

        var result = CaseExpressionBinding.Bind(node, whenThenPairs, elseNode, _diagnostics.NullSuspiciousTypes);

        if (enumType != null)
            MarkEnumExpression(result, enumType);
        PushSemanticNode(result);
        _diagnostics.NullSuspiciousTypes.Clear();
    }

    public override void Visit(WhenNode node)
    {
        var expression = PopSemanticNode();

        ValidateExpressionIsBoolean(expression, "CASE WHEN");

        var newNode = new WhenNode(expression).CopySpansFrom(node);

        PushSemanticNode(newNode);
    }

    public override void Visit(ThenNode node)
    {
        var newNode = new ThenNode(PopSemanticNode()).CopySpansFrom(node);

        _diagnostics.NullSuspiciousTypes.Add(newNode.ReturnType ?? typeof(object));

        PushSemanticNode(newNode);
    }

    public override void Visit(ElseNode node)
    {
        var newNode = new ElseNode(PopSemanticNode()).CopySpansFrom(node);

        _diagnostics.NullSuspiciousTypes.Add(newNode.ReturnType ?? typeof(object));

        PushSemanticNode(newNode);
    }

    public void SetQueryPart(QueryPart part)
    {
        _queryState.QueryPart = part;
    }

    public void QueryBegins()
    {
        GroupByAllQueryBegins();
        _sourceBinding.SchemaFromKey = _semanticState.Subqueries.NextQueryKey(_sourceBinding.SchemaFromKey);
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
