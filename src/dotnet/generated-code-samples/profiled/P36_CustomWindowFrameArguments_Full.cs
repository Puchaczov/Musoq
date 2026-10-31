// === Parsed Query ===
/*
select Position, ScaledTotal(Amount, Position) over (partition by Partition order by Position
    rows between 1 preceding and current row) as Total,
    SeededTotal(Amount) over (partition by Partition order by Position) as Seeded
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Seeded]
      Window [ScaledTotal(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, ko3iko.Position; frame: rows between 1 preceding and current row), SeededTotal(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Total, WindowRef(1) as Seeded]
      PhysicalWindow [ScaledTotal(idx:0; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount, ko3iko.Position; frame: rows between 1 preceding and current row), SeededTotal(idx:1; partition: ko3iko.Partition; order: ko3iko.Position; args: ko3iko.Amount)]
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
      Seeded: decimal <- field Seeded

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeScaledTotalWindowKernel[BoundedRows] [resultScaledTotals0 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame rows between 1 preceding and current row]
    ComputeSeededTotalWindowKernel[BoundedRows] [resultSeededTotals1 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition order by ko3iko.Position ASC frame range between unbounded preceding and current row]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Total: resultScaledTotals0[windowIndex], Seeded: resultSeededTotals1[windowIndex])]
    SortOwnedBuffer [result by Position ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P36_CustomWindowFrameArguments_Full
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
            new Column("Seeded", typeof(decimal), 2)
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
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Seeded);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Total, __musoqShapeRow.Seeded);
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#schema");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                var resultScaledTotals0PartitionKeys = new string[resultWindowRows.Count];
                var resultScaledTotals0OrderKeys = new WindowResultScaledTotals0OrderKeysKey[resultWindowRows.Count];
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    resultScaledTotals0PartitionKeys[windowIndex] = (string)ko3iko.Partition;
                    resultScaledTotals0OrderKeys[windowIndex] = new WindowResultScaledTotals0OrderKeysKey(ko3iko.Position);
                }

                var resultScaledTotals0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultScaledTotals0PartitionKeys);
                WindowFunctionHelpers.SortStructPartitionSetInPlace(resultScaledTotals0Partitions, resultScaledTotals0OrderKeys, false);
                var resultScaledTotals0 = new decimal? [resultWindowRows.Count];
                var resultScaledTotals0BatchValues = new decimal? [resultWindowRows.Count];
                var resultScaledTotals0CapturedArgument1 = new int[resultWindowRows.Count];
                for (int resultScaledTotals0CaptureIndex = 0; resultScaledTotals0CaptureIndex < resultWindowRows.Count; ++resultScaledTotals0CaptureIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultScaledTotals0CaptureIndex];
                    resultScaledTotals0BatchValues[resultScaledTotals0CaptureIndex] = (decimal?)ko3iko.Amount;
                    resultScaledTotals0CapturedArgument1[resultScaledTotals0CaptureIndex] = (int)ko3iko.Position;
                }

                for (int resultScaledTotals0PartitionSetIndex = 0; resultScaledTotals0PartitionSetIndex < resultScaledTotals0Partitions.PartitionCount; ++resultScaledTotals0PartitionSetIndex)
                {
                    var resultScaledTotals0PartitionStart = resultScaledTotals0Partitions.GetStart(resultScaledTotals0PartitionSetIndex);
                    var resultScaledTotals0PartitionCount = resultScaledTotals0Partitions.GetLength(resultScaledTotals0PartitionSetIndex);
                    var resultScaledTotals0PartitionIndices = resultScaledTotals0Partitions.Indices;
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.State resultScaledTotals0State = default(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.State)!;
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref resultScaledTotals0State);
                    for (int resultScaledTotals0PartitionIndex = 0; resultScaledTotals0PartitionIndex < resultScaledTotals0PartitionCount; ++resultScaledTotals0PartitionIndex)
                    {
                        var resultScaledTotals0OutputIndex = resultScaledTotals0PartitionIndices[resultScaledTotals0PartitionStart + resultScaledTotals0PartitionIndex];
                        var resultScaledTotals0FrameStart = Math.Max(0, resultScaledTotals0PartitionIndex - 1);
                        var resultScaledTotals0FrameEnd = resultScaledTotals0PartitionIndex;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref resultScaledTotals0State);
                        for (var resultScaledTotals0FrameIndex = Math.Max(0, resultScaledTotals0FrameStart); resultScaledTotals0FrameIndex <= resultScaledTotals0FrameEnd && resultScaledTotals0FrameIndex < resultScaledTotals0PartitionCount; resultScaledTotals0FrameIndex++)
                        {
                            var resultScaledTotals0CurrentIndex = resultScaledTotals0PartitionIndices[resultScaledTotals0PartitionStart + resultScaledTotals0FrameIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Set(ref resultScaledTotals0State, (decimal?)resultScaledTotals0BatchValues[resultScaledTotals0CurrentIndex], (int)resultScaledTotals0CapturedArgument1[resultScaledTotals0CurrentIndex]);
                        }

                        resultScaledTotals0[resultScaledTotals0OutputIndex] = Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Get(in resultScaledTotals0State);
                    }
                }

                var resultSeededTotals1 = new decimal[resultWindowRows.Count];
                for (int resultSeededTotals1PartitionSetIndex = 0; resultSeededTotals1PartitionSetIndex < resultScaledTotals0Partitions.PartitionCount; ++resultSeededTotals1PartitionSetIndex)
                {
                    var resultSeededTotals1PartitionStart = resultScaledTotals0Partitions.GetStart(resultSeededTotals1PartitionSetIndex);
                    var resultSeededTotals1PartitionCount = resultScaledTotals0Partitions.GetLength(resultSeededTotals1PartitionSetIndex);
                    var resultSeededTotals1PartitionIndices = resultScaledTotals0Partitions.Indices;
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State resultSeededTotals1State = default(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State)!;
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Reset(ref resultSeededTotals1State);
                    for (int resultSeededTotals1PartitionIndex = 0; resultSeededTotals1PartitionIndex < resultSeededTotals1PartitionCount;)
                    {
                        var resultSeededTotals1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultScaledTotals0OrderKeys, resultSeededTotals1PartitionIndices, resultSeededTotals1PartitionStart, resultSeededTotals1PartitionCount, resultSeededTotals1PartitionIndex);
                        for (int resultSeededTotals1PeerIndex = resultSeededTotals1PartitionIndex; resultSeededTotals1PeerIndex <= resultSeededTotals1PeerEnd; ++resultSeededTotals1PeerIndex)
                        {
                            var resultSeededTotals1CurrentIndex = resultSeededTotals1PartitionIndices[resultSeededTotals1PartitionStart + resultSeededTotals1PeerIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSeededTotals1CurrentIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref resultSeededTotals1State, (decimal?)ko3iko.Amount);
                        }

                        var resultSeededTotals1PeerValue = Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Get(in resultSeededTotals1State);
                        for (int resultSeededTotals1PeerIndex = resultSeededTotals1PartitionIndex; resultSeededTotals1PeerIndex <= resultSeededTotals1PeerEnd; ++resultSeededTotals1PeerIndex)
                        {
                            var resultSeededTotals1CurrentIndex = resultSeededTotals1PartitionIndices[resultSeededTotals1PartitionStart + resultSeededTotals1PeerIndex];
                            resultSeededTotals1[resultSeededTotals1CurrentIndex] = resultSeededTotals1PeerValue;
                        }

                        resultSeededTotals1PartitionIndex = resultSeededTotals1PeerEnd + 1;
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
                    result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultScaledTotals0[windowIndex], (decimal)resultSeededTotals1[windowIndex]) { __musoqSequence = result.Count });
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
                    var __op12Handle = profileRecorder?.GetOperatorHandle("op12", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op13Handle = profileRecorder?.GetOperatorHandle("op13", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op14Handle = profileRecorder?.GetOperatorHandle("op14", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op15Handle = profileRecorder?.GetOperatorHandle("op15", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "ComputeScaledTotalWindowKernel") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "ComputeSeededTotalWindowKernel") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "SortOwnedBuffer") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op20OutputRows = 0L;
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
                    var resultScaledTotals0PartitionKeys = new string[resultWindowRows.Count];
                    var resultScaledTotals0OrderKeys = new WindowResultScaledTotals0OrderKeysKey[resultWindowRows.Count];
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                        resultScaledTotals0PartitionKeys[windowIndex] = (string)ko3iko.Partition;
                        resultScaledTotals0OrderKeys[windowIndex] = new WindowResultScaledTotals0OrderKeysKey(ko3iko.Position);
                    }

                    var resultScaledTotals0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultScaledTotals0PartitionKeys);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultScaledTotals0Partitions, resultScaledTotals0OrderKeys, false);
                    var resultScaledTotals0 = new decimal? [resultWindowRows.Count];
                    var resultScaledTotals0BatchValues = new decimal? [resultWindowRows.Count];
                    var resultScaledTotals0CapturedArgument1 = new int[resultWindowRows.Count];
                    for (int resultScaledTotals0CaptureIndex = 0; resultScaledTotals0CaptureIndex < resultWindowRows.Count; ++resultScaledTotals0CaptureIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultScaledTotals0CaptureIndex];
                        resultScaledTotals0BatchValues[resultScaledTotals0CaptureIndex] = (decimal?)ko3iko.Amount;
                        resultScaledTotals0CapturedArgument1[resultScaledTotals0CaptureIndex] = (int)ko3iko.Position;
                    }

                    for (int resultScaledTotals0PartitionSetIndex = 0; resultScaledTotals0PartitionSetIndex < resultScaledTotals0Partitions.PartitionCount; ++resultScaledTotals0PartitionSetIndex)
                    {
                        var resultScaledTotals0PartitionStart = resultScaledTotals0Partitions.GetStart(resultScaledTotals0PartitionSetIndex);
                        var resultScaledTotals0PartitionCount = resultScaledTotals0Partitions.GetLength(resultScaledTotals0PartitionSetIndex);
                        var resultScaledTotals0PartitionIndices = resultScaledTotals0Partitions.Indices;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.State resultScaledTotals0State = default(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.State)!;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref resultScaledTotals0State);
                        for (int resultScaledTotals0PartitionIndex = 0; resultScaledTotals0PartitionIndex < resultScaledTotals0PartitionCount; ++resultScaledTotals0PartitionIndex)
                        {
                            var resultScaledTotals0OutputIndex = resultScaledTotals0PartitionIndices[resultScaledTotals0PartitionStart + resultScaledTotals0PartitionIndex];
                            var resultScaledTotals0FrameStart = Math.Max(0, resultScaledTotals0PartitionIndex - 1);
                            var resultScaledTotals0FrameEnd = resultScaledTotals0PartitionIndex;
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Reset(ref resultScaledTotals0State);
                            for (var resultScaledTotals0FrameIndex = Math.Max(0, resultScaledTotals0FrameStart); resultScaledTotals0FrameIndex <= resultScaledTotals0FrameEnd && resultScaledTotals0FrameIndex < resultScaledTotals0PartitionCount; resultScaledTotals0FrameIndex++)
                            {
                                var resultScaledTotals0CurrentIndex = resultScaledTotals0PartitionIndices[resultScaledTotals0PartitionStart + resultScaledTotals0FrameIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Set(ref resultScaledTotals0State, (decimal?)resultScaledTotals0BatchValues[resultScaledTotals0CurrentIndex], (int)resultScaledTotals0CapturedArgument1[resultScaledTotals0CurrentIndex]);
                            }

                            resultScaledTotals0[resultScaledTotals0OutputIndex] = Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.ScaledTotalKernel.Get(in resultScaledTotals0State);
                        }
                    }

                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    var resultSeededTotals1 = new decimal[resultWindowRows.Count];
                    for (int resultSeededTotals1PartitionSetIndex = 0; resultSeededTotals1PartitionSetIndex < resultScaledTotals0Partitions.PartitionCount; ++resultSeededTotals1PartitionSetIndex)
                    {
                        var resultSeededTotals1PartitionStart = resultScaledTotals0Partitions.GetStart(resultSeededTotals1PartitionSetIndex);
                        var resultSeededTotals1PartitionCount = resultScaledTotals0Partitions.GetLength(resultSeededTotals1PartitionSetIndex);
                        var resultSeededTotals1PartitionIndices = resultScaledTotals0Partitions.Indices;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State resultSeededTotals1State = default(Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.State)!;
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Reset(ref resultSeededTotals1State);
                        for (int resultSeededTotals1PartitionIndex = 0; resultSeededTotals1PartitionIndex < resultSeededTotals1PartitionCount;)
                        {
                            var resultSeededTotals1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultScaledTotals0OrderKeys, resultSeededTotals1PartitionIndices, resultSeededTotals1PartitionStart, resultSeededTotals1PartitionCount, resultSeededTotals1PartitionIndex);
                            for (int resultSeededTotals1PeerIndex = resultSeededTotals1PartitionIndex; resultSeededTotals1PeerIndex <= resultSeededTotals1PeerEnd; ++resultSeededTotals1PeerIndex)
                            {
                                var resultSeededTotals1CurrentIndex = resultSeededTotals1PartitionIndices[resultSeededTotals1PartitionStart + resultSeededTotals1PeerIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSeededTotals1CurrentIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Set(ref resultSeededTotals1State, (decimal?)ko3iko.Amount);
                            }

                            var resultSeededTotals1PeerValue = Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionLibrary.SeededTotalKernel.Get(in resultSeededTotals1State);
                            for (int resultSeededTotals1PeerIndex = resultSeededTotals1PartitionIndex; resultSeededTotals1PeerIndex <= resultSeededTotals1PeerEnd; ++resultSeededTotals1PeerIndex)
                            {
                                var resultSeededTotals1CurrentIndex = resultSeededTotals1PartitionIndices[resultSeededTotals1PartitionStart + resultSeededTotals1PeerIndex];
                                resultSeededTotals1[resultSeededTotals1CurrentIndex] = resultSeededTotals1PeerValue;
                            }

                            resultSeededTotals1PartitionIndex = resultSeededTotals1PeerEnd + 1;
                        }
                    }

                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op18Scope.Dispose();
                    long __op19InputRows = 0L;
                    long __op19OutputRows = 0L;
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                            __op19InputRows++;
                            __op19OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (decimal?)resultScaledTotals0[windowIndex], (decimal)resultSeededTotals1[windowIndex]) { __musoqSequence = result.Count });
                            __op20OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op19Scope.AddInputRows(__op19InputRows);
                        __op19Scope.AddOutputRows(__op19OutputRows);
                        __op19Scope.Dispose();
                    }

                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    __op21Scope.AddInputRows(result.Count);
                    result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                    {
                        var comparison = left.Position.CompareTo(right.Position);
                        if (comparison != 0)
                            return comparison;
                        return left.__musoqSequence.CompareTo(right.__musoqSequence);
                    }));
                    __op21Scope.AddOutputRows(result.Count);
                    __op21Scope.Dispose();
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op23Scope.Dispose();
                    if (__op20OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op20Handle, __op20OutputRows);
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
            public ResultRow0(int __value0, decimal? __value1, decimal __value2)
            {
                Position = __value0;
                Total = __value1;
                Seeded = __value2;
            }

            public override int Count => 3;
            public int Position { get; private set; }
            public decimal Seeded { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Seeded = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal>(this.Seeded),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Total" => true,
                "Seeded" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Seeded, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, decimal? Total, decimal Seeded)
            {
                this.Position = Position;
                this.Total = Total;
                this.Seeded = Seeded;
            }

            public int Position { get; }
            public decimal Seeded { get; }
            public decimal? Total { get; }
            public int __musoqSequence { get; init; }
        }

        private readonly struct WindowResultScaledTotals0OrderKeysKey : System.IEquatable<WindowResultScaledTotals0OrderKeysKey>, System.IComparable<WindowResultScaledTotals0OrderKeysKey>
        {
            private readonly int _value0;
            public WindowResultScaledTotals0OrderKeysKey(int value0)
            {
                _value0 = value0;
            }

            public int RawValues => _value0;

            public int CompareTo(WindowResultScaledTotals0OrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultScaledTotals0OrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<int>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultScaledTotals0OrderKeysKey other && Equals(other);
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
