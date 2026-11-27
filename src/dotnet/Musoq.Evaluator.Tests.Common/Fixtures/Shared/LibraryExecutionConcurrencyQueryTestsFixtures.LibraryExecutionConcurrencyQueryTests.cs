using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionConcurrencyQueryTestsFixtures
{
    internal static readonly CompilationOptions ParallelOptions = new(
        ParallelizationMode.Full,
        usePrimitiveTypeValidation: false,
        maxDegreeOfParallelismOverride: 2,
        forceTableResultMaterialization: true
    );

    internal static ISchemaProvider CreateProvider(
        ConcurrencyProbe probe,
        bool allowed,
        SourceExecutionPermissions? permissions = null
    )
    {
        var rows = Enumerable
            .Range(0, 4096)
            .Select(position => new ConcurrencyRow(position, position % 8, probe))
            .ToArray();
        var source = new ConcurrencyRowSource(rows);
        var schema = new GenericSchema<ConcurrencyLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                ["first"] = (new GenericEntityTable<ConcurrencyRow>(), source),
            },
            executionPermissions: permissions
                ?? (
                    allowed
                        ? SourceExecutionPermissions.ConcurrentAccess
                            | SourceExecutionPermissions.ConcurrentRowProcessing
                        : SourceExecutionPermissions.None
                )
        );
        return new GenericSchemaProvider(new Dictionary<string, ISchema> { ["#schema"] = schema });
    }

    internal sealed class ConcurrencyRowSource(ConcurrencyRow[] rows) : RowSourceBase<ConcurrencyRow>
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
        [AggregateFunction(
            typeof(CountNullableAggregateKernel<int>),
            ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations
                | FunctionExecutionPermissions.IndependentGroups
                | FunctionExecutionPermissions.EquivalentPartialMerge
        )]
        [WindowFunction(
            typeof(CountNullableAggregateKernel<int>),
            WindowEvaluationModes.WholePartition,
            AggregateEmptyResultBehavior.Zero,
            ExecutionPermissions = FunctionExecutionPermissions.None
        )]
        public long PermittedCount(int? value) => AggregateFunction.NotInvoked<long>();

        [AggregateFunction(typeof(CountNullableAggregateKernel<int>))]
        public long SerialCount(int? value) => AggregateFunction.NotInvoked<long>();

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public static int PermittedWork(ConcurrencyProbe probe, int value) => probe.Visit(value);

        [BindableMethod]
        public static int SerialWork(ConcurrencyProbe probe, int value) => probe.Visit(value);

        [BindableMethod(ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations)]
        public static int ThrowOnZero(int value) =>
            value == 0 ? throw new FormatException("Typed worker failure.") : value;

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
        private readonly TestWatchdog _watchdog = new();
        private readonly TaskCompletionSource _windowStarted = TestWatchdog.Signal();
        private int _active;
        private int _maximum;
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public int MaxConcurrent => Volatile.Read(ref _maximum);
        internal CancellationToken CancellationToken => _cancellation.Token;

        public void Cancel() => _cancellation.Cancel();

        internal Task WindowStarted => _windowStarted.Task;

        public void EnterWindow(CancellationToken token)
        {
            _windowStarted.TrySetResult();
            WaitHandle.WaitAny([token.WaitHandle, _watchdog.Token.WaitHandle]);
            token.ThrowIfCancellationRequested();
            throw new AssertFailedException("The custom window operation did not receive query cancellation.");
        }

        public int Visit(int value)
        {
            var active = Interlocked.Increment(ref _active);
            var maximum = Volatile.Read(ref _maximum);
            while (active > maximum && Interlocked.CompareExchange(ref _maximum, active, maximum) != maximum)
                maximum = Volatile.Read(ref _maximum);
            try
            {
                if (Interlocked.Increment(ref _calls) <= 2 && requireOverlap)
                    _watchdog.Rendezvous(_barrier, "both authorized library workers", _cancellation.Token);
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
            _watchdog.Dispose();
        }
    }
}
