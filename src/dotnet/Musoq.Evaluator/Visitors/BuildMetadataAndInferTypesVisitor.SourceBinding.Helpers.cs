using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool HasAlreadyUsedAlias(string queryAlias)
    {
        return _sourceBindingService.HasAlreadyUsedAlias(queryAlias);
    }

    private ISchemaTable GetTableFromSchema(ISchema schema, SchemaFromNode schemaFrom)
    {
        var metadataContext = new SourceMetadataContext(
            schemaFrom.QueryId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CancellationToken.None,
            GetColumnsForAlias(schemaFrom.Alias, _sourceBinding.SchemaFromKey),
            GetResolvedSourceRuntimeSettings(GetSourceContextIdForAlias(schemaFrom.Alias, schemaFrom.Id)),
            _logger
        );

        return schema.GetTableByName(schemaFrom.Method, metadataContext, SchemaArgumentBinder.BindStaticArguments(schemaFrom.Parameters));
    }

    private void UpdateQueryAliasAndSymbolTable(PropertyFromNode node, ISchema schema, ISchemaTable table)
    {
        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;

        if (!string.IsNullOrEmpty(node.Alias) && table.Columns is { Length: > 0 })
            _sourceBinding.InferredColumnsByAlias[node.Alias] = table.Columns;
    }

    internal void UpdateInferredColumnsByAlias(string alias, ISchemaColumn[] columns)
    {
        _sourceBinding.InferredColumnsByAlias[alias] = columns;
    }

    private void PushErrorRecoveryState(PropertyFromNode node, ISchema schema)
    {
        var emptyTable = new DynamicTable([], typeof(object));
        UpdateQueryAliasAndSymbolTable(node, schema, emptyTable);
        PushSemanticNode(new Parser.PropertyFromNode(node.Alias, node.SourceAlias, node.PropertiesChain));
    }

    private bool TryResolveAsStandaloneFunction(AliasedFromNode node)
    {
        var sourceAlias = _sourceBinding.QueryAlias;

        if (string.IsNullOrEmpty(sourceAlias))
            return false;

        ISchema schema;
        ISchemaTable sourceTable;

        if (_sourceBinding.AliasToSchemaFromNodeMap.TryGetValue(sourceAlias, out var schemaFrom))
        {
            schema = _provider.GetSchema(schemaFrom.Schema);
            var sourceSymbol = FindTableSymbolInScopeHierarchy(sourceAlias);
            sourceTable = sourceSymbol.FullTable;
        }
        else if (_sourceBinding.AliasMapToInMemoryTableMap.TryGetValue(sourceAlias, out var inMemoryName))
        {
            var sourceSymbol = FindTableSymbolInScopeHierarchy(inMemoryName);
            sourceTable = sourceSymbol.FullTable;
            schema = new TransitionSchema(inMemoryName, sourceTable);
        }
        else
        {
            return false;
        }

        var entityType = sourceTable.Metadata?.TableEntityType;
        var args = (ArgsListNode)PopSemanticNode();
        var argTypes = args.Args.Select(a => a.ReturnType ?? typeof(object)).ToArray();

        if (!schema.TryResolveMethod(node.Identifier, argTypes, entityType, out var method) &&
            !schema.TryResolveRawMethod(node.Identifier, argTypes, out method))
            return false;

        var returnType = method.ReturnType;
        var convertedTable = TurnTypeIntoTableWithDiagnostics(returnType, node);

        if (convertedTable == null)
            return false;

        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var functionToken = new FunctionToken(node.Identifier, TextSpan.Empty);
        var canSkipInjectSource = schema.TryResolveRawMethod(node.Identifier, argTypes, out _);
        var accessMethodNode = new AccessMethodNode(functionToken, args, null, canSkipInjectSource, method,
            sourceAlias);

        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, convertedTable, !string.IsNullOrEmpty(node.Alias));
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
        _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, sourceAlias);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

        if (method.DeclaringType != null)
            AddAssembly(method.DeclaringType.Assembly);

        PushSemanticNode(new Parser.AccessMethodFromNode(node.Alias, sourceAlias, accessMethodNode, returnType));
        return true;
    }

    private DynamicTable? TurnTypeIntoTableWithDiagnostics(Type type, Node? node)
    {
        var columns = new List<ISchemaColumn>();

        Type? nestedType;
        if (type.IsArray)
        {
            nestedType = type.GetElementType();
        }
        else if (!IsGenericEnumerable(type, out nestedType))
        {
            return TryReportColumnMustBeArray(node) ? null : throw new ColumnMustBeAnArrayOrImplementIEnumerableException();
        }

        if (nestedType == null) throw new InvalidOperationException("Element type is null.");

        if (nestedType.IsPrimitive || nestedType == typeof(string))
            return new DynamicTable([new SchemaColumn(nameof(PrimitiveTypeEntity<>.Value), 0, nestedType)]);

        foreach (var property in nestedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            columns.Add(new SchemaColumn(property.Name, columns.Count, property.PropertyType));

        return new DynamicTable(columns.ToArray(), nestedType);
    }

    private bool ValidateBindablePropertyAsTableWithDiagnostics(ISchemaTable table, ISchemaColumn targetColumn,
        Node? node)
    {
        var propertyInfo = table.Metadata.TableEntityType.GetProperty(targetColumn.ColumnName);
        var bindablePropertyAsTableAttribute = propertyInfo?.GetCustomAttribute<BindablePropertyAsTableAttribute>();

        if (bindablePropertyAsTableAttribute == null) return false;

        var isValid = IsGenericEnumerable(propertyInfo!.PropertyType, out var elementType) ||
                      IsArray(propertyInfo.PropertyType, out elementType) ||
                      (elementType != null && (elementType.IsPrimitive || elementType == typeof(string)));

        if (!isValid)
        {
            if (TryReportColumnNotBindable(targetColumn.ColumnName, node))
                return true;
            throw new ColumnMustBeMarkedAsBindablePropertyAsTableException();
        }

        return false;
    }

    private Type? FollowPropertiesWithDiagnostics(Type type, PropertyFromNode.PropertyNameAndTypePair[] propertiesChain,
        Node? node)
    {
        var propertiesWithoutColumnType = propertiesChain.Skip(1);

        foreach (var property in propertiesWithoutColumnType)
        {
            var propertyInfo = type.GetProperty(property.PropertyName);

            if (propertyInfo == null)
            {
                if (TryReportUnknownPropertyWithSuggestions(property.PropertyName, type.GetProperties(), node))
                    return null;
                var span = node.SpanOrEmpty();
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, type.GetProperties(), span);
                return null;
            }

            type = propertyInfo.PropertyType;
        }

        return type;
    }

    private PropertyFromNode.PropertyNameAndTypePair[]? RewritePropertiesChainWithTargetColumnWithDiagnostics(
        ISchemaColumn targetColumn, PropertyFromNode.PropertyNameAndTypePair[] nodePropertiesChain, Node? node)
    {
        var propertiesChain = new PropertyFromNode.PropertyNameAndTypePair[nodePropertiesChain.Length];
        var rootType = targetColumn.ColumnType;
        propertiesChain[0] = new PropertyFromNode.PropertyNameAndTypePair(
            targetColumn.ColumnName, rootType, targetColumn.IntendedTypeName);

        for (var i = 1; i < nodePropertiesChain.Length; i++)
        {
            var property = nodePropertiesChain[i];
            var propertyInfo = rootType.GetProperty(property.PropertyName);

            if (propertyInfo == null)
            {
                if (TryReportUnknownPropertyWithSuggestions(property.PropertyName, rootType.GetProperties(), node))
                    return null;
                var span = node.SpanOrEmpty();
                PrepareAndThrowUnknownPropertyExceptionMessage(property.PropertyName, rootType.GetProperties(), span);
                return null;
            }

            propertiesChain[i] =
                new PropertyFromNode.PropertyNameAndTypePair(propertyInfo.Name, propertyInfo.PropertyType);
        }

        return propertiesChain;
    }

    private TableSymbol FindTableSymbolInScopeHierarchy(string name)
    {
        var scope = _sourceBinding.CurrentScope;
        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<TableSymbol>(name, out var tableSymbol)) return tableSymbol;
            scope = scope.Parent;
        }

        return _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(name);
    }



    private ISchemaColumn[] GetColumnsForAlias(string alias, int schemaFromKey)
    {
        var key = alias + schemaFromKey;
        if (_columns.TryGetValue(key, out var columnNames))
            return columnNames
                .Select<string, ISchemaColumn>((f, i) => new SchemaColumn(f, i, typeof(object)))
                .ToArray();


        return [];
    }

    private string GetSourceContextIdForAlias(string alias, string fallbackSourceContextId)
    {
        if (_sourceBinding.SchemaFromInfo.TryGetValue(alias, out var info)) return info.SourceContextId;

        return fallbackSourceContextId;
    }
}
