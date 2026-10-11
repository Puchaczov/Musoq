using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class LibraryExecutionConcurrencyQueryTests : MSTestContextTestBase
{
    private static readonly CompilationOptions ParallelOptions = new(ParallelizationMode.Full,
        usePrimitiveTypeValidation: false, maxDegreeOfParallelismOverride: 2, forceTableResultMaterialization: true);
    private readonly TestsLoggerResolver _logger = new();

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void ScalarQuery_WhenPermissionsIntersect_ShouldUseOnlyAuthorizedConcurrency(bool sourceAllowed, bool functionAllowed)
    {
        var concurrent = sourceAllowed && functionAllowed;
        using var probe = new ConcurrencyProbe(concurrent);
        var provider = CreateProvider(probe, sourceAllowed);
        var function = functionAllowed ? "PermittedWork" : "SerialWork";
        var query = $"select Position, {function}(Probe, Position) as Value from #schema.first()";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.AreEqual(concurrent, inspection.GeneratedCSharpCode.Contains("ProjectChunkedRowsParallel<", StringComparison.Ordinal), inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Value", typeof(int)));
        Assert.HasCount(4096, rows);
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index, rows[index].GetValue<int>(0));
            Assert.AreEqual(index + 1, rows[index].GetValue<int>(1));
        }
        Assert.AreEqual(4096, probe.Calls);
        Assert.AreEqual(concurrent ? 2 : 1, probe.MaxConcurrent);
        if (!sourceAllowed)
            Assert.Contains("does not explicitly permit ConcurrentRowProcessing", inspection.PlanningText);
        else if (!functionAllowed)
            Assert.Contains("does not explicitly permit concurrent invocations", inspection.PlanningText);
    }

    private static ISchemaProvider CreateProvider(ConcurrencyProbe probe, bool allowed,
        SourceExecutionPermissions? permissions = null)
    {
        var rows = Enumerable.Range(0, 4096).Select(position => new ConcurrencyRow(position, position % 8, probe)).ToArray();
        var source = new ConcurrencyRowSource(rows);
        var schema = new GenericSchema<ConcurrencyLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                ["first"] = (new GenericEntityTable<ConcurrencyRow>(), source)
            }, executionPermissions: permissions ?? (allowed ? SourceExecutionPermissions.ConcurrentAccess |
                SourceExecutionPermissions.ConcurrentRowProcessing : SourceExecutionPermissions.None));
        return new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
    }

    private sealed class ConcurrencyRowSource(ConcurrencyRow[] rows) : RowSourceBase<ConcurrencyRow>
    {
        protected override void CollectChunks(IChunkWriter<ConcurrencyRow> writer)
        {
            writer.Write(rows[..2048]);
            writer.Write(rows[2048..]);
        }
    }

    public sealed record ConcurrencyRow(int Position, int Group, ConcurrencyProbe Probe);

    public sealed partial class ConcurrencyLibrary : LibraryBase
    {
        [AggregateFunction(typeof(CountNullableAggregateKernel<int>),
            ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
                FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge)]
        [WindowFunction(typeof(CountNullableAggregateKernel<int>), WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Zero, ExecutionPermissions = FunctionExecutionPermissions.None)]
        public long PermittedCount(int? value) => AggregateFunction.NotInvoked<long>();

        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long SerialCount(int? value) => AggregateFunction.NotInvoked<long>();

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public static int PermittedWork(ConcurrencyProbe probe, int value)
            => probe.Visit(value);

        [BindableMethod]
        public static int SerialWork(ConcurrencyProbe probe, int value)
            => probe.Visit(value);

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public static int ThrowOnZero(int value)
            => value == 0 ? throw new FormatException("Typed worker failure.") : value;

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public static int CancelOnZero(ConcurrencyProbe probe, int value)
        {
            if (value == 0)
                probe.Cancel();
            return value;
        }
    }

    public sealed class ConcurrencyProbe(bool requireOverlap) : IDisposable
    {
        private readonly Barrier _barrier = new(2);
        private readonly CancellationTokenSource _cancellation = new();
        private readonly ManualResetEventSlim _windowStarted = new();
        private int _active;
        private int _maximum;
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public int MaxConcurrent => Volatile.Read(ref _maximum);
        internal CancellationToken CancellationToken => _cancellation.Token;
        public void Cancel() => _cancellation.Cancel();
        public bool WaitForWindow() => _windowStarted.Wait(TimeSpan.FromSeconds(30));
        public void EnterWindow(CancellationToken token)
        {
            _windowStarted.Set();
            token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
        }

        public int Visit(int value)
        {
            var active = Interlocked.Increment(ref _active);
            var maximum = Volatile.Read(ref _maximum);
            while (active > maximum && Interlocked.CompareExchange(ref _maximum, active, maximum) != maximum)
                maximum = Volatile.Read(ref _maximum);
            try
            {
                if (Interlocked.Increment(ref _calls) <= 2 && requireOverlap &&
                    !_barrier.SignalAndWait(TimeSpan.FromSeconds(30)))
                    throw new InvalidOperationException("Authorized workers failed to overlap at the deterministic barrier.");
                return value + 1;
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
        public void Dispose()
        {
            _barrier.Dispose();
            _cancellation.Dispose();
            _windowStarted.Dispose();
        }
    }
}
