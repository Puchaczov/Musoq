using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.RuntimeSettings;
using Musoq.Parser;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private bool _hasDeclaredSourceRuntimeSettings;
    private bool _hasSourceRuntimeSettingValues;

    public bool HasDeclaredSourceRuntimeSettings => _hasDeclaredSourceRuntimeSettings;

    public bool HasSourceRuntimeSettingValues => _hasSourceRuntimeSettingValues;

    private IReadOnlyDictionary<string, string> ResolveSourceRuntimeSettings(
        ISchema schema,
        Musoq.Evaluator.Parser.SchemaFromNode sourceNode,
        object?[] parameters,
        IReadOnlyCollection<ISchemaColumn> columns,
        string queryId,
        string? profileName = null,
        SourceRuntimeSettingsResolutionMode mode = SourceRuntimeSettingsResolutionMode.EnforceRequiredSettings)
    {
        var sourceContextId = sourceNode.Id;
        ResolvedSourceRuntimeSettings resolved;
        try
        {
            resolved = _sourceRuntimeSettingsResolutionService.Resolve(
                schema,
                sourceNode,
                parameters,
                columns,
                queryId,
                profileName,
                RetrieveInitialSourceRuntimeSettings(sourceContextId, sourceNode),
                _logger,
                mode);
        }
        catch (SchemaProviderFailureException exception) when (sourceNode.HasRequiredRuntimeArguments)
        {
            throw new SourceMetadataRequiresDefaultException(
                sourceNode.Schema,
                sourceNode.Method,
                sourceNode.HasSpan ? sourceNode.Span : TextSpan.Empty,
                exception);
        }
        catch (SchemaArgumentException exception) when (
            sourceNode.HasRequiredRuntimeArguments &&
            !string.Equals(exception.ParamName, "methodName", StringComparison.Ordinal))
        {
            throw new SourceMetadataRequiresDefaultException(
                sourceNode.Schema,
                sourceNode.Method,
                sourceNode.HasSpan ? sourceNode.Span : TextSpan.Empty,
                exception);
        }

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
