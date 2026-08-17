using System.Collections.Generic;
using Musoq.Evaluator.RuntimeSettings;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool _hasDeclaredSourceRuntimeSettings;
    private bool _hasSourceRuntimeSettingValues;

    public bool HasDeclaredSourceRuntimeSettings => _hasDeclaredSourceRuntimeSettings;

    public bool HasSourceRuntimeSettingValues => _hasSourceRuntimeSettingValues;

    private IReadOnlyDictionary<string, string> ResolveSourceRuntimeSettings(
        ISchema schema,
        SchemaFromNode sourceNode,
        object?[] parameters,
        IReadOnlyCollection<ISchemaColumn> columns,
        string queryId,
        string? profileName = null,
        SourceRuntimeSettingsResolutionMode mode = SourceRuntimeSettingsResolutionMode.EnforceRequiredSettings)
    {
        var sourceContextId = sourceNode.Id;
        var resolved = _sourceRuntimeSettingsResolutionService.Resolve(
            schema,
            sourceNode,
            parameters,
            columns,
            queryId,
            profileName,
            RetrieveInitialSourceRuntimeSettings(sourceContextId, sourceNode),
            _logger,
            mode);

        if (resolved.HasDeclaredRequirements)
            _hasDeclaredSourceRuntimeSettings = true;

        if (resolved.HasResolvedValues)
            _hasSourceRuntimeSettingValues = true;

        InternalSourceRuntimeSettingsBySourceContextId[sourceContextId] = resolved.Values;
        InternalSourceRuntimeSettingDescriptionsBySourceContextId[sourceContextId] = resolved.Descriptions;

        return resolved.Values;
    }

    private IReadOnlyDictionary<string, string> GetResolvedSourceRuntimeSettings(string sourceContextId)
    {
        return InternalSourceRuntimeSettingsBySourceContextId.TryGetValue(sourceContextId, out var settings)
            ? settings
            : new Dictionary<string, string>();
    }

    private SourceRuntimeSettingsResolutionMode GetSourceRuntimeSettingsResolutionMode()
    {
        return IsDescribingSourceRuntimeSettings
            ? SourceRuntimeSettingsResolutionMode.DescribeOnly
            : SourceRuntimeSettingsResolutionMode.EnforceRequiredSettings;
    }
}
