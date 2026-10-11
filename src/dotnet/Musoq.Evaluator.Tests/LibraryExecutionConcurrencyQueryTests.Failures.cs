using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ScalarQuery_WhenWorkerThrows_ShouldPreserveResponsibleFailure(bool sourceAllowed)
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, sourceAllowed);
        const string query = "select ThrowOnZero(Position) as Value from #schema.first()";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        var failure = Assert.ThrowsExactly<QueryExecutionException>(() =>
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            _ = result.ToArray();
        });
        var responsible = failure.GetBaseException();
        Assert.IsInstanceOfType<FormatException>(responsible);
        Assert.AreEqual("Typed worker failure.", responsible.Message);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ScalarQuery_WhenInvocationCancelsExecution_ShouldStopAndReleaseResult(bool sourceAllowed)
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, sourceAllowed);
        const string query = "select CancelOnZero(Probe, Position) as Value from #schema.first()";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var result = compiled.Run(probe.CancellationToken);
            _ = result.ToArray();
        });
        Assert.IsTrue(probe.CancellationToken.IsCancellationRequested);
    }
}
