// === Parsed Query ===
/*
select Position, Sum(Amount) over (partition by Partition order by Position
    rows between 1 preceding and current row) as Total
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total]
      Window [Sum(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total]
      PhysicalWindow [Sum(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Partition: string <- property Partition
      Amount: decimal? <- property Amount
    Generated [ResultRow0]
      Position: int <- field Position
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame rows between 1 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultSums[windowIndex])]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K30_TypedWindowOutputValueCarrier
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

    public sealed class CompiledQuery : BaseOperations, ITypedRunnable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput>, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(string), 1), new Column("Amount", typeof(decimal?), 2) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
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
        public IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput> Run(TypedQueryRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            QueryRunContext? __musoqEnumerationRunContext = null;
            var __musoqInitialRunContext = QueryRunContext.Capture(options, this);
            return new QueryEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput>((queryToken) =>
            {
                var queryContext = new QueryRunContext(queryToken, __musoqInitialRunContext.TypedParameters, __musoqInitialRunContext.PhaseChanged, __musoqInitialRunContext.DataSourceProgress, __musoqInitialRunContext.Sender, queryId: __musoqInitialRunContext.QueryId, queryProgress: __musoqInitialRunContext.QueryProgress, queryProgressOptions: __musoqInitialRunContext.QueryProgress == null ? null : __musoqInitialRunContext.QueryProgressOptions)
                {
                    TypedParameters = __musoqInitialRunContext.TypedParameters
                };
                __musoqEnumerationRunContext = queryContext;
                return ComputeRows_compiled_0(__musoqInitialRunContext.Provider, __musoqInitialRunContext.SourceRuntimeSettingsBySourceContextId, __musoqInitialRunContext.SourceExecutionPlans, __musoqInitialRunContext.Logger, queryContext);
            }, options.CancellationToken, onCompleted: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()), onException: __musoqInitialRunContext.QueryProgress == null ? null : (Action<Exception>)(_ => __musoqEnumerationRunContext?.CompleteQueryProgress()), onDisposed: __musoqInitialRunContext.QueryProgress == null ? null : (Action)(() => __musoqEnumerationRunContext?.CompleteQueryProgress()));
        }

        public IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput> Run(CancellationToken token)
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
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultSumsIntOrderBuilder = new Musoq.Evaluator.Helpers.WindowIntOrderBuilder<string>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultSumsIntOrderBuilder.Add((string)ko3iko.Partition, (int)ko3iko.Position, windowIndex);
                }

                var resultSumsPartitions = resultSumsIntOrderBuilder.ToSortedPartitionSet(false);
                var resultSums = new decimal? [resultWindowRows.Count];
                var resultSumsBatchValues = new decimal? [resultWindowRows.Count];
                for (int resultSumsCaptureIndex = 0; resultSumsCaptureIndex < resultWindowRows.Count; ++resultSumsCaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSumsCaptureIndex];
                    resultSumsBatchValues[resultSumsCaptureIndex] = (decimal?)(ko3iko.Amount);
                }

                for (int resultSumsPartitionSetIndex = 0; resultSumsPartitionSetIndex < resultSumsPartitions.PartitionCount; ++resultSumsPartitionSetIndex)
                {
                    var resultSumsPartitionStart = resultSumsPartitions.GetStart(resultSumsPartitionSetIndex);
                    var resultSumsPartitionCount = resultSumsPartitions.GetLength(resultSumsPartitionSetIndex);
                    var resultSumsPartitionIndices = resultSumsPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<decimal>.State resultSumsState = default(Musoq.Plugins.SumAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                    {
                        var resultSumsBatchStarts = new int[resultSumsPartitionCount];
                        var resultSumsBatchEnds = new int[resultSumsPartitionCount];
                        for (int resultSumsBatchPosition = 0; resultSumsBatchPosition < resultSumsPartitionCount; ++resultSumsBatchPosition)
                        {
                            if ((resultSumsBatchPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            resultSumsBatchStarts[resultSumsBatchPosition] = Math.Max(0, resultSumsBatchPosition - 1);
                            resultSumsBatchEnds[resultSumsBatchPosition] = resultSumsBatchPosition;
                        }

                        var resultSumsBatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultSumsBatchValues, new System.ReadOnlySpan<int>(resultSumsPartitionIndices, resultSumsPartitionStart, resultSumsPartitionCount), resultSumsBatchStarts, resultSumsBatchEnds, resultSums, token);
                        if (!Musoq.Plugins.DecimalSumWindowBatch.TryEvaluate(in resultSumsBatchContext))
                        {
                            for (int resultSumsPartitionIndex = 0; resultSumsPartitionIndex < resultSumsPartitionCount; ++resultSumsPartitionIndex)
                            {
                                var resultSumsOutputIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsPartitionIndex];
                                var resultSumsFrameStart = Math.Max(0, resultSumsPartitionIndex - 1);
                                var resultSumsFrameEnd = resultSumsPartitionIndex;
                                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSumsState);
                                for (var resultSumsFrameIndex = Math.Max(0, resultSumsFrameStart); resultSumsFrameIndex <= resultSumsFrameEnd && resultSumsFrameIndex < resultSumsPartitionCount; resultSumsFrameIndex++)
                                {
                                    var resultSumsCurrentIndex = resultSumsPartitionIndices[resultSumsPartitionStart + resultSumsFrameIndex];
                                    Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSumsState, (decimal?)(resultSumsBatchValues[resultSumsCurrentIndex]));
                                }

                                resultSums[resultSumsOutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSumsState);
                            }
                        }
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultSums[windowIndex]) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
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

        private IEnumerable<Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return new Musoq.Evaluator.Tests.LibraryExecutionWindowQueryTests.WindowOutput((int)__musoqShapeRow.Position, (decimal?)__musoqShapeRow.Total);
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
            public ResultShape0(int Position, decimal? Total)
            {
                this.Position = Position;
                this.Total = Total;
            }

            public int Position { get; }
            public decimal? Total { get; }
            public int __musoqSequence { get; init; }
        }

        private readonly struct WindowResultSumsOrderKeysKey : System.IEquatable<WindowResultSumsOrderKeysKey>, System.IComparable<WindowResultSumsOrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSumsOrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSumsOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumsOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumsOrderKeysKey other && Equals(other);
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
