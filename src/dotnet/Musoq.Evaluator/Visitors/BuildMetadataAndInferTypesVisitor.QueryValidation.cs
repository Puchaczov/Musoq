using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using NotSupportedException = System.NotSupportedException;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private void ValidateGroupBySemantics(SelectNode select, GroupByNode groupBy)
    {
        _queryValidationService.ValidateGroupBySemantics(select, groupBy);
    }

    private void ValidateSelectFieldsArePrimitive(FieldNode[] fields, string context)
    {
        _queryValidationService.ValidateSelectFieldsArePrimitive(fields, context);
    }

    private void ValidateExpressionIsPrimitive(Node expression, string context)
    {
        _queryValidationService.ValidateExpressionIsPrimitive(expression, context);
    }

    private void ValidateExpressionIsBoolean(Node expression, string context)
    {
        _queryValidationService.ValidateExpressionIsBoolean(expression, context);
    }

    private void ValidateOrderByExpression(FieldOrderedNode field)
    {
        if (TryGetEnumExpressionType(field.Expression, out var enumType))
        {
            ReportEnumSemanticError(
                DiagnosticCode.MQ3110_UnsupportedEnumOperator,
                $"ORDER BY is not supported for enum type '{enumType.DisplayName}' in v1.",
                field);
            return;
        }

        if (field.Expression is not IntegerNode integerNode)
            return;

        if (!string.IsNullOrEmpty(field.FieldName) &&
            !string.Equals(field.FieldName, integerNode.ToString(), StringComparison.Ordinal))
            return;

        const string message = "ORDER BY column position is not supported. Use a column name or alias instead of a numeric position.";

        if (TryReportSemanticError<NotSupportedException>(DiagnosticCode.MQ3093_OrderByOrdinalUnsupported, message, field))
            return;

        throw new NotSupportedException(message);
    }
}
