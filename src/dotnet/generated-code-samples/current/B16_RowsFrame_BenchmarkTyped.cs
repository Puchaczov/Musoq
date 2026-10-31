// === Parsed Query ===
/*
select r.Id as Id, Sum(r.Amount) over (order by r.Id rows between 1 preceding and current row) as Total, FirstValue(r.Amount) over (order by r.Id rows between 1 preceding and current row) as First
from bench.rows() r
order by r.Id
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [r.Id]
    Project [r.Id as Id, WindowRef(0) as Total, WindowRef(1) as First]
      Window [Sum(idx:0; order: r.Id; args: r.Amount; frame: rows between 1 preceding and current row), FirstValue(idx:1; order: r.Id; args: r.Amount; frame: rows between 1 preceding and current row)]
        SchemaScan [#bench.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [r.Id]
    PhysicalProject [r.Id as Id, WindowRef(0) as Total, WindowRef(1) as First]
      PhysicalWindow [Sum(idx:0; order: r.Id; args: r.Amount; frame: rows between 1 preceding and current row), FirstValue(idx:1; order: r.Id; args: r.Amount; frame: rows between 1 preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#bench.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Id: int <- property Id
      Amount: decimal <- property Amount
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
      First: decimal? <- field First

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: BenchmarkSampleRow] -> rRows
    MaterializeChunked [rRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value r.Amount order by r.Id ASC frame rows between 1 preceding and current row]
    ComputeFirstValueWindow [resultFirstValues1 <- resultWindowRows value r.Amount order by r.Id ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, r in resultWindowRows]
      AppendShape [result <- ResultShape0(Id: r.Id, Total: resultSums0[windowIndex], First: resultFirstValues1[windowIndex])]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B16_RowsFrame_BenchmarkTyped
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

    public sealed class CompiledQuery : BaseOperations, ITypedRunnable<Musoq.Evaluator.Tests.BenchmarkWindowOutput>, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("First", typeof(decimal?), 2)
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
        public IEnumerable<Musoq.Evaluator.Tests.BenchmarkWindowOutput> Run(TypedQueryRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            QueryRunContext? __musoqEnumerationRunContext = null;
            var __musoqInitialRunContext = QueryRunContext.Capture(options, this);
            return new QueryEnumerable<Musoq.Evaluator.Tests.BenchmarkWindowOutput>((queryToken) =>
            {
                var queryContext = new QueryRunContext(queryToken, __musoqInitialRunContext.TypedParameters, __musoqInitialRunContext.PhaseChanged, __musoqInitialRunContext.DataSourceProgress, __musoqInitialRunContext.Sender, queryId: __musoqInitialRunContext.QueryId, queryProgress: __musoqInitialRunContext.QueryProgress, queryProgressOptions: __musoqInitialRunContext.QueryProgress == null ? null : __musoqInitialRunContext.QueryProgressOptions)
                {
                    TypedParameters = __musoqInitialRunContext.TypedParameters
                };
                __musoqEnumerationRunContext = queryContext;
                return ComputeRows_compiled_0(__musoqInitialRunContext.Provider, __musoqInitialRunContext.SourceRuntimeSettingsBySourceContextId, __musoqInitialRunContext.SourceExecutionPlans, __musoqInitialRunContext.Logger, queryContext);
            }, options.CancellationToken, onCompleted: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()), onException: __musoqInitialRunContext.QueryProgress == null ? null : (Action<Exception>)(_ => __musoqEnumerationRunContext?.CompleteQueryProgress()), onDisposed: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()));
        }

        public IEnumerable<Musoq.Evaluator.Tests.BenchmarkWindowOutput> Run(CancellationToken token)
        {
            return Run(new TypedQueryRunOptions(token, TypedParameters, PhaseChanged, DataSourceProgress, QueryProgress, null));
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
                var rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(rRows);
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.BenchmarkSampleRow r = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(r.Id);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new decimal? [resultWindowRows.Count];
                var resultSums0BatchValues = new decimal? [resultWindowRows.Count];
                for (int resultSums0CaptureIndex = 0; resultSums0CaptureIndex < resultWindowRows.Count; ++resultSums0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.BenchmarkSampleRow r = resultWindowRows[resultSums0CaptureIndex];
                    resultSums0BatchValues[resultSums0CaptureIndex] = (decimal?)(r.Amount);
                }

                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                    {
                        var resultSums0BatchStarts = new int[resultSums0PartitionCount];
                        var resultSums0BatchEnds = new int[resultSums0PartitionCount];
                        for (int resultSums0BatchPosition = 0; resultSums0BatchPosition < resultSums0PartitionCount; ++resultSums0BatchPosition)
                        {
                            if ((resultSums0BatchPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            resultSums0BatchStarts[resultSums0BatchPosition] = Math.Max(0, resultSums0BatchPosition - 1);
                            resultSums0BatchEnds[resultSums0BatchPosition] = resultSums0BatchPosition;
                        }

                        var resultSums0BatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultSums0BatchValues, new System.ReadOnlySpan<int>(resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount), resultSums0BatchStarts, resultSums0BatchEnds, resultSums0, token);
                        if (!Musoq.Plugins.DecimalSumWindowBatch.TryEvaluate(in resultSums0BatchContext))
                        {
                            for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                            {
                                var resultSums0OutputIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                                var resultSums0FrameStart = Math.Max(0, resultSums0PartitionIndex - 1);
                                var resultSums0FrameEnd = resultSums0PartitionIndex;
                                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                                for (var resultSums0FrameIndex = Math.Max(0, resultSums0FrameStart); resultSums0FrameIndex <= resultSums0FrameEnd && resultSums0FrameIndex < resultSums0PartitionCount; resultSums0FrameIndex++)
                                {
                                    var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0FrameIndex];
                                    Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)(resultSums0BatchValues[resultSums0CurrentIndex]));
                                }

                                resultSums0[resultSums0OutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                            }
                        }
                    }
                }

                var resultFirstValues1Values = new decimal? [resultWindowRows.Count];
                for (int resultFirstValues1ExtractionIndex = 0; resultFirstValues1ExtractionIndex < resultWindowRows.Count; ++resultFirstValues1ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.BenchmarkSampleRow r = resultWindowRows[resultFirstValues1ExtractionIndex];
                    resultFirstValues1Values[resultFirstValues1ExtractionIndex] = (decimal?)((decimal?)r.Amount);
                }

                var resultFirstValues1 = new decimal? [resultWindowRows.Count];
                for (int resultFirstValues1Partition = 0; resultFirstValues1Partition < resultSums0Partitions.PartitionCount; ++resultFirstValues1Partition)
                {
                    var resultFirstValues1Start = resultSums0Partitions.GetStart(resultFirstValues1Partition);
                    var resultFirstValues1Count = resultSums0Partitions.GetLength(resultFirstValues1Partition);
                    var resultFirstValues1Indices = resultSums0Partitions.Indices;
                    for (int resultFirstValues1Position = 0; resultFirstValues1Position < resultFirstValues1Count; ++resultFirstValues1Position)
                    {
                        if ((resultFirstValues1Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultFirstValues1Current = resultFirstValues1Indices[resultFirstValues1Start + resultFirstValues1Position];
                        var resultFirstValues1FrameStart = Math.Max(0, resultFirstValues1Position - 1);
                        var resultFirstValues1FrameEnd = resultFirstValues1Position;
                        var resultFirstValues1Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultFirstValues1Values, new System.ReadOnlySpan<int>(resultFirstValues1Indices, resultFirstValues1Start, resultFirstValues1Count), resultFirstValues1Position, resultFirstValues1FrameStart, resultFirstValues1FrameEnd, token);
                        resultFirstValues1[resultFirstValues1Current] = Musoq.Plugins.FirstValueKernel<decimal>.Get(in resultFirstValues1Context);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.BenchmarkSampleRow r = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(r.Id, (decimal?)resultSums0[windowIndex], (decimal?)resultFirstValues1[windowIndex]) { __musoqSequence = result.Count });
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

        private IEnumerable<Musoq.Evaluator.Tests.BenchmarkWindowOutput> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return new Musoq.Evaluator.Tests.BenchmarkWindowOutput((int)__musoqShapeRow.Id, (decimal?)__musoqShapeRow.Total, (decimal?)__musoqShapeRow.First);
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

        private readonly struct ResultShape0
        {
            public ResultShape0(int Id, decimal? Total, decimal? First)
            {
                this.Id = Id;
                this.Total = Total;
                this.First = First;
            }

            public int Id { get; }
            public decimal? Total { get; }
            public decimal? First { get; }
            public int __musoqSequence { get; init; }
        }

        private readonly struct WindowResultSums0OrderKeysKey : System.IEquatable<WindowResultSums0OrderKeysKey>, System.IComparable<WindowResultSums0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSums0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSums0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSums0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSums0OrderKeysKey other && Equals(other);
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
