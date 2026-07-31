using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool _semanticPhaseCompleted;

    internal SemanticMetadataSnapshot CreateSemanticMetadataSnapshot()
    {
        if (_semanticPhaseCompleted)
            throw new InvalidOperationException("The semantic analysis phase has already been completed.");

        var snapshot = _metadataSnapshotBuilder.Build(new SemanticMetadataSnapshotInput
        {
            Root = Root,
            InferredColumns = _sourceBinding.InferredColumns.Select(ToReadOnlyColumnEntry),
            InferredColumnsByAlias = _sourceBinding.InferredColumnsByAlias.Select(ToReadOnlyColumnEntry),
            UsedColumns = _sourceBinding.UsedColumns.Select(ToReadOnlyColumnEntry),
            UsedWhereNodes = _sourceBinding.UsedWhereNodes,
            SourcePlanRequestsPerSchema = _sourceBinding.SourcePlanRequestsPerSchema,
            SourceContractDiagnosticLocationsPerSchema = _sourceBinding.SourceContractDiagnosticLocationsPerSchema,
            SourceContracts = CreateSourceContracts(),
            ScriptParameterDefinitions = _scriptParameters.DefinitionsForSnapshot,
            ScriptVariableDefinitions = _scriptVariables.DefinitionsForSnapshot,
            SourceRuntimeSettingsBySourceContextId = InternalSourceRuntimeSettingsBySourceContextId,
            SourceRuntimeSettingDescriptionsBySourceContextId = InternalSourceRuntimeSettingDescriptionsBySourceContextId,
            HasDeclaredSourceRuntimeSettings = HasDeclaredSourceRuntimeSettings,
            HasSourceRuntimeSettingValues = HasSourceRuntimeSettingValues,
            Assemblies = _methodResolution.Assemblies,
            SetOperatorFieldPositions = _queryState.SetOperatorFieldPositions,
            SetOperatorFieldTypes = _queryState.SetOperatorFieldTypes,
            ResultShape = new SemanticResultShapeSnapshotInput
            {
                GeneratedAliases = _resultShape.GeneratedAliases,
                GeneratedColumns = _resultShape.GeneratedColumns,
                SelectFieldAliases = _resultShape.SelectFieldAliases,
                TheMostInnerIdentifier = _resultShape.TheMostInnerIdentifier
            }
        });
        _semanticPhaseCompleted = true;
        return snapshot;
    }

    internal IReadOnlyList<string> SemanticGeneratedAliases => _resultShape.GeneratedAliases;
    internal IReadOnlyDictionary<string, List<FieldNode>> SemanticGeneratedColumns => _resultShape.GeneratedColumns;
    internal IReadOnlyDictionary<string, Node> SemanticSelectFieldAliases => _resultShape.SelectFieldAliases;
    internal IdentifierNode? SemanticMostInnerIdentifier => _resultShape.TheMostInnerIdentifier;

    private static KeyValuePair<TKey, IEnumerable<ISchemaColumn>> ToReadOnlyColumnEntry<TKey>(
        KeyValuePair<TKey, ISchemaColumn[]> entry) where TKey : notnull =>
        new(entry.Key, entry.Value);

    private static KeyValuePair<TKey, IEnumerable<ISchemaColumn>> ToReadOnlyColumnEntry<TKey>(
        KeyValuePair<TKey, List<ISchemaColumn>> entry) where TKey : notnull =>
        new(entry.Key, entry.Value);

    private IReadOnlyList<BoundSourceContract> CreateSourceContracts()
    {
        var contracts = new List<BoundSourceContract>(_sourceBinding.InferredColumns.Count);

        foreach (var entry in _sourceBinding.InferredColumns)
        {
            var source = entry.Key;
            var sourceContextId = source.Id;
            if (_sourceBinding.SchemaFromInfo.TryGetValue(source.Alias, out var sourceInfo))
                sourceContextId = sourceInfo.SourceContextId;

            var identity = _sourceBinding.SourcePlanRequestsPerSchema.TryGetValue(source, out var request)
                ? request.Identity
                : new SourceIdentity(source.Schema, source.Method, sourceContextId, source.Alias);
            var columns = entry.Value.Select(BoundSchemaColumn.Capture).ToArray();
            var requiredMembers = columns
                .Select(static column =>
                    $"{column.ColumnName}:{column.ColumnType.AssemblyQualifiedName ?? column.ColumnType.FullName}:{column.IsNullable}")
                .ToArray();
            var methodSignature = string.Concat(
                source.Schema,
                ".",
                source.Method,
                "(",
                string.Join(",", source.Parameters.Args.Select(static argument =>
                    argument.ReturnType?.AssemblyQualifiedName ?? argument.ReturnType?.FullName ?? "object")),
                ")");

            contracts.Add(new BoundSourceContract(
                source.Id,
                identity,
                Array.AsReadOnly(columns),
                Array.AsReadOnly(requiredMembers),
                methodSignature));
        }

        return contracts;
    }
}
