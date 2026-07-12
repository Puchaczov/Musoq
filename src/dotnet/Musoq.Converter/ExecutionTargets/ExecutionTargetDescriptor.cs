using System;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Build;

internal sealed record ExecutionTargetDescriptor(
    ExecutionTargetId TargetId,
    IQueryExecutionBackend? RenderPhase,
    IRenderedQueryFinalizer? FinalizationPhase,
    IClrExecutableQueryActivator? ActivationPhase,
    IRenderedQueryInspector? InspectionPhase,
    Func<TargetRenderInputBuildContext, TargetBackendRenderInputs> CreateRenderInputs,
    Func<TargetFinalizationOptionsContext, TargetFinalizationOptions> CreateFinalizationOptions,
    Func<RenderedQueryArtifact, RenderedArtifactBuildContribution> CreateRenderBuildContribution,
    Func<TargetArtifactPackagingContext, TargetArtifactPackage>? CreateArtifactPackage)
{
    public ExecutionTargetCapabilities Capabilities =>
        RenderPhase?.Capabilities ?? ExecutionTargetCapabilities.Create();

    public static ExecutionTargetDescriptor Create(
        ExecutionTargetId targetId,
        IQueryExecutionBackend? renderPhase = null,
        IRenderedQueryFinalizer? finalizationPhase = null,
        IClrExecutableQueryActivator? activationPhase = null,
        IRenderedQueryInspector? inspectionPhase = null,
        Func<TargetRenderInputBuildContext, TargetBackendRenderInputs>? createRenderInputs = null,
        Func<TargetFinalizationOptionsContext, TargetFinalizationOptions>? createFinalizationOptions = null,
        Func<RenderedQueryArtifact, RenderedArtifactBuildContribution>? createRenderBuildContribution = null,
        Func<TargetArtifactPackagingContext, TargetArtifactPackage>? createArtifactPackage = null)
    {
        if (renderPhase is not null)
            ValidateTargetId(targetId, renderPhase.TargetId, nameof(renderPhase));
        if (finalizationPhase is not null)
            ValidateTargetId(targetId, finalizationPhase.TargetId, nameof(finalizationPhase));
        if (activationPhase is not null)
            ValidateTargetId(targetId, activationPhase.TargetId, nameof(activationPhase));
        if (inspectionPhase is not null)
            ValidateTargetId(targetId, inspectionPhase.TargetId, nameof(inspectionPhase));

        return new ExecutionTargetDescriptor(
            targetId,
            renderPhase,
            finalizationPhase,
            activationPhase,
            inspectionPhase,
            createRenderInputs ?? (_ => new EmptyTargetBackendRenderInputs(targetId)),
            createFinalizationOptions ?? (_ => TargetFinalizationOptions.Empty),
            createRenderBuildContribution ?? (_ => RenderedArtifactBuildContribution.Empty),
            createArtifactPackage);
    }

    private static void ValidateTargetId(
        ExecutionTargetId descriptorTargetId,
        ExecutionTargetId componentTargetId,
        string componentName)
    {
        if (descriptorTargetId == componentTargetId)
            return;

        throw new ArgumentException(
            $"Execution target descriptor '{descriptorTargetId}' cannot use {componentName} for target '{componentTargetId}'.",
            componentName);
    }
}
