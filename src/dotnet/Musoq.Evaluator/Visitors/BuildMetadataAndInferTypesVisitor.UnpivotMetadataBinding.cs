using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(UnpivotFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var keepFields = PopUnpivotKeepFields(node);
        var entries = PopUnpivotEntries(node);
        var source = SafeCast<FromNode>(PopSemanticNode("Visit(UnpivotFromNode).Source"), "Visit(UnpivotFromNode).Source");

        var valueType = ResolveUnpivotValueColumnType(node.ValueColumn, entries.Select(entry => entry.Expression).ToArray(), node);
        entries = RetypeUnpivotNulls(entries, valueType);
        var columns = CreateUnpivotColumns(node, keepFields, valueType);

        RegisterUnpivotSource(node, columns);

        var unpivot = new UnpivotFromNode(source, node.NameColumn, node.ValueColumn, entries, keepFields, typeof(object));
        if (node.HasSpan)
            unpivot.WithSpan(node.Span);
        if (!node.FullSpan.IsEmpty)
            unpivot.WithFullSpan(node.FullSpan);

        PushSemanticNode(unpivot);
    }

    private FieldNode[] PopUnpivotKeepFields(UnpivotFromNode node)
    {
        var keepFields = new FieldNode[node.KeepFields.Count];
        for (var index = node.KeepFields.Count - 1; index >= 0; index--)
        {
            keepFields[index] = SafeCast<FieldNode>(
                PopSemanticNode("Visit(UnpivotFromNode).KeepField"),
                "Visit(UnpivotFromNode).KeepField");
        }

        return keepFields;
    }

    private UnpivotEntryNode[] PopUnpivotEntries(UnpivotFromNode node)
    {
        var entries = new UnpivotEntryNode[node.Entries.Count];
        for (var index = node.Entries.Count - 1; index >= 0; index--)
        {
            var sourceEntry = node.Entries[index];
            entries[index] = new UnpivotEntryNode(
                PopSemanticNode("Visit(UnpivotFromNode).Entry"),
                sourceEntry.NameValue,
                sourceEntry.NameValueSpan);
        }

        return entries;
    }

    private static ISchemaColumn[] CreateUnpivotColumns(
        UnpivotFromNode node,
        IReadOnlyList<FieldNode> keepFields,
        Type valueType)
    {
        var columns = new List<ISchemaColumn>(keepFields.Count + 2);

        foreach (var keepField in keepFields)
        {
            columns.Add(new SchemaColumn(
                keepField.FieldName,
                columns.Count,
                keepField.Expression.ReturnType ?? typeof(object)));
        }

        columns.Add(new SchemaColumn(node.NameColumn, columns.Count, typeof(string)));
        columns.Add(new SchemaColumn(node.ValueColumn, columns.Count, valueType));

        return columns.ToArray();
    }

    private void RegisterUnpivotSource(UnpivotFromNode node, ISchemaColumn[] columns)
    {
        _sourceBinding.QueryAlias = UnpivotFromNode.DefaultAlias;

        if (HasAlreadyUsedAlias(_sourceBinding.QueryAlias))
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new AliasAlreadyUsedException(_sourceBinding.QueryAlias, span);
        }

        _resultShapeBindingService.RegisterAlias(_sourceBinding.QueryAlias);

        var table = new DynamicTable(columns);
        var schema = new TransitionSchema(_sourceBinding.QueryAlias, table);
        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, table, false);

        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
        _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, _sourceBinding.QueryAlias);
        _sourceBinding.InferredColumnsByAlias[_sourceBinding.QueryAlias] = columns;
        _sourceBinding.UsedSchemasQuantity += 1;

        foreach (var column in columns)
            AddAssembly(column.ColumnType.Assembly);
    }

    private static UnpivotEntryNode[] RetypeUnpivotNulls(UnpivotEntryNode[] entries, Type valueType)
    {
        var result = new UnpivotEntryNode[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var expression = CommonColumnTypeResolver.IsExplicitNullType(entry.Expression.ReturnType)
                ? new NullNode(valueType, entry.Expression.Span)
                : entry.Expression;
            result[index] = new UnpivotEntryNode(expression, entry.NameValue, entry.NameValueSpan);
        }

        return result;
    }

    private static Type ResolveUnpivotValueColumnType(
        string columnName,
        IReadOnlyList<Node> expressions,
        UnpivotFromNode node)
    {
        return CommonColumnTypeResolver.Resolve(
            columnName,
            expressions,
            GetUnpivotExpressionSpan(expressions, node),
            CommonColumnTypeDiagnosticKind.Unpivot);
    }

    private static TextSpan GetUnpivotExpressionSpan(IReadOnlyList<Node> expressions, UnpivotFromNode node)
    {
        var expression = expressions.FirstOrDefault(expression => expression.HasSpan);
        return expression?.Span ?? node.SpanOrEmpty();
    }
}
