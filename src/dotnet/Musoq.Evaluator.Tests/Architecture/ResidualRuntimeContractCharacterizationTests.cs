using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualRuntimeContractCharacterizationTests
{
    [TestMethod]
    public async Task SynchronousTableRunnable_RunAsync_ShouldPreserveTheSynchronousResultContract()
    {
        var runnable = new CharacterizationRunnable();

        var result = await ((ITableRunnable)runnable).RunAsync(CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, runnable.RunCount);
    }

    [TestMethod]
    public void RuntimeEnvironment_References_ShouldBeReturnedAsIndependentArrays()
    {
        using var environment = new EvaluatorRuntimeEnvironment();

        var first = environment.References;
        var second = environment.References;

        Assert.AreNotSame(first, second);
        CollectionAssert.AreEqual(first, second);
    }

    private sealed class CharacterizationRunnable : ITableRunnable
    {
        public int RunCount { get; private set; }

        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = new NullLogger<object>();

        public event QueryPhaseEventHandler? PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler? DataSourceProgress
        {
            add { }
            remove { }
        }

        public Table Run(CancellationToken token)
        {
            RunCount++;
            return new Table("empty", []);
        }
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException("This runnable does not expose a schema.");
        }
    }
}
