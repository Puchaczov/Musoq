using System.Threading;
using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ISchema schema;
        try
        {
            schema = _provider.GetSchema(node.Schema);
        }
        catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedSchemaLookupFailure(ex))
        {
            var span = node.HasSpan ? node.Span : TextSpan.Empty;
            throw new UnknownInterpretationSchemaException(
                node.Schema,
                $"Unknown schema '{node.Schema}'.",
                span);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SchemaProviderFailureException(ex);
        }

        const bool hasExternallyProvidedTypes = false;

        _sourceBinding.QueryAlias = AliasGenerator.CreateAliasIfEmpty(node.Alias, _resultShape.GeneratedAliases, _sourceBinding.SchemaFromKey.ToString(System.Globalization.CultureInfo.InvariantCulture));

        if (HasAlreadyUsedAlias(_sourceBinding.QueryAlias) &&
            TryReportDuplicateAlias(node, _sourceBinding.QueryAlias, node))
            return;

        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        var schemaArgsNode = (ArgsListNode)PopSemanticNode();
        _scriptParameters.ValidateSchemaArguments(schemaArgsNode, node);
        var queryId = node.QueryId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        BoundSchemaInvocation? boundInvocation = null;
        if (!IsDescribingConstructors)
        {
            var bindingResult = SchemaSourceArgumentBinder.Bind(
                schemaArgsNode,
                SchemaProviderBoundary.Invoke(() => schema.GetRawConstructors(
                    node.Method,
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
                    bindingFailure.Span);

            boundInvocation = bindingResult.Invocation;
        }
        var staticSchemaArguments = SchemaArgumentBinder.BindStaticArguments(
            schemaArgsNode,
            _scriptParameters.DefinitionsByName,
            _scriptVariables.DefinitionsByName,
            boundInvocation);
        var aliasedSchemaFromNode = new Parser.SchemaFromNode(node.Schema, node.Method, schemaArgsNode,
            _sourceBinding.QueryAlias, node.QueryId, hasExternallyProvidedTypes);
        if (boundInvocation != null)
            aliasedSchemaFromNode.SetBoundInvocation(boundInvocation);
        if (node.HasSpan)
            aliasedSchemaFromNode.WithSpan(node.Span);

        var isDesc = _sourceBinding.CurrentScope.Name == "Desc";
        var sourceRuntimeSettings = isDesc && string.IsNullOrWhiteSpace(node.Method)
            ? RetrieveInitialSourceRuntimeSettings(aliasedSchemaFromNode.Id, aliasedSchemaFromNode)
            : ResolveSourceRuntimeSettings(
                schema,
                aliasedSchemaFromNode,
                staticSchemaArguments,
                GetColumnsForAlias(_sourceBinding.QueryAlias, _sourceBinding.SchemaFromKey),
                queryId,
                mode: GetSourceRuntimeSettingsResolutionMode());
        var table = !isDesc
            ? SchemaProviderBoundary.Invoke(() => schema.GetTableByName(
                node.Method,
                new SourceMetadataContext(
                    queryId,
                    CancellationToken.None,
                    GetColumnsForAlias(_sourceBinding.QueryAlias, _sourceBinding.SchemaFromKey),
                    sourceRuntimeSettings,
                    _logger
                ),
                staticSchemaArguments))
            : new DynamicTable([]);

        _sourceBinding.SchemaFromInfo.Add(_sourceBinding.QueryAlias, (_sourceBinding.SchemaFromKey, aliasedSchemaFromNode.Id));

        AddAssembly(schema.GetType().Assembly);

        var tableSymbol = new TableSymbol(_sourceBinding.QueryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias, tableSymbol);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);

        _sourceBinding.AliasToSchemaFromNodeMap.Add(_sourceBinding.QueryAlias, aliasedSchemaFromNode);
        _sourceBinding.AllUsedSchemaNames.Add(aliasedSchemaFromNode.Schema);

        if (!_sourceBinding.InferredColumns.ContainsKey(aliasedSchemaFromNode))
            _sourceBinding.InferredColumns.Add(aliasedSchemaFromNode, table.Columns);

        if (!_sourceBinding.UsedColumns.ContainsKey(aliasedSchemaFromNode))
            _sourceBinding.UsedColumns.Add(aliasedSchemaFromNode, []);

        _sourceBinding.UsedWhereNodes.TryAdd(aliasedSchemaFromNode, AllTrueWhereNode);
        _sourceBinding.UsedSchemasQuantity += 1;

        PushSemanticNode(aliasedSchemaFromNode);
    }

    public override void Visit(SchemaMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.UsedSchemasQuantity += 1;
        PushSemanticNode(new Parser.SchemaMethodFromNode(node.Alias, node.Schema, node.Method));
    }
}
