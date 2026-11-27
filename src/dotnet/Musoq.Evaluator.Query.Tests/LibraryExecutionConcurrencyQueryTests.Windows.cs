using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Tests.Common;
using static Musoq.Evaluator.Tests.LibraryExecutionConcurrencyQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    public async Task WindowQuery_WhenCustomOperationWaitsForCancellation_ShouldReceiveQueryToken()
    {
        using var probe = new ConcurrencyProbe(false);
        using var watchdog = new TestWatchdog();
        var provider = CreateProvider(probe, false);
        const string query =
            "select CancellableValue(Position, Probe) over () as Value from #schema.first() where Position = 0";
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString("N"),
            provider,
            _logger,
            LibraryExecutionConcurrencyQueryTestsFixtures.ParallelOptions
        );
        var execution = TestWatchdog.StartWorker(() =>
        {
            using var result = compiled.Run(probe.CancellationToken);
            _ = result.ToArray();
        });
        async Task ObserveCancellation() => await Assert.ThrowsAsync<OperationCanceledException>(() => execution);

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(
                    probe.WindowStarted,
                    execution,
                    "the custom window operation"
                );
                probe.Cancel();
                await watchdog.WaitAsync(ObserveCancellation(), "window cancellation");
            },
            () =>
            {
                probe.Cancel();
                return ObserveCancellation();
            }
        );
    }
}
