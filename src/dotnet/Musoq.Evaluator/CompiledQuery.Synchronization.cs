using System.Threading;

namespace Musoq.Evaluator;

public partial class CompiledQuery
{
    internal enum DrainStage
    {
        Admissions,
        ActiveRuns,
        DeferredResults,
    }

    internal Action<SemaphoreSlim, CancellationToken>? SynchronousAdmissionWait { private get; init; }
    internal Action<DrainStage, ManualResetEventSlim>? DisposalWait { private get; init; }

    private void WaitForExecutionGate(CancellationToken token)
    {
        if (SynchronousAdmissionWait is { } wait)
            wait(_executionGate, token);
        else
            _executionGate.Wait(token);
    }

    private void WaitForDrain(DrainStage stage, ManualResetEventSlim signal)
    {
        if (DisposalWait is { } wait)
            wait(stage, signal);
        else
            signal.Wait();
    }
}
