using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticQueryValidationService(
    SemanticDiagnosticReporter diagnosticReporter,
    CompilationOptions? compilationOptions = null)
{
    private readonly CompilationOptions _compilationOptions = compilationOptions ?? new CompilationOptions();

    public void ValidateExpressionIsBoolean(Node expression, string context)
    {
        var expressionType = BinaryOperatorTypeRules.NormalizeOperandType(expression.ReturnType);
        if (BinaryOperatorTypeRules.CanSkipStaticTypeValidation(expressionType))
            return;

        if (expressionType == typeof(bool))
            return;

        var message = SemanticExpressionDiagnosticFacts.CreateBooleanContextTypeMismatchMessage(
            expression,
            expressionType,
            context);

        if (diagnosticReporter.TryReportTypeMismatch(message, expression))
            return;

        throw new TypeMismatchException(
            typeof(bool),
            expressionType,
            expression.HasSpan ? expression.Span : TextSpan.Empty);
    }

    public void ValidateSelectFieldsArePrimitive(FieldNode[] fields, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation)
            return;

        foreach (var field in fields)
        {
            var returnType = field.Expression.ReturnType;
            if (IsValidQueryExpressionType(returnType))
                continue;

            if (diagnosticReporter.TryReportInvalidExpressionType(field, returnType, context, field.Expression))
                continue;

            throw new InvalidQueryExpressionTypeException(field, returnType, context);
        }
    }

    public void ValidateExpressionIsPrimitive(Node expression, string context)
    {
        if (!_compilationOptions.UsePrimitiveTypeValidation)
            return;

        var returnType = expression.ReturnType;
        if (IsValidQueryExpressionType(returnType))
            return;

        if (diagnosticReporter.TryReportInvalidExpressionType(expression.ToString(), returnType, context, expression))
            return;

        throw new InvalidQueryExpressionTypeException(expression.ToString(), returnType, context);
    }

    public void ValidateGroupBySemantics(SelectNode select, GroupByNode groupBy)
    {
        var groupByExpressionStrings = new HashSet<string>(
            groupBy.Fields.Select(f => f.Expression.ToString()),
            StringComparer.OrdinalIgnoreCase);

        var groupByColumnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in groupBy.Fields)
            CollectColumnNames(field.Expression, groupByColumnNames);

        var nonAggregatedFields = new List<(FieldNode Field, string ColumnName)>();
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

            nonAggregatedFields.Add((field, nonGroupedColumns[0]));
        }

        var groupByNames = groupBy.Fields
            .Select(f => f.Expression.ToString())
            .ToArray();
        var groupByFields = groupBy.Fields
            .Select(field => (field.Expression, GetExpressionSpan(field.Expression)))
            .ToArray();

        foreach (var (field, columnName) in nonAggregatedFields)
        {

            if (diagnosticReporter.TryReportNonAggregatedColumnInSelect(
                    columnName,
                    groupByNames,
                    groupByFields,
                    field))
                continue;

            throw new NonAggregatedColumnInSelectException(columnName, groupByNames,
                field.SpanOrEmpty());
        }
    }

    private static TextSpan GetExpressionSpan(Node expression)
    {
        if (expression.HasSpan)
            return expression.Span;

        var childSpans = ParserNodeTraversalRegistry.EnumerateChildren(expression)
            .Select(GetExpressionSpan)
            .Where(static span => !span.IsEmpty)
            .ToArray();
        if (childSpans.Length == 0)
            return TextSpan.Empty;

        return childSpans[0].Through(childSpans[^1]);
    }
}
