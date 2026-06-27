using System.Linq;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ISchemaTable table;
        ISchema schema;

        if (_sourceBinding.AliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
            table = GetTableFromSchema(schema, schemaFrom);
        }
        else
        {
            var name = _sourceBinding.AliasMapToInMemoryTableMap[node.SourceAlias];
            table = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.SourceAlias).FullTable;
            schema = new TransitionSchema(name, table);
        }

        _sourceBinding.AliasMapToInMemoryTableMap.Add(node.Alias, node.SourceAlias);

        var targetColumn = table.GetColumnByName(node.FirstProperty.PropertyName);
        if (targetColumn == null)
        {
            if (TryReportOrThrowUnknownColumn(node.FirstProperty.PropertyName, table.Columns, node))
            {
                PushErrorRecoveryState(node, schema);
                return;
            }

            return;
        }

        if (ValidateBindablePropertyAsTableWithDiagnostics(table, targetColumn, node))
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        AddAssembly(targetColumn.ColumnType.Assembly);


        if (node.PropertiesChain.Length > 1
            && targetColumn.ColumnType == typeof(object)
            && !string.IsNullOrEmpty(targetColumn.IntendedTypeName)
            && SchemaRegistry != null)
        {
            var resolved = ResolveSchemaPropertyChain(
                targetColumn.IntendedTypeName,
                node.PropertiesChain.Skip(1).ToArray());

            if (resolved != null)
            {
                var resolvedNestedTable = TurnTypeIntoTableWithIntendedTypeName(
                    resolved.Value.ClrType,
                    resolved.Value.IntendedTypeName,
                    node);
                if (resolvedNestedTable == null)
                {
                    PushErrorRecoveryState(node, schema);
                    return;
                }

                table = resolvedNestedTable;

                UpdateQueryAliasAndSymbolTable(node, schema, table);
                var resolvedChain = new PropertyFromNode.PropertyNameAndTypePair[node.PropertiesChain.Length];
                resolvedChain[0] = new PropertyFromNode.PropertyNameAndTypePair(
                    targetColumn.ColumnName, targetColumn.ColumnType, targetColumn.IntendedTypeName);
                for (var i = 1; i < node.PropertiesChain.Length; i++)
                {
                    var propType = i == node.PropertiesChain.Length - 1
                        ? resolved.Value.ClrType
                        : typeof(object);
                    resolvedChain[i] = new PropertyFromNode.PropertyNameAndTypePair(
                        node.PropertiesChain[i].PropertyName, propType);
                }

                Nodes.Push(
                    new Parser.PropertyFromNode(
                        node.Alias,
                        node.SourceAlias,
                        resolvedChain
                    )
                );
                return;
            }
        }

        var followedType = FollowPropertiesWithDiagnostics(targetColumn.ColumnType, node.PropertiesChain, node);
        if (followedType == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        var nestedTable = TurnTypeIntoTableWithIntendedTypeName(
            followedType,
            targetColumn.IntendedTypeName,
            node);
        if (nestedTable == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        table = nestedTable;

        UpdateQueryAliasAndSymbolTable(node, schema, table);

        var rewrittenChain =
            RewritePropertiesChainWithTargetColumnWithDiagnostics(targetColumn, node.PropertiesChain, node);
        if (rewrittenChain == null)
        {
            PushErrorRecoveryState(node, schema);
            return;
        }

        Nodes.Push(
            new Parser.PropertyFromNode(
                node.Alias,
                node.SourceAlias,
                rewrittenChain
            )
        );
    }

    public override void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ISchemaTable table;
        ISchema schema;

        if (_sourceBinding.AliasToSchemaFromNodeMap.TryGetValue(node.SourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
        }
        else
        {
            var name = _sourceBinding.AliasMapToInMemoryTableMap[node.SourceAlias];
            table = FindTableSymbolInScopeHierarchy(name).FullTable;
            schema = new TransitionSchema(name, table);
        }

        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var accessMethodNode = (AccessMethodNode)Nodes.Pop();
        var convertedTable = TurnTypeIntoTableWithDiagnostics(accessMethodNode.ReturnType, node);
        if (convertedTable == null)
            return;
        table = convertedTable;
        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
        _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, node.SourceAlias);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

        Nodes.Push(new Parser.AccessMethodFromNode(node.Alias, node.SourceAlias, accessMethodNode,
            accessMethodNode.ReturnType));
    }
}
