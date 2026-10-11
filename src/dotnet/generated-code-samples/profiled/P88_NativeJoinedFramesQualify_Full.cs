// === Parsed Query ===
/*
with joined as (
    select a.Position as Position, a.Partition as Partition,
        a.Value + b.Value as Value
    from #schema.first() a left join #schema.first() b on b.Position = a.Position - 1)
select Position, Partition,
    Sum(Value) over (partition by Partition order by Position
        rows between 2 preceding and 1 following) as Total,
    Avg(Value) over (partition by Partition order by Position
        rows between 2 preceding and 1 following) as Average,
    Count(Value) over (partition by Partition order by Position
        rows between 2 preceding and 1 following) as Present,
    Lag(Value, 1) over (partition by Partition order by Position) as Previous,
    RowNumber() over (partition by Partition order by Position) as Number
from joined qualify Number <= 5 order by Position
*/

// === Logical Plan ===
/*
Cte
  Definition [joined]
    MultiStatement
      Project [a.Position as a.Position, a.Partition as a.Partition, a.Value as a.Value, b.Position as b.Position, b.Value as b.Value]
        Join [LeftOuter] [(b.Position = (a.Position - 1))]
          SchemaScan [#schema.first() as a]
          SchemaScan [#schema.first() as b]
      Project [a.Position as Position, a.Partition as Partition, (a.Value + b.Value) as Value]
        CteRef [ab as ab]
  Query
    MultiStatement
      Sort [joined.Position]
        Project [joined.Position as Position, joined.Partition as Partition, WindowRef(1) as Total, WindowRef(2) as Average, WindowRef(3) as Present, WindowRef(4) as Previous, WindowRef(0) as Number]
          Qualify [(WindowRef(0) <= 5)]
            Window [RowNumber(idx:0; partition: joined.Partition; order: joined.Position), Sum(idx:1; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Avg(idx:2; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Count(idx:3; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Lag(idx:4; partition: joined.Partition; order: joined.Position; args: joined.Value, 1)]
              CteRef [joined as joined]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [joined]
    PhysicalMultiStatement
      PhysicalProject [a.Position as a.Position, a.Partition as a.Partition, a.Value as a.Value, b.Position as b.Position, b.Value as b.Value]
        PhysicalHashJoin [LeftOuter] [build: b.Position] [probe: (a.Position - 1)]
          PhysicalSchemaScan [#schema.first() as a]
          PhysicalSchemaScan [#schema.first() as b]
      PhysicalProject [a.Position as Position, a.Partition as Partition, (a.Value + b.Value) as Value]
        PhysicalCteRef [ab as ab]
  Query
    PhysicalMultiStatement
      PhysicalSort [joined.Position]
        PhysicalProject [joined.Position as Position, joined.Partition as Partition, WindowRef(1) as Total, WindowRef(2) as Average, WindowRef(3) as Present, WindowRef(4) as Previous, WindowRef(0) as Number]
          PhysicalQualify [(WindowRef(0) <= 5)]
            PhysicalWindow [RowNumber(idx:0; partition: joined.Partition; order: joined.Position), Sum(idx:1; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Avg(idx:2; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Count(idx:3; partition: joined.Partition; order: joined.Position; args: joined.Value; frame: rows between 2 preceding and 1 following), Lag(idx:4; partition: joined.Partition; order: joined.Position; args: joined.Value, 1)]
              PhysicalMaterialize
                PhysicalCteRef [joined as joined]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: CoverageRow]
      Position: int <- property Position
      Partition: int? <- property Partition
      Value: int? <- property Value
    SourceEntity [b: CoverageRow]
      Position: int <- property Position
      Value: int? <- property Value
    Generated [Cte0Row0]
      Position: int <- field Position
      Partition: int? <- field Partition
      Value: int? <- field Value
    TableRow [joined]
      Position: int <- field Position
      Partition: int? <- field Partition
      Value: int? <- field Value
    Generated [ResultRow0]
      Position: int <- field Position
      Partition: int? <- field Partition
      Total: int? <- field Total
      Average: decimal? <- field Average
      Present: long <- field Present
      Previous: int? <- field Previous
      Number: long <- field Number

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: CoverageRow] -> cte0_aRows
    SourceScan [b: CoverageRow] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    CreateHash [cte0BHash: int? -> CoverageRow]
    ChunkedForEach [b in cte0_bRows]
      HashAdd [cte0BHash[b.Position] += b]
    ChunkedForEach [a in cte0_aRows]
      HashProbe [cte0BHash[(a.Position - 1)] -> cte0BHashMatches] [match: cte0BHashHasMatch]
        ForEach [b in cte0BHashMatches]
          Assign [cte0BHashHasMatch = TRUE]
          AppendRow [cte0 <- Cte0Row0(Position: a.Position, Partition: a.Partition, Value: (a.Value + b.Value))]
      HashProbeNoMatch
        AppendRow [cte0 <- Cte0Row0(Position: a.Position, Partition: a.Partition, Value: NULL)]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers0 <- resultWindowRows partition by joined.Partition order by joined.Position ASC qualify <= 5]
    ComputeSumWindowKernel[BoundedRows] [resultSums1 <- resultWindowRows value joined.Value partition by joined.Partition order by joined.Position ASC frame rows between 2 preceding and 1 following]
    ComputeAvgWindowKernel[BoundedRows] [resultAvgs2 <- resultWindowRows value joined.Value partition by joined.Partition order by joined.Position ASC frame rows between 2 preceding and 1 following]
    ComputeCountWindowKernel[BoundedRows] [resultCounts3 <- resultWindowRows value joined.Value partition by joined.Partition order by joined.Position ASC frame rows between 2 preceding and 1 following]
    ComputeLagWindow [resultLags4 <- resultWindowRows value joined.Value partition by joined.Partition order by joined.Position ASC frame range between unbounded preceding and current row args 1, NULL]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, joined in resultWindowRows]
      If [((resultRowNumbers0[windowIndex] > 0) AND (resultRowNumbers0[windowIndex] <= 5))]
        AppendShape [result <- ResultShape0(Position: joined.Position, Partition: joined.Partition, Total: resultSums1[windowIndex], Average: resultAvgs2[windowIndex], Present: resultCounts3[windowIndex], Previous: resultLags4[windowIndex], Number: resultRowNumbers0[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P88_NativeJoinedFramesQualify_Full
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Musoq.Schema;
    using Musoq.Schema.Diagnostics;
    using Musoq.Schema.Optimization;
    using Musoq.Evaluator;
    using Musoq.Evaluator.Diagnostics;
    using Musoq.Evaluator.Tables;
    using Musoq.Evaluator.Helpers;
    using Musoq.Evaluator.Runtime;
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable, IProfiledRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_2 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Partition", typeof(int?), 1),
            new Column("Value", typeof(int?), 2)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("Partition", typeof(int?), 1),
            new Column("Total", typeof(int?), 2),
            new Column("Average", typeof(decimal?), 3),
            new Column("Present", typeof(long), 4),
            new Column("Previous", typeof(int?), 5),
            new Column("Number", typeof(long), 6)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(int?), 1), new Column("Value", typeof(int?), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Value", typeof(int?), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Partition, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.Present, __musoqShapeRow.Previous, __musoqShapeRow.Number);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Partition, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.Present, __musoqShapeRow.Previous, __musoqShapeRow.Number);
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
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var resultRowNumbers0PartitionKeys = new int? [resultWindowRows.Count];
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
                    var resultRowNumbers0PartitionLimit = (int)System.Math.Min((long)resultRowNumbers0PartitionCount, 5L);
                    for (int resultRowNumbers0PartitionIndex = 0; resultRowNumbers0PartitionIndex < resultRowNumbers0PartitionLimit; ++resultRowNumbers0PartitionIndex)
                    {
                        var resultRowNumbers0CurrentIndex = resultRowNumbers0PartitionIndices[resultRowNumbers0PartitionStart + resultRowNumbers0PartitionIndex];
                        var resultRowNumbers0Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PartitionIndex, 0, 0, 0L, resultRowNumbers0PartitionCount, token);
                        resultRowNumbers0[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers0Context);
                    }
                }

                var resultSums1 = new int? [resultWindowRows.Count];
                var resultSums1BatchValues = new int? [resultWindowRows.Count];
                for (int resultSums1CaptureIndex = 0; resultSums1CaptureIndex < resultWindowRows.Count; ++resultSums1CaptureIndex)
                {
                    Cte0Row0 joined = resultWindowRows[resultSums1CaptureIndex];
                    resultSums1BatchValues[resultSums1CaptureIndex] = (int?)joined.Value;
                }

                for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                {
                    var resultSums1PartitionStart = resultRowNumbers0Partitions.GetStart(resultSums1PartitionSetIndex);
                    var resultSums1PartitionCount = resultRowNumbers0Partitions.GetLength(resultSums1PartitionSetIndex);
                    var resultSums1PartitionIndices = resultRowNumbers0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<int>.State resultSums1State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                    Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums1State);
                    for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount; ++resultSums1PartitionIndex)
                    {
                        var resultSums1OutputIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];
                        var resultSums1FrameStart = Math.Max(0, resultSums1PartitionIndex - 2);
                        var resultSums1FrameEnd = Math.Min(resultSums1PartitionCount - 1, resultSums1PartitionIndex + 1);
                        Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums1State);
                        for (var resultSums1FrameIndex = Math.Max(0, resultSums1FrameStart); resultSums1FrameIndex <= resultSums1FrameEnd && resultSums1FrameIndex < resultSums1PartitionCount; resultSums1FrameIndex++)
                        {
                            var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1FrameIndex];
                            Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums1State, (int?)resultSums1BatchValues[resultSums1CurrentIndex]);
                        }

                        resultSums1[resultSums1OutputIndex] = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums1State);
                    }
                }

                var resultAvgs2 = new decimal? [resultWindowRows.Count];
                var resultAvgs2BatchValues = new int? [resultWindowRows.Count];
                for (int resultAvgs2CaptureIndex = 0; resultAvgs2CaptureIndex < resultWindowRows.Count; ++resultAvgs2CaptureIndex)
                {
                    Cte0Row0 joined = resultWindowRows[resultAvgs2CaptureIndex];
                    resultAvgs2BatchValues[resultAvgs2CaptureIndex] = (int?)joined.Value;
                }

                for (int resultAvgs2PartitionSetIndex = 0; resultAvgs2PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultAvgs2PartitionSetIndex)
                {
                    var resultAvgs2PartitionStart = resultRowNumbers0Partitions.GetStart(resultAvgs2PartitionSetIndex);
                    var resultAvgs2PartitionCount = resultRowNumbers0Partitions.GetLength(resultAvgs2PartitionSetIndex);
                    var resultAvgs2PartitionIndices = resultRowNumbers0Partitions.Indices;
                    Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgs2State = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                    Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs2State);
                    for (int resultAvgs2PartitionIndex = 0; resultAvgs2PartitionIndex < resultAvgs2PartitionCount; ++resultAvgs2PartitionIndex)
                    {
                        var resultAvgs2OutputIndex = resultAvgs2PartitionIndices[resultAvgs2PartitionStart + resultAvgs2PartitionIndex];
                        var resultAvgs2FrameStart = Math.Max(0, resultAvgs2PartitionIndex - 2);
                        var resultAvgs2FrameEnd = Math.Min(resultAvgs2PartitionCount - 1, resultAvgs2PartitionIndex + 1);
                        Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs2State);
                        for (var resultAvgs2FrameIndex = Math.Max(0, resultAvgs2FrameStart); resultAvgs2FrameIndex <= resultAvgs2FrameEnd && resultAvgs2FrameIndex < resultAvgs2PartitionCount; resultAvgs2FrameIndex++)
                        {
                            var resultAvgs2CurrentIndex = resultAvgs2PartitionIndices[resultAvgs2PartitionStart + resultAvgs2FrameIndex];
                            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgs2State, (int?)resultAvgs2BatchValues[resultAvgs2CurrentIndex]);
                        }

                        resultAvgs2[resultAvgs2OutputIndex] = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgs2State);
                    }
                }

                var resultCounts3 = new long[resultWindowRows.Count];
                var resultCounts3BatchValues = new int? [resultWindowRows.Count];
                for (int resultCounts3CaptureIndex = 0; resultCounts3CaptureIndex < resultWindowRows.Count; ++resultCounts3CaptureIndex)
                {
                    Cte0Row0 joined = resultWindowRows[resultCounts3CaptureIndex];
                    resultCounts3BatchValues[resultCounts3CaptureIndex] = (int?)joined.Value;
                }

                for (int resultCounts3PartitionSetIndex = 0; resultCounts3PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultCounts3PartitionSetIndex)
                {
                    var resultCounts3PartitionStart = resultRowNumbers0Partitions.GetStart(resultCounts3PartitionSetIndex);
                    var resultCounts3PartitionCount = resultRowNumbers0Partitions.GetLength(resultCounts3PartitionSetIndex);
                    var resultCounts3PartitionIndices = resultRowNumbers0Partitions.Indices;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.State resultCounts3State = default(Musoq.Plugins.CountNullableAggregateKernel<int>.State)!;
                    Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts3State);
                    for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                    {
                        var resultCounts3OutputIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                        var resultCounts3FrameStart = Math.Max(0, resultCounts3PartitionIndex - 2);
                        var resultCounts3FrameEnd = Math.Min(resultCounts3PartitionCount - 1, resultCounts3PartitionIndex + 1);
                        Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts3State);
                        for (var resultCounts3FrameIndex = Math.Max(0, resultCounts3FrameStart); resultCounts3FrameIndex <= resultCounts3FrameEnd && resultCounts3FrameIndex < resultCounts3PartitionCount; resultCounts3FrameIndex++)
                        {
                            var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3FrameIndex];
                            Musoq.Plugins.CountNullableAggregateKernel<int>.Set(ref resultCounts3State, (int?)resultCounts3BatchValues[resultCounts3CurrentIndex]);
                        }

                        resultCounts3[resultCounts3OutputIndex] = Musoq.Plugins.CountNullableAggregateKernel<int>.Get(in resultCounts3State);
                    }
                }

                var resultLags4Values = new int? [resultWindowRows.Count];
                var resultLags4Argument0 = new int[resultWindowRows.Count];
                var resultLags4Argument1 = new int? [resultWindowRows.Count];
                for (int resultLags4ExtractionIndex = 0; resultLags4ExtractionIndex < resultWindowRows.Count; ++resultLags4ExtractionIndex)
                {
                    Cte0Row0 joined = resultWindowRows[resultLags4ExtractionIndex];
                    resultLags4Values[resultLags4ExtractionIndex] = (int?)(int?)joined.Value;
                    resultLags4Argument0[resultLags4ExtractionIndex] = (int)(int)1;
                    resultLags4Argument1[resultLags4ExtractionIndex] = (int?)(int?)null;
                }

                var resultLags4 = new int? [resultWindowRows.Count];
                for (int resultLags4Partition = 0; resultLags4Partition < resultRowNumbers0Partitions.PartitionCount; ++resultLags4Partition)
                {
                    var resultLags4Start = resultRowNumbers0Partitions.GetStart(resultLags4Partition);
                    var resultLags4Count = resultRowNumbers0Partitions.GetLength(resultLags4Partition);
                    var resultLags4Indices = resultRowNumbers0Partitions.Indices;
                    for (int resultLags4Position = 0; resultLags4Position < resultLags4Count; ++resultLags4Position)
                    {
                        if ((resultLags4Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultLags4Current = resultLags4Indices[resultLags4Start + resultLags4Position];
                        var resultLags4FrameStart = 0;
                        var resultLags4FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultRowNumbers0OrderKeys, resultLags4Indices, resultLags4Start, resultLags4Count, resultLags4Position);
                        var resultLags4Context = new Musoq.Plugins.WindowFrameContext<int?>(resultLags4Values, new System.ReadOnlySpan<int>(resultLags4Indices, resultLags4Start, resultLags4Count), resultLags4Position, resultLags4FrameStart, resultLags4FrameEnd, token);
                        resultLags4[resultLags4Current] = Musoq.Plugins.LagKernel<int>.Get(in resultLags4Context, resultLags4Argument0[resultLags4Current], resultLags4Argument1[resultLags4Current]);
                    }
                }

                var result = new List<ResultShape0>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 joined = resultWindowRows[windowIndex];
                    if ((((long)resultRowNumbers0[windowIndex] > 0L) && ((long)resultRowNumbers0[windowIndex] <= 5)))
                    {
                        result.Add(new ResultShape0(joined.Position, joined.Partition, (int?)resultSums1[windowIndex], (decimal?)resultAvgs2[windowIndex], (long)resultCounts3[windowIndex], (int?)resultLags4[windowIndex], (long)resultRowNumbers0[windowIndex]));
                    }
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

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
            try
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
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op28Handle = profileRecorder?.GetOperatorHandle("op28", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op31Handle = profileRecorder?.GetOperatorHandle("op31", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "Materialize") ?? OperatorProfileHandle.None;
                    var __op51Handle = profileRecorder?.GetOperatorHandle("op51", "ComputeRowNumberWindow") ?? OperatorProfileHandle.None;
                    var __op52Handle = profileRecorder?.GetOperatorHandle("op52", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op53Handle = profileRecorder?.GetOperatorHandle("op53", "ComputeAvgWindowKernel") ?? OperatorProfileHandle.None;
                    var __op54Handle = profileRecorder?.GetOperatorHandle("op54", "ComputeCountWindowKernel") ?? OperatorProfileHandle.None;
                    var __op55Handle = profileRecorder?.GetOperatorHandle("op55", "ComputeLagWindow") ?? OperatorProfileHandle.None;
                    var __op56Handle = profileRecorder?.GetOperatorHandle("op56", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op57Handle = profileRecorder?.GetOperatorHandle("op57", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op59Handle = profileRecorder?.GetOperatorHandle("op59", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op60Handle = profileRecorder?.GetOperatorHandle("op60", "SortShapeRows") ?? OperatorProfileHandle.None;
                    long __op59OutputRows = 0L;
                    var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op27Scope.Dispose();
                    var __op28Scope = profileRecorder?.BeginOperatorValue(__op28Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op28Scope.Dispose();
                    var __op31Scope = profileRecorder?.BeginOperatorValue(__op31Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op31Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                    __op50Scope.AddOutputRows(resultWindowRows.Count);
                    __op50Scope.Dispose();
                    var __op51Scope = profileRecorder?.BeginOperatorValue(__op51Handle) ?? OperatorProfileValueScope.None;
                    var resultRowNumbers0PartitionKeys = new int? [resultWindowRows.Count];
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
                        var resultRowNumbers0PartitionLimit = (int)System.Math.Min((long)resultRowNumbers0PartitionCount, 5L);
                        for (int resultRowNumbers0PartitionIndex = 0; resultRowNumbers0PartitionIndex < resultRowNumbers0PartitionLimit; ++resultRowNumbers0PartitionIndex)
                        {
                            var resultRowNumbers0CurrentIndex = resultRowNumbers0PartitionIndices[resultRowNumbers0PartitionStart + resultRowNumbers0PartitionIndex];
                            var resultRowNumbers0Context = new Musoq.Plugins.WindowRankingContext(resultRowNumbers0PartitionIndex, 0, 0, 0L, resultRowNumbers0PartitionCount, token);
                            resultRowNumbers0[resultRowNumbers0CurrentIndex] = Musoq.Plugins.RowNumberKernel.Get(in resultRowNumbers0Context);
                        }
                    }

                    __op51Scope.Dispose();
                    var __op52Scope = profileRecorder?.BeginOperatorValue(__op52Handle) ?? OperatorProfileValueScope.None;
                    var resultSums1 = new int? [resultWindowRows.Count];
                    var resultSums1BatchValues = new int? [resultWindowRows.Count];
                    for (int resultSums1CaptureIndex = 0; resultSums1CaptureIndex < resultWindowRows.Count; ++resultSums1CaptureIndex)
                    {
                        Cte0Row0 joined = resultWindowRows[resultSums1CaptureIndex];
                        resultSums1BatchValues[resultSums1CaptureIndex] = (int?)joined.Value;
                    }

                    for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)
                    {
                        var resultSums1PartitionStart = resultRowNumbers0Partitions.GetStart(resultSums1PartitionSetIndex);
                        var resultSums1PartitionCount = resultRowNumbers0Partitions.GetLength(resultSums1PartitionSetIndex);
                        var resultSums1PartitionIndices = resultRowNumbers0Partitions.Indices;
                        Musoq.Plugins.SumAggregateKernel<int>.State resultSums1State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                        Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums1State);
                        for (int resultSums1PartitionIndex = 0; resultSums1PartitionIndex < resultSums1PartitionCount; ++resultSums1PartitionIndex)
                        {
                            var resultSums1OutputIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];
                            var resultSums1FrameStart = Math.Max(0, resultSums1PartitionIndex - 2);
                            var resultSums1FrameEnd = Math.Min(resultSums1PartitionCount - 1, resultSums1PartitionIndex + 1);
                            Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums1State);
                            for (var resultSums1FrameIndex = Math.Max(0, resultSums1FrameStart); resultSums1FrameIndex <= resultSums1FrameEnd && resultSums1FrameIndex < resultSums1PartitionCount; resultSums1FrameIndex++)
                            {
                                var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1FrameIndex];
                                Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums1State, (int?)resultSums1BatchValues[resultSums1CurrentIndex]);
                            }

                            resultSums1[resultSums1OutputIndex] = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums1State);
                        }
                    }

                    __op52Scope.Dispose();
                    var __op53Scope = profileRecorder?.BeginOperatorValue(__op53Handle) ?? OperatorProfileValueScope.None;
                    var resultAvgs2 = new decimal? [resultWindowRows.Count];
                    var resultAvgs2BatchValues = new int? [resultWindowRows.Count];
                    for (int resultAvgs2CaptureIndex = 0; resultAvgs2CaptureIndex < resultWindowRows.Count; ++resultAvgs2CaptureIndex)
                    {
                        Cte0Row0 joined = resultWindowRows[resultAvgs2CaptureIndex];
                        resultAvgs2BatchValues[resultAvgs2CaptureIndex] = (int?)joined.Value;
                    }

                    for (int resultAvgs2PartitionSetIndex = 0; resultAvgs2PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultAvgs2PartitionSetIndex)
                    {
                        var resultAvgs2PartitionStart = resultRowNumbers0Partitions.GetStart(resultAvgs2PartitionSetIndex);
                        var resultAvgs2PartitionCount = resultRowNumbers0Partitions.GetLength(resultAvgs2PartitionSetIndex);
                        var resultAvgs2PartitionIndices = resultRowNumbers0Partitions.Indices;
                        Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgs2State = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                        Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs2State);
                        for (int resultAvgs2PartitionIndex = 0; resultAvgs2PartitionIndex < resultAvgs2PartitionCount; ++resultAvgs2PartitionIndex)
                        {
                            var resultAvgs2OutputIndex = resultAvgs2PartitionIndices[resultAvgs2PartitionStart + resultAvgs2PartitionIndex];
                            var resultAvgs2FrameStart = Math.Max(0, resultAvgs2PartitionIndex - 2);
                            var resultAvgs2FrameEnd = Math.Min(resultAvgs2PartitionCount - 1, resultAvgs2PartitionIndex + 1);
                            Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs2State);
                            for (var resultAvgs2FrameIndex = Math.Max(0, resultAvgs2FrameStart); resultAvgs2FrameIndex <= resultAvgs2FrameEnd && resultAvgs2FrameIndex < resultAvgs2PartitionCount; resultAvgs2FrameIndex++)
                            {
                                var resultAvgs2CurrentIndex = resultAvgs2PartitionIndices[resultAvgs2PartitionStart + resultAvgs2FrameIndex];
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgs2State, (int?)resultAvgs2BatchValues[resultAvgs2CurrentIndex]);
                            }

                            resultAvgs2[resultAvgs2OutputIndex] = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgs2State);
                        }
                    }

                    __op53Scope.Dispose();
                    var __op54Scope = profileRecorder?.BeginOperatorValue(__op54Handle) ?? OperatorProfileValueScope.None;
                    var resultCounts3 = new long[resultWindowRows.Count];
                    var resultCounts3BatchValues = new int? [resultWindowRows.Count];
                    for (int resultCounts3CaptureIndex = 0; resultCounts3CaptureIndex < resultWindowRows.Count; ++resultCounts3CaptureIndex)
                    {
                        Cte0Row0 joined = resultWindowRows[resultCounts3CaptureIndex];
                        resultCounts3BatchValues[resultCounts3CaptureIndex] = (int?)joined.Value;
                    }

                    for (int resultCounts3PartitionSetIndex = 0; resultCounts3PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultCounts3PartitionSetIndex)
                    {
                        var resultCounts3PartitionStart = resultRowNumbers0Partitions.GetStart(resultCounts3PartitionSetIndex);
                        var resultCounts3PartitionCount = resultRowNumbers0Partitions.GetLength(resultCounts3PartitionSetIndex);
                        var resultCounts3PartitionIndices = resultRowNumbers0Partitions.Indices;
                        Musoq.Plugins.CountNullableAggregateKernel<int>.State resultCounts3State = default(Musoq.Plugins.CountNullableAggregateKernel<int>.State)!;
                        Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts3State);
                        for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                        {
                            var resultCounts3OutputIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                            var resultCounts3FrameStart = Math.Max(0, resultCounts3PartitionIndex - 2);
                            var resultCounts3FrameEnd = Math.Min(resultCounts3PartitionCount - 1, resultCounts3PartitionIndex + 1);
                            Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref resultCounts3State);
                            for (var resultCounts3FrameIndex = Math.Max(0, resultCounts3FrameStart); resultCounts3FrameIndex <= resultCounts3FrameEnd && resultCounts3FrameIndex < resultCounts3PartitionCount; resultCounts3FrameIndex++)
                            {
                                var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3FrameIndex];
                                Musoq.Plugins.CountNullableAggregateKernel<int>.Set(ref resultCounts3State, (int?)resultCounts3BatchValues[resultCounts3CurrentIndex]);
                            }

                            resultCounts3[resultCounts3OutputIndex] = Musoq.Plugins.CountNullableAggregateKernel<int>.Get(in resultCounts3State);
                        }
                    }

                    __op54Scope.Dispose();
                    var __op55Scope = profileRecorder?.BeginOperatorValue(__op55Handle) ?? OperatorProfileValueScope.None;
                    var resultLags4Values = new int? [resultWindowRows.Count];
                    var resultLags4Argument0 = new int[resultWindowRows.Count];
                    var resultLags4Argument1 = new int? [resultWindowRows.Count];
                    for (int resultLags4ExtractionIndex = 0; resultLags4ExtractionIndex < resultWindowRows.Count; ++resultLags4ExtractionIndex)
                    {
                        Cte0Row0 joined = resultWindowRows[resultLags4ExtractionIndex];
                        resultLags4Values[resultLags4ExtractionIndex] = (int?)(int?)joined.Value;
                        resultLags4Argument0[resultLags4ExtractionIndex] = (int)(int)1;
                        resultLags4Argument1[resultLags4ExtractionIndex] = (int?)(int?)null;
                    }

                    var resultLags4 = new int? [resultWindowRows.Count];
                    for (int resultLags4Partition = 0; resultLags4Partition < resultRowNumbers0Partitions.PartitionCount; ++resultLags4Partition)
                    {
                        var resultLags4Start = resultRowNumbers0Partitions.GetStart(resultLags4Partition);
                        var resultLags4Count = resultRowNumbers0Partitions.GetLength(resultLags4Partition);
                        var resultLags4Indices = resultRowNumbers0Partitions.Indices;
                        for (int resultLags4Position = 0; resultLags4Position < resultLags4Count; ++resultLags4Position)
                        {
                            if ((resultLags4Position & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultLags4Current = resultLags4Indices[resultLags4Start + resultLags4Position];
                            var resultLags4FrameStart = 0;
                            var resultLags4FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultRowNumbers0OrderKeys, resultLags4Indices, resultLags4Start, resultLags4Count, resultLags4Position);
                            var resultLags4Context = new Musoq.Plugins.WindowFrameContext<int?>(resultLags4Values, new System.ReadOnlySpan<int>(resultLags4Indices, resultLags4Start, resultLags4Count), resultLags4Position, resultLags4FrameStart, resultLags4FrameEnd, token);
                            resultLags4[resultLags4Current] = Musoq.Plugins.LagKernel<int>.Get(in resultLags4Context, resultLags4Argument0[resultLags4Current], resultLags4Argument1[resultLags4Current]);
                        }
                    }

                    __op55Scope.Dispose();
                    var __op56Scope = profileRecorder?.BeginOperatorValue(__op56Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op56Scope.Dispose();
                    long __op57InputRows = 0L;
                    long __op57OutputRows = 0L;
                    var __op57Scope = profileRecorder?.BeginOperatorValue(__op57Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 joined = resultWindowRows[windowIndex];
                            __op57InputRows++;
                            __op57OutputRows++;
                            if ((((long)resultRowNumbers0[windowIndex] > 0L) && ((long)resultRowNumbers0[windowIndex] <= 5)))
                            {
                                result.Add(new ResultShape0(joined.Position, joined.Partition, (int?)resultSums1[windowIndex], (decimal?)resultAvgs2[windowIndex], (long)resultCounts3[windowIndex], (int?)resultLags4[windowIndex], (long)resultRowNumbers0[windowIndex]));
                                __op59OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op57Scope.AddInputRows(__op57InputRows);
                        __op57Scope.AddOutputRows(__op57OutputRows);
                        __op57Scope.Dispose();
                    }

                    var __op60Scope = profileRecorder?.BeginOperatorValue(__op60Handle) ?? OperatorProfileValueScope.None;
                    __op60Scope.AddInputRows(result.Count);
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

                    __op60Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op60Scope.Dispose();
                    if (__op59OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op59Handle, __op59OutputRows);
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
            catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
            {
                profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                throw;
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_aSchema = provider.GetSchema("#schema");
                var cte0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__cte0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : cte0_aRowsSource.Chunks;
                var __cte0_bSchema = provider.GetSchema("#schema");
                var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__cte0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : cte0_bRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>>();
                foreach (var bChunk in cte0_bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                int? key = b.Position;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                                    }
                                    else
                                    {
                                        matches.Add(b);
                                    }
                                }
                            }

                            continue;
                        }

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                int? key = b.Position;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                                    }
                                    else
                                    {
                                        matches.Add(b);
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        int? key = b.Position;
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                            }
                            else
                            {
                                matches.Add(b);
                            }
                        }
                    }
                }

                foreach (var aChunk in cte0_aRows)
                {
                    if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> aChunkView)
                    {
                        if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] aChunkViewArray)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                bool cte0BHashHasMatch = false;
                                int? key = (a.Position - 1);
                                if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                                {
                                    foreach (var b in cte0BHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        cte0BHashHasMatch = true;
                                        cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                                    }
                                }

                                if (!cte0BHashHasMatch)
                                {
                                    cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                                }
                            }

                            continue;
                        }

                        if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> aChunkViewList)
                        {
                            int aChunkViewOffset = aChunkView.Offset;
                            for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunkViewList[aChunkViewOffset + aIndex];
                                bool cte0BHashHasMatch = false;
                                int? key = (a.Position - 1);
                                if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                                {
                                    foreach (var b in cte0BHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        cte0BHashHasMatch = true;
                                        cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                                    }
                                }

                                if (!cte0BHashHasMatch)
                                {
                                    cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                                }
                            }

                            continue;
                        }
                    }

                    for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                    {
                        if ((aIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var a = aChunk[aIndex];
                        bool cte0BHashHasMatch = false;
                        int? key = (a.Position - 1);
                        if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                        {
                            foreach (var b in cte0BHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                cte0BHashHasMatch = true;
                                cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                            }
                        }

                        if (!cte0BHashHasMatch)
                        {
                            cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                        }
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
                try
                {
                    var __op33Handle = profileRecorder?.GetOperatorHandle("op33", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "CreateTable") ?? OperatorProfileHandle.None;
                    var __op36Handle = profileRecorder?.GetOperatorHandle("op36", "CreateHash") ?? OperatorProfileHandle.None;
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op38Handle = profileRecorder?.GetOperatorHandle("op38", "HashAdd") ?? OperatorProfileHandle.None;
                    var __op39Handle = profileRecorder?.GetOperatorHandle("op39", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op40Handle = profileRecorder?.GetOperatorHandle("op40", "HashProbe") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "ForEach") ?? OperatorProfileHandle.None;
                    var __op43Handle = profileRecorder?.GetOperatorHandle("op43", "AppendRow") ?? OperatorProfileHandle.None;
                    var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "AppendRow") ?? OperatorProfileHandle.None;
                    long __op38OutputRows = 0L;
                    long __op40InputRows = 0L;
                    long __op41InputRows = 0L;
                    long __op41OutputRows = 0L;
                    long __op43OutputRows = 0L;
                    long __op45OutputRows = 0L;
                    var __op33Scope = profileRecorder?.BeginOperatorValue(__op33Handle) ?? OperatorProfileValueScope.None;
                    var __cte0_aSchema = provider.GetSchema("#schema");
                    var cte0_aRowsProfile = profileRecorder?.CreateSourceRecorder("a");
                    var cte0_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__cte0_aSchema, "first", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress, cte0_aRowsProfile == null ? SourceDiagnostics.None : cte0_aRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0_aRows = cte0_aRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : cte0_aRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_aRowsSource.Chunks, __musoqProgressContext, "a:1") : cte0_aRowsSource.Chunks, cte0_aRowsProfile);
                    __op33Scope.Dispose();
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                    var __cte0_bSchema = provider.GetSchema("#schema");
                    var cte0_bRowsProfile = profileRecorder?.CreateSourceRecorder("b");
                    var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__cte0_bSchema, "first", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress, cte0_bRowsProfile == null ? SourceDiagnostics.None : cte0_bRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var cte0_bRows = cte0_bRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : cte0_bRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:1") : cte0_bRowsSource.Chunks, cte0_bRowsProfile);
                    __op34Scope.Dispose();
                    var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                    var cte0 = new List<Cte0Row0>();
                    __op35Scope.Dispose();
                    var __op36Scope = profileRecorder?.BeginOperatorValue(__op36Handle) ?? OperatorProfileValueScope.None;
                    var cte0BHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>>();
                    __op36Scope.Dispose();
                    long __op37InputRows = 0L;
                    long __op37OutputRows = 0L;
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var bChunk in cte0_bRows)
                        {
                            if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> bChunkView)
                            {
                                if (bChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] bChunkViewArray)
                                {
                                    int bChunkViewOffset = bChunkView.Offset;
                                    for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                    {
                                        if ((bIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                        __op37InputRows++;
                                        __op37OutputRows++;
                                        int? key = b.Position;
                                        if (key == null)
                                            continue;
                                        {
                                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                                            if (!matchesExists)
                                            {
                                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                                            }
                                            else
                                            {
                                                matches.Add(b);
                                            }
                                        }

                                        __op38OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (bChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> bChunkViewList)
                                {
                                    int bChunkViewOffset = bChunkView.Offset;
                                    for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                                    {
                                        if ((bIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var b = bChunkViewList[bChunkViewOffset + bIndex];
                                        __op37InputRows++;
                                        __op37OutputRows++;
                                        int? key = b.Position;
                                        if (key == null)
                                            continue;
                                        {
                                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                                            if (!matchesExists)
                                            {
                                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                                            }
                                            else
                                            {
                                                matches.Add(b);
                                            }
                                        }

                                        __op38OutputRows += 1;
                                    }

                                    continue;
                                }
                            }

                            for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunk[bIndex];
                                __op37InputRows++;
                                __op37OutputRows++;
                                int? key = b.Position;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0BHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(b);
                                    }
                                    else
                                    {
                                        matches.Add(b);
                                    }
                                }

                                __op38OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op37Scope.AddInputRows(__op37InputRows);
                        __op37Scope.AddOutputRows(__op37OutputRows);
                        __op37Scope.Dispose();
                    }

                    long __op39InputRows = 0L;
                    long __op39OutputRows = 0L;
                    var __op39Scope = profileRecorder?.BeginOperatorValue(__op39Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var aChunk in cte0_aRows)
                        {
                            if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> aChunkView)
                            {
                                if (aChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] aChunkViewArray)
                                {
                                    int aChunkViewOffset = aChunkView.Offset;
                                    for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                    {
                                        if ((aIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var a = aChunkViewArray[aChunkViewOffset + aIndex];
                                        __op39InputRows++;
                                        __op39OutputRows++;
                                        __op40InputRows += 1;
                                        bool cte0BHashHasMatch = false;
                                        int? key = (a.Position - 1);
                                        if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                                        {
                                            foreach (var b in cte0BHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                __op41InputRows++;
                                                __op41OutputRows++;
                                                cte0BHashHasMatch = true;
                                                cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                                                __op43OutputRows += 1;
                                            }
                                        }

                                        if (!cte0BHashHasMatch)
                                        {
                                            cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                                            __op45OutputRows += 1;
                                        }
                                    }

                                    continue;
                                }

                                if (aChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> aChunkViewList)
                                {
                                    int aChunkViewOffset = aChunkView.Offset;
                                    for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                                    {
                                        if ((aIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var a = aChunkViewList[aChunkViewOffset + aIndex];
                                        __op39InputRows++;
                                        __op39OutputRows++;
                                        __op40InputRows += 1;
                                        bool cte0BHashHasMatch = false;
                                        int? key = (a.Position - 1);
                                        if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                                        {
                                            foreach (var b in cte0BHashMatches)
                                            {
                                                token.ThrowIfCancellationRequested();
                                                __op41InputRows++;
                                                __op41OutputRows++;
                                                cte0BHashHasMatch = true;
                                                cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                                                __op43OutputRows += 1;
                                            }
                                        }

                                        if (!cte0BHashHasMatch)
                                        {
                                            cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                                            __op45OutputRows += 1;
                                        }
                                    }

                                    continue;
                                }
                            }

                            for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                            {
                                if ((aIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var a = aChunk[aIndex];
                                __op39InputRows++;
                                __op39OutputRows++;
                                __op40InputRows += 1;
                                bool cte0BHashHasMatch = false;
                                int? key = (a.Position - 1);
                                if (key != null && cte0BHash.TryGetValue(key, out var cte0BHashMatches))
                                {
                                    foreach (var b in cte0BHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __op41InputRows++;
                                        __op41OutputRows++;
                                        cte0BHashHasMatch = true;
                                        cte0.Add(new Cte0Row0(a.Position, a.Partition, (int?)(a.Value + b.Value)));
                                        __op43OutputRows += 1;
                                    }
                                }

                                if (!cte0BHashHasMatch)
                                {
                                    cte0.Add(new Cte0Row0(a.Position, a.Partition, null));
                                    __op45OutputRows += 1;
                                }
                            }
                        }
                    }
                    finally
                    {
                        __op39Scope.AddInputRows(__op39InputRows);
                        __op39Scope.AddOutputRows(__op39OutputRows);
                        __op39Scope.Dispose();
                    }

                    if (__op38OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op38Handle, __op38OutputRows);
                    if (__op40InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op40Handle, __op40InputRows);
                    if (__op41InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op41Handle, __op41InputRows);
                    if (__op41OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op41Handle, __op41OutputRows);
                    if (__op43OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op43Handle, __op43OutputRows);
                    if (__op45OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op45Handle, __op45OutputRows);
                    return cte0;
                }
                catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
                {
                    profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                    throw;
                }
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractResultRowNumbers0WindowKeys(IReadOnlyList<Cte0Row0> resultWindowRows, int? [] resultRowNumbers0PartitionKeys, WindowResultRowNumbers0OrderKeysKey[] resultRowNumbers0OrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 joined = resultWindowRows[windowIndex];
                resultRowNumbers0PartitionKeys[windowIndex] = (int?)joined.Partition;
                resultRowNumbers0OrderKeys[windowIndex] = new WindowResultRowNumbers0OrderKeysKey(joined.Position);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, int? __value1, int? __value2)
            {
                Position = __value0;
                Partition = __value1;
                Value = __value2;
            }

            public int? Partition { get; }
            public int Position { get; }
            public int? Value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, int? __value1, int? __value2, decimal? __value3, long __value4, int? __value5, long __value6)
            {
                Position = __value0;
                Partition = __value1;
                Total = __value2;
                Average = __value3;
                Present = __value4;
                Previous = __value5;
                Number = __value6;
            }

            public decimal? Average { get; private set; }
            public override int Count => 7;
            public long Number { get; private set; }
            public int? Partition { get; private set; }
            public int Position { get; private set; }
            public long Present { get; private set; }
            public int? Previous { get; private set; }
            public int? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Previous = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Number = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Partition),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                5 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Previous),
                6 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Number),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Partition" => true,
                "Total" => true,
                "Average" => true,
                "Present" => true,
                "Previous" => true,
                "Number" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Present, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(5, this.Previous, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(6, this.Number, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? Partition, int? Total, decimal? Average, long Present, int? Previous, long Number)
            {
                this.Position = Position;
                this.Partition = Partition;
                this.Total = Total;
                this.Average = Average;
                this.Present = Present;
                this.Previous = Previous;
                this.Number = Number;
            }

            public decimal? Average { get; }
            public long Number { get; }
            public int? Partition { get; }
            public int Position { get; }
            public long Present { get; }
            public int? Previous { get; }
            public int? Total { get; }
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
