using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Musoq.Converter.Build;

internal static class ExecutionTargetCatalog
{
    private static readonly ExecutionTargetDescriptor CSharpClr = CSharpClrTargetComposition.CreateDescriptor();

    private static readonly IReadOnlyDictionary<ExecutionTargetId, ExecutionTargetDescriptor> ProductionDescriptors =
        new Dictionary<ExecutionTargetId, ExecutionTargetDescriptor>
        {
            [CSharpClr.TargetId] = CSharpClr
        };

    private static readonly AsyncLocal<TemporaryDescriptorScope?> TemporaryDescriptorScopes = new();

    public static IQueryExecutionBackend ResolveBackend(ExecutionTargetId targetId)
    {
        return ResolveDescriptor(targetId, "Execution target").RenderPhase ??
               throw CreateUnsupportedPhaseException(targetId, "rendering");
    }

    public static TargetRenderResult Render(TargetRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BackendInputs.TargetId != request.TargetId)
        {
            throw new InvalidOperationException(
                $"Execution target '{request.TargetId}' cannot render with backend inputs for target '{request.BackendInputs.TargetId}'.");
        }

        if (!request.SemanticsContract.IsEquivalentTo(request.ExecutionPlan.SemanticsContract))
        {
            throw new InvalidOperationException(
                $"Execution target '{request.TargetId}' render request semantics do not match its execution plan semantics.");
        }

        if (request.ExecutionIrVersion != request.ExecutionPlan.ExecutionIrVersion)
        {
            throw new InvalidOperationException(
                $"Execution target '{request.TargetId}' render request IR version does not match its execution plan.");
        }

        if (request.ExecutionIrVersion != TargetContractVersions.ExecutionIr ||
            request.HostAbiVersion != TargetContractVersions.HostAbi)
        {
            return TargetRenderResult.Failed(
                request.TargetId,
                [TargetDiagnostic.Error(
                    TargetDiagnosticCodes.UnsupportedLowering,
                    $"Execution target '{request.TargetId}' does not support execution IR version {request.ExecutionIrVersion} with host ABI version {request.HostAbiVersion}.")]);
        }

        var descriptor = ResolveDescriptor(request.TargetId, "Execution target");
        var backend = descriptor.RenderPhase ?? throw CreateUnsupportedPhaseException(request.TargetId, "rendering");
        var capabilityFailure = CreateCapabilityFailure(descriptor, request);
        if (capabilityFailure != null)
            return capabilityFailure;

