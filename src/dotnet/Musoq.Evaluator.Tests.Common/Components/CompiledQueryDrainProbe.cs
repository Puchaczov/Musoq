using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Components;

internal sealed class CompiledQueryDrainProbe(CompiledQuery.DrainStage target, TestWatchdog watchdog)
{
    private readonly TaskCompletionSource _entered = TestWatchdog.Signal();

    internal void Wait(CompiledQuery.DrainStage stage, ManualResetEventSlim signal)
    {
        if (stage == target)
        {
            Assert.IsFalse(signal.IsSet, $"The {stage} gate must be closed while the fixture owns the work.");
            _entered.SetResult();
        }

        watchdog.Wait(signal, $"draining {stage}");
    }

    internal Task ObserveAsync(Task disposal) =>
        watchdog.ReachedBeforeCompletionAsync(_entered.Task, disposal, $"the {target} drain wait");
}
