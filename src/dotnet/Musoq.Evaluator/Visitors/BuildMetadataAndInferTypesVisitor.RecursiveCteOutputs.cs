using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private string? _activeRecursiveCteName;

    internal void VisitRecursiveCteBoundary(string cteName, SetOperatorNode boundary)
    {
        var previousName = _activeRecursiveCteName;
        _activeRecursiveCteName = cteName;
        try
        {
            boundary.Accept(this);
        }
        finally
        {
            _activeRecursiveCteName = previousName;
        }
    }

    private void ValidateRecursiveCteOutput(
        QueryNode anchor,
        QueryNode recursiveMember,
        string cachedSetOperatorKey,
        string cteName)
    {
        var anchorFields = anchor.Select.Fields;
        var memberFields = recursiveMember.Select.Fields;
        var aggregate = FindRecursiveAggregate(recursiveMember);
        if (aggregate != null)
        {
            _queryState.CachedSetFields.TryAdd(cachedSetOperatorKey, anchorFields);
            ReportUnsupportedRecursiveOperator("aggregation", aggregate);
            return;
        }

        if (anchorFields.Length != memberFields.Length)
        {
            ReportRecursiveCteOutputMismatch(
                $"CTE '{cteName}' anchor projects {anchorFields.Length} column(s), but its recursive member projects {memberFields.Length}.",
                recursiveMember.Select);
            return;
        }

        for (var index = 0; index < anchorFields.Length; index++)
        {
            var anchorType = anchorFields[index].Expression.ReturnType ?? typeof(object);
            var memberType = memberFields[index].Expression.ReturnType ?? typeof(object);
            if (anchorType == memberType)
                continue;

            if (anchorType is NullNode.NullType)
            {
                anchorFields[index] = ContextualizeNull(
                    anchorFields[index],
                    MakeNullableWhenRequired(memberType));
                continue;
            }

            if (memberType is NullNode.NullType)
            {
                memberFields[index] = ContextualizeNull(memberFields[index], anchorType);
                continue;
            }

            var columnName = anchorFields[index].FieldName;
            ReportRecursiveCteOutputMismatch(
                $"CTE '{cteName}' column {index + 1} '{columnName}' has anchor type " +
                $"'{FormatRecursiveCteType(anchorType)}', but its recursive member produces " +
                $"'{FormatRecursiveCteType(memberType)}'. Cast the anchor expression explicitly using postfix syntax (::Type).",
                memberFields[index].Expression);
        }

        _queryState.CachedSetFields.TryAdd(
            cachedSetOperatorKey,
            ResolveFieldsForCache(anchorFields, memberFields));
        UpdateProvisionalRecursiveColumns(cteName, anchorFields);
    }

    private static FieldNode ContextualizeNull(FieldNode field, Type type)
    {
        return new FieldNode(
            new NullNode(type, field.Expression.Span),
            field.FieldOrder,
            field.FieldName,
            field.Span);
    }

    private void UpdateProvisionalRecursiveColumns(string cteName, FieldNode[] anchorFields)
    {
        if (!_provisionalRecursiveCteColumns.TryGetValue(cteName, out var columns) ||
            columns.Length != anchorFields.Length)
        {
            return;
        }

        var updated = new ISchemaColumn[columns.Length];
        for (var index = 0; index < columns.Length; index++)
        {
            updated[index] = new SchemaColumn(
                columns[index].ColumnName,
                columns[index].ColumnIndex,
                anchorFields[index].Expression.ReturnType ?? columns[index].ColumnType);
        }

        _provisionalRecursiveCteColumns[cteName] = updated;
    }

    private static Type MakeNullableWhenRequired(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null
            ? typeof(Nullable<>).MakeGenericType(type)
            : type;
    }

    private void ReportRecursiveCteOutputMismatch(string details, Node node)
    {
        var message = ErrorCatalog.GetMessage(
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
            details);
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
                message,
                node);
            return;
        }

        throw new RecursiveCteValidationException(
            DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
            message,
            node.SpanOrEmpty());
    }

    private void ReportUnsupportedRecursiveOperator(string operatorName, Node node)
    {
        var message = ErrorCatalog.GetMessage(
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            operatorName);
        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
                message,
                node);
            return;
        }

        throw new RecursiveCteValidationException(
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            message,
            node.SpanOrEmpty());
    }

    private static AccessMethodNode? FindRecursiveAggregate(Node node)
    {
        if (node is AccessMethodNode { IsAggregate: true } aggregate)
            return aggregate;

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
        {
            var childAggregate = FindRecursiveAggregate(child);
            if (childAggregate != null)
                return childAggregate;
        }

        return null;
    }

    private static string FormatRecursiveCteType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        return underlying == null ? type.Name : $"{underlying.Name}?";
    }
}
