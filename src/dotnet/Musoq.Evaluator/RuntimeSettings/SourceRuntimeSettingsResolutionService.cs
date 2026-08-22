using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Schema;

namespace Musoq.Evaluator.RuntimeSettings;

internal sealed class SourceRuntimeSettingsResolutionService(
    CompilationOptions compilationOptions,
    DiagnosticContext? diagnosticContext)
{
    public ResolvedSourceRuntimeSettings Resolve(
        ISchema schema,
        SchemaFromNode sourceNode,
        object?[] parameters,
        IReadOnlyCollection<ISchemaColumn> columns,
        string queryId,
        string? profileName,
        IReadOnlyDictionary<string, string> initialSettings,
        ILogger logger,
        SourceRuntimeSettingsResolutionMode mode)
    {
        var sourceContextId = sourceNode.Id;
        var values = new Dictionary<string, string>(initialSettings, StringComparer.Ordinal);
        var identity = new SourceIdentity(sourceNode.Schema, sourceNode.Method, sourceContextId, sourceNode.Alias);
        var metadataContext = new SourceMetadataContext(
            queryId,
            CancellationToken.None,
            columns,
            values,
            logger);
        var requirements = SchemaProviderBoundary.Invoke(() => schema.DescribeSourceRuntimeSettings(
            sourceNode.Method,
            new SourceRuntimeSettingsDescribeContext(identity, metadataContext),
            parameters)) ?? [];

        if (requirements.Count > 0 || !compilationOptions.UsesDefaultSourceRuntimeSettingsResolver)
        {
            var resolvedSettings = compilationOptions.SourceRuntimeSettingsResolver.Resolve(
                new SourceRuntimeSettingsResolutionRequest(identity, profileName, requirements, parameters));

            foreach (var setting in resolvedSettings ?? new Dictionary<string, string>())
                values[setting.Key] = setting.Value;
        }

        var descriptions = CreateDescriptions(requirements, values);
        var hasMissingRequired = descriptions.Any(static description =>
            description.Status == SourceRuntimeSettingResolutionStatus.Missing);

        if (mode == SourceRuntimeSettingsResolutionMode.EnforceRequiredSettings)
            ReportMissingSourceRuntimeSettings(descriptions, identity, sourceNode);

        return new ResolvedSourceRuntimeSettings(
            sourceContextId,
            profileName,
            values,
            descriptions,
            requirements.Count > 0,
            values.Count > 0,
            hasMissingRequired);
    }

    private static IReadOnlyList<SourceRuntimeSettingDescription> CreateDescriptions(
        IReadOnlyList<SourceRuntimeSettingRequirement> requirements,
        IReadOnlyDictionary<string, string> values)
    {
        return requirements
            .OrderBy(static requirement => requirement.Name, StringComparer.Ordinal)
            .Select(requirement => new SourceRuntimeSettingDescription(
                requirement.Name,
                requirement.Required,
                requirement.Secret,
                requirement.Phases,
                ResolveStatus(requirement, values),
                requirement.Description))
            .ToArray();
    }

    private static SourceRuntimeSettingResolutionStatus ResolveStatus(
        SourceRuntimeSettingRequirement requirement,
        IReadOnlyDictionary<string, string> values)
    {
        if (values.ContainsKey(requirement.Name))
            return SourceRuntimeSettingResolutionStatus.Provided;

        return requirement.Required
            ? SourceRuntimeSettingResolutionStatus.Missing
            : SourceRuntimeSettingResolutionStatus.Default;
    }

    private void ReportMissingSourceRuntimeSettings(
        IEnumerable<SourceRuntimeSettingDescription> descriptions,
        SourceIdentity identity,
        Node node)
    {
        foreach (var description in descriptions)
        {
            if (description.Status != SourceRuntimeSettingResolutionStatus.Missing)
                continue;

            var message =
                $"Source '{identity.SchemaName}.{identity.MethodName}' with context '{identity.SourceContextId}' requires runtime setting '{description.Name}'.";

            if (diagnosticContext != null)
            {
                diagnosticContext.ReportError(DiagnosticCode.MQ3067_MissingSourceRuntimeSetting, message, node);
                continue;
            }

            throw new NotSupportedException(message);
        }
    }
}
