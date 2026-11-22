// === Parsed Query ===
/*
select r.Id as Id, RowNumber() over (partition by r.Key
order by r.Id) as Position, Sum(r.Amount) over (order by r.Id) as Total
from bench.rows() r
qualify Position = 1
order by Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id, WindowRef(0) as Position, WindowRef(1) as Total]
      Qualify [(WindowRef(0) = 1)]
        Window [RowNumber(idx:0; partition: r.Key; order: r.Id), Sum(idx:1; order: r.Id; args: r.Amount)]
          SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, WindowRef(0) as Position, WindowRef(1) as Total]
      PhysicalQualify [(WindowRef(0) = 1)]
        PhysicalWindow [RowNumber(idx:0; partition: r.Key; order: r.Id), Sum(idx:1; order: r.Id; args: r.Amount)]
          PhysicalMaterialize
            PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
      Amount: decimal <- property Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Position: long <- field Position
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    MaterializeChunked [rRows -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows partition by r.Key order by r.Id ASC]
    ComputeSumWindowKernel[BoundedRows] [resultSums1 <- resultWindowRows value r.Amount order by r.Id ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, r in resultWindowRows]
      If [(resultRowNumbers0[windowIndex] = 1)]
        AppendShape [result <- ResultShape0(Id: r.Id, Position: resultRowNumbers0[windowIndex], Total: resultSums1[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B17_MultipleWindows_BenchmarkTable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1), new Column("Amount", typeof(decimal), 2) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Position", typeof(long), 1),
            new Column("Total", typeof(decimal?), 2)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(rRows);
                var resultRowNumbers0PartitionKeys = new int[resultWindowRows.Count];
                var resultRowNumbers0OrderKeys = new WindowResultRowNumbers0OrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbers0WindowKeys(resultWindowRows, resultRowNumbers0PartitionKeys, resultRowNumbers0OrderKeys);
                var resultRowNumbers0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRowNumbers0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRowNumbers0Partitions, resultRowNumbers0OrderKeys, false);
                var resultRowNumbers0 = new long[resultWindowRows.Count];
                for (int resultRowNumbers0PartitionSetIndex = 0; resultRowNumbers0PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultRowNumbers0PartitionSetIndex)
                {
                    var resultRowNumbers0PartitionStart = resultRowNumbers0Partitions.GetStart(resultRowNumbers0PartitionSetIndex);
                    var resultRowNumbers0PartitionCount = resultRowNumbers0Partitions.GetLength(resultRowNumbers0PartitionSetIndex);
                    var resultRowNumbers0PartitionIndices = resultRowNumbers0Partitions.Indices;
                    var resultRowNumbers0PartitionLimit = resultRowNumbers0PartitionCount;
                    for (int resultRowNumbers0PartitionIndex = 0; resultRowNumbers0PartitionIndex < resultRowNumbers0PartitionLimit; ++resultRowNumbers0PartitionIndex)
                    {
                        var resultRowNumbers0CurrentIndex = resultRowNumbers0PartitionIndices[resultRowNumbers0PartitionStart + resultRowNumbers0PartitionIndex];
                        var resultRowNumbers0Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PartitionIndex, 0, 0, 0L, resultRowNumbers0PartitionCount, token);
                        resultRowNumbers0[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers0Context);
                    }
                }

                var resultSums1Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums1Partitions, resultRowNumbers0OrderKeys, false);
                var resultSums1 = new decimal? [resultWindowRows.Count];
                for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultSums1Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                {
                    var resultSums1PartitionStart = resultSums1Partitions.GetStart(resultSums1PartitionSetIndex);
                    var resultSums1PartitionCount = resultSums1Partitions.GetLength(resultSums1PartitionSetIndex);
                    var resultSums1PartitionIndices = resultSums1Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums1State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums1State);
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount;)
                    {
                        var resultSums1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultRowNumbers0OrderKeys, resultSums1PartitionIndices, resultSums1PartitionStart, resultSums1PartitionCount, resultSums1PartitionIndex);
                        for (int resultSums1PeerIndex = resultSums1PartitionIndex; resultSums1PeerIndex <= resultSums1PeerEnd; ++resultSums1PeerIndex)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PeerIndex];
                            Musoq.Benchmarks.Scenarios.FixtureRow r = resultWindowRows[resultSums1CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums1State, (decimal?)(r.Amount));
                        }

                        var resultSums1PeerValue = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums1State);
                        for (int resultSums1PeerIndex = resultSums1PartitionIndex; resultSums1PeerIndex <= resultSums1PeerEnd; ++resultSums1PeerIndex)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PeerIndex];
                            resultSums1[resultSums1CurrentIndex] = resultSums1PeerValue;
                        }

                        resultSums1PartitionIndex = resultSums1PeerEnd + 1;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Benchmarks.Scenarios.FixtureRow r = resultWindowRows[windowIndex];
                    if (((long)resultRowNumbers0[windowIndex] == 1))
                    {
                        result.Add(new ResultShape0(r.Id, (long)resultRowNumbers0[windowIndex], (decimal?)resultSums1[windowIndex]) { __musoqSequence = result.Count });
                    }
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                OnPhaseChanged("compiled", QueryPhase.Select);
                return result;
            }
            finally
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }
        }

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractResultRowNumbers0WindowKeys(IReadOnlyList<Musoq.Benchmarks.Scenarios.FixtureRow> resultWindowRows, int[] resultRowNumbers0PartitionKeys, WindowResultRowNumbers0OrderKeysKey[] resultRowNumbers0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Musoq.Benchmarks.Scenarios.FixtureRow r = resultWindowRows[windowIndex];
                resultRowNumbers0PartitionKeys[windowIndex] = (int)(r.Key);
                resultRowNumbers0OrderKeys[windowIndex] = new WindowResultRowNumbers0OrderKeysKey(r.Id);
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, long Position, decimal? Total)
            {
                this.Id = Id;
                this.Position = Position;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public long Position { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Position" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private readonly struct WindowResultRowNumbers0OrderKeysKey : System.IEquatable<WindowResultRowNumbers0OrderKeysKey>, System.IComparable<WindowResultRowNumbers0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultRowNumbers0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultRowNumbers0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultRowNumbers0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbers0OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int left, int right)
            {
                var comparison = left.CompareTo(right);
                return comparison;
            }
        }
    }
}
