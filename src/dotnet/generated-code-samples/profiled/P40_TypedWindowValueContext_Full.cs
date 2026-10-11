// === Parsed Query ===
/*
select Position, FirstValue(Amount) over (partition by Partition order by Position
        rows between 1 preceding and current row) as First,
    NthValue(Amount, Position) over (partition by Partition order by Position) as Nth,
    Lag(Amount, 1, 0) over (partition by Partition order by Position) as Previous,
    Ntile(2) over (partition by Partition order by Position) as Tile
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as First, WindowRef(1) as Nth, WindowRef(2) as Previous, WindowRef(3) as Tile]
      Window [FirstValue(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 preceding and current row), NthValue(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, ko3iko.Position), Lag(idx:2; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, 1, 0), Ntile(idx:3; partition: ko3iko.Partition; order: ko3iko.Position; args: 2)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as First, WindowRef(1) as Nth, WindowRef(2) as Previous, WindowRef(3) as Tile]
      PhysicalWindow [FirstValue(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 preceding and current row), NthValue(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, ko3iko.Position), Lag(idx:2; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, 1, 0), Ntile(idx:3; partition: ko3iko.Partition; order: ko3iko.Position; args: 2)]
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
      First: decimal? <- field First
      Nth: decimal? <- field Nth
      Previous: decimal? <- field Previous
      Tile: long <- field Tile

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeFirstValueWindow [resultFirstValues0 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame rows between 1 preceding and current row]
    ComputeNthValueWindow [resultNthValues1 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame range between unbounded preceding and current row args ko3iko.Position]
    ComputeLagWindow [resultLags2 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame range between unbounded preceding and current row args 1, 0]
    ComputeNtileWindow [resultNtiles3 <- resultWindowRows value 2 partition by ko3iko.Partition order by ko3iko.Position ASC]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, First: resultFirstValues0[windowIndex], Nth: resultNthValues1[windowIndex], Previous: resultLags2[windowIndex], Tile: resultNtiles3[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P40_TypedWindowValueContext_Full
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Position", typeof(int), 0),
            new Column("First", typeof(decimal?), 1),
            new Column("Nth", typeof(decimal?), 2),
            new Column("Previous", typeof(decimal?), 3),
            new Column("Tile", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(string), 1), new Column("Amount", typeof(decimal?), 2) });
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.First, __musoqShapeRow.Nth, __musoqShapeRow.Previous, __musoqShapeRow.Tile);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.First, __musoqShapeRow.Nth, __musoqShapeRow.Previous, __musoqShapeRow.Tile);
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
                var resultFirstValues0PartitionKeys = new string[resultWindowRows.Count];
                var resultFirstValues0OrderKeys = new WindowResultFirstValues0OrderKeysKey[resultWindowRows.Count];
                var resultFirstValues0Values = new decimal? [resultWindowRows.Count];
                for (int resultFirstValues0ExtractionIndex = 0; resultFirstValues0ExtractionIndex < resultWindowRows.Count; ++resultFirstValues0ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultFirstValues0ExtractionIndex];
                    resultFirstValues0PartitionKeys[resultFirstValues0ExtractionIndex] = (string)ko3iko.Partition;
                    resultFirstValues0OrderKeys[resultFirstValues0ExtractionIndex] = new WindowResultFirstValues0OrderKeysKey(ko3iko.Position);
                    resultFirstValues0Values[resultFirstValues0ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                }

                var resultFirstValues0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultFirstValues0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultFirstValues0Partitions, resultFirstValues0OrderKeys, false);
                var resultFirstValues0 = new decimal? [resultWindowRows.Count];
                for (int resultFirstValues0Partition = 0; resultFirstValues0Partition < resultFirstValues0Partitions.PartitionCount; ++resultFirstValues0Partition)
                {
                    var resultFirstValues0Start = resultFirstValues0Partitions.GetStart(resultFirstValues0Partition);
                    var resultFirstValues0Count = resultFirstValues0Partitions.GetLength(resultFirstValues0Partition);
                    var resultFirstValues0Indices = resultFirstValues0Partitions.Indices;
                    for (int resultFirstValues0Position = 0; resultFirstValues0Position < resultFirstValues0Count; ++resultFirstValues0Position)
                    {
                        if ((resultFirstValues0Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultFirstValues0Current = resultFirstValues0Indices[resultFirstValues0Start + resultFirstValues0Position];
                        var resultFirstValues0FrameStart = Math.Max(0, resultFirstValues0Position - 1);
                        var resultFirstValues0FrameEnd = resultFirstValues0Position;
                        var resultFirstValues0Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultFirstValues0Values, new System.ReadOnlySpan<int>(resultFirstValues0Indices, resultFirstValues0Start, resultFirstValues0Count), resultFirstValues0Position, resultFirstValues0FrameStart, resultFirstValues0FrameEnd, token);
                        resultFirstValues0[resultFirstValues0Current] = Musoq.Plugins.FirstValueKernel<decimal>.Get(in resultFirstValues0Context);
                    }
                }

                var resultNthValues1Values = new decimal? [resultWindowRows.Count];
                var resultNthValues1Argument0 = new int[resultWindowRows.Count];
                for (int resultNthValues1ExtractionIndex = 0; resultNthValues1ExtractionIndex < resultWindowRows.Count; ++resultNthValues1ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultNthValues1ExtractionIndex];
                    resultNthValues1Values[resultNthValues1ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                    resultNthValues1Argument0[resultNthValues1ExtractionIndex] = (int)(int)ko3iko.Position;
                }

                var resultNthValues1 = new decimal? [resultWindowRows.Count];
                for (int resultNthValues1Partition = 0; resultNthValues1Partition < resultFirstValues0Partitions.PartitionCount; ++resultNthValues1Partition)
                {
                    var resultNthValues1Start = resultFirstValues0Partitions.GetStart(resultNthValues1Partition);
                    var resultNthValues1Count = resultFirstValues0Partitions.GetLength(resultNthValues1Partition);
                    var resultNthValues1Indices = resultFirstValues0Partitions.Indices;
                    for (int resultNthValues1Position = 0; resultNthValues1Position < resultNthValues1Count; ++resultNthValues1Position)
                    {
                        if ((resultNthValues1Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultNthValues1Current = resultNthValues1Indices[resultNthValues1Start + resultNthValues1Position];
                        var resultNthValues1FrameStart = 0;
                        var resultNthValues1FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultFirstValues0OrderKeys, resultNthValues1Indices, resultNthValues1Start, resultNthValues1Count, resultNthValues1Position);
                        var resultNthValues1Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultNthValues1Values, new System.ReadOnlySpan<int>(resultNthValues1Indices, resultNthValues1Start, resultNthValues1Count), resultNthValues1Position, resultNthValues1FrameStart, resultNthValues1FrameEnd, token);
                        resultNthValues1[resultNthValues1Current] = Musoq.Plugins.NthValueKernel<decimal>.Get(in resultNthValues1Context, resultNthValues1Argument0[resultNthValues1Current]);
                    }
                }

                var resultLags2Values = new decimal? [resultWindowRows.Count];
                var resultLags2Argument0 = new int[resultWindowRows.Count];
                var resultLags2Argument1 = new decimal? [resultWindowRows.Count];
                for (int resultLags2ExtractionIndex = 0; resultLags2ExtractionIndex < resultWindowRows.Count; ++resultLags2ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultLags2ExtractionIndex];
                    resultLags2Values[resultLags2ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                    resultLags2Argument0[resultLags2ExtractionIndex] = (int)(int)1;
                    resultLags2Argument1[resultLags2ExtractionIndex] = (decimal?)(decimal?)0;
                }

                var resultLags2 = new decimal? [resultWindowRows.Count];
                for (int resultLags2Partition = 0; resultLags2Partition < resultFirstValues0Partitions.PartitionCount; ++resultLags2Partition)
                {
                    var resultLags2Start = resultFirstValues0Partitions.GetStart(resultLags2Partition);
                    var resultLags2Count = resultFirstValues0Partitions.GetLength(resultLags2Partition);
                    var resultLags2Indices = resultFirstValues0Partitions.Indices;
                    for (int resultLags2Position = 0; resultLags2Position < resultLags2Count; ++resultLags2Position)
                    {
                        if ((resultLags2Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultLags2Current = resultLags2Indices[resultLags2Start + resultLags2Position];
                        var resultLags2FrameStart = 0;
                        var resultLags2FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultFirstValues0OrderKeys, resultLags2Indices, resultLags2Start, resultLags2Count, resultLags2Position);
                        var resultLags2Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultLags2Values, new System.ReadOnlySpan<int>(resultLags2Indices, resultLags2Start, resultLags2Count), resultLags2Position, resultLags2FrameStart, resultLags2FrameEnd, token);
                        resultLags2[resultLags2Current] = Musoq.Plugins.LagKernel<decimal>.Get(in resultLags2Context, resultLags2Argument0[resultLags2Current], resultLags2Argument1[resultLags2Current]);
                    }
                }

                var resultNtiles3Values = new int[resultWindowRows.Count];
                for (int resultNtiles3ExtractionIndex = 0; resultNtiles3ExtractionIndex < resultWindowRows.Count; ++resultNtiles3ExtractionIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultNtiles3ExtractionIndex];
                    resultNtiles3Values[resultNtiles3ExtractionIndex] = (int)(int)2;
                }

                var resultNtiles3 = new long[resultWindowRows.Count];
                for (int resultNtiles3Partition = 0; resultNtiles3Partition < resultFirstValues0Partitions.PartitionCount; ++resultNtiles3Partition)
                {
                    var resultNtiles3Start = resultFirstValues0Partitions.GetStart(resultNtiles3Partition);
                    var resultNtiles3Count = resultFirstValues0Partitions.GetLength(resultNtiles3Partition);
                    var resultNtiles3Indices = resultFirstValues0Partitions.Indices;
                    for (int resultNtiles3Position = 0; resultNtiles3Position < resultNtiles3Count; ++resultNtiles3Position)
                    {
                        if ((resultNtiles3Position & 1023) == 0)
                            token.ThrowIfCancellationRequested();
                        var resultNtiles3Current = resultNtiles3Indices[resultNtiles3Start + resultNtiles3Position];
                        var resultNtiles3FrameStart = 0;
                        var resultNtiles3FrameEnd = resultNtiles3Position;
                        var resultNtiles3Context = new Musoq.Plugins.WindowFrameContext<int>(resultNtiles3Values, new System.ReadOnlySpan<int>(resultNtiles3Indices, resultNtiles3Start, resultNtiles3Count), resultNtiles3Position, resultNtiles3FrameStart, resultNtiles3FrameEnd, token);
                        resultNtiles3[resultNtiles3Current] = Musoq.Plugins.NtileKernel.Get(in resultNtiles3Context);
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
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultFirstValues0[windowIndex], (decimal?)resultNthValues1[windowIndex], (decimal?)resultLags2[windowIndex], (long)resultNtiles3[windowIndex]));
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
                    var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                    ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                    var __musoqFinalShapeRows = new List<ResultShape0>();
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "ComputeFirstValueWindow") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "ComputeNthValueWindow") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "ComputeLagWindow") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "ComputeNtileWindow") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op24OutputRows = 0L;
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op17Scope.AddOutputRows(resultWindowRows.Count);
                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var resultFirstValues0PartitionKeys = new string[resultWindowRows.Count];
                    var resultFirstValues0OrderKeys = new WindowResultFirstValues0OrderKeysKey[resultWindowRows.Count];
                    var resultFirstValues0Values = new decimal? [resultWindowRows.Count];
                    for (int resultFirstValues0ExtractionIndex = 0; resultFirstValues0ExtractionIndex < resultWindowRows.Count; ++resultFirstValues0ExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultFirstValues0ExtractionIndex];
                        resultFirstValues0PartitionKeys[resultFirstValues0ExtractionIndex] = (string)ko3iko.Partition;
                        resultFirstValues0OrderKeys[resultFirstValues0ExtractionIndex] = new WindowResultFirstValues0OrderKeysKey(ko3iko.Position);
                        resultFirstValues0Values[resultFirstValues0ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                    }

                    var resultFirstValues0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultFirstValues0PartitionKeys);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultFirstValues0Partitions, resultFirstValues0OrderKeys, false);
                    var resultFirstValues0 = new decimal? [resultWindowRows.Count];
                    for (int resultFirstValues0Partition = 0; resultFirstValues0Partition < resultFirstValues0Partitions.PartitionCount; ++resultFirstValues0Partition)
                    {
                        var resultFirstValues0Start = resultFirstValues0Partitions.GetStart(resultFirstValues0Partition);
                        var resultFirstValues0Count = resultFirstValues0Partitions.GetLength(resultFirstValues0Partition);
                        var resultFirstValues0Indices = resultFirstValues0Partitions.Indices;
                        for (int resultFirstValues0Position = 0; resultFirstValues0Position < resultFirstValues0Count; ++resultFirstValues0Position)
                        {
                            if ((resultFirstValues0Position & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultFirstValues0Current = resultFirstValues0Indices[resultFirstValues0Start + resultFirstValues0Position];
                            var resultFirstValues0FrameStart = Math.Max(0, resultFirstValues0Position - 1);
                            var resultFirstValues0FrameEnd = resultFirstValues0Position;
                            var resultFirstValues0Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultFirstValues0Values, new System.ReadOnlySpan<int>(resultFirstValues0Indices, resultFirstValues0Start, resultFirstValues0Count), resultFirstValues0Position, resultFirstValues0FrameStart, resultFirstValues0FrameEnd, token);
                            resultFirstValues0[resultFirstValues0Current] = Musoq.Plugins.FirstValueKernel<decimal>.Get(in resultFirstValues0Context);
                        }
                    }

                    __op18Scope.Dispose();
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    var resultNthValues1Values = new decimal? [resultWindowRows.Count];
                    var resultNthValues1Argument0 = new int[resultWindowRows.Count];
                    for (int resultNthValues1ExtractionIndex = 0; resultNthValues1ExtractionIndex < resultWindowRows.Count; ++resultNthValues1ExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultNthValues1ExtractionIndex];
                        resultNthValues1Values[resultNthValues1ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                        resultNthValues1Argument0[resultNthValues1ExtractionIndex] = (int)(int)ko3iko.Position;
                    }

                    var resultNthValues1 = new decimal? [resultWindowRows.Count];
                    for (int resultNthValues1Partition = 0; resultNthValues1Partition < resultFirstValues0Partitions.PartitionCount; ++resultNthValues1Partition)
                    {
                        var resultNthValues1Start = resultFirstValues0Partitions.GetStart(resultNthValues1Partition);
                        var resultNthValues1Count = resultFirstValues0Partitions.GetLength(resultNthValues1Partition);
                        var resultNthValues1Indices = resultFirstValues0Partitions.Indices;
                        for (int resultNthValues1Position = 0; resultNthValues1Position < resultNthValues1Count; ++resultNthValues1Position)
                        {
                            if ((resultNthValues1Position & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultNthValues1Current = resultNthValues1Indices[resultNthValues1Start + resultNthValues1Position];
                            var resultNthValues1FrameStart = 0;
                            var resultNthValues1FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultFirstValues0OrderKeys, resultNthValues1Indices, resultNthValues1Start, resultNthValues1Count, resultNthValues1Position);
                            var resultNthValues1Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultNthValues1Values, new System.ReadOnlySpan<int>(resultNthValues1Indices, resultNthValues1Start, resultNthValues1Count), resultNthValues1Position, resultNthValues1FrameStart, resultNthValues1FrameEnd, token);
                            resultNthValues1[resultNthValues1Current] = Musoq.Plugins.NthValueKernel<decimal>.Get(in resultNthValues1Context, resultNthValues1Argument0[resultNthValues1Current]);
                        }
                    }

                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    var resultLags2Values = new decimal? [resultWindowRows.Count];
                    var resultLags2Argument0 = new int[resultWindowRows.Count];
                    var resultLags2Argument1 = new decimal? [resultWindowRows.Count];
                    for (int resultLags2ExtractionIndex = 0; resultLags2ExtractionIndex < resultWindowRows.Count; ++resultLags2ExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultLags2ExtractionIndex];
                        resultLags2Values[resultLags2ExtractionIndex] = (decimal?)(decimal?)ko3iko.Amount;
                        resultLags2Argument0[resultLags2ExtractionIndex] = (int)(int)1;
                        resultLags2Argument1[resultLags2ExtractionIndex] = (decimal?)(decimal?)0;
                    }

                    var resultLags2 = new decimal? [resultWindowRows.Count];
                    for (int resultLags2Partition = 0; resultLags2Partition < resultFirstValues0Partitions.PartitionCount; ++resultLags2Partition)
                    {
                        var resultLags2Start = resultFirstValues0Partitions.GetStart(resultLags2Partition);
                        var resultLags2Count = resultFirstValues0Partitions.GetLength(resultLags2Partition);
                        var resultLags2Indices = resultFirstValues0Partitions.Indices;
                        for (int resultLags2Position = 0; resultLags2Position < resultLags2Count; ++resultLags2Position)
                        {
                            if ((resultLags2Position & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultLags2Current = resultLags2Indices[resultLags2Start + resultLags2Position];
                            var resultLags2FrameStart = 0;
                            var resultLags2FrameEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultFirstValues0OrderKeys, resultLags2Indices, resultLags2Start, resultLags2Count, resultLags2Position);
                            var resultLags2Context = new Musoq.Plugins.WindowFrameContext<decimal?>(resultLags2Values, new System.ReadOnlySpan<int>(resultLags2Indices, resultLags2Start, resultLags2Count), resultLags2Position, resultLags2FrameStart, resultLags2FrameEnd, token);
                            resultLags2[resultLags2Current] = Musoq.Plugins.LagKernel<decimal>.Get(in resultLags2Context, resultLags2Argument0[resultLags2Current], resultLags2Argument1[resultLags2Current]);
                        }
                    }

                    __op20Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    var resultNtiles3Values = new int[resultWindowRows.Count];
                    for (int resultNtiles3ExtractionIndex = 0; resultNtiles3ExtractionIndex < resultWindowRows.Count; ++resultNtiles3ExtractionIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultNtiles3ExtractionIndex];
                        resultNtiles3Values[resultNtiles3ExtractionIndex] = (int)(int)2;
                    }

                    var resultNtiles3 = new long[resultWindowRows.Count];
                    for (int resultNtiles3Partition = 0; resultNtiles3Partition < resultFirstValues0Partitions.PartitionCount; ++resultNtiles3Partition)
                    {
                        var resultNtiles3Start = resultFirstValues0Partitions.GetStart(resultNtiles3Partition);
                        var resultNtiles3Count = resultFirstValues0Partitions.GetLength(resultNtiles3Partition);
                        var resultNtiles3Indices = resultFirstValues0Partitions.Indices;
                        for (int resultNtiles3Position = 0; resultNtiles3Position < resultNtiles3Count; ++resultNtiles3Position)
                        {
                            if ((resultNtiles3Position & 1023) == 0)
                                token.ThrowIfCancellationRequested();
                            var resultNtiles3Current = resultNtiles3Indices[resultNtiles3Start + resultNtiles3Position];
                            var resultNtiles3FrameStart = 0;
                            var resultNtiles3FrameEnd = resultNtiles3Position;
                            var resultNtiles3Context = new Musoq.Plugins.WindowFrameContext<int>(resultNtiles3Values, new System.ReadOnlySpan<int>(resultNtiles3Indices, resultNtiles3Start, resultNtiles3Count), resultNtiles3Position, resultNtiles3FrameStart, resultNtiles3FrameEnd, token);
                            resultNtiles3[resultNtiles3Current] = Musoq.Plugins.NtileKernel.Get(in resultNtiles3Context);
                        }
                    }

                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op22Scope.Dispose();
                    long __op23InputRows = 0L;
                    long __op23OutputRows = 0L;
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                            __op23InputRows++;
                            __op23OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultFirstValues0[windowIndex], (decimal?)resultNthValues1[windowIndex], (decimal?)resultLags2[windowIndex], (long)resultNtiles3[windowIndex]));
                            __op24OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op23Scope.AddInputRows(__op23InputRows);
                        __op23Scope.AddOutputRows(__op23OutputRows);
                        __op23Scope.Dispose();
                    }

                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    __op25Scope.AddInputRows(result.Count);
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

                    __op25Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op25Scope.Dispose();
                    var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op27Scope.Dispose();
                    if (__op24OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op24Handle, __op24OutputRows);
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1, decimal? __value2, decimal? __value3, long __value4)
            {
                Position = __value0;
                First = __value1;
                Nth = __value2;
                Previous = __value3;
                Tile = __value4;
            }

            public override int Count => 5;
            public decimal? First { get; private set; }
            public decimal? Nth { get; private set; }
            public int Position { get; private set; }
            public decimal? Previous { get; private set; }
            public long Tile { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.First = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Nth = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Previous = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Tile = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.First),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Nth),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Previous),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Tile),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "First" => true,
                "Nth" => true,
                "Previous" => true,
                "Tile" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.First, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Nth, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Previous, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Tile, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? First, decimal? Nth, decimal? Previous, long Tile)
            {
                this.Position = Position;
                this.First = First;
                this.Nth = Nth;
                this.Previous = Previous;
                this.Tile = Tile;
            }

            public decimal? First { get; }
            public decimal? Nth { get; }
            public int Position { get; }
            public decimal? Previous { get; }
            public long Tile { get; }
        }

        private readonly struct WindowResultFirstValues0OrderKeysKey : System.IEquatable<WindowResultFirstValues0OrderKeysKey>, System.IComparable<WindowResultFirstValues0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultFirstValues0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultFirstValues0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultFirstValues0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultFirstValues0OrderKeysKey other && Equals(other);
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
