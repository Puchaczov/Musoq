using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private FieldNode[] CreateFields(FieldNode[] oldFields)
    {
        var reorderedList = new FieldNode[oldFields.Length];
        for (var i = reorderedList.Length - 1; i >= 0; i--)
            reorderedList[i] = PopSemanticNode() as FieldNode
                               ?? throw new VisitorException(
                                   VisitorName,
                                   "CreateFields",
                                   "Expected SELECT field node on visitor stack.");

        var fields = new List<FieldNode>(reorderedList.Length);
        var positionCounter = 0;

        foreach (var field in reorderedList)
            if (field.Expression is AllColumnsNode allColumnsNode)
                _resultShapeBindingService.AddAllColumnsFields(_sourceBinding, fields, allColumnsNode, ref positionCounter);
            else
                fields.Add(new FieldNode(field.Expression, positionCounter++, field.FieldName, field.HasExplicitFieldName));

        return fields.ToArray();
    }

    private void CollectSelectFieldAliases(FieldNode[] fields)
    {
        _resultShape.SelectFieldAliases.Clear();

        foreach (var field in fields)
        {
            if (field.Expression is AllColumnsNode)
                continue;

            var expressionText = field.Expression.ToString();
            var alias = field.FieldName;

            if (string.IsNullOrEmpty(alias))
                continue;

            if (string.Equals(alias, expressionText, StringComparison.Ordinal))
                continue;

            _resultShape.SelectFieldAliases.TryAdd(alias, field.Expression);
        }
    }

    private List<FieldNode> GetOrCreateGeneratedColumns(string identifier)
    {
        if (!_resultShape.GeneratedColumns.TryGetValue(identifier, out var generatedColumns))
        {
            generatedColumns = [];
            _resultShape.GeneratedColumns.Add(identifier, generatedColumns);
        }
        else
        {
            generatedColumns.Clear();
        }

        return generatedColumns;
    }

    private void AddColumnToGeneratedColumns(TableSymbol tableSymbol, ISchemaColumn column, int index,
        string identifier, List<FieldNode> generatedColumns, bool isCompoundTable = false, string? outputName = null)
    {
        AddAssembly(column.ColumnType.Assembly);

        var accessColumn = new AccessColumnNode(column.ColumnName, identifier, column.ColumnType, TextSpan.Empty,
            column.IntendedTypeName);
        var fieldName = outputName ?? (isCompoundTable
            ? $"{identifier}.{column.ColumnName}"
            : tableSymbol.HasAlias ? $"{identifier}.{column.ColumnName}" : column.ColumnName);
        generatedColumns.Add(new FieldNode(accessColumn, index, fieldName, false));
    }

    private void UpdateUsedColumns(string identifier, ISchemaTable table)
    {
        if (_sourceBinding.AliasToSchemaFromNodeMap.TryGetValue(identifier, out var schemaFromNode))
            _sourceBinding.UsedColumns[schemaFromNode] = table.Columns.ToList();
    }
}
