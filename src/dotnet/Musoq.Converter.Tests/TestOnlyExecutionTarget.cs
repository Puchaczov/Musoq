using System;
using System.Collections.Generic;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Tests;

internal static class TestOnlyExecutionTarget
{
    public static ExecutionTargetDescriptor CreateDescriptor(
        IQueryExecutionBackend? backend = null,
        IRenderedQueryFinalizer? finalizer = null,
        IClrExecutableQueryActivator? activator = null,
        IRenderedQueryInspector? inspector = null,
        Func<TargetRenderInputBuildContext, TargetBackendRenderInputs>? createRenderInputs = null,
        Func<RenderedQueryArtifact, RenderedArtifactBuildContribution>? createRenderBuildContribution = null,
        Func<TargetArtifactPackagingContext, TargetArtifactPackage>? createArtifactPackage = null)
    {
        return ExecutionTargetDescriptor.Create(
            TestExecutionTargetIds.TestOnlyNonClr,
            renderPhase: backend,
            finalizationPhase: finalizer,
            activationPhase: activator,
            inspectionPhase: inspector,
            createRenderInputs: createRenderInputs,
            createRenderBuildContribution: createRenderBuildContribution,
            createArtifactPackage: createArtifactPackage);
    }

    internal sealed class Backend : IQueryExecutionBackend
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutionTargetCapabilities Capabilities { get; } = ExecutionTargetCapabilities.Create();

        public TargetRenderResult Render(TargetRenderRequest request)
        {
            throw new NotSupportedException("Test-only backend does not render.");
        }
    }

    internal sealed class Finalizer : IRenderedQueryFinalizer
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public TargetFinalizationResult Finalize(RenderedQueryArtifact artifact, TargetFinalizationOptions options)
        {
            throw new NotSupportedException("Test-only finalizer does not finalize.");
        }
    }

    internal sealed class Activator : IClrExecutableQueryActivator
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public ExecutableQueryArtifact CreateLoadedExecutableArtifact(Type runnableType, IDisposable? lifetimeOwner = null)
        {
            throw new NotSupportedException("Test-only activator does not create loaded executable artifacts.");
        }

        public ITableRunnable ActivateTable(ExecutableQueryArtifact executable, QueryRuntimeBinding binding)
        {
            throw new NotSupportedException("Test-only activator does not activate.");
        }

        public ITypedRunnable<TOut> ActivateTyped<TOut>(ExecutableQueryArtifact executable, QueryRuntimeBinding binding)
        {
            throw new NotSupportedException("Test-only activator does not activate.");
        }
    }

    internal sealed class Inspector : IRenderedQueryInspector
    {
        public ExecutionTargetId TargetId => TestExecutionTargetIds.TestOnlyNonClr;

        public RenderedQueryInspection Inspect(RenderedQueryArtifact artifact)
        {
            return new RenderedQueryInspection(
                TargetId,
                null,
                new Dictionary<string, string>(StringComparer.Ordinal));
        }
    }
}
