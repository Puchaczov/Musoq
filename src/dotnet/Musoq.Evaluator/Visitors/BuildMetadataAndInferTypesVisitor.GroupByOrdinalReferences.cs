using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private GroupByNode? NormalizeGroupByOrdinals(SelectNode select, GroupByNode? groupBy, GroupByNode? originalGroupBy)
    {
        if (groupBy is null || groupBy.IsAll)
            return groupBy;

        var fields = new FieldNode[groupBy.Fields.Length];
        var needsDiagnosticRecovery = false;
        for (var index = 0; index < groupBy.Fields.Length; index++)
        {
            var field = groupBy.Fields[index];
            if (originalGroupBy?.Fields[index].Expression is not IntegerNode integerNode)
            {
                fields[index] = field;
                continue;
            }

            var ordinal = Convert.ToInt64(integerNode.ObjValue, CultureInfo.InvariantCulture);
            if (ordinal <= 0 || ordinal > select.Fields.Length)
            {
                needsDiagnosticRecovery = ReportGroupByOrdinalOutOfRange(ordinal, select.Fields.Length, field);
                fields[index] = field;
                continue;
            }

            var projectedField = select.Fields[(int)ordinal - 1];
            var normalizedField = new FieldNode(
                CloneExpression(projectedField.Expression),
                field.FieldOrder,
                field.HasExplicitFieldName ? field.FieldName : string.Empty,
                field.HasExplicitFieldName,
                field.Span);
            if (TryReportAggregateInGroupBy(normalizedField))
            {
                needsDiagnosticRecovery = true;
                fields[index] = normalizedField;
                continue;
            }

            fields[index] = normalizedField;
        }

        return needsDiagnosticRecovery
            ? CreateGroupByDiagnosticRecovery(select, groupBy)
            : new GroupByNode(fields, groupBy.Having, false, groupBy.Span);
    }

    private void EnsureGroupByFieldContainsNoAggregate(FieldNode field)
    {
        TryReportAggregateInGroupBy(field);
    }

    private bool TryReportAggregateInGroupBy(FieldNode field)
    {
        if (!BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(field.Expression))
            return false;

        const string message = "GROUP BY expressions cannot contain aggregate functions or aggregate SELECT aliases.";
        TryReportSemanticError<NotSupportedException>(DiagnosticCode.MQ3092_AggregateInGroupBy, message, field);
        return DiagnosticContext != null;
    }

    private bool ReportGroupByOrdinalOutOfRange(long ordinal, int selectFieldCount, Node node)
    {
        var message =
            $"GROUP BY position {ordinal} is out of range. SELECT projection contains {selectFieldCount} field(s).";

        if (DiagnosticContext != null)
        {
            DiagnosticContext.ReportError(DiagnosticCode.MQ3024_GroupByIndexOutOfRange, message, node);
            return true;
        }

        throw new GroupByIndexOutOfRangeException((int)ordinal, selectFieldCount, node.SpanOrEmpty());
    }

    private static GroupByNode CreateGroupByDiagnosticRecovery(SelectNode select, GroupByNode groupBy)
    {
        var fields = select.Fields
            .Where(field => !BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(field.Expression))
            .Select((field, index) => new FieldNode(CloneExpression(field.Expression), index, string.Empty))
            .ToArray();

        if (fields.Length == 0)
            fields = [new FieldNode(new IntegerNode("1", "s"), 0, string.Empty)];

        return new GroupByNode(fields, groupBy.Having, false, groupBy.Span);
    }
}
