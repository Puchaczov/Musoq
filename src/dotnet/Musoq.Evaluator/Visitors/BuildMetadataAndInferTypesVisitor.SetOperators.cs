using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
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

        ReconcileFieldTypesForSetOperation(leftFields, rightFields);

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

        ReconcileFieldTypesForSetOperation(leftFields, rightFields);

        _queryState.CachedSetFields.TryAdd(currentSetOperatorKey, ResolveFieldsForCache(leftFields, rightFields));
    }

    private void ReconcileFieldTypesForSetOperation(FieldNode[] leftFields, FieldNode[] rightFields)
    {
        for (var i = 0; i < leftFields.Length; i++)
        {
            var leftType = leftFields[i].Expression.ReturnType ?? typeof(object);
            var rightType = rightFields[i].Expression.ReturnType ?? typeof(object);
            var leftHasEnum = TryGetEnumExpressionType(leftFields[i].Expression, out var leftEnum);
            var rightHasEnum = TryGetEnumExpressionType(rightFields[i].Expression, out var rightEnum);

            if (leftHasEnum || rightHasEnum)
            {
                if (leftHasEnum && rightHasEnum)
                {
                    if (!leftEnum.Equals(rightEnum))
                        ReportEnumIdentityMismatch(leftEnum, rightEnum, rightFields[i]);
                }
                else
                {
                    var enumType = leftHasEnum ? leftEnum : rightEnum;
                    var ordinaryField = leftHasEnum ? rightFields[i] : leftFields[i];
                    if (ordinaryField.Expression is NullNode)
                    {
                        var enumField = leftHasEnum ? leftFields[i] : rightFields[i];
                        var contextualNull = new NullNode(
                            enumField.Expression.ReturnType ??
                            EnumScalarTypeFacts.GetCarrierType(enumType.UnderlyingKind),
                            ordinaryField.Expression.Span);
                        MarkEnumExpression(contextualNull, enumType);
                        var replacement = new FieldNode(
                            contextualNull,
                            ordinaryField.FieldOrder,
                            ordinaryField.FieldName,
                            ordinaryField.Span);
                        if (leftHasEnum)
                            rightFields[i] = replacement;
                        else
                            leftFields[i] = replacement;
                    }
                    else
                    {
                        ReportEnumSemanticError(
                            DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                            $"SET operands cannot combine enum type '{enumType.DisplayName}' with an ordinary '{ordinaryField.Expression.ReturnType?.Name ?? "unknown"}' value.",
                            ordinaryField);
                    }
                }

                if (leftType == rightType ||
                    leftFields[i].Expression is NullNode ||
                    rightFields[i].Expression is NullNode)
                    continue;
            }

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

            if (TryReportSetOperatorColumnTypes(leftFields[i], rightFields[i], rightFields[i]))
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

        throw new UnknownAliasException(alias, node.SpanOrEmpty(), GetVisibleAliases());
    }

    private void VisitSetOperationNode(SetOperatorNode node, string setOperatorName)
    {
        var right = PopSemanticNode();
        var left = PopSemanticNode();

        if (left is not QueryNode leftQuery)
            throw new InvalidOperationException($"Expected left side of {setOperatorName} to be a query node.");

        var keys = node.Keys;
        if (node.Keys.Length > 0 && _activeRecursiveCteName != null)
        {
            if (!TryCanonicalizeRecursiveSetOperatorKeys(leftQuery, node.Keys, node, out keys))
            {
                PushSemanticNode(left);
                PushSemanticNode(right);
                PushSemanticNode(CreateSetOperatorNode(setOperatorName, node, keys, left, right));
                return;
            }
        }
        else if (node.Keys.Length > 0 && !ValidateSetOperatorKeys(leftQuery, node.Keys, node))
        {
            PushSemanticNode(left);
            PushSemanticNode(right);
            PushSemanticNode(CreateSetOperatorNode(setOperatorName, node, keys, left, right));
            return;
        }

        var key = CreateSetOperatorPositionKey();
        _sourceBinding.CurrentScope[MetaAttributes.SetOperatorName] = key;

        if (right is QueryNode rightAsQueryNode)
        {
            if (_activeRecursiveCteName is { } recursiveCteName)
                ValidateRecursiveCteOutput(leftQuery, rightAsQueryNode, key, recursiveCteName);
            else
                MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, rightAsQueryNode, key);
        }
        else
            MakeSureBothSideFieldsAreOfAssignableTypes(leftQuery, PreviousSetOperatorPositionKey(), key);

        MutableSetOperatorFieldPositions.Add(key,
            CreateSetOperatorPositionIndexes(leftQuery, keys));
        MutableSetOperatorFieldTypes.Add(key,
            CreateSetOperatorPositionTypes(leftQuery, keys));

        var rightMethodName = TraversalFrame.PopMethod(VisitorName, "Visit(SetOperatorNode).RightMethod");
        var leftMethodName = TraversalFrame.PopMethod(VisitorName, "Visit(SetOperatorNode).LeftMethod");

        var methodName = $"{leftMethodName}_{setOperatorName}_{rightMethodName}";
        TraversalFrame.PushMethod(methodName);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(methodName,
            _sourceBinding.CurrentScope.Child[0].ScopeSymbolTable.GetSymbol(leftQuery.From.Alias));

        PushSemanticNode(CreateSetOperatorNode(setOperatorName, node, keys, left, right));
    }

}
