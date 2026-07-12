using System;
using Musoq.Evaluator;

namespace Musoq.Targets.Execution;

internal interface IClrExecutableQueryActivator
{
    ExecutionTargetId TargetId { get; }

    ExecutableQueryArtifact CreateLoadedExecutableArtifact(Type runnableType, IDisposable? lifetimeOwner = null);

    ITableRunnable ActivateTable(ExecutableQueryArtifact executable, QueryRuntimeBinding binding);

    ITypedRunnable<TOut> ActivateTyped<TOut>(ExecutableQueryArtifact executable, QueryRuntimeBinding binding);
}
