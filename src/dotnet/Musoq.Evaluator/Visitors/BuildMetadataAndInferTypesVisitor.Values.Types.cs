using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static Type ResolveValuesColumnType(string columnName, IReadOnlyList<Node> expressions, ValuesFromNode node)
    {
        return CommonColumnTypeResolver.Resolve(
            columnName,
            expressions,
            ValuesSourceSpanHelpers.GetColumnExpressionSpan(expressions, node),
            CommonColumnTypeDiagnosticKind.Values);
    }

    private static ValuesRowNode[] RetypeValuesNulls(ValuesRowNode[] rows, IReadOnlyList<ISchemaColumn> columns)
    {
        var result = new ValuesRowNode[rows.Length];
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            var fields = new ValuesFieldNode[row.Fields.Count];
            for (var fieldIndex = 0; fieldIndex < row.Fields.Count; fieldIndex++)
            {
                var field = row.Fields[fieldIndex];
                var column = columns.Single(column =>
                    string.Equals(column.ColumnName, field.Name, StringComparison.OrdinalIgnoreCase));
                var expression = CommonColumnTypeResolver.IsExplicitNullType(field.Expression.ReturnType)
                    ? new NullNode(column.ColumnType, field.Expression.Span)
                    : field.Expression;
                fields[fieldIndex] = new ValuesFieldNode(field.Name, expression, field.NameSpan);
            }

            result[rowIndex] = new ValuesRowNode(fields, row.Span);
        }

        return result;
    }
}
