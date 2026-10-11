// === Parsed Query ===
/*
select Position, Min(Amount) over (partition by Partition order by Position
    rows between 2 preceding and current row) as Minimum,
    Max(Floating) over (partition by Partition order by Position
        rows between 1 preceding and 1 following) as Maximum,
    Min(Integral) over (partition by Partition order by Integral
        range between unbounded preceding and current row) as PeerMinimum
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Minimum, WindowRef(1) as Maximum, WindowRef(2) as PeerMinimum]
      Window [Min(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 2 preceding and current row), Max(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Floating; frame: rows between 1 preceding and 1 following), Min(idx:2; partition: ko3iko.Partition; order: ko3iko.Integral; args: ko3iko.Integral; frame: range between unbounded preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Minimum, WindowRef(1) as Maximum, WindowRef(2) as PeerMinimum]
      PhysicalWindow [Min(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 2 preceding and current row), Max(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Floating; frame: rows between 1 preceding and 1 following), Min(idx:2; partition: ko3iko.Partition; order: ko3iko.Integral; args: ko3iko.Integral; frame: range between unbounded preceding and current row)]
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
      Integral: int? <- property Integral
      Amount: decimal? <- property Amount
      Floating: double? <- property Floating
    Generated [ResultRow0]
      Position: int <- field Position
      Minimum: decimal? <- field Minimum
      Maximum: double? <- field Maximum
      PeerMinimum: int? <- field PeerMinimum

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeMinWindowKernel[BoundedRows] [resultMins0 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame rows between 2 preceding and current row]
    ComputeMaxWindowKernel[BoundedRows] [resultMaxs1 <- resultWindowRows value ko3iko.Floating partition by ko3iko.Partition order by ko3iko.Position ASC frame rows between 1 preceding and 1 following]
    ComputeMinWindowKernel[BoundedRows] [resultMins2 <- resultWindowRows value ko3iko.Integral partition by ko3iko.Partition order by ko3iko.Integral ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Minimum: resultMins0[windowIndex], Maximum: resultMaxs1[windowIndex], PeerMinimum: resultMins2[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K32_TypedWindowExtremaBatch
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Minimum", typeof(decimal?), 1),
            new Column("Maximum", typeof(double?), 2),
            new Column("PeerMinimum", typeof(int?), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(string), 1), new Column("Integral", typeof(int?), 2), new Column("Amount", typeof(decimal?), 3), new Column("Floating", typeof(double?), 4) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Minimum, __musoqShapeRow.Maximum, __musoqShapeRow.PeerMinimum);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultMins0PartitionKeys = new string[resultWindowRows.Count];
                var resultMins0OrderKeys = new WindowResultMins0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultMins0PartitionKeys[windowIndex] = (string)ko3iko.Partition;
                    resultMins0OrderKeys[windowIndex] = new WindowResultMins0OrderKeysKey(ko3iko.Position);
                }

                var resultMins0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultMins0PartitionKeys);
                var resultMins0SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultMins0Partitions, resultMins0OrderKeys, false);
                var resultMins0 = new decimal? [resultWindowRows.Count];
                var resultMins0BatchValues = new decimal? [resultWindowRows.Count];
                for (int resultMins0CaptureIndex = 0; resultMins0CaptureIndex < resultWindowRows.Count; ++resultMins0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultMins0CaptureIndex];
                    resultMins0BatchValues[resultMins0CaptureIndex] = (decimal?)ko3iko.Amount;
                }

                for (int resultMins0PartitionSetIndex = 0; resultMins0PartitionSetIndex < resultMins0SortedPartitions.PartitionCount; ++resultMins0PartitionSetIndex)
                {
                    var resultMins0PartitionStart = resultMins0SortedPartitions.GetStart(resultMins0PartitionSetIndex);
                    var resultMins0PartitionCount = resultMins0SortedPartitions.GetLength(resultMins0PartitionSetIndex);
                    var resultMins0PartitionIndices = resultMins0SortedPartitions.Indices;
                    Musoq.Plugins.MinAggregateKernel<decimal>.State resultMins0State = default(Musoq.Plugins.MinAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.MinAggregateKernel<decimal>.Reset(ref resultMins0State);
                    {
                        var resultMins0BatchStarts = new int[resultMins0PartitionCount];
                        var resultMins0BatchEnds = new int[resultMins0PartitionCount];
                        for (int resultMins0BatchPosition = 0; resultMins0BatchPosition < resultMins0PartitionCount; ++resultMins0BatchPosition)
                        {
                            if ((resultMins0BatchPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            resultMins0BatchStarts[resultMins0BatchPosition] = Math.Max(0, resultMins0BatchPosition - 2);
                            resultMins0BatchEnds[resultMins0BatchPosition] = resultMins0BatchPosition;
                        }

                        var resultMins0BatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultMins0BatchValues, new System.ReadOnlySpan<int>(resultMins0PartitionIndices, resultMins0PartitionStart, resultMins0PartitionCount), resultMins0BatchStarts, resultMins0BatchEnds, resultMins0, token);
                        if (!Musoq.Plugins.MinWindowBatch<decimal>.TryEvaluate(in resultMins0BatchContext))
                        {
                            for (int resultMins0PartitionIndex = 0; resultMins0PartitionIndex < resultMins0PartitionCount; ++resultMins0PartitionIndex)
                            {
                                var resultMins0OutputIndex = resultMins0PartitionIndices[resultMins0PartitionStart + resultMins0PartitionIndex];
                                var resultMins0FrameStart = Math.Max(0, resultMins0PartitionIndex - 2);
                                var resultMins0FrameEnd = resultMins0PartitionIndex;
                                Musoq.Plugins.MinAggregateKernel<decimal>.Reset(ref resultMins0State);
                                for (var resultMins0FrameIndex = Math.Max(0, resultMins0FrameStart); resultMins0FrameIndex <= resultMins0FrameEnd && resultMins0FrameIndex < resultMins0PartitionCount; resultMins0FrameIndex++)
                                {
                                    var resultMins0CurrentIndex = resultMins0PartitionIndices[resultMins0PartitionStart + resultMins0FrameIndex];
                                    Musoq.Plugins.MinAggregateKernel<decimal>.Set(ref resultMins0State, (decimal?)resultMins0BatchValues[resultMins0CurrentIndex]);
                                }

                                resultMins0[resultMins0OutputIndex] = Musoq.Plugins.MinAggregateKernel<decimal>.Get(in resultMins0State);
                            }
                        }
                    }
                }

                var resultMaxs1 = new double? [resultWindowRows.Count];
                var resultMaxs1BatchValues = new double? [resultWindowRows.Count];
                for (int resultMaxs1CaptureIndex = 0; resultMaxs1CaptureIndex < resultWindowRows.Count; ++resultMaxs1CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultMaxs1CaptureIndex];
                    resultMaxs1BatchValues[resultMaxs1CaptureIndex] = (double?)ko3iko.Floating;
                }

                for (int resultMaxs1PartitionSetIndex = 0; resultMaxs1PartitionSetIndex < resultMins0SortedPartitions.PartitionCount; ++resultMaxs1PartitionSetIndex)
                {
                    var resultMaxs1PartitionStart = resultMins0SortedPartitions.GetStart(resultMaxs1PartitionSetIndex);
                    var resultMaxs1PartitionCount = resultMins0SortedPartitions.GetLength(resultMaxs1PartitionSetIndex);
                    var resultMaxs1PartitionIndices = resultMins0SortedPartitions.Indices;
                    Musoq.Plugins.MaxAggregateKernel<double>.State resultMaxs1State = default(Musoq.Plugins.MaxAggregateKernel<double>.State)!;
                    Musoq.Plugins.MaxAggregateKernel<double>.Reset(ref resultMaxs1State);
                    {
                        var resultMaxs1BatchStarts = new int[resultMaxs1PartitionCount];
                        var resultMaxs1BatchEnds = new int[resultMaxs1PartitionCount];
                        for (int resultMaxs1BatchPosition = 0; resultMaxs1BatchPosition < resultMaxs1PartitionCount; ++resultMaxs1BatchPosition)
                        {
                            if ((resultMaxs1BatchPosition & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            resultMaxs1BatchStarts[resultMaxs1BatchPosition] = Math.Max(0, resultMaxs1BatchPosition - 1);
                            resultMaxs1BatchEnds[resultMaxs1BatchPosition] = Math.Min(resultMaxs1PartitionCount - 1, resultMaxs1BatchPosition + 1);
                        }

                        var resultMaxs1BatchContext = new Musoq.Plugins.WindowBatchContext<double?, double?>(resultMaxs1BatchValues, new System.ReadOnlySpan<int>(resultMaxs1PartitionIndices, resultMaxs1PartitionStart, resultMaxs1PartitionCount), resultMaxs1BatchStarts, resultMaxs1BatchEnds, resultMaxs1, token);
                        if (!Musoq.Plugins.MaxWindowBatch<double>.TryEvaluate(in resultMaxs1BatchContext))
                        {
                            for (int resultMaxs1PartitionIndex = 0; resultMaxs1PartitionIndex < resultMaxs1PartitionCount; ++resultMaxs1PartitionIndex)
                            {
                                var resultMaxs1OutputIndex = resultMaxs1PartitionIndices[resultMaxs1PartitionStart + resultMaxs1PartitionIndex];
                                var resultMaxs1FrameStart = Math.Max(0, resultMaxs1PartitionIndex - 1);
                                var resultMaxs1FrameEnd = Math.Min(resultMaxs1PartitionCount - 1, resultMaxs1PartitionIndex + 1);
                                Musoq.Plugins.MaxAggregateKernel<double>.Reset(ref resultMaxs1State);
                                for (var resultMaxs1FrameIndex = Math.Max(0, resultMaxs1FrameStart); resultMaxs1FrameIndex <= resultMaxs1FrameEnd && resultMaxs1FrameIndex < resultMaxs1PartitionCount; resultMaxs1FrameIndex++)
                                {
                                    var resultMaxs1CurrentIndex = resultMaxs1PartitionIndices[resultMaxs1PartitionStart + resultMaxs1FrameIndex];
                                    Musoq.Plugins.MaxAggregateKernel<double>.Set(ref resultMaxs1State, (double?)resultMaxs1BatchValues[resultMaxs1CurrentIndex]);
                                }

                                resultMaxs1[resultMaxs1OutputIndex] = Musoq.Plugins.MaxAggregateKernel<double>.Get(in resultMaxs1State);
                            }
                        }
                    }
                }

                var resultMins2OrderKeys = new WindowResultMins2OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultMins2OrderKeys[windowIndex] = new WindowResultMins2OrderKeysKey(ko3iko.Integral);
                }

                var resultMins2SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultMins0Partitions, resultMins2OrderKeys, false);
                var resultMins2 = new int? [resultWindowRows.Count];
                for (int resultMins2PartitionSetIndex = 0; resultMins2PartitionSetIndex < resultMins2SortedPartitions.PartitionCount; ++resultMins2PartitionSetIndex)
                {
                    var resultMins2PartitionStart = resultMins2SortedPartitions.GetStart(resultMins2PartitionSetIndex);
                    var resultMins2PartitionCount = resultMins2SortedPartitions.GetLength(resultMins2PartitionSetIndex);
                    var resultMins2PartitionIndices = resultMins2SortedPartitions.Indices;
                    Musoq.Plugins.MinAggregateKernel<int>.State resultMins2State = default(Musoq.Plugins.MinAggregateKernel<int>.State)!;
                    Musoq.Plugins.MinAggregateKernel<int>.Reset(ref resultMins2State);
                    for (int resultMins2PartitionIndex = 0; resultMins2PartitionIndex < resultMins2PartitionCount;)
                    {
                        var resultMins2PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultMins2OrderKeys, resultMins2PartitionIndices, resultMins2PartitionStart, resultMins2PartitionCount, resultMins2PartitionIndex);
                        for (int resultMins2PeerIndex = resultMins2PartitionIndex; resultMins2PeerIndex <= resultMins2PeerEnd; ++resultMins2PeerIndex)
                        {
                            var resultMins2CurrentIndex = resultMins2PartitionIndices[resultMins2PartitionStart + resultMins2PeerIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultMins2CurrentIndex];
                            Musoq.Plugins.MinAggregateKernel<int>.Set(ref resultMins2State, (int?)ko3iko.Integral);
                        }

                        var resultMins2PeerValue = Musoq.Plugins.MinAggregateKernel<int>.Get(in resultMins2State);
                        for (int resultMins2PeerIndex = resultMins2PartitionIndex; resultMins2PeerIndex <= resultMins2PeerEnd; ++resultMins2PeerIndex)
                        {
                            var resultMins2CurrentIndex = resultMins2PartitionIndices[resultMins2PartitionStart + resultMins2PeerIndex];
                            resultMins2[resultMins2CurrentIndex] = resultMins2PeerValue;
                        }

                        resultMins2PartitionIndex = resultMins2PeerEnd + 1;
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
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultMins0[windowIndex], (double?)resultMaxs1[windowIndex], (int?)resultMins2[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Position.CompareTo(right.Position);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                return __musoqFinalShapeRows;
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1, double? __value2, int? __value3)
            {
                Position = __value0;
                Minimum = __value1;
                Maximum = __value2;
                PeerMinimum = __value3;
            }

            public override int Count => 4;
            public double? Maximum { get; private set; }
            public decimal? Minimum { get; private set; }
            public int? PeerMinimum { get; private set; }
            public int Position { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Minimum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Maximum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.PeerMinimum = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Minimum),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Maximum),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.PeerMinimum),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Minimum" => true,
                "Maximum" => true,
                "PeerMinimum" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Minimum, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Maximum, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.PeerMinimum, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? Minimum, double? Maximum, int? PeerMinimum)
            {
                this.Position = Position;
                this.Minimum = Minimum;
                this.Maximum = Maximum;
                this.PeerMinimum = PeerMinimum;
            }

            public double? Maximum { get; }
            public decimal? Minimum { get; }
            public int? PeerMinimum { get; }
            public int Position { get; }
        }

        private readonly struct WindowResultMins0OrderKeysKey : System.IEquatable<WindowResultMins0OrderKeysKey>, System.IComparable<WindowResultMins0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultMins0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultMins0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultMins0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultMins0OrderKeysKey other && Equals(other);
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

        private readonly struct WindowResultMins2OrderKeysKey : System.IEquatable<WindowResultMins2OrderKeysKey>, System.IComparable<WindowResultMins2OrderKeysKey>
        {
            private readonly int? _value0;
            public WindowResultMins2OrderKeysKey(int? value0)
            {
                _value0 = value0;
            }

            public int? RawValues => _value0;

            public int CompareTo(WindowResultMins2OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultMins2OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultMins2OrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(int? left, int? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : -1;
                if (!right.HasValue)
                    return 1;
                var comparison = left.Value.CompareTo(right.Value);
                return comparison;
            }
        }
    }
}
