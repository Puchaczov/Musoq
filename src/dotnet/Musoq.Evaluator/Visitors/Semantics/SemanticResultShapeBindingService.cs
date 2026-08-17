using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticResultShapeBindingService(ResultShapeState resultShape)
{
    public string CreateAlias(string alias, int schemaFromKey)
    {
        return AliasGenerator.CreateAliasIfEmpty(
            alias,
            resultShape.GeneratedAliases,
            schemaFromKey.ToString(CultureInfo.InvariantCulture));
    }

    public void RegisterAlias(string alias)
    {
        resultShape.GeneratedAliases.Add(alias);
    }

    public void AddAllColumnsFields(
        SourceBindingState sourceBinding,
        List<FieldNode> fields,
        AllColumnsNode allColumnsNode,
        ref int positionCounter)
    {
        var identifier = !string.IsNullOrWhiteSpace(allColumnsNode.Alias)
            ? allColumnsNode.Alias
            : sourceBinding.Identifier;

        if (resultShape.GeneratedColumns.TryGetValue(identifier, out var generatedColumns))
        {
            AppendGeneratedColumns(fields, generatedColumns, ref positionCounter);
            return;
        }

        if (!string.IsNullOrWhiteSpace(allColumnsNode.Alias))
            return;

        var tableSymbol = sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(sourceBinding.Identifier);
        foreach (var compoundTableIdentifier in tableSymbol.CompoundTables)
        {
            if (!resultShape.GeneratedColumns.TryGetValue(compoundTableIdentifier, out var compoundColumns))
                continue;

            AppendGeneratedColumns(fields, compoundColumns, ref positionCounter);
        }
    }

    private static void AppendGeneratedColumns(
        List<FieldNode> fields,
        List<FieldNode> generatedColumns,
        ref int positionCounter)
    {
        foreach (var column in generatedColumns)
            fields.Add(new FieldNode(column.Expression, positionCounter++, column.FieldName, column.HasExplicitFieldName));
    }
}
