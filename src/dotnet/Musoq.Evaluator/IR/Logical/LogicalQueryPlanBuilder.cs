using System.Linq;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{

    public void Visit(WhereNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var predicate = _converter.Convert(node.Expression);
        var source = _nodeStack.Pop();
        _nodeStack.Push(new IrNodes.FilterNode(predicate, source));
    }

    public void Visit(GroupByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var groupKeys = new IrExpression[node.Fields.Length];
        var groupKeyNames = new string[node.Fields.Length];
        var groupKeyTypes = new Type[node.Fields.Length];

        for (var i = 0; i < node.Fields.Length; i++)
        {
            groupKeys[i] = _converter.Convert(node.Fields[i].Expression);
            groupKeyNames[i] = node.Fields[i].FieldName;
            groupKeyTypes[i] = node.Fields[i].ReturnType ??
                               throw new InvalidOperationException($"GROUP BY field '{node.Fields[i].FieldName}' has no inferred return type.");
        }

        var source = _nodeStack.Pop();
        _nodeStack.Push(new IrNodes.AggregateNode(groupKeys, groupKeyNames, groupKeyTypes, [], source));
    }

    public void Visit(HavingNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _havingPredicate = _converter.Convert(node.Expression);
    }

    public void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _skipValue = (int)node.Value;
    }

    public void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _takeValue = (int)node.Value;
    }

    public void Visit(QueryNode node)
    {
        FinalizeQueryNode();
    }

    public void Visit(DescNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Type == DescForType.Query)
        {
            var query = _nodeStack.Pop();
            _nodeStack.Push(new IrNodes.DescNode(
                string.Empty,
                string.Empty,
                IrNodes.DescType.Query,
                null,
                [],
                string.Empty,
                CreateDescriptionOutputSchema(),
                query.OutputSchema));
            return;
        }

        var from = node.From as SchemaFromNode;
        var schemaName = from?.Schema ?? string.Empty;
        var methodName = from?.Method ?? string.Empty;
        var column = node.Column?.ToString();
        var sourceContextId = from?.Id ?? string.Empty;
        var arguments = from is Musoq.Evaluator.Parser.SchemaFromNode semanticSource &&
                        semanticSource.BoundInvocation is { } invocation
            ? ConvertArguments(from.Parameters, invocation)
            : from?.Parameters.Args.Select(_converter.Convert).ToArray() ?? [];

        var descType = node.Type switch
        {
            DescForType.Schema => IrNodes.DescType.Schema,
            DescForType.Constructors => IrNodes.DescType.Constructors,
            DescForType.FunctionsForSchema => IrNodes.DescType.Functions,
            DescForType.SpecificColumn => IrNodes.DescType.Column,
            DescForType.Settings => IrNodes.DescType.Settings,
            _ => IrNodes.DescType.Table
        };

        _nodeStack.Push(new IrNodes.DescNode(schemaName, methodName, descType, column, arguments, sourceContextId, OutputSchema.Empty));
    }

    public void Visit(InternalQueryNode node)
    {
        FinalizeQueryNode();
    }

    public void Visit(RootNode node)
    {
    }

    public void Visit(SingleSetNode node) { }

    public void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        BuildSetOperation(IrNodes.SetOpKind.Union, node.Keys);
    }

    public void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        BuildSetOperation(IrNodes.SetOpKind.UnionAll, node.Keys);
    }

    public void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        BuildSetOperation(IrNodes.SetOpKind.Except, node.Keys);
    }

    public void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        BuildSetOperation(IrNodes.SetOpKind.Intersect, node.Keys);
    }

    public void Visit(MultiStatementNode node)
    {
        var baseDepth = _multiStatementBaseDepths.Count > 0 ? _multiStatementBaseDepths.Pop() : 0;
        var produced = _nodeStack.Count - baseDepth;
        if (produced < 0)
            produced = 0;

        var statements = new LogicalNode[produced];
        for (var i = produced - 1; i >= 0; i--)
            statements[i] = _nodeStack.Pop();
        _nodeStack.Push(new IrNodes.MultiStatementNode(statements));
    }

    internal void EnterMultiStatement() => _multiStatementBaseDepths.Push(_nodeStack.Count);

    public void Visit(CteExpressionNode node)
    {
        var query = _nodeStack.Pop();
        _nodeStack.Push(new IrNodes.CteNode([.. _cteDefinitions], query));
        _cteDefinitions.Clear();
    }

    public void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var plan = _nodeStack.Pop();
        if (node.IsRecursiveDefinition)
        {
            if (plan is not IrNodes.SetOperationNode setOperation)
                throw UnsupportedShape.Of($"Recursive CTE '{node.Name}' did not lower to a set-operation boundary.");

            var unionKind = setOperation.Kind switch
            {
                IrNodes.SetOpKind.UnionAll => IrNodes.RecursiveCteUnionKind.All,
                IrNodes.SetOpKind.Union when setOperation.Keys.Length == 0 =>
                    IrNodes.RecursiveCteUnionKind.FullRow,
                IrNodes.SetOpKind.Union => IrNodes.RecursiveCteUnionKind.Keyed,
                _ => throw UnsupportedShape.Of(
                    $"Recursive CTE '{node.Name}' has unsupported set operation '{setOperation.Kind}'.")
            };

            plan = new IrNodes.RecursiveCteNode(
                node.Name,
                setOperation.Left,
                setOperation.Right,
                unionKind,
                setOperation.Keys,
                ResolveRecursiveIdentityFieldIndexes(
                    node.Name,
                    unionKind,
                    setOperation.Keys,
                    setOperation.Left.OutputSchema));
        }

        _cteDefinitions.Add(new IrNodes.CteDefinition(node.Name, plan));
    }

    private static int[] ResolveRecursiveIdentityFieldIndexes(
        string cteName,
        IrNodes.RecursiveCteUnionKind unionKind,
        IReadOnlyList<string> keys,
        OutputSchema outputSchema)
    {
        if (unionKind != IrNodes.RecursiveCteUnionKind.Keyed)
            return [];

        var indexes = new int[keys.Count];
        for (var keyIndex = 0; keyIndex < keys.Count; keyIndex++)
        {
            var key = keys[keyIndex];
            var fieldIndex = Array.FindIndex(
                outputSchema.Columns,
                column => string.Equals(column.Name, key, StringComparison.OrdinalIgnoreCase));
            if (fieldIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Recursive CTE '{cteName}' key '{key}' was not resolved during semantic binding.");
            }

            indexes[keyIndex] = fieldIndex;
        }

        return indexes;
    }

    public void Visit(OrderByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _orderFields.Clear();

        foreach (var field in node.Fields)
        {
            var expr = _converter.Convert(field.Expression);
            var descending = field.Order == Order.Descending;
            _orderFields.Add(new OrderField(expr, descending, ConvertNullOrdering(field.NullOrdering)));
        }
    }

    private void BuildSetOperation(IrNodes.SetOpKind kind, string[] keys)
    {
        var right = _nodeStack.Pop();
        var left = _nodeStack.Pop();
        _nodeStack.Push(LeftAssociateSetOperation(kind, left, right, keys ?? []));
    }

    private static Nodes.SetOperationNode LeftAssociateSetOperation(
        IrNodes.SetOpKind kind,
        LogicalNode left,
        LogicalNode right,
        string[] keys)
    {
        while (right is IrNodes.SetOperationNode rightSet)
        {
            left = LeftAssociateSetOperation(kind, left, rightSet.Left, keys);
            kind = rightSet.Kind;
            keys = rightSet.Keys;
            right = rightSet.Right;
        }

        return new IrNodes.SetOperationNode(kind, left, right, keys);
    }

    private void FinalizeQueryNode()
    {
        var source = _nodeStack.Pop();

        if (_havingPredicate != null)
        {
            source = new IrNodes.HavingFilterNode(_havingPredicate, source);
            _havingPredicate = null;
        }

        if (_windowRegistrations.Count > 0)
        {
            var windowRegistrations = DeduplicateWindowRegistrations(out var windowIndexMap);

            if (windowIndexMap.Count > 0)
                RewriteWindowReferences(windowIndexMap);

            source = new IrNodes.WindowNode(windowRegistrations, source);
            _windowRegistrations.Clear();
        }

        if (_qualifyPredicate != null)
        {
            source = new IrNodes.QualifyFilterNode(_qualifyPredicate, source);
            _qualifyPredicate = null;
        }

        if (_projectedFields.Count > 0)
        {
            if (_refreshMethods.Count > 0)
                source = ExtractAggregateBindings(source);

            source = new IrNodes.ProjectNode([.. _projectedFields], source) { IsDistinct = _selectIsDistinct };
            _projectedFields.Clear();
        }
        else if (_selectVisited)
        {
            source = new IrNodes.ProjectNode([], source) { IsDistinct = _selectIsDistinct };
        }

        _selectVisited = false;
        _selectIsDistinct = false;
        _refreshMethods.Clear();

        if (_orderFields.Count > 0)
        {
            source = new IrNodes.SortNode([.. _orderFields], source);
            _orderFields.Clear();
        }

        if (_skipValue.HasValue)
        {
            source = new IrNodes.SkipNode(_skipValue.Value, source);
            _skipValue = null;
        }

        if (_takeValue.HasValue)
        {
            source = new IrNodes.TakeNode(_takeValue.Value, source);
            _takeValue = null;
        }

        _windowDefinitions.Clear();

        _nodeStack.Push(source);
    }

    private static OutputSchema CreateDescriptionOutputSchema()
    {
        return new OutputSchema(
        [
            new ColumnSchema("Name", typeof(string), 0),
            new ColumnSchema("Index", typeof(int), 1),
            new ColumnSchema("Type", typeof(string), 2)
        ]);
    }
}
