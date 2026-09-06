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
        SourceRuntimeSettingsResolutionMode mode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourceContextId = sourceNode.Id;
        var values = new Dictionary<string, string>(initialSettings, StringComparer.Ordinal);
        var identity = new SourceIdentity(sourceNode.Schema, sourceNode.Method, sourceContextId, sourceNode.Alias);
        var metadataContext = new SourceMetadataContext(
            queryId,
            cancellationToken,
            columns,
            values,
            logger);
        cancellationToken.ThrowIfCancellationRequested();
        var requirements = SchemaProviderBoundary.Invoke(() => schema.DescribeSourceRuntimeSettings(
            sourceNode.Method,
            new SourceRuntimeSettingsDescribeContext(identity, metadataContext),
            parameters)) ?? [];
        cancellationToken.ThrowIfCancellationRequested();

        if (requirements.Count > 0 || !compilationOptions.UsesDefaultSourceRuntimeSettingsResolver)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resolvedSettings = compilationOptions.SourceRuntimeSettingsResolver.Resolve(
                new SourceRuntimeSettingsResolutionRequest(identity, profileName, requirements, parameters)
                {
                    CancellationToken = cancellationToken
                });
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var setting in resolvedSettings ?? new Dictionary<string, string>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                values[setting.Key] = setting.Value;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var descriptions = CreateDescriptions(requirements, values, cancellationToken);
        var hasMissingRequired = descriptions.Any(static description =>
            description.Status == SourceRuntimeSettingResolutionStatus.Missing);

        if (mode == SourceRuntimeSettingsResolutionMode.EnforceRequiredSettings)
            ReportMissingSourceRuntimeSettings(descriptions, identity, sourceNode, cancellationToken);

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
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken)
    {
        var descriptions = new List<SourceRuntimeSettingDescription>(requirements.Count);
        foreach (var requirement in requirements.OrderBy(static requirement => requirement.Name, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            descriptions.Add(new SourceRuntimeSettingDescription(
                requirement.Name,
                requirement.Required,
                requirement.Secret,
                requirement.Phases,
                ResolveStatus(requirement, values),
                requirement.Description));
        }

        return descriptions;
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
        Node node,
        CancellationToken cancellationToken)
    {
        foreach (var description in descriptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
