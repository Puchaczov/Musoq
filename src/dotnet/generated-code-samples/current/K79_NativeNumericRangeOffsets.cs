// === Parsed Query ===
/*
select Position, Sum(Value) over (order by Value range between 2 preceding and 1 following) as Total, Count(Value) over (order by Value range between 2 preceding and 1 following) as Present from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Present]
      Window [Sum(idx:0; order: ko3iko.Value; args: ko3iko.Value; frame: range between 2 preceding and 1 following), Count(idx:1; order: ko3iko.Value; args: ko3iko.Value; frame: range between 2 preceding and 1 following)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Present]
      PhysicalWindow [Sum(idx:0; order: ko3iko.Value; args: ko3iko.Value; frame: range between 2 preceding and 1 following), Count(idx:1; order: ko3iko.Value; args: ko3iko.Value; frame: range between 2 preceding and 1 following)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: CoverageRow]
      Position: int <- property Position
      Value: int? <- property Value
    Generated [ResultRow0]
      Position: int <- field Position
      Total: int? <- field Total
      Present: long <- field Present

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: CoverageRow] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value ko3iko.Value order by ko3iko.Value ASC frame range between 2 preceding and 1 following]
    ComputeCountWindowKernel[BoundedRows] [resultCounts1 <- resultWindowRows value ko3iko.Value order by ko3iko.Value ASC frame range between 2 preceding and 1 following]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultSums0[windowIndex], Present: resultCounts1[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K79_NativeNumericRangeOffsets
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
            new Column("Total", typeof(int?), 1),
            new Column("Present", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Value", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Present);
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
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                var resultSums0RangeKeys = new int? [resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow ko3iko = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ko3iko.Value);
                    resultSums0RangeKeys[windowIndex] = (int?)resultSums0OrderKeys[windowIndex].RawValues;
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new int? [resultWindowRows.Count];
                var resultSums0BatchValues = new int? [resultWindowRows.Count];
                for (int resultSums0CaptureIndex = 0; resultSums0CaptureIndex < resultWindowRows.Count; ++resultSums0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow ko3iko = resultWindowRows[resultSums0CaptureIndex];
                    resultSums0BatchValues[resultSums0CaptureIndex] = (int?)ko3iko.Value;
                }

                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<int>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                    Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                    {
                        var resultSums0OutputIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                        var resultSums0FrameStart = WindowFunctionHelpers.ResolveRangeFrameStart(resultSums0RangeKeys, resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex, -2, false, true);
                        var resultSums0FrameEnd = WindowFunctionHelpers.ResolveRangeFrameEnd(resultSums0RangeKeys, resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex, 1, false, true);
                        Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                        for (var resultSums0FrameIndex = Math.Max(0, resultSums0FrameStart); resultSums0FrameIndex <= resultSums0FrameEnd && resultSums0FrameIndex < resultSums0PartitionCount; resultSums0FrameIndex++)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0FrameIndex];
                            Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums0State, (int?)resultSums0BatchValues[resultSums0CurrentIndex]);
                        }

                        resultSums0[resultSums0OutputIndex] = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums0State);
                    }
                }

                var resultCounts1RangeKeys = new int? [resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow ko3iko = resultWindowRows[windowIndex];
                    resultCounts1RangeKeys[windowIndex] = (int?)resultSums0OrderKeys[windowIndex].RawValues;
                }

                var resultCounts1 = new long[resultWindowRows.Count];
                var resultCounts1BatchValues = new int? [resultWindowRows.Count];
                for (int resultCounts1CaptureIndex = 0; resultCounts1CaptureIndex < resultWindowRows.Count; ++resultCounts1CaptureIndex)
                {
                    Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow ko3iko = resultWindowRows[resultCounts1CaptureIndex];
                    resultCounts1BatchValues[resultCounts1CaptureIndex] = (int?)ko3iko.Value;
                }

                for (int resultCounts1PartitionSetIndex = 0; resultCounts1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultCounts1PartitionSetIndex)
                {
                    var resultCounts1PartitionStart = resultSums0Partitions.GetStart(resultCounts1PartitionSetIndex);
                    var resultCounts1PartitionCount = resultSums0Partitions.GetLength(resultCounts1PartitionSetIndex);
                    var resultCounts1PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.State resultCounts1State = default(Musoq.Plugins.CountNullableAggregateKernel<int>.State)!;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts1State);
                    for (int resultCounts1PartitionIndex = 0; resultCounts1PartitionIndex < resultCounts1PartitionCount; ++resultCounts1PartitionIndex)
                    {
                        var resultCounts1OutputIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1PartitionIndex];
                        var resultCounts1FrameStart = WindowFunctionHelpers.ResolveRangeFrameStart(resultCounts1RangeKeys, resultSums0OrderKeys, resultCounts1PartitionIndices, resultCounts1PartitionStart, resultCounts1PartitionCount, resultCounts1PartitionIndex, -2, false, true);
                        var resultCounts1FrameEnd = WindowFunctionHelpers.ResolveRangeFrameEnd(resultCounts1RangeKeys, resultSums0OrderKeys, resultCounts1PartitionIndices, resultCounts1PartitionStart, resultCounts1PartitionCount, resultCounts1PartitionIndex, 1, false, true);
                        Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts1State);
                        for (var resultCounts1FrameIndex = Math.Max(0, resultCounts1FrameStart); resultCounts1FrameIndex <= resultCounts1FrameEnd && resultCounts1FrameIndex < resultCounts1PartitionCount; resultCounts1FrameIndex++)
                        {
                            var resultCounts1CurrentIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1FrameIndex];
                            Musoq.Plugins.CountNullableAggregateKernel<int>.Set(ref resultCounts1State, (int?)resultCounts1BatchValues[resultCounts1CurrentIndex]);
                        }

                        resultCounts1[resultCounts1OutputIndex] = Musoq.Plugins.CountNullableAggregateKernel<int>.Get(in resultCounts1State);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow ko3iko = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(ko3iko.Position, (int?)resultSums0[windowIndex], (long)resultCounts1[windowIndex]));
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
            public ResultRow0(int __value0, int? __value1, long __value2)
            {
                Position = __value0;
                Total = __value1;
                Present = __value2;
            }

            public override int Count => 3;
            public int Position { get; private set; }
            public long Present { get; private set; }
            public int? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                "Present" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Present, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? Total, long Present)
            {
                this.Position = Position;
                this.Total = Total;
                this.Present = Present;
            }

            public int Position { get; }
            public long Present { get; }
            public int? Total { get; }
        }

        private readonly struct WindowResultSums0OrderKeysKey : System.IEquatable<WindowResultSums0OrderKeysKey>, System.IComparable<WindowResultSums0OrderKeysKey>
        {
            private readonly int? _value0;
            public WindowResultSums0OrderKeysKey(int? value0)
            {
                _value0 = value0;
            }

            public int? RawValues => _value0;

            public int CompareTo(WindowResultSums0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSums0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int?>.Default.Equals(_value0, other._value0);
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
