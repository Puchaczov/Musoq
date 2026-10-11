// === Parsed Query ===
/*
select Position, SumDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Total,
    AvgDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Average,
    CountDistinct(Integral) over (order by Position rows between 2 preceding and current row) as Values
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Average, WindowRef(2) as Values]
      Window [SumDistinct(idx:0; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row), AvgDistinct(idx:1; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row), CountDistinct(idx:2; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Average, WindowRef(2) as Values]
      PhysicalWindow [SumDistinct(idx:0; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row), AvgDistinct(idx:1; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row), CountDistinct(idx:2; order: ko3iko.Position; args: ko3iko.Integral; frame: rows between 2 preceding and current row)]
        PhysicalMaterialize
          PhysicalSchemaScan [#schema.first() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: LibraryExecutionEntity]
      Position: int <- property Position
      Integral: int? <- property Integral
    Generated [ResultRow0]
      Position: int <- field Position
      Total: int? <- field Total
      Average: decimal? <- field Average
      Values: long <- field Values

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumDistinctWindowKernel[BoundedRows] [resultSumDistincts0 <- resultWindowRows value ko3iko.Integral order by ko3iko.Position ASC frame rows between 2 preceding and current row]
    ComputeAvgDistinctWindowKernel[BoundedRows] [resultAvgDistincts1 <- resultWindowRows value ko3iko.Integral order by ko3iko.Position ASC frame rows between 2 preceding and current row]
    ComputeCountDistinctWindowKernel[BoundedRows] [resultCountDistincts2 <- resultWindowRows value ko3iko.Integral order by ko3iko.Position ASC frame rows between 2 preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultSumDistincts0[windowIndex], Average: resultAvgDistincts1[windowIndex], Values: resultCountDistincts2[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P38_TypedDistinctWindowFrames_Full
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
            new Column("Total", typeof(int?), 1),
            new Column("Average", typeof(decimal?), 2),
            new Column("Values", typeof(long), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Integral", typeof(int?), 1) });
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.Values);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Average, __musoqShapeRow.Values);
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
                var resultSumDistincts0OrderKeys = new WindowResultSumDistincts0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultSumDistincts0OrderKeys[windowIndex] = new WindowResultSumDistincts0OrderKeysKey(ko3iko.Position);
                }

                var resultSumDistincts0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumDistincts0Partitions, resultSumDistincts0OrderKeys, false);
                var resultSumDistincts0 = new int? [resultWindowRows.Count];
                var resultSumDistincts0BatchValues = new int? [resultWindowRows.Count];
                for (int resultSumDistincts0CaptureIndex = 0; resultSumDistincts0CaptureIndex < resultWindowRows.Count; ++resultSumDistincts0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSumDistincts0CaptureIndex];
                    resultSumDistincts0BatchValues[resultSumDistincts0CaptureIndex] = (int?)ko3iko.Integral;
                }

                for (int resultSumDistincts0PartitionSetIndex = 0; resultSumDistincts0PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultSumDistincts0PartitionSetIndex)
                {
                    var resultSumDistincts0PartitionStart = resultSumDistincts0Partitions.GetStart(resultSumDistincts0PartitionSetIndex);
                    var resultSumDistincts0PartitionCount = resultSumDistincts0Partitions.GetLength(resultSumDistincts0PartitionSetIndex);
                    var resultSumDistincts0PartitionIndices = resultSumDistincts0Partitions.Indices;
                    Musoq.Plugins.SumDistinctAggregateKernel<int>.State resultSumDistincts0State = default(Musoq.Plugins.SumDistinctAggregateKernel<int>.State)!;
                    Musoq.Plugins.SumDistinctAggregateKernel<int>.Reset(ref resultSumDistincts0State);
                    for (int resultSumDistincts0PartitionIndex = 0; resultSumDistincts0PartitionIndex < resultSumDistincts0PartitionCount; ++resultSumDistincts0PartitionIndex)
                    {
                        var resultSumDistincts0OutputIndex = resultSumDistincts0PartitionIndices[resultSumDistincts0PartitionStart + resultSumDistincts0PartitionIndex];
                        var resultSumDistincts0FrameStart = Math.Max(0, resultSumDistincts0PartitionIndex - 2);
                        var resultSumDistincts0FrameEnd = resultSumDistincts0PartitionIndex;
                        Musoq.Plugins.SumDistinctAggregateKernel<int>.Reset(ref resultSumDistincts0State);
                        for (var resultSumDistincts0FrameIndex = Math.Max(0, resultSumDistincts0FrameStart); resultSumDistincts0FrameIndex <= resultSumDistincts0FrameEnd && resultSumDistincts0FrameIndex < resultSumDistincts0PartitionCount; resultSumDistincts0FrameIndex++)
                        {
                            var resultSumDistincts0CurrentIndex = resultSumDistincts0PartitionIndices[resultSumDistincts0PartitionStart + resultSumDistincts0FrameIndex];
                            Musoq.Plugins.SumDistinctAggregateKernel<int>.Set(ref resultSumDistincts0State, (int?)resultSumDistincts0BatchValues[resultSumDistincts0CurrentIndex]);
                        }

                        resultSumDistincts0[resultSumDistincts0OutputIndex] = Musoq.Plugins.SumDistinctAggregateKernel<int>.Get(in resultSumDistincts0State);
                    }
                }

                var resultAvgDistincts1 = new decimal? [resultWindowRows.Count];
                var resultAvgDistincts1BatchValues = new int? [resultWindowRows.Count];
                for (int resultAvgDistincts1CaptureIndex = 0; resultAvgDistincts1CaptureIndex < resultWindowRows.Count; ++resultAvgDistincts1CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultAvgDistincts1CaptureIndex];
                    resultAvgDistincts1BatchValues[resultAvgDistincts1CaptureIndex] = (int?)ko3iko.Integral;
                }

                for (int resultAvgDistincts1PartitionSetIndex = 0; resultAvgDistincts1PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultAvgDistincts1PartitionSetIndex)
                {
                    var resultAvgDistincts1PartitionStart = resultSumDistincts0Partitions.GetStart(resultAvgDistincts1PartitionSetIndex);
                    var resultAvgDistincts1PartitionCount = resultSumDistincts0Partitions.GetLength(resultAvgDistincts1PartitionSetIndex);
                    var resultAvgDistincts1PartitionIndices = resultSumDistincts0Partitions.Indices;
                    Musoq.Plugins.AverageDistinctKernel<int, decimal>.State resultAvgDistincts1State = default(Musoq.Plugins.AverageDistinctKernel<int, decimal>.State)!;
                    Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref resultAvgDistincts1State);
                    for (int resultAvgDistincts1PartitionIndex = 0; resultAvgDistincts1PartitionIndex < resultAvgDistincts1PartitionCount; ++resultAvgDistincts1PartitionIndex)
                    {
                        var resultAvgDistincts1OutputIndex = resultAvgDistincts1PartitionIndices[resultAvgDistincts1PartitionStart + resultAvgDistincts1PartitionIndex];
                        var resultAvgDistincts1FrameStart = Math.Max(0, resultAvgDistincts1PartitionIndex - 2);
                        var resultAvgDistincts1FrameEnd = resultAvgDistincts1PartitionIndex;
                        Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref resultAvgDistincts1State);
                        for (var resultAvgDistincts1FrameIndex = Math.Max(0, resultAvgDistincts1FrameStart); resultAvgDistincts1FrameIndex <= resultAvgDistincts1FrameEnd && resultAvgDistincts1FrameIndex < resultAvgDistincts1PartitionCount; resultAvgDistincts1FrameIndex++)
                        {
                            var resultAvgDistincts1CurrentIndex = resultAvgDistincts1PartitionIndices[resultAvgDistincts1PartitionStart + resultAvgDistincts1FrameIndex];
                            Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref resultAvgDistincts1State, (int?)resultAvgDistincts1BatchValues[resultAvgDistincts1CurrentIndex]);
                        }

                        resultAvgDistincts1[resultAvgDistincts1OutputIndex] = Musoq.Plugins.AverageDistinctKernel<int, decimal>.Get(in resultAvgDistincts1State);
                    }
                }

                var resultCountDistincts2 = new long[resultWindowRows.Count];
                var resultCountDistincts2BatchValues = new int? [resultWindowRows.Count];
                for (int resultCountDistincts2CaptureIndex = 0; resultCountDistincts2CaptureIndex < resultWindowRows.Count; ++resultCountDistincts2CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCountDistincts2CaptureIndex];
                    resultCountDistincts2BatchValues[resultCountDistincts2CaptureIndex] = (int?)ko3iko.Integral;
                }

                for (int resultCountDistincts2PartitionSetIndex = 0; resultCountDistincts2PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultCountDistincts2PartitionSetIndex)
                {
                    var resultCountDistincts2PartitionStart = resultSumDistincts0Partitions.GetStart(resultCountDistincts2PartitionSetIndex);
                    var resultCountDistincts2PartitionCount = resultSumDistincts0Partitions.GetLength(resultCountDistincts2PartitionSetIndex);
                    var resultCountDistincts2PartitionIndices = resultSumDistincts0Partitions.Indices;
                    Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.State resultCountDistincts2State = default(Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.State)!;
                    Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Reset(ref resultCountDistincts2State);
                    for (int resultCountDistincts2PartitionIndex = 0; resultCountDistincts2PartitionIndex < resultCountDistincts2PartitionCount; ++resultCountDistincts2PartitionIndex)
                    {
                        var resultCountDistincts2OutputIndex = resultCountDistincts2PartitionIndices[resultCountDistincts2PartitionStart + resultCountDistincts2PartitionIndex];
                        var resultCountDistincts2FrameStart = Math.Max(0, resultCountDistincts2PartitionIndex - 2);
                        var resultCountDistincts2FrameEnd = resultCountDistincts2PartitionIndex;
                        Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Reset(ref resultCountDistincts2State);
                        for (var resultCountDistincts2FrameIndex = Math.Max(0, resultCountDistincts2FrameStart); resultCountDistincts2FrameIndex <= resultCountDistincts2FrameEnd && resultCountDistincts2FrameIndex < resultCountDistincts2PartitionCount; resultCountDistincts2FrameIndex++)
                        {
                            var resultCountDistincts2CurrentIndex = resultCountDistincts2PartitionIndices[resultCountDistincts2PartitionStart + resultCountDistincts2FrameIndex];
                            Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Set(ref resultCountDistincts2State, (int?)resultCountDistincts2BatchValues[resultCountDistincts2CurrentIndex]);
                        }

                        resultCountDistincts2[resultCountDistincts2OutputIndex] = Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Get(in resultCountDistincts2State);
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
                    result.Add(new ResultShape0(ko3iko.Position, (int?)resultSumDistincts0[windowIndex], (decimal?)resultAvgDistincts1[windowIndex], (long)resultCountDistincts2[windowIndex]));
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
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ComputeSumDistinctWindowKernel") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "ComputeAvgDistinctWindowKernel") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "ComputeCountDistinctWindowKernel") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op21OutputRows = 0L;
                    var __op12Scope = profileRecorder?.BeginOperatorValue(__op12Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op12Scope.Dispose();
                    var __op13Scope = profileRecorder?.BeginOperatorValue(__op13Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op13Scope.Dispose();
                    var __op14Scope = profileRecorder?.BeginOperatorValue(__op14Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op14Scope.Dispose();
                    var __op15Scope = profileRecorder?.BeginOperatorValue(__op15Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op15Scope.AddOutputRows(resultWindowRows.Count);
                    __op15Scope.Dispose();
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    var resultSumDistincts0OrderKeys = new WindowResultSumDistincts0OrderKeysKey[resultWindowRows.Count];
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                        resultSumDistincts0OrderKeys[windowIndex] = new WindowResultSumDistincts0OrderKeysKey(ko3iko.Position);
                    }

                    var resultSumDistincts0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSumDistincts0Partitions, resultSumDistincts0OrderKeys, false);
                    var resultSumDistincts0 = new int? [resultWindowRows.Count];
                    var resultSumDistincts0BatchValues = new int? [resultWindowRows.Count];
                    for (int resultSumDistincts0CaptureIndex = 0; resultSumDistincts0CaptureIndex < resultWindowRows.Count; ++resultSumDistincts0CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSumDistincts0CaptureIndex];
                        resultSumDistincts0BatchValues[resultSumDistincts0CaptureIndex] = (int?)ko3iko.Integral;
                    }

                    for (int resultSumDistincts0PartitionSetIndex = 0; resultSumDistincts0PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultSumDistincts0PartitionSetIndex)
                    {
                        var resultSumDistincts0PartitionStart = resultSumDistincts0Partitions.GetStart(resultSumDistincts0PartitionSetIndex);
                        var resultSumDistincts0PartitionCount = resultSumDistincts0Partitions.GetLength(resultSumDistincts0PartitionSetIndex);
                        var resultSumDistincts0PartitionIndices = resultSumDistincts0Partitions.Indices;
                        Musoq.Plugins.SumDistinctAggregateKernel<int>.State resultSumDistincts0State = default(Musoq.Plugins.SumDistinctAggregateKernel<int>.State)!;
                        Musoq.Plugins.SumDistinctAggregateKernel<int>.Reset(ref resultSumDistincts0State);
                        for (int resultSumDistincts0PartitionIndex = 0; resultSumDistincts0PartitionIndex < resultSumDistincts0PartitionCount; ++resultSumDistincts0PartitionIndex)
                        {
                            var resultSumDistincts0OutputIndex = resultSumDistincts0PartitionIndices[resultSumDistincts0PartitionStart + resultSumDistincts0PartitionIndex];
                            var resultSumDistincts0FrameStart = Math.Max(0, resultSumDistincts0PartitionIndex - 2);
                            var resultSumDistincts0FrameEnd = resultSumDistincts0PartitionIndex;
                            Musoq.Plugins.SumDistinctAggregateKernel<int>.Reset(ref resultSumDistincts0State);
                            for (var resultSumDistincts0FrameIndex = Math.Max(0, resultSumDistincts0FrameStart); resultSumDistincts0FrameIndex <= resultSumDistincts0FrameEnd && resultSumDistincts0FrameIndex < resultSumDistincts0PartitionCount; resultSumDistincts0FrameIndex++)
                            {
                                var resultSumDistincts0CurrentIndex = resultSumDistincts0PartitionIndices[resultSumDistincts0PartitionStart + resultSumDistincts0FrameIndex];
                                Musoq.Plugins.SumDistinctAggregateKernel<int>.Set(ref resultSumDistincts0State, (int?)resultSumDistincts0BatchValues[resultSumDistincts0CurrentIndex]);
                            }

                            resultSumDistincts0[resultSumDistincts0OutputIndex] = Musoq.Plugins.SumDistinctAggregateKernel<int>.Get(in resultSumDistincts0State);
                        }
                    }

                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var resultAvgDistincts1 = new decimal? [resultWindowRows.Count];
                    var resultAvgDistincts1BatchValues = new int? [resultWindowRows.Count];
                    for (int resultAvgDistincts1CaptureIndex = 0; resultAvgDistincts1CaptureIndex < resultWindowRows.Count; ++resultAvgDistincts1CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultAvgDistincts1CaptureIndex];
                        resultAvgDistincts1BatchValues[resultAvgDistincts1CaptureIndex] = (int?)ko3iko.Integral;
                    }

                    for (int resultAvgDistincts1PartitionSetIndex = 0; resultAvgDistincts1PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultAvgDistincts1PartitionSetIndex)
                    {
                        var resultAvgDistincts1PartitionStart = resultSumDistincts0Partitions.GetStart(resultAvgDistincts1PartitionSetIndex);
                        var resultAvgDistincts1PartitionCount = resultSumDistincts0Partitions.GetLength(resultAvgDistincts1PartitionSetIndex);
                        var resultAvgDistincts1PartitionIndices = resultSumDistincts0Partitions.Indices;
                        Musoq.Plugins.AverageDistinctKernel<int, decimal>.State resultAvgDistincts1State = default(Musoq.Plugins.AverageDistinctKernel<int, decimal>.State)!;
                        Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref resultAvgDistincts1State);
                        for (int resultAvgDistincts1PartitionIndex = 0; resultAvgDistincts1PartitionIndex < resultAvgDistincts1PartitionCount; ++resultAvgDistincts1PartitionIndex)
                        {
                            var resultAvgDistincts1OutputIndex = resultAvgDistincts1PartitionIndices[resultAvgDistincts1PartitionStart + resultAvgDistincts1PartitionIndex];
                            var resultAvgDistincts1FrameStart = Math.Max(0, resultAvgDistincts1PartitionIndex - 2);
                            var resultAvgDistincts1FrameEnd = resultAvgDistincts1PartitionIndex;
                            Musoq.Plugins.AverageDistinctKernel<int, decimal>.Reset(ref resultAvgDistincts1State);
                            for (var resultAvgDistincts1FrameIndex = Math.Max(0, resultAvgDistincts1FrameStart); resultAvgDistincts1FrameIndex <= resultAvgDistincts1FrameEnd && resultAvgDistincts1FrameIndex < resultAvgDistincts1PartitionCount; resultAvgDistincts1FrameIndex++)
                            {
                                var resultAvgDistincts1CurrentIndex = resultAvgDistincts1PartitionIndices[resultAvgDistincts1PartitionStart + resultAvgDistincts1FrameIndex];
                                Musoq.Plugins.AverageDistinctKernel<int, decimal>.Set(ref resultAvgDistincts1State, (int?)resultAvgDistincts1BatchValues[resultAvgDistincts1CurrentIndex]);
                            }

                            resultAvgDistincts1[resultAvgDistincts1OutputIndex] = Musoq.Plugins.AverageDistinctKernel<int, decimal>.Get(in resultAvgDistincts1State);
                        }
                    }

                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var resultCountDistincts2 = new long[resultWindowRows.Count];
                    var resultCountDistincts2BatchValues = new int? [resultWindowRows.Count];
                    for (int resultCountDistincts2CaptureIndex = 0; resultCountDistincts2CaptureIndex < resultWindowRows.Count; ++resultCountDistincts2CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCountDistincts2CaptureIndex];
                        resultCountDistincts2BatchValues[resultCountDistincts2CaptureIndex] = (int?)ko3iko.Integral;
                    }

                    for (int resultCountDistincts2PartitionSetIndex = 0; resultCountDistincts2PartitionSetIndex < resultSumDistincts0Partitions.PartitionCount; ++resultCountDistincts2PartitionSetIndex)
                    {
                        var resultCountDistincts2PartitionStart = resultSumDistincts0Partitions.GetStart(resultCountDistincts2PartitionSetIndex);
                        var resultCountDistincts2PartitionCount = resultSumDistincts0Partitions.GetLength(resultCountDistincts2PartitionSetIndex);
                        var resultCountDistincts2PartitionIndices = resultSumDistincts0Partitions.Indices;
                        Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.State resultCountDistincts2State = default(Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.State)!;
                        Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Reset(ref resultCountDistincts2State);
                        for (int resultCountDistincts2PartitionIndex = 0; resultCountDistincts2PartitionIndex < resultCountDistincts2PartitionCount; ++resultCountDistincts2PartitionIndex)
                        {
                            var resultCountDistincts2OutputIndex = resultCountDistincts2PartitionIndices[resultCountDistincts2PartitionStart + resultCountDistincts2PartitionIndex];
                            var resultCountDistincts2FrameStart = Math.Max(0, resultCountDistincts2PartitionIndex - 2);
                            var resultCountDistincts2FrameEnd = resultCountDistincts2PartitionIndex;
                            Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Reset(ref resultCountDistincts2State);
                            for (var resultCountDistincts2FrameIndex = Math.Max(0, resultCountDistincts2FrameStart); resultCountDistincts2FrameIndex <= resultCountDistincts2FrameEnd && resultCountDistincts2FrameIndex < resultCountDistincts2PartitionCount; resultCountDistincts2FrameIndex++)
                            {
                                var resultCountDistincts2CurrentIndex = resultCountDistincts2PartitionIndices[resultCountDistincts2PartitionStart + resultCountDistincts2FrameIndex];
                                Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Set(ref resultCountDistincts2State, (int?)resultCountDistincts2BatchValues[resultCountDistincts2CurrentIndex]);
                            }

                            resultCountDistincts2[resultCountDistincts2OutputIndex] = Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Get(in resultCountDistincts2State);
                        }
                    }

                    __op18Scope.Dispose();
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op19Scope.Dispose();
                    long __op20InputRows = 0L;
                    long __op20OutputRows = 0L;
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                            __op20InputRows++;
                            __op20OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (int?)resultSumDistincts0[windowIndex], (decimal?)resultAvgDistincts1[windowIndex], (long)resultCountDistincts2[windowIndex]));
                            __op21OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op20Scope.AddInputRows(__op20InputRows);
                        __op20Scope.AddOutputRows(__op20OutputRows);
                        __op20Scope.Dispose();
                    }

                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    __op22Scope.AddInputRows(result.Count);
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

                    __op22Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op22Scope.Dispose();
                    var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op24Scope.Dispose();
                    if (__op21OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op21Handle, __op21OutputRows);
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
            public ResultRow0(int __value0, int? __value1, decimal? __value2, long __value3)
            {
                Position = __value0;
                Total = __value1;
                Average = __value2;
                Values = __value3;
            }

            public decimal? Average { get; private set; }
            public override int Count => 4;
            public int Position { get; private set; }
            public int? Total { get; private set; }
            public long Values { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                "Average" => true,
                "Values" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Values, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? Total, decimal? Average, long Values)
            {
                this.Position = Position;
                this.Total = Total;
                this.Average = Average;
                this.Values = Values;
            }

            public decimal? Average { get; }
            public int Position { get; }
            public int? Total { get; }
            public long Values { get; }
        }

        private readonly struct WindowResultSumDistincts0OrderKeysKey : System.IEquatable<WindowResultSumDistincts0OrderKeysKey>, System.IComparable<WindowResultSumDistincts0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultSumDistincts0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultSumDistincts0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultSumDistincts0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultSumDistincts0OrderKeysKey other && Equals(other);
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
