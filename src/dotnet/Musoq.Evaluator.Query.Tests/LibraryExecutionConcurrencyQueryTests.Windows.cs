using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Tests.LibraryExecutionConcurrencyQueryTestsFixtures;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    public async Task WindowQuery_WhenCustomOperationWaitsForCancellation_ShouldReceiveQueryToken()
    {
        using var probe = new ConcurrencyProbe(false);
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
}
