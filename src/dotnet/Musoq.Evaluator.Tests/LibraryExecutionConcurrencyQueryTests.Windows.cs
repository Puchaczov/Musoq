using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    public async Task WindowQuery_WhenCustomOperationWaitsForCancellation_ShouldReceiveQueryToken()
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, false);
        const string query = "select CancellableValue(Position, Probe) over () as Value from #schema.first() where Position = 0";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        var execution = Task.Run(() =>
        {
            using var result = compiled.Run(probe.CancellationToken);
            _ = result.ToArray();
        });
        var started = probe.WaitForWindow();
        probe.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => execution);
        Assert.IsTrue(started, "The registered typed context operation did not reach the synchronization point.");
    }

    public sealed partial class ConcurrencyLibrary
    {
        [WindowFunction(typeof(CancellableValueKernel), WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Custom, ContextType = typeof(WindowFrameContext<>))]
        public int CancellableValue(int value, ConcurrencyProbe probe) => AggregateFunction.NotInvoked<int>();

        public static class CancellableValueKernel
        {
            public static int Get(in WindowFrameContext<int> context, ConcurrencyProbe probe)
            {
                probe.EnterWindow(context.CancellationToken);
                return context.Read(context.Position);
            }
        }
    }
}
