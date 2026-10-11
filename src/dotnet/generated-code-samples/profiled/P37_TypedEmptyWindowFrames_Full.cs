// === Parsed Query ===
/*
select Position, Sum(Amount) over (order by Position rows between 1 following and 1 following) as Total,
    Count(*) over (order by Position rows between 1 following and 1 following) as Rows
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Rows]
      Window [Sum(idx:0; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 following and 1 following), Count(idx:1; order: ko3iko.Position; frame: rows between 1 following and 1 following)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Rows]
      PhysicalWindow [Sum(idx:0; order: ko3iko.Position; args: ko3iko.Amount; frame: rows between 1 following and 1 following), Count(idx:1; order: ko3iko.Position; frame: rows between 1 following and 1 following)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Amount: decimal? <- property Amount
    Generated [ResultRow0]
      Position: int <- field Position
      Total: decimal? <- field Total
      Rows: long <- field Rows

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value ko3iko.Amount order by ko3iko.Position ASC frame rows between 1 following and 1 following]
    ComputeCountWindowKernel[BoundedRows] [resultCounts1 <- resultWindowRows value 1 order by ko3iko.Position ASC frame rows between 1 following and 1 following]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultSums0[windowIndex], Rows: resultCounts1[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P37_TypedEmptyWindowFrames_Full
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
            new Column("Total", typeof(decimal?), 1),
            new Column("Rows", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Amount", typeof(decimal?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Rows);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Rows);
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
                var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ko3iko.Position);
                }

                var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                var resultSums0 = new decimal? [resultWindowRows.Count];
                var resultSums0BatchValues = new decimal? [resultWindowRows.Count];
                for (int resultSums0CaptureIndex = 0; resultSums0CaptureIndex < resultWindowRows.Count; ++resultSums0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums0CaptureIndex];
                    resultSums0BatchValues[resultSums0CaptureIndex] = (decimal?)ko3iko.Amount;
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
                            resultSums0BatchStarts[resultSums0BatchPosition] = Math.Min(resultSums0PartitionCount, resultSums0BatchPosition + 1);
                            resultSums0BatchEnds[resultSums0BatchPosition] = Math.Min(resultSums0PartitionCount - 1, resultSums0BatchPosition + 1);
                        }

                        var resultSums0BatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultSums0BatchValues, new System.ReadOnlySpan<int>(resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount), resultSums0BatchStarts, resultSums0BatchEnds, resultSums0, token);
                        if (!Musoq.Plugins.DecimalSumWindowBatch.TryEvaluate(in resultSums0BatchContext))
                        {
                            for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                            {
                                var resultSums0OutputIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                                var resultSums0FrameStart = Math.Min(resultSums0PartitionCount, resultSums0PartitionIndex + 1);
                                var resultSums0FrameEnd = Math.Min(resultSums0PartitionCount - 1, resultSums0PartitionIndex + 1);
                                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                                for (var resultSums0FrameIndex = Math.Max(0, resultSums0FrameStart); resultSums0FrameIndex <= resultSums0FrameEnd && resultSums0FrameIndex < resultSums0PartitionCount; resultSums0FrameIndex++)
                                {
                                    var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0FrameIndex];
                                    Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)resultSums0BatchValues[resultSums0CurrentIndex]);
                                }

                                resultSums0[resultSums0OutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                            }
                        }
                    }
                }

                var resultCounts1 = new long[resultWindowRows.Count];
                for (int resultCounts1CaptureIndex = 0; resultCounts1CaptureIndex < resultWindowRows.Count; ++resultCounts1CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCounts1CaptureIndex];
                }

                for (int resultCounts1PartitionSetIndex = 0; resultCounts1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultCounts1PartitionSetIndex)
                {
                    var resultCounts1PartitionStart = resultSums0Partitions.GetStart(resultCounts1PartitionSetIndex);
                    var resultCounts1PartitionCount = resultSums0Partitions.GetLength(resultCounts1PartitionSetIndex);
                    var resultCounts1PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.CountAllAggregateKernel.State resultCounts1State = default(Musoq.Plugins.CountAllAggregateKernel.State)!;
                    Musoq.Plugins.CountAllAggregateKernel.Reset(ref resultCounts1State);
                    for (int resultCounts1PartitionIndex = 0; resultCounts1PartitionIndex < resultCounts1PartitionCount; ++resultCounts1PartitionIndex)
                    {
                        var resultCounts1OutputIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1PartitionIndex];
                        var resultCounts1FrameStart = Math.Min(resultCounts1PartitionCount, resultCounts1PartitionIndex + 1);
                        var resultCounts1FrameEnd = Math.Min(resultCounts1PartitionCount - 1, resultCounts1PartitionIndex + 1);
                        Musoq.Plugins.CountAllAggregateKernel.Reset(ref resultCounts1State);
                        for (var resultCounts1FrameIndex = Math.Max(0, resultCounts1FrameStart); resultCounts1FrameIndex <= resultCounts1FrameEnd && resultCounts1FrameIndex < resultCounts1PartitionCount; resultCounts1FrameIndex++)
                        {
                            var resultCounts1CurrentIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1FrameIndex];
                            Musoq.Plugins.CountAllAggregateKernel.Set(ref resultCounts1State);
                        }

                        resultCounts1[resultCounts1OutputIndex] = Musoq.Plugins.CountAllAggregateKernel.Get(in resultCounts1State);
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
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultSums0[windowIndex], (long)resultCounts1[windowIndex]));
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
                    var __op11Handle = profileRecorder?.GetOperatorHandle("op11", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ComputeCountWindowKernel") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op19OutputRows = 0L;
                    var __op11Scope = profileRecorder?.BeginOperatorValue(__op11Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op11Scope.Dispose();
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op12Scope.Dispose();
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op14Scope.AddOutputRows(resultWindowRows.Count);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                        resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ko3iko.Position);
                    }

                    var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                    var resultSums0 = new decimal? [resultWindowRows.Count];
                    var resultSums0BatchValues = new decimal? [resultWindowRows.Count];
                    for (int resultSums0CaptureIndex = 0; resultSums0CaptureIndex < resultWindowRows.Count; ++resultSums0CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums0CaptureIndex];
                        resultSums0BatchValues[resultSums0CaptureIndex] = (decimal?)ko3iko.Amount;
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
                                resultSums0BatchStarts[resultSums0BatchPosition] = Math.Min(resultSums0PartitionCount, resultSums0BatchPosition + 1);
                                resultSums0BatchEnds[resultSums0BatchPosition] = Math.Min(resultSums0PartitionCount - 1, resultSums0BatchPosition + 1);
                            }

                            var resultSums0BatchContext = new Musoq.Plugins.WindowBatchContext<decimal?, decimal?>(resultSums0BatchValues, new System.ReadOnlySpan<int>(resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount), resultSums0BatchStarts, resultSums0BatchEnds, resultSums0, token);
                            if (!Musoq.Plugins.DecimalSumWindowBatch.TryEvaluate(in resultSums0BatchContext))
                            {
                                for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount; ++resultSums0PartitionIndex)
                                {
                                    var resultSums0OutputIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PartitionIndex];
                                    var resultSums0FrameStart = Math.Min(resultSums0PartitionCount, resultSums0PartitionIndex + 1);
                                    var resultSums0FrameEnd = Math.Min(resultSums0PartitionCount - 1, resultSums0PartitionIndex + 1);
                                    Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref resultSums0State);
                                    for (var resultSums0FrameIndex = Math.Max(0, resultSums0FrameStart); resultSums0FrameIndex <= resultSums0FrameEnd && resultSums0FrameIndex < resultSums0PartitionCount; resultSums0FrameIndex++)
                                    {
                                        var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0FrameIndex];
                                        Musoq.Plugins.SumAggregateKernel<decimal>.Set(ref resultSums0State, (decimal?)resultSums0BatchValues[resultSums0CurrentIndex]);
                                    }

                                    resultSums0[resultSums0OutputIndex] = Musoq.Plugins.SumAggregateKernel<decimal>.Get(in resultSums0State);
                                }
                            }
                        }
                    }

                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    var resultCounts1 = new long[resultWindowRows.Count];
                    for (int resultCounts1CaptureIndex = 0; resultCounts1CaptureIndex < resultWindowRows.Count; ++resultCounts1CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCounts1CaptureIndex];
                    }

                    for (int resultCounts1PartitionSetIndex = 0; resultCounts1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultCounts1PartitionSetIndex)
                    {
                        var resultCounts1PartitionStart = resultSums0Partitions.GetStart(resultCounts1PartitionSetIndex);
                        var resultCounts1PartitionCount = resultSums0Partitions.GetLength(resultCounts1PartitionSetIndex);
                        var resultCounts1PartitionIndices = resultSums0Partitions.Indices;
                        Musoq.Plugins.CountAllAggregateKernel.State resultCounts1State = default(Musoq.Plugins.CountAllAggregateKernel.State)!;
                        Musoq.Plugins.CountAllAggregateKernel.Reset(ref resultCounts1State);
                        for (int resultCounts1PartitionIndex = 0; resultCounts1PartitionIndex < resultCounts1PartitionCount; ++resultCounts1PartitionIndex)
                        {
                            var resultCounts1OutputIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1PartitionIndex];
                            var resultCounts1FrameStart = Math.Min(resultCounts1PartitionCount, resultCounts1PartitionIndex + 1);
                            var resultCounts1FrameEnd = Math.Min(resultCounts1PartitionCount - 1, resultCounts1PartitionIndex + 1);
                            Musoq.Plugins.CountAllAggregateKernel.Reset(ref resultCounts1State);
                            for (var resultCounts1FrameIndex = Math.Max(0, resultCounts1FrameStart); resultCounts1FrameIndex <= resultCounts1FrameEnd && resultCounts1FrameIndex < resultCounts1PartitionCount; resultCounts1FrameIndex++)
                            {
                                var resultCounts1CurrentIndex = resultCounts1PartitionIndices[resultCounts1PartitionStart + resultCounts1FrameIndex];
                                Musoq.Plugins.CountAllAggregateKernel.Set(ref resultCounts1State);
                            }

                            resultCounts1[resultCounts1OutputIndex] = Musoq.Plugins.CountAllAggregateKernel.Get(in resultCounts1State);
                        }
                    }

                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op17Scope.Dispose();
                    long __op18InputRows = 0L;
                    long __op18OutputRows = 0L;
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                            __op18InputRows++;
                            __op18OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultSums0[windowIndex], (long)resultCounts1[windowIndex]));
                            __op19OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op18Scope.AddInputRows(__op18InputRows);
                        __op18Scope.AddOutputRows(__op18OutputRows);
                        __op18Scope.Dispose();
                    }

                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    __op20Scope.AddInputRows(result.Count);
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

                    __op20Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op20Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op22Scope.Dispose();
                    if (__op19OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op19Handle, __op19OutputRows);
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
            public ResultRow0(int __value0, decimal? __value1, long __value2)
            {
                Position = __value0;
                Total = __value1;
                Rows = __value2;
            }

            public override int Count => 3;
            public int Position { get; private set; }
            public long Rows { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rows = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Rows),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                "Rows" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Rows, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? Total, long Rows)
            {
                this.Position = Position;
                this.Total = Total;
                this.Rows = Rows;
            }

            public int Position { get; }
            public long Rows { get; }
            public decimal? Total { get; }
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
