using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Exceptions;
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

            var args = (ArgsListNode)PopSemanticNode();

            if (args.HasNamedArguments)
                throw new CannotResolveMethodException(
                    "Named arguments are not supported for Interpret or Parse sources.",
                    Musoq.Parser.Diagnostics.DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                    args.Span);

            if (node.TypeParameter is not { } schemaName)
                throw new InvalidOperationException("Interpret function source must provide a type parameter in this branch.");

            var isPartialInterpret = IsPartialResultInterpretFunction(node.Identifier);
            var interpretTable = isPartialInterpret
                ? CreatePartialInterpretTable()
                : CreateInterpretTable(schemaName);

            var schemaRegistration = SchemaRegistry?.TryGetSchema(schemaName, out var registeredSchema) == true
                ? registeredSchema
                : null;
            if (schemaRegistration == null)
                throw new UnknownInterpretationSchemaException(
                    schemaName,
                    $"Interpretation schema '{schemaName}' is not defined.",
                    node.SpanOrEmpty());

            var isTextSchema = schemaRegistration.Node is global::Musoq.Parser.Nodes.InterpretationSchema.TextSchemaNode;
            var isParse = node.Identifier.Equals("Parse", StringComparison.OrdinalIgnoreCase) ||
                          node.Identifier.Equals("TryParse", StringComparison.OrdinalIgnoreCase) ||
                          node.Identifier.Equals("PartialParse", StringComparison.OrdinalIgnoreCase);
            if (isParse != isTextSchema)
                throw new QuerySyntaxException(
                    isParse
                        ? $"{node.Identifier} requires a text interpretation schema."
                        : $"{node.Identifier} requires a binary interpretation schema.",
                    node.SpanOrEmpty());

            var expectedArgumentCount = node.Identifier.Equals("InterpretAt", StringComparison.OrdinalIgnoreCase)
                ? 2
                : 1;
            if (args.Args.Length != expectedArgumentCount)
                throw new QuerySyntaxException(
                    $"{node.Identifier}<{schemaName}> expects {expectedArgumentCount} argument{(expectedArgumentCount == 1 ? string.Empty : "s")}, but received {args.Args.Length}.",
                    args.Span);

            var dataType = args.Args[0].ReturnType;
            var dataTypeIsCompatible = dataType == null || dataType == typeof(object) ||
                                        (isTextSchema ? dataType == typeof(string) : dataType == typeof(byte[]));
            if (!dataTypeIsCompatible)
                throw new QuerySyntaxException(
                    isTextSchema
                        ? $"{node.Identifier}<{schemaName}> requires a string source value."
                        : $"{node.Identifier}<{schemaName}> requires a byte-array source value.",
                    args.Args[0].SpanOrEmpty());


            Type? returnType = null;
            returnType = schemaRegistration.GeneratedType;
            if (returnType != null && isPartialInterpret)
                returnType = typeof(Musoq.Schema.Interpreters.PartialInterpretResult<>).MakeGenericType(returnType);

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

            PushSemanticNode(new AliasedFromNode(node.Identifier, args, _sourceBinding.QueryAlias, returnType ?? node.ReturnType ?? typeof(object),
                node.InSourcePosition, node.TypeParameter));
            return;
        }

        if (IsInterpretFunction(node.Identifier) && node.TypeParameter == null)
        {
            ThrowIfOldInterpretSyntax(node.Identifier, node.Args);
            throw new QuerySyntaxException(
                $"The {node.Identifier} source requires a schema type parameter, for example {node.Identifier}<Schema>(data).",
                node.SpanOrEmpty());
        }

        // A function-shaped FROM item is not necessarily a datasource.  Reject
        // named arguments before standalone function resolution so an unknown
        // scalar/aggregate call cannot fall through to the coupled-source map
        // and produce a misleading lookup failure.
        if (!_sourceBinding.ExplicitlyCoupledSources.ContainsKey(node.Identifier) &&
            node.Args.HasNamedArguments)
        {
            throw new CannotResolveMethodException(
                "Named arguments are supported only for datasource source calls and explicitly coupled sources.",
                Musoq.Parser.Diagnostics.DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
                node.Args.Span);
        }

        if (!_sourceBinding.ExplicitlyCoupledSources.ContainsKey(node.Identifier) && TryResolveAsStandaloneFunction(node))
            return;

        if (!_sourceBinding.ExplicitlyCoupledSources.TryGetValue(node.Identifier, out var definition))
        {
            if (IsInterpretFunction(node.Identifier))
                throw new MethodResolutionException(
                    $"The source-shaped callable '{node.Identifier}' could not be resolved. " +
                    "Declare it with COUPLE or use a supported standalone function.");

            throw new TableIsNotDefinedException(node.Identifier, node.SpanOrEmpty());
        }

        var schemaInfo = definition.SchemaMethodNode;
        var table = definition.TableName != null
            ? _sourceBinding.ExplicitlyDefinedTables.TryGetValue(definition.TableName, out var definedTable)
                ? definedTable
                : throw new TableIsNotDefinedException(definition.TableName, node.SpanOrEmpty())
            : null;
        var hasExternallyProvidedTypes = table != null;

        var schema = SchemaProviderBoundary.Invoke(() => _provider.GetSchema(schemaInfo.Schema));

        AddAssembly(schema.GetType().Assembly);

        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var aliasedSchemaFromNode = new Parser.SchemaFromNode(
            schemaInfo.Schema,
            schemaInfo.Method,
            (ArgsListNode)PopSemanticNode(),
            _sourceBinding.QueryAlias,
            node.InSourcePosition,
            hasExternallyProvidedTypes
        );
        var queryId = node.InSourcePosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var bindingResult = SchemaSourceArgumentBinder.Bind(
            aliasedSchemaFromNode.Parameters,
            SchemaProviderBoundary.Invoke(() => schema.GetRawConstructors(
                schemaInfo.Method,
                new SourceMetadataContext(
                    queryId,
                    CancellationToken.None,
                    GetColumnsForAlias(_sourceBinding.QueryAlias, _sourceBinding.SchemaFromKey),
                    new Dictionary<string, string>(),
                    _logger))));
        if (bindingResult.Failure is { } bindingFailure)
            throw new CannotResolveMethodException(
                bindingFailure.Message,
                bindingFailure.Code,
                bindingFailure.Span,
                bindingFailure.Arguments);

        var boundInvocation = bindingResult.Invocation;
        if (boundInvocation != null)
            aliasedSchemaFromNode.SetBoundInvocation(boundInvocation);

        var staticSchemaArguments = SchemaArgumentBinder.BindStaticArguments(
            aliasedSchemaFromNode.Parameters,
            _scriptParameters.DefinitionsByName,
            _scriptVariables.DefinitionsByName,
            boundInvocation);
        var metadataColumns = table?.Columns ?? GetColumnsForAlias(_sourceBinding.QueryAlias, _sourceBinding.SchemaFromKey);
        var sourceRuntimeSettings = ResolveSourceRuntimeSettings(
            schema,
            aliasedSchemaFromNode,
            staticSchemaArguments,
            metadataColumns,
            queryId,
            definition.ProfileName,
            GetSourceRuntimeSettingsResolutionMode());

        table = SchemaProviderBoundary.Invoke(() => schema.GetTableByName(
            schemaInfo.Method,
            new SourceMetadataContext(
                queryId,
                CancellationToken.None,
                metadataColumns,
                sourceRuntimeSettings,
                _logger
            ),
            staticSchemaArguments
        )) ?? table ?? throw new InvalidOperationException($"Schema method '{schemaInfo.Method}' did not provide table metadata.");
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

        PushSemanticNode(aliasedSchemaFromNode);
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
