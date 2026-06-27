using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using AliasedFromNode = Musoq.Parser.Nodes.From.AliasedFromNode;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static readonly Action<ILogger, string, string, string, Exception?> InterpretFunctionProcessingLog =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Debug,
            new EventId(1001, nameof(LogInterpretFunctionProcessing)),
            "Visit(AliasedFromNode): Processing Interpret function '{Identifier}' with alias '{Alias}' -> _queryAlias='{QueryAlias}'");

    private static readonly Action<ILogger, string, int, string, Exception?> InterpretTableRegistrationLog =
        LoggerMessage.Define<string, int, string>(
            LogLevel.Debug,
            new EventId(1002, nameof(LogInterpretTableRegistration)),
            "Visit(AliasedFromNode): Registered TableSymbol '{QueryAlias}' with {ColumnCount} columns in scope '{ScopeName}'");

    public override void Visit(AliasedFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (IsInterpretFunction(node.Identifier) && node.TypeParameter != null)
        {
            _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
            _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

            LogInterpretFunctionProcessing(_logger, node, _sourceBinding.QueryAlias);

            var args = (ArgsListNode)Nodes.Pop();

            if (node.TypeParameter is not { } schemaName)
                throw new InvalidOperationException("Interpret function source must provide a type parameter in this branch.");

            var isPartialInterpret = IsPartialResultInterpretFunction(node.Identifier);
            var interpretTable = isPartialInterpret
                ? CreatePartialInterpretTable()
                : CreateInterpretTable(schemaName);


            Type? returnType = null;
            if (schemaName != null && SchemaRegistry != null &&
                SchemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
            {
                returnType = schemaRegistration?.GeneratedType;
                if (returnType != null && isPartialInterpret)
                    returnType = typeof(Musoq.Schema.Interpreters.PartialInterpretResult<>).MakeGenericType(returnType);
            }

            var interpretTableSymbol = new TableSymbol(
                _sourceBinding.QueryAlias,
                new TransitionSchema(_sourceBinding.QueryAlias, interpretTable),
                interpretTable,
                !string.IsNullOrEmpty(node.Alias)
            );

            _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, interpretTableSymbol);
            _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);
            _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
            _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, _sourceBinding.QueryAlias);

            var interpretSchemaFromNode = new Parser.SchemaFromNode(
                schemaName ?? throw new InvalidOperationException("Interpret schema name cannot be null."),
                node.Identifier,
                args,
                _sourceBinding.QueryAlias,
                node.InSourcePosition,
                true);

            if (!_sourceBinding.InferredColumns.ContainsKey(interpretSchemaFromNode))
                _sourceBinding.InferredColumns.Add(interpretSchemaFromNode, interpretTable.Columns.ToArray());

            LogInterpretTableRegistration(_logger, _sourceBinding.QueryAlias, interpretTable, _sourceBinding.CurrentScope.Name);

            Nodes.Push(new AliasedFromNode(node.Identifier, args, _sourceBinding.QueryAlias, returnType ?? node.ReturnType ?? typeof(object),
                node.InSourcePosition, node.TypeParameter));
            return;
        }

        if (IsInterpretFunction(node.Identifier) && node.TypeParameter == null)
        {
            ThrowIfOldInterpretSyntax(node.Identifier, node.Args);
        }

        if (!_sourceBinding.ExplicitlyCoupledSources.ContainsKey(node.Identifier) && TryResolveAsStandaloneFunction(node))
            return;

        var definition = _sourceBinding.ExplicitlyCoupledSources[node.Identifier];
        var schemaInfo = definition.SchemaMethodNode;
        var table = definition.TableName != null
            ? _sourceBinding.ExplicitlyDefinedTables[definition.TableName]
            : null;
        var hasExternallyProvidedTypes = table != null;

        var schema = _provider.GetSchema(schemaInfo.Schema);

        AddAssembly(schema.GetType().Assembly);

        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(
            schemaInfo.Schema,
            schemaInfo.Method,
            (ArgsListNode)Nodes.Pop(),
            _sourceBinding.QueryAlias,
            node.InSourcePosition,
            hasExternallyProvidedTypes
        );
        var staticSchemaArguments = SchemaArgumentBinder.BindStaticArguments(aliasedSchemaFromNode.Parameters);
        var queryId = node.InSourcePosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var metadataColumns = table?.Columns ?? GetColumnsForAlias(_sourceBinding.QueryAlias, _sourceBinding.SchemaFromKey);
        var sourceRuntimeSettings = ResolveSourceRuntimeSettings(
            schema,
            aliasedSchemaFromNode,
            staticSchemaArguments,
            metadataColumns,
            queryId,
            definition.ProfileName,
            GetSourceRuntimeSettingsResolutionMode());

        table = schema.GetTableByName(
            schemaInfo.Method,
            new SourceMetadataContext(
                queryId,
                CancellationToken.None,
                metadataColumns,
                sourceRuntimeSettings,
                _logger
            ),
            staticSchemaArguments
        ) ?? table ?? throw new InvalidOperationException($"Schema method '{schemaInfo.Method}' did not provide table metadata.");
        var tableSymbol = new TableSymbol(
            _sourceBinding.QueryAlias,
            schema,
            table,
            !string.IsNullOrEmpty(node.Alias)
        );
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;

        if (!_sourceBinding.InferredColumns.ContainsKey(aliasedSchemaFromNode))
            _sourceBinding.InferredColumns.Add(aliasedSchemaFromNode, table.Columns);

        if (definition.TableName != null &&
            _sourceBinding.ExplicitlyDefinedTableDiagnosticLocations.TryGetValue(
                definition.TableName,
                out var diagnosticLocations))
        {
            _sourceBinding.SourceContractDiagnosticLocationsPerSchema[aliasedSchemaFromNode] = diagnosticLocations;
        }

        if (!_sourceBinding.UsedColumns.ContainsKey(aliasedSchemaFromNode))
            _sourceBinding.UsedColumns.Add(aliasedSchemaFromNode, []);

        _sourceBinding.UsedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);
        _sourceBinding.UsedSchemasQuantity += 1;
        _sourceBinding.SchemaFromInfo.Add(_sourceBinding.QueryAlias, (_sourceBinding.SchemaFromKey, aliasedSchemaFromNode.Id));
        _sourceBinding.AliasToSchemaFromNodeMap.Add(_sourceBinding.QueryAlias, aliasedSchemaFromNode);
        _sourceBinding.AllUsedSchemaNames.Add(aliasedSchemaFromNode.Schema);

        Nodes.Push(aliasedSchemaFromNode);
    }

    private static void LogInterpretFunctionProcessing(ILogger? logger, AliasedFromNode node, string queryAlias)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Debug))
            return;

        InterpretFunctionProcessingLog(logger, node.Identifier, node.Alias, queryAlias, null);
    }

    private static void LogInterpretTableRegistration(ILogger? logger, string queryAlias, ISchemaTable? interpretTable, string scopeName)
    {
        if (logger == null || !logger.IsEnabled(LogLevel.Debug))
            return;

        var columnCount = interpretTable?.Columns?.Length ?? 0;
        InterpretTableRegistrationLog(logger, queryAlias, columnCount, scopeName, null);
    }
}
