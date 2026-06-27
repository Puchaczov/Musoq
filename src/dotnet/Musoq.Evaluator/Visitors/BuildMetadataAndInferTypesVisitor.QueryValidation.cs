using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using NotSupportedException = System.NotSupportedException;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void ValidateGroupBySemantics(SelectNode select, GroupByNode groupBy)
    {
        var groupByExpressionStrings = new HashSet<string>(
            groupBy.Fields.Select(f => f.Expression.ToString()),
            StringComparer.OrdinalIgnoreCase);

        var groupByColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in groupBy.Fields) CollectColumnNames(field.Expression, groupByColumnNames);

        foreach (var field in select.Fields)
        {
            if (IsConstantExpression(field.Expression))
                continue;

            if (groupByExpressionStrings.Contains(field.Expression.ToString()))
                continue;

            if (ContainsAggregateFunction(field.Expression))
                continue;

            var nonGroupedColumns = new List<string>();
            FindNonGroupedColumns(field.Expression, groupByExpressionStrings, groupByColumnNames, nonGroupedColumns);

            if (nonGroupedColumns.Count <= 0)
                continue;

            var columnName = nonGroupedColumns[0];
            var groupByNames = groupBy.Fields
                .Select(f => f.Expression.ToString())
                .ToArray();

            if (TryReportNonAggregatedColumnInSelect(columnName, groupByNames, field.Expression))
                continue;

            throw new NonAggregatedColumnInSelectException(columnName, groupByNames,
                field.Expression.HasSpan ? field.Expression.Span : TextSpan.Empty);
        }
    }

    private bool TryReportNonAggregatedColumnInSelect(string columnName, string[] groupByColumns, Node? node)
    {
        if (DiagnosticContext != null)
        {
            var groupByList = groupByColumns.Length > 0
                ? string.Join(", ", groupByColumns)
                : "(none)";
            DiagnosticContext.ReportError(
                DiagnosticCode.MQ3012_NonAggregateInSelect,
                $"Column '{columnName}' must appear in the GROUP BY clause or be used in an aggregate function. " +
                $"Current GROUP BY columns: {groupByList}.",
                node);
            return true;
        }

        return false;
    }

    private void ValidateSelectFieldsArePrimitive(FieldNode[] fields, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation) return;

        foreach (var field in fields)
        {
            var returnType = field.Expression.ReturnType;
            if (!IsValidQueryExpressionType(returnType))
            {
                if (TryReportInvalidExpressionType(field, returnType, context, field.Expression))
                    continue;
                throw new InvalidQueryExpressionTypeException(field, returnType, context);
            }
        }
    }

    private void ValidateExpressionIsPrimitive(Node expression, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation) return;

        var returnType = expression.ReturnType;
        if (!IsValidQueryExpressionType(returnType))
        {
            if (TryReportInvalidExpressionType(expression.ToString(), returnType, context, expression))
                return;
            throw new InvalidQueryExpressionTypeException(expression.ToString(), returnType, context);
        }
    }

    private void ValidateExpressionIsBoolean(Node expression, string context)
    {
        _queryValidationService.ValidateExpressionIsBoolean(expression, context);
    }

    private static string CreateBooleanContextTypeMismatchMessage(Node expression, Type expressionType, string context)
    {
        var subject = string.Equals(context, "CASE WHEN", StringComparison.Ordinal)
            ? "CASE WHEN requires a boolean expression"
            : $"{context} clause requires a boolean expression";

        return TryFindFirstScriptParameterReference(expression, out var parameter)
            ? $"{subject}, but script parameter '${parameter.Name}' has type '{FormatTypeName(parameter.ReturnType ?? expressionType)}'."
            : $"{subject}, but got '{expressionType.Name}'.";
    }

    private void ValidateOrderByExpression(FieldOrderedNode field)
    {
        if (field.Expression is not IntegerNode integerNode)
            return;

        if (!string.IsNullOrEmpty(field.FieldName) &&
            !string.Equals(field.FieldName, integerNode.ToString(), StringComparison.Ordinal))
            return;

        const string message = "ORDER BY column position is not supported. Use a column name or alias instead of a numeric position.";

        if (TryReportSemanticError<NotSupportedException>(DiagnosticCode.MQ2030_UnsupportedSyntax, message, field))
            return;

        throw new NotSupportedException(message);
    }
}
