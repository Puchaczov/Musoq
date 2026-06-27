using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.QueryAlias = _resultShapeBindingService.CreateAlias(node.Alias, _sourceBinding.SchemaFromKey);

        if (HasAlreadyUsedAlias(_sourceBinding.QueryAlias))
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new AliasAlreadyUsedException(_sourceBinding.QueryAlias, span);
        }

        _resultShapeBindingService.RegisterAlias(_sourceBinding.QueryAlias);

        var rows = PopValuesRows(node);
        var columns = ValidateValuesRowsAndCreateColumns(rows, node);
        rows = RetypeValuesNulls(rows, columns);

        var table = new DynamicTable(columns);
        var schema = new TransitionSchema(_sourceBinding.QueryAlias, table);
        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));

        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
        _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, _sourceBinding.QueryAlias);
        _sourceBinding.InferredColumnsByAlias[_sourceBinding.QueryAlias] = columns;
        _sourceBinding.UsedSchemasQuantity += 1;

        var valuesFromNode = new ValuesFromNode(rows, _sourceBinding.QueryAlias, typeof(object));
        if (node.HasSpan)
            valuesFromNode.WithSpan(node.Span);
        if (!node.FullSpan.IsEmpty)
            valuesFromNode.WithFullSpan(node.FullSpan);

        Nodes.Push(valuesFromNode);
    }

}
