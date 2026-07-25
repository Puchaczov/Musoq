using System.Collections.Generic;
using Musoq.Parser.Nodes;

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
            InferredColumns = InferredColumns,
            InferredColumnsByAlias = InferredColumnsByAlias,
            UsedColumns = UsedColumns,
            UsedWhereNodes = UsedWhereNodes,
            SourcePlanRequestsPerSchema = SourcePlanRequestsPerSchema,
            SourceContractDiagnosticLocationsPerSchema = SourceContractDiagnosticLocationsPerSchema,
            ScriptParameterDefinitions = ScriptParameterDefinitions,
            ScriptVariableDefinitions = ScriptVariableDefinitions,
            SourceRuntimeSettingsBySourceContextId = SourceRuntimeSettingsBySourceContextId,
            SourceRuntimeSettingDescriptionsBySourceContextId = SourceRuntimeSettingDescriptionsBySourceContextId,
            HasDeclaredSourceRuntimeSettings = HasDeclaredSourceRuntimeSettings,
            HasSourceRuntimeSettingValues = HasSourceRuntimeSettingValues,
            Assemblies = Assemblies,
            SetOperatorFieldPositions = SetOperatorFieldPositions,
            SetOperatorFieldTypes = SetOperatorFieldTypes,
            ResultShape = new SemanticResultShapeSnapshotInput
            {
                GeneratedAliases = SemanticGeneratedAliases,
                GeneratedColumns = SemanticGeneratedColumns,
                SelectFieldAliases = SemanticSelectFieldAliases,
                TheMostInnerIdentifier = SemanticMostInnerIdentifier
            }
        });
        _semanticPhaseCompleted = true;
        return snapshot;
    }

    internal IReadOnlyList<string> SemanticGeneratedAliases => _resultShape.GeneratedAliases;

    internal IReadOnlyDictionary<string, List<FieldNode>> SemanticGeneratedColumns => _resultShape.GeneratedColumns;

    internal IReadOnlyDictionary<string, Node> SemanticSelectFieldAliases => _resultShape.SelectFieldAliases;

    internal IdentifierNode? SemanticMostInnerIdentifier => _resultShape.TheMostInnerIdentifier;
}
