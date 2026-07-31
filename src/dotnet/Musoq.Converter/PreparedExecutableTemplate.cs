using System;
using Musoq.Targets.Abstractions;

namespace Musoq.Converter;

/// <summary>
/// Immutable executable code that can be activated repeatedly with different
/// runtime bindings. Providers, source settings, parameters, loggers, and
/// result state deliberately do not belong to this object.
/// </summary>
internal sealed record PreparedExecutableTemplate
{
    internal PreparedExecutableTemplate(
        ExecutableQueryArtifact executableArtifact,
        ExecutionTargetId targetId,
        string runnableTypeName,
        string? semanticContractFingerprint)
    {
        ExecutableArtifact = executableArtifact ?? throw new ArgumentNullException(nameof(executableArtifact));
        if (targetId != executableArtifact.TargetId)
        {
            throw new InvalidOperationException(
                $"Prepared executable target '{targetId}' does not match artifact target '{executableArtifact.TargetId}'.");
        }

        if (string.IsNullOrWhiteSpace(runnableTypeName))
            throw new ArgumentException("Runnable type name cannot be empty.", nameof(runnableTypeName));

        TargetId = targetId;
        RunnableTypeName = runnableTypeName;
        SemanticContractFingerprint = semanticContractFingerprint;
    }

    internal ExecutableQueryArtifact ExecutableArtifact { get; }

    internal ExecutionTargetId TargetId { get; }

    internal string RunnableTypeName { get; }

    internal string? SemanticContractFingerprint { get; }
}
