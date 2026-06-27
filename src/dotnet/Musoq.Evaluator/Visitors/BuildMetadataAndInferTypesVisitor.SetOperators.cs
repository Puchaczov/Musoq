using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using NotSupportedException = System.NotSupportedException;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private string CreateSetOperatorPositionKey()
    {
        var key = _queryState.SetKey++;
        return key.ToString(System.Globalization.CultureInfo.InvariantCulture).ToSetOperatorKey(key.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private string PreviousSetOperatorPositionKey()
    {
        return (_queryState.SetKey - 2).ToString(System.Globalization.CultureInfo.InvariantCulture).ToSetOperatorKey((_queryState.SetKey - 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, QueryNode right,
        string cachedSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = right.Select.Fields;

        ValidateSelectFieldsArePrimitive(leftFields, "SET operator (left side)");
        ValidateSelectFieldsArePrimitive(rightFields, "SET operator (right side)");

        if (leftFields.Length != rightFields.Length)
        {
            if (TryReportSetOperatorColumnCount(right))
                return;
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        ReconcileFieldTypesForSetOperation(leftFields, rightFields, rightFields[0].Expression);

        _queryState.CachedSetFields.TryAdd(cachedSetOperatorKey, ResolveFieldsForCache(leftFields, rightFields));
    }

    private void MakeSureBothSideFieldsAreOfAssignableTypes(QueryNode left, string cachedSetOperatorKey,
        string currentSetOperatorKey)
    {
        var leftFields = left.Select.Fields;
        var rightFields = _queryState.CachedSetFields[cachedSetOperatorKey];

        ValidateSelectFieldsArePrimitive(leftFields, "SET operator");

        if (leftFields.Length != rightFields.Length)
        {
            if (TryReportSetOperatorColumnCount(left))
                return;
            throw new SetOperatorMustHaveSameQuantityOfColumnsException();
        }

        ReconcileFieldTypesForSetOperation(leftFields, rightFields, leftFields[0].Expression);

        _queryState.CachedSetFields.TryAdd(currentSetOperatorKey, ResolveFieldsForCache(leftFields, rightFields));
    }

    private void ReconcileFieldTypesForSetOperation(FieldNode[] leftFields, FieldNode[] rightFields,
        Node errorContextNode)
    {
        for (var i = 0; i < leftFields.Length; i++)
        {
            var leftType = leftFields[i].Expression.ReturnType ?? typeof(object);
            var rightType = rightFields[i].Expression.ReturnType ?? typeof(object);

            if (leftType == rightType)
                continue;

            var leftIsNull = leftType is NullNode.NullType;
            var rightIsNull = rightType is NullNode.NullType;

            if (leftIsNull && rightIsNull)
                continue;

            if (leftIsNull)
            {
                leftFields[i] = new FieldNode(
                    new NullNode(rightType, leftFields[i].Expression.Span),
                    leftFields[i].FieldOrder,
                    leftFields[i].FieldName,
                    leftFields[i].Span);
                continue;
            }

            if (rightIsNull)
            {
                rightFields[i] = new FieldNode(
                    new NullNode(leftType, rightFields[i].Expression.Span),
                    rightFields[i].FieldOrder,
                    rightFields[i].FieldName,
                    rightFields[i].Span);
                continue;
            }

            if (TryReportSetOperatorColumnTypes(leftFields[i], rightFields[i], errorContextNode))
                continue;
            throw new SetOperatorMustHaveSameTypesOfColumnsException(leftFields[i], rightFields[i]);
        }
    }

    /// <summary>
    ///     Reports or throws an unknown column exception. If diagnostic context is available,
    ///     reports the error and returns true (to allow continuation). Otherwise throws.
    /// </summary>
    /// <param name="identifier">The column identifier that was not found.</param>
    /// <param name="columns">Available columns for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (continue execution), false if thrown.</returns>
    protected bool TryReportOrThrowUnknownColumn(string identifier, ISchemaColumn[] columns, Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (DiagnosticContext != null)
        {
            var dialectMessage = GetDialectColumnHint(identifier);
            if (dialectMessage != null)
            {
                DiagnosticContext.ReportError(DiagnosticCode.MQ3001_UnknownColumn, dialectMessage, node);
                return true;
            }

            var availableColumns = columns.Select(c => c.ColumnName);
            DiagnosticContext.ReportUnknownColumn(identifier, availableColumns, node);
            return true;
        }

        var span = node.SpanOrEmpty();
        PrepareAndThrowUnknownColumnExceptionMessage(identifier, columns, span);
        return false;
    }

    /// <summary>
    ///     Reports an unknown alias error if diagnostic context is available.
    /// </summary>
    /// <param name="alias">The alias that was not found.</param>
    /// <param name="availableAliases">Available aliases for suggestions.</param>
    /// <param name="node">The node where the error occurred.</param>
    /// <returns>True if error was reported (continue execution), false otherwise.</returns>
    protected bool TryReportUnknownAlias(string alias, string[] availableAliases, Node node)
    {
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownAlias(alias, availableAliases, node);
            return true;
        }

        return false;
    }

    private void VisitSetOperationNode(SetOperatorNode node, string setOperatorName)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        if (left is not QueryNode leftQuery)
            throw new InvalidOperationException($"Expected left side of {setOperatorName} to be a query node.");

        if (node.Keys.Length > 0 && !ValidateSetOperatorKeys(leftQuery, node.Keys, node))
        {
            Nodes.Push(left);
            Nodes.Push(right);
            Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
            return;
        }

        var key = CreateSetOperatorPositionKey();
        _sourceBinding.CurrentScope[MetaAttributes.SetOperatorName] = key;

        if (right is QueryNode rightAsQueryNode)
            MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, rightAsQueryNode, key);
        else
            MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, PreviousSetOperatorPositionKey(), key);

        SetOperatorFieldPositions.Add(key,
            CreateSetOperatorPositionIndexes(leftQuery, node.Keys));
        SetOperatorFieldTypes.Add(key,
            CreateSetOperatorPositionTypes(leftQuery, node.Keys));

        var rightMethodName = Methods.Pop();
        var leftMethodName = Methods.Pop();

        var methodName = $"{leftMethodName}_{setOperatorName}_{rightMethodName}";
        Methods.Push(methodName);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(methodName,
            _sourceBinding.CurrentScope.Child[0].ScopeSymbolTable.GetSymbol(leftQuery.From.Alias));

        Nodes.Push(CreateSetOperatorNode(setOperatorName, node, left, right));
    }

    private bool ValidateSetOperatorKeys(QueryNode query, IReadOnlyCollection<string> keys, Node node)
    {
        var availableFieldNames = query.Select.Fields
            .SelectMany(field => new[] { field.FieldName, field.Expression.ToString() })
            .Where(fieldName => !string.IsNullOrWhiteSpace(fieldName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingKey = keys.FirstOrDefault(key =>
            !TryGetSetOperatorFieldPosition(query, key, out _));
        if (missingKey == null)
            return true;

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportUnknownColumn(missingKey, availableFieldNames, node);
            return false;
        }

        throw new InvalidOperationException($"Unknown column '{missingKey}'.");
    }

    private static SetOperatorNode CreateSetOperatorNode(string setOperatorName, SetOperatorNode node, Node left, Node right)
    {
        return setOperatorName switch
        {
            "Union" => new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "UnionAll" => new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne),
            "Except" => new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            "Intersect" => new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
                node.IsTheLastOne),
            _ => throw new NotSupportedException($"Set operator '{setOperatorName}' is not supported.")
        };
    }
}