        var result = backend.Render(request) ??
                     throw new InvalidOperationException(
                         $"Execution target '{request.TargetId}' rendering returned no result.");
        ValidateProducedTarget(request.TargetId, result.TargetId, "render result");
        if (result.Artifact is { } artifact)
            ValidateProducedTarget(request.TargetId, artifact.TargetId, "rendered artifact");
        return result;
    }

    public static TargetBackendRenderInputs CreateRenderInputs(
        ExecutionTargetId targetId,
        TargetRenderInputBuildContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var inputs = ResolveDescriptor(targetId, "Execution target").CreateRenderInputs(context);
        if (inputs.TargetId == targetId)
            return inputs;

        throw new InvalidOperationException(
            $"Execution target '{targetId}' render input factory produced inputs for target '{inputs.TargetId}'.");
    }

    public static IRenderedQueryFinalizer ResolveFinalizer(ExecutionTargetId targetId)
    {
        return ResolveDescriptor(targetId, "Rendered query target").FinalizationPhase ??
               throw CreateUnsupportedPhaseException(targetId, "finalization");
    }

    public static TargetFinalizationResult FinalizeArtifact(
        RenderedQueryArtifact artifact,
        TargetFinalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(options);

        var result = ResolveFinalizer(artifact.TargetId).Finalize(artifact, options) ??
                     throw new InvalidOperationException(
                         $"Execution target '{artifact.TargetId}' finalization returned no result.");
        ValidateProducedTarget(artifact.TargetId, result.TargetId, "finalization result");
        if (result.Artifact is { } executableArtifact)
            ValidateProducedTarget(artifact.TargetId, executableArtifact.TargetId, "executable artifact");

        return result;
    }

    public static RenderedArtifactBuildContribution CreateRenderBuildContribution(
        RenderedQueryArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return ResolveDescriptor(
            artifact.TargetId,
            "Rendered query target").CreateRenderBuildContribution(artifact);
    }

    public static TargetFinalizationOptions CreateFinalizationOptions(
        ExecutionTargetId targetId,
        TargetFinalizationOptionsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ResolveDescriptor(targetId, "Rendered query target").CreateFinalizationOptions(context);
    }

    public static TargetArtifactPackage CreateArtifactPackage(
        TargetArtifactPackagingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var packageFactory = ResolveDescriptor(
                context.TargetId,
                "Executable artifact target")
            .CreateArtifactPackage ?? throw CreateUnsupportedPhaseException(context.TargetId, "artifact packaging");
        var package = packageFactory(context);
        if (package.TargetId != context.TargetId)
        {
            throw new InvalidOperationException(
                $"Execution target '{context.TargetId}' artifact package factory produced a package for target '{package.TargetId}'.");
        }

        if (package.ExecutionIrVersion != context.ExecutionIrVersion)
        {
            throw new InvalidOperationException(
                $"Execution target '{context.TargetId}' artifact package IR version '{package.ExecutionIrVersion}' does not match packaging context version '{context.ExecutionIrVersion}'.");
        }

        if (!package.SemanticsContract.IsEquivalentTo(context.SemanticsContract))
        {
            throw new InvalidOperationException(
                $"Execution target '{context.TargetId}' artifact package semantics do not match the packaging context.");
        }

        return package;
    }

    public static IClrExecutableQueryActivator ResolveActivator(ExecutionTargetId targetId)
    {
        return ResolveDescriptor(targetId, "Executable query target").ActivationPhase ??
               throw CreateUnsupportedPhaseException(targetId, "activation");
    }

    public static IRenderedQueryInspector ResolveInspector(ExecutionTargetId targetId)
    {
        return ResolveDescriptor(
                   targetId,
                   "No rendered query inspector is registered for execution target").InspectionPhase ??
               throw CreateUnsupportedPhaseException(targetId, "inspection");
    }

    public static RenderedQueryInspection InspectArtifact(RenderedQueryArtifact artifact)
    {
        if (TryInspectArtifact(artifact, out var inspection))
            return inspection;

        throw CreateUnsupportedPhaseException(artifact.TargetId, "inspection");
    }

    public static bool TryInspectArtifact(
        RenderedQueryArtifact artifact,
        out RenderedQueryInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var descriptor = ResolveDescriptor(artifact.TargetId, "Rendered query target");
        if (descriptor.InspectionPhase is not { } inspector)
        {
            inspection = null!;
            return false;
        }

        inspection = inspector.Inspect(artifact) ??
                     throw new InvalidOperationException(
                         $"Execution target '{artifact.TargetId}' inspection returned no result.");
        ValidateProducedTarget(artifact.TargetId, inspection.TargetId, "inspection result");
        return true;
    }

    internal static IDisposable UseTemporaryDescriptor(ExecutionTargetDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var scope = new TemporaryDescriptorScope(descriptor, TemporaryDescriptorScopes.Value);
        TemporaryDescriptorScopes.Value = scope;
        return new TemporaryExecutionTargetCatalogOverride(() => DisposeTemporaryScope(scope));
    }

    private static ExecutionTargetDescriptor ResolveDescriptor(
        ExecutionTargetId targetId,
        string diagnosticPrefix)
    {
        for (var scope = TemporaryDescriptorScopes.Value; scope is not null; scope = scope.Parent)
        {
            if (!scope.IsDisposed && scope.Descriptor.TargetId == targetId)
                return scope.Descriptor;
        }

        if (ProductionDescriptors.TryGetValue(targetId, out var productionDescriptor))
            return productionDescriptor;

        throw CreateUnsupportedTarget(targetId, diagnosticPrefix);
    }

    private static Exception CreateUnsupportedTarget(
        ExecutionTargetId targetId,
        string diagnosticPrefix)
    {
        if (diagnosticPrefix.StartsWith("No ", StringComparison.Ordinal))
            return new NotSupportedException($"{diagnosticPrefix} '{targetId}'.");

        return new NotSupportedException($"{diagnosticPrefix} '{targetId}' is not supported.");
    }

    private static Exception CreateUnsupportedPhaseException(
        ExecutionTargetId targetId,
        string phaseName)
    {
        return new NotSupportedException(
            $"Execution target '{targetId}' does not support {phaseName}.");
    }

    private static TargetRenderResult? CreateCapabilityFailure(
        ExecutionTargetDescriptor descriptor,
        TargetRenderRequest request)
    {
        var validation = descriptor.Capabilities.Validate(
            request.OperationReport,
            request.SemanticsContract,
            request.CompatibilityReport,
            request.RuntimeContract,
            request.FeatureReport);
        if (validation.IsSupported)
            return null;

        var operationDiagnostics = validation.UnsupportedOperations.Select(operation =>
            TargetDiagnostic.Error(
                TargetDiagnosticCodes.UnsupportedOperation,
                $"Execution target '{descriptor.TargetId}' does not support operation '{operation}'."));
        var requirementDiagnostics = validation.UnsupportedRequirements.Select(requirement =>
            TargetDiagnostic.Error(
                TargetDiagnosticCodes.UnsupportedRequirement,
                $"Execution target '{descriptor.TargetId}' does not support {requirement.Kind}: {requirement.Detail}."));
        var semanticsDiagnostics = validation.UnsupportedSemanticsVersions.Select(version =>
            TargetDiagnostic.Error(
                TargetDiagnosticCodes.UnsupportedSemantics,
                $"Execution target '{descriptor.TargetId}' does not support execution semantics version {version}."));
        var semanticsFingerprintDiagnostics = validation.UnsupportedSemanticsFingerprints.Select(fingerprint =>
            TargetDiagnostic.Error(
                TargetDiagnosticCodes.UnsupportedSemantics,
                $"Execution target '{descriptor.TargetId}' does not support execution semantics fingerprint {fingerprint}."));
        var featureDiagnostics = validation.UnsupportedFeatures.Select(feature =>
            TargetDiagnostic.Error(
                TargetDiagnosticCodes.UnsupportedRequirement,
                $"Execution target '{descriptor.TargetId}' does not support feature '{feature.StableId}'."));

        return TargetRenderResult.Failed(
            descriptor.TargetId,
            operationDiagnostics.Concat(requirementDiagnostics).Concat(semanticsDiagnostics).Concat(semanticsFingerprintDiagnostics).Concat(featureDiagnostics));
    }

    private static void ValidateProducedTarget(
        ExecutionTargetId expectedTargetId,
        ExecutionTargetId actualTargetId,
        string outputName)
    {
        if (expectedTargetId == actualTargetId)
            return;

        throw new InvalidOperationException(
            $"Execution target '{expectedTargetId}' produced a {outputName} for target '{actualTargetId}'.");
    }

    private static void DisposeTemporaryScope(TemporaryDescriptorScope scope)
    {
        scope.Dispose();

        var current = TemporaryDescriptorScopes.Value;
        while (current is { IsDisposed: true })
            current = current.Parent;

        TemporaryDescriptorScopes.Value = current;
    }

    private sealed class TemporaryDescriptorScope(
        ExecutionTargetDescriptor descriptor,
        TemporaryDescriptorScope? parent)
    {
        private int _disposed;

        public ExecutionTargetDescriptor Descriptor { get; } = descriptor;

        public TemporaryDescriptorScope? Parent { get; } = parent;

        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
        }
    }
}
