using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private ValuesRowNode[] PopValuesRows(ValuesFromNode node)
    {
        var rows = new ValuesRowNode[node.Rows.Count];
        for (var rowIndex = node.Rows.Count - 1; rowIndex >= 0; rowIndex--)
        {
            var sourceRow = node.Rows[rowIndex];
            var fields = new ValuesFieldNode[sourceRow.Fields.Count];
            for (var fieldIndex = sourceRow.Fields.Count - 1; fieldIndex >= 0; fieldIndex--)
            {
                var sourceField = sourceRow.Fields[fieldIndex];
                fields[fieldIndex] = new ValuesFieldNode(
                    sourceField.Name,
                    PopSemanticNode("Visit(ValuesFromNode).Field"),
                    sourceField.NameSpan);
            }

            rows[rowIndex] = new ValuesRowNode(fields, sourceRow.Span);
        }

        return rows;
    }

    private static ISchemaColumn[] ValidateValuesRowsAndCreateColumns(ValuesRowNode[] rows, ValuesFromNode node)
    {
        if (rows.Length == 0)
            throw CreateValuesSourceException("VALUES source requires at least one row.", node);

        var firstRow = rows[0];
        if (firstRow.Fields.Count == 0)
            throw CreateValuesSourceException("VALUES rows require at least one field.", node);

        ValidateValuesRowHasNoDuplicateFields(firstRow, 0, node);

        var columnNames = firstRow.Fields.Select(field => field.Name).ToArray();
        var columnNameSet = new HashSet<string>(columnNames, StringComparer.OrdinalIgnoreCase);
        var columnTypes = new Type[columnNames.Length];

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = rows[rowIndex];
            ValidateValuesRowHasNoDuplicateFields(row, rowIndex, node);

            if (row.Fields.Count != columnNames.Length)
                ThrowValuesShapeMismatch(row, rowIndex, columnNames, columnNameSet, node);

            foreach (var field in row.Fields)
            {
                if (!columnNameSet.Contains(field.Name))
                    ThrowUnexpectedValuesField(field, rowIndex, columnNames, node);

                if (!ValuesStaticExpressionRules.IsStaticScalarExpression(field.Expression))
                    throw new ValuesSourceException(
                        $"VALUES field '{field.Name}' must be a constant literal expression or scalar script parameter/let expression. Use literals, NULL, scalar script parameters, scalar let variables, or arithmetic over them.",
                        GetExpressionSpan(field, node));
            }
        }

        for (var columnIndex = 0; columnIndex < columnNames.Length; columnIndex++)
        {
            var columnName = columnNames[columnIndex];
            var expressions = rows
                .Select(row => row.Fields.Single(field => string.Equals(field.Name, columnName, StringComparison.OrdinalIgnoreCase)).Expression)
                .ToArray();

            columnTypes[columnIndex] = ResolveValuesColumnType(columnName, expressions, node);
        }

        var columns = new ISchemaColumn[columnNames.Length];
        for (var index = 0; index < columnNames.Length; index++)
            columns[index] = new SchemaColumn(columnNames[index], index, columnTypes[index]);

        return columns;
    }

    private static void ThrowValuesShapeMismatch(
        ValuesRowNode row,
        int rowIndex,
        string[] columnNames,
        HashSet<string> columnNameSet,
        ValuesFromNode node)
    {
        var rowNames = row.Fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingField = columnNames.FirstOrDefault(columnName => !rowNames.Contains(columnName));
        if (!string.IsNullOrEmpty(missingField))
        {
            throw new ValuesSourceException(
                $"VALUES row {rowIndex + 1} is missing field '{missingField}'. Every row must contain the same fields as the first row: {string.Join(", ", columnNames)}.",
                GetRowSpan(row, node));
        }

        var unexpectedField = row.Fields.FirstOrDefault(field => !columnNameSet.Contains(field.Name));
        if (unexpectedField != null)
            ThrowUnexpectedValuesField(unexpectedField, rowIndex, columnNames, node);

        throw new ValuesSourceException(
            $"VALUES row {rowIndex + 1} must contain exactly {columnNames.Length} field(s): {string.Join(", ", columnNames)}.",
            GetRowSpan(row, node));
    }

    private static void ThrowUnexpectedValuesField(
        ValuesFieldNode field,
        int rowIndex,
        IReadOnlyList<string> columnNames,
        ValuesFromNode node)
    {
        throw new ValuesSourceException(
            $"VALUES row {rowIndex + 1} contains unexpected field '{field.Name}'. Expected fields are: {string.Join(", ", columnNames)}.",
            GetFieldNameSpan(field, node));
    }

    private static void ValidateValuesRowHasNoDuplicateFields(ValuesRowNode row, int rowIndex, ValuesFromNode node)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in row.Fields)
            if (!names.Add(field.Name))
                throw new ValuesSourceException(
                    $"VALUES row {rowIndex + 1} contains duplicate field '{field.Name}'. Field names in a VALUES row are case-insensitive and must be unique.",
                    GetFieldNameSpan(field, node));
    }

}
