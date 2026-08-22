using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Stack<SetResultModifierBindingState> _setResultModifierBindings = new();

    internal void BeginSetResultModifierBinding()
    {
        var setOperator = PeekSemanticNode() as SetOperatorNode
                          ?? throw new VisitorException(
                              VisitorName,
                              nameof(BeginSetResultModifierBinding),
                              "Expected a semantic set operator before binding result modifiers.");
        var leftQuery = setOperator.Left as QueryNode
                        ?? throw new VisitorException(
                            VisitorName,
                            nameof(BeginSetResultModifierBinding),
                            "Expected the left set operand to expose the combined result columns.");
        var previousAliases = new Dictionary<string, Node>(
            _resultShape.SelectFieldAliases,
            StringComparer.OrdinalIgnoreCase);
        _setResultModifierBindings.Push(new SetResultModifierBindingState(previousAliases, _queryState.QueryPart));

        _resultShape.SelectFieldAliases.Clear();
        foreach (var field in leftQuery.Select.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.FieldName))
                continue;

            _resultShape.SelectFieldAliases.TryAdd(
                field.FieldName,
                new AccessColumnNode(
                    field.FieldName,
                    string.Empty,
                    field.Expression.ReturnType ?? typeof(object),
                    field.Span));
        }

        _queryState.QueryPart = QueryPart.OrderBy;
    }

    internal void AttachSetResultModifiers(SetOperatorNode syntaxNode)
    {
        var take = syntaxNode.ResultTake != null ? PopSemanticNode() as TakeNode : null;
        var skip = syntaxNode.ResultSkip != null ? PopSemanticNode() as SkipNode : null;
        var orderBy = syntaxNode.ResultOrderBy != null ? PopSemanticNode() as OrderByNode : null;
        var setOperator = PopSemanticNode() as SetOperatorNode
                          ?? throw new VisitorException(
                              VisitorName,
                              nameof(AttachSetResultModifiers),
                              "Expected a semantic set operator below its result modifiers.");

        if (orderBy != null)
            foreach (var field in orderBy.Fields)
            {
                ValidateOrderByExpression(field);
                ValidateExpressionIsPrimitive(field.Expression, "set result ORDER BY");
            }

        PushSemanticNode(CreateSetOperatorNode(
            GetSetOperatorName(setOperator),
            setOperator,
            setOperator.Keys,
            setOperator.Left,
            setOperator.Right,
            orderBy,
            skip,
            take));
    }

    internal void EndSetResultModifierBinding()
    {
        var state = _setResultModifierBindings.Pop();
        _resultShape.SelectFieldAliases.Clear();
        foreach (var (alias, expression) in state.SelectFieldAliases)
            _resultShape.SelectFieldAliases.Add(alias, expression);
        _queryState.QueryPart = state.QueryPart;
    }

    private bool TryBindSetResultModifierColumn(string name, string alias, Node node)
    {
        if (_setResultModifierBindings.Count == 0)
            return false;

        if (!string.IsNullOrWhiteSpace(alias))
        {
            TryReportUnknownAlias(alias, [], node);
            PushSemanticNode(new AccessColumnNode(name, alias, typeof(object), node.Span));
            return true;
        }

        if (_resultShape.SelectFieldAliases.TryGetValue(name, out var expression) &&
            expression is AccessColumnNode column)
        {
            PushSemanticNode(new AccessColumnNode(column.Name, string.Empty, column.ReturnType, node.Span));
            return true;
        }

        var availableColumns = _resultShape.SelectFieldAliases
            .Select(static pair => (ISchemaColumn)new SchemaColumn(
                pair.Key,
                0,
                pair.Value.ReturnType ?? typeof(object)))
            .ToArray();
        TryReportOrThrowUnknownColumn(name, availableColumns, node);
        PushSemanticNode(new AccessColumnNode(name, string.Empty, typeof(object), node.Span));
        return true;
    }

    private SetOperatorNode CreateSetOperatorNode(
        string setOperatorName,
        SetOperatorNode node,
        string[] keys,
        Node left,
        Node right,
        OrderByNode? resultOrderBy = null,
        SkipNode? resultSkip = null,
        TakeNode? resultTake = null)
    {
        return setOperatorName switch
        {
            "Union" => new UnionNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                resultOrderBy, resultSkip, resultTake),
            "UnionAll" => new UnionAllNode(node.ResultTableName, keys, left, right, node.IsNested,
                node.IsTheLastOne, resultOrderBy, resultSkip, resultTake),
            "Except" => new ExceptNode(node.ResultTableName, keys, left, right, node.IsNested, node.IsTheLastOne,
                resultOrderBy, resultSkip, resultTake),
            "Intersect" => new IntersectNode(node.ResultTableName, keys, left, right, node.IsNested,
                node.IsTheLastOne, resultOrderBy, resultSkip, resultTake),
            _ => throw new VisitorException(
                VisitorName,
                nameof(CreateSetOperatorNode),
                $"Set operator '{setOperatorName}' is not supported.")
        };
    }

    private string GetSetOperatorName(SetOperatorNode node)
    {
        return node switch
        {
            UnionNode => "Union",
            UnionAllNode => "UnionAll",
            ExceptNode => "Except",
            IntersectNode => "Intersect",
            _ => throw new VisitorException(
                VisitorName,
                nameof(GetSetOperatorName),
                $"Set operator '{node.GetType().Name}' is not supported.")
        };
    }

    private sealed record SetResultModifierBindingState(
        IReadOnlyDictionary<string, Node> SelectFieldAliases,
        QueryPart QueryPart);
}
