using System.Threading;

namespace Musoq.Converter.Build;

internal sealed record TargetArtifactPackagingContext
{
    public TargetArtifactPackagingContext(
        ExecutionTargetId targetId,
        string packageName,
        string script,
        string compilationOptionsSignature,
        RenderedQueryArtifact renderedArtifact,
        ExecutableQueryArtifact executableArtifact,
        TargetArtifactSemanticFacts semanticFacts,
        ExecutionSemanticsContract semanticsContract,
        TargetRuntimeContract? runtimeContract = null,
        ExecutionTargetReadinessReport? readinessReport = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr)
        : this(
            CancellationToken.None,
            targetId,
            packageName,
            script,
            compilationOptionsSignature,
            renderedArtifact,
            executableArtifact,
            semanticFacts,
            semanticsContract,
            runtimeContract,
            readinessReport,
            executionIrVersion)
    {
    }

    public TargetArtifactPackagingContext(
        CancellationToken cancellationToken,
        ExecutionTargetId targetId,
        string packageName,
        string script,
        string compilationOptionsSignature,
        RenderedQueryArtifact renderedArtifact,
        ExecutableQueryArtifact executableArtifact,
        TargetArtifactSemanticFacts semanticFacts,
        ExecutionSemanticsContract semanticsContract,
        TargetRuntimeContract? runtimeContract = null,
        ExecutionTargetReadinessReport? readinessReport = null,
        int executionIrVersion = TargetContractVersions.ExecutionIr)
    {
        CancellationToken = cancellationToken;
        if (executionIrVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionIrVersion));

        TargetId = targetId;
        PackageName = RequireText(packageName, nameof(packageName));
        Script = script ?? string.Empty;
        CompilationOptionsSignature = RequireText(compilationOptionsSignature, nameof(compilationOptionsSignature));
        RenderedArtifact = renderedArtifact ?? throw new ArgumentNullException(nameof(renderedArtifact));
        ExecutableArtifact = executableArtifact ?? throw new ArgumentNullException(nameof(executableArtifact));
        SemanticFacts = semanticFacts ?? throw new ArgumentNullException(nameof(semanticFacts));
        SemanticsContract = semanticsContract ?? throw new ArgumentNullException(nameof(semanticsContract));
        RuntimeContract = runtimeContract;
        ReadinessReport = readinessReport;
        ExecutionIrVersion = executionIrVersion;
    }

    public CancellationToken CancellationToken { get; }

    public ExecutionTargetId TargetId { get; }

    public string PackageName { get; }

    public string Script { get; }

    public string CompilationOptionsSignature { get; }

    public RenderedQueryArtifact RenderedArtifact { get; }

    public ExecutableQueryArtifact ExecutableArtifact { get; }

    public TargetArtifactSemanticFacts SemanticFacts { get; }

    public ExecutionSemanticsContract SemanticsContract { get; }

    public TargetRuntimeContract? RuntimeContract { get; }

    public ExecutionTargetReadinessReport? ReadinessReport { get; }

    public int ExecutionIrVersion { get; }

    private static string RequireText(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or whitespace.", parameterName)
            : value;
    }
}
