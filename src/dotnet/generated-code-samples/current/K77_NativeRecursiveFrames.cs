// === Parsed Query ===
/*
with recursive counter (Value) as (
    select 0 from values {(Seed: 1)} seed union all
    select c.Value + 1 from counter c where c.Value < 39)
select Value, Sum(Value) over (order by Value rows between 2 preceding and current row) as Total,
    Avg(Value) over () as Average,
    Count(Value) over (partition by Value % 3) as Present
from counter order by Value
*/

// === Logical Plan ===
/*
Cte
  Definition [counter]
    RecursiveCte [counter] [All]
      Anchor
        MultiStatement
          Project [0 as Value]
            ValuesScan [1 rows as seed]
      RecursiveMember
        MultiStatement
          Project [(c.Value + 1) as c.Value + 1]
            Filter [(c.Value < 39)]
              CteRef [counter as c]
  Query
    MultiStatement
      Sort [counter.Value]
        Project [counter.Value as Value, WindowRef(0) as Total, WindowRef(1) as Average, WindowRef(2) as Present]
          Window [Sum(idx:0; order: counter.Value; args: counter.Value; frame: rows between 2 preceding and current row), Avg(idx:1; args: counter.Value), Count(idx:2; partition: (counter.Value % 3); args: counter.Value)]
            CteRef [counter as counter]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [counter]
    PhysicalRecursiveCte [counter] [All]
      Anchor
        PhysicalMultiStatement
          PhysicalProject [0 as Value]
            PhysicalValuesScan [1 rows as seed]
      RecursiveMember
        PhysicalMultiStatement
          PhysicalProject [(c.Value + 1) as c.Value + 1]
            PhysicalFilter [(c.Value < 39)]
              PhysicalCteRef [counter as c]
  Query
    PhysicalMultiStatement
      PhysicalSort [counter.Value]
        PhysicalProject [counter.Value as Value, WindowRef(0) as Total, WindowRef(1) as Average, WindowRef(2) as Present]
          PhysicalWindow [Sum(idx:0; order: counter.Value; args: counter.Value; frame: rows between 2 preceding and current row), Avg(idx:1; args: counter.Value), Count(idx:2; partition: (counter.Value % 3); args: counter.Value)]
            PhysicalMaterialize
              PhysicalCteRef [counter as counter]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    UnknownShape [ValuesRowShape]
      Seed: int <- field Seed
    Generated [Cte0Row0]
      Value: int <- field Value
    TableRow [c]
      Value: int <- field Value
    TableRow [counter]
      Value: int <- field Value
    Generated [ResultRow0]
      Value: int <- field Value
      Total: int? <- field Total
      Average: decimal? <- field Average
      Present: long <- field Present

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    RecursiveCte [counter; result cte0; frontiers cte0CurrentFrontier, cte0NextFrontier; identity none; max iterations 1000; max rows 10000000; max snapshot rows 10000000]
      Anchor
        CreateValuesRows [cte0CurrentFrontier_seedRows: seedValuesD6F9BDFERow0 x 1]
        ForEach [seed in cte0CurrentFrontier_seedRows]
          RecursiveAppend [cte0CurrentFrontier <- Cte0Row0(Value: 0); identity none; guard cte0.Count + cte0CurrentFrontier.Count < 10000000]
      RecursiveMember
        ForEach [c in cte0CurrentFrontier]
          If [(c.Value < 39)]
            RecursiveAppend [cte0NextFrontier <- Cte0Row0(Value: (c.Value + 1)); identity none; guard cte0.Count + cte0NextFrontier.Count < 10000000]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value counter.Value order by counter.Value ASC frame rows between 2 preceding and current row]
    ComputeAvgWindowKernel[WholePartition] [resultAvgs1 <- resultWindowRows value counter.Value]
    ComputeCountWindowKernel[WholePartition] [resultCounts2 <- resultWindowRows value counter.Value partition by (counter.Value % 3)]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, counter in resultWindowRows]
      AppendShape [result <- ResultShape0(Value: counter.Value, Total: resultSums0[windowIndex], Average: resultAvgs1[windowIndex], Present: resultCounts2[windowIndex])]
    SortShapeRows [result -> resultSorted by Value ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K77_NativeRecursiveFrames
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
        private static readonly Column[] __columns_compiled_result_0 = new Column[]
        {
            new Column("Value", typeof(int), 0),
            new Column("Total", typeof(int?), 1),
            new Column("Average", typeof(decimal?), 2),
            new Column("Present", typeof(long), 3)
        };
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_0, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Value, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.Present);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var cte0 = new List<Cte0Row0>();
                    var cte0CurrentFrontier = new List<Cte0Row0>();
                    var cte0NextFrontier = new List<Cte0Row0>();
                    int __cte0Iteration = 0;
                    int __cte0CancellationCounter = 0;
                    seedValuesD6F9BDFERow0[] cte0CurrentFrontier_seedRows = new seedValuesD6F9BDFERow0[]
                    {
                        new seedValuesD6F9BDFERow0(1)
                    };
                    foreach (var seed in cte0CurrentFrontier_seedRows)
                    {
                        token.ThrowIfCancellationRequested();
                        if (cte0.Count + cte0CurrentFrontier.Count >= 10000000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("counter", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                        }

                        cte0CurrentFrontier.Add(new Cte0Row0(0));
                    }

                    cte0.AddRange(cte0CurrentFrontier);
                    while (cte0CurrentFrontier.Count > 0)
                    {
                        if ((__cte0Iteration & 63) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        if (__cte0Iteration >= 1000)
                        {
                            throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("counter", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded, 1000);
                        }

                        __cte0Iteration++;
                        cte0NextFrontier.Clear();
                        for (int cte0CurrentFrontierIndex = 0; cte0CurrentFrontierIndex < cte0CurrentFrontier.Count; ++cte0CurrentFrontierIndex)
                        {
                            if (cte0CurrentFrontierIndex != 0 && (cte0CurrentFrontierIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 c = (Cte0Row0)cte0CurrentFrontier[cte0CurrentFrontierIndex];
                            if ((c.Value < 39))
                            {
                                ++__cte0CancellationCounter;
                                if ((__cte0CancellationCounter & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                if (cte0.Count + cte0NextFrontier.Count >= 10000000)
                                {
                                    throw new global::Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException("counter", global::Musoq.Parser.Diagnostics.DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded, 10000000);
                                }

                                cte0NextFrontier.Add(new Cte0Row0((c.Value + 1)));
                            }
                        }

                        cte0.AddRange(cte0NextFrontier);
                        var __cte0FrontierSwap = cte0CurrentFrontier;
                        cte0CurrentFrontier = cte0NextFrontier;
                        cte0NextFrontier = __cte0FrontierSwap;
                    }

                    OnPhaseChanged("compiled:cte0", QueryPhase.Where);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    _cteRowResults.Slot0 = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Cte0Row0 counter = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(counter.Value);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                var resultSums0SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new int? [resultWindowRows.Count];
                var resultSums0BatchValues = new int? [resultWindowRows.Count];
                for (int resultSums0CaptureIndex = 0; resultSums0CaptureIndex < resultWindowRows.Count; ++resultSums0CaptureIndex)
                {
                    Cte0Row0 counter = resultWindowRows[resultSums0CaptureIndex];
                    resultSums0BatchValues[resultSums0CaptureIndex] = (int?)counter.Value;
                }

                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0SortedPartitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0SortedPartitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0SortedPartitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0SortedPartitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<int>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                    Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                    {
                        var resultSums0OutputIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                        var resultSums0FrameStart = Math.Max(0, resultSums0PartitionIndex - 2);
                        var resultSums0FrameEnd = resultSums0PartitionIndex;
                        Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                        for (var resultSums0FrameIndex = Math.Max(0, resultSums0FrameStart); resultSums0FrameIndex <= resultSums0FrameEnd && resultSums0FrameIndex < resultSums0PartitionCount; resultSums0FrameIndex++)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0FrameIndex];
                            Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums0State, (int?)resultSums0BatchValues[resultSums0CurrentIndex]);
                        }

                        resultSums0[resultSums0OutputIndex] = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums0State);
                    }
                }

                var resultAvgs1 = new decimal? [resultWindowRows.Count];
                for (int resultAvgs1PartitionSetIndex = 0; resultAvgs1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultAvgs1PartitionSetIndex)
                {
                    var resultAvgs1PartitionStart = resultSums0Partitions.GetStart(resultAvgs1PartitionSetIndex);
                    var resultAvgs1PartitionCount = resultSums0Partitions.GetLength(resultAvgs1PartitionSetIndex);
                    var resultAvgs1PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgs1State = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                    Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs1State);
                    for (int resultAvgs1PartitionIndex = 0; resultAvgs1PartitionIndex < resultAvgs1PartitionCount; ++resultAvgs1PartitionIndex)
                    {
                        var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PartitionIndex];
                        Cte0Row0 counter = resultWindowRows[resultAvgs1CurrentIndex];
                        Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgs1State, (int?)counter.Value);
                    }

                    var resultAvgs1FinalValue = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgs1State);
                    for (int resultAvgs1PartitionIndex = 0; resultAvgs1PartitionIndex < resultAvgs1PartitionCount; ++resultAvgs1PartitionIndex)
                    {
                        var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PartitionIndex];
                        resultAvgs1[resultAvgs1CurrentIndex] = resultAvgs1FinalValue;
                    }
                }

                var resultCounts2PartitionBuilder = new Musoq.Evaluator.Helpers.WindowPartitionBuilder<int>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Cte0Row0 counter = resultWindowRows[windowIndex];
                    int resultCounts2PartitionKeysValue = (int)(counter.Value % 3);
                    resultCounts2PartitionBuilder.Add(resultCounts2PartitionKeysValue, windowIndex);
                }

                var resultCounts2Partitions = resultCounts2PartitionBuilder.ToPartitionSet();
                var resultCounts2 = new long[resultWindowRows.Count];
                for (int resultCounts2PartitionSetIndex = 0; resultCounts2PartitionSetIndex < resultCounts2Partitions.PartitionCount; ++resultCounts2PartitionSetIndex)
                {
                    var resultCounts2PartitionStart = resultCounts2Partitions.GetStart(resultCounts2PartitionSetIndex);
                    var resultCounts2PartitionCount = resultCounts2Partitions.GetLength(resultCounts2PartitionSetIndex);
                    var resultCounts2PartitionIndices = resultCounts2Partitions.Indices;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.State resultCounts2State = default(Musoq.Plugins.CountNullableAggregateKernel<int>.State)!;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts2State);
                    for (int resultCounts2PartitionIndex = 0; resultCounts2PartitionIndex < resultCounts2PartitionCount; ++resultCounts2PartitionIndex)
                    {
                        var resultCounts2CurrentIndex = resultCounts2PartitionIndices[resultCounts2PartitionStart + resultCounts2PartitionIndex];
                        Cte0Row0 counter = resultWindowRows[resultCounts2CurrentIndex];
                        Musoq.Plugins.CountNullableAggregateKernel<int>.Set(ref resultCounts2State, (int?)counter.Value);
                    }

                    var resultCounts2FinalValue = Musoq.Plugins.CountNullableAggregateKernel<int>.Get(in resultCounts2State);
                    for (int resultCounts2PartitionIndex = 0; resultCounts2PartitionIndex < resultCounts2PartitionCount; ++resultCounts2PartitionIndex)
                    {
                        var resultCounts2CurrentIndex = resultCounts2PartitionIndices[resultCounts2PartitionStart + resultCounts2PartitionIndex];
                        resultCounts2[resultCounts2CurrentIndex] = resultCounts2FinalValue;
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 counter = resultWindowRows[windowIndex];
                    result.Add(new ResultShape0(counter.Value, (int?)resultSums0[windowIndex], (decimal?)resultAvgs1[windowIndex], (long)resultCounts2[windowIndex]));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Value.CompareTo(right.Value);
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

        private readonly struct Cte0Row0
        {
            public Cte0Row0(int Value)
            {
                this.Value = Value;
            }

            public int Value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int? __value1, decimal? __value2, long __value3)
            {
                Value = __value0;
                Total = __value1;
                Average = __value2;
                Present = __value3;
            }

            public decimal? Average { get; private set; }
            public override int Count => 4;
            public long Present { get; private set; }
            public int? Total { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                "Total" => true,
                "Average" => true,
                "Present" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Present, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Value, int? Total, decimal? Average, long Present)
            {
                this.Value = Value;
                this.Total = Total;
                this.Average = Average;
                this.Present = Present;
            }

            public decimal? Average { get; }
            public long Present { get; }
            public int? Total { get; }
            public int Value { get; }
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

        private sealed class seedValuesD6F9BDFERow0 : Row
        {
            public seedValuesD6F9BDFERow0(int __value0)
            {
                Seed = __value0;
            }

            public override int Count => 1;
            public int Seed { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Seed = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Seed),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Seed" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Seed, ref visitor);
            }
        }
    }
}
