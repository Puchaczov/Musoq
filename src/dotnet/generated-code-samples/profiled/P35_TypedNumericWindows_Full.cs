// === Parsed Query ===
/*
select Position, Sum(Integral) over (order by Position) as Integers,
    Avg(Integral) over (order by Position) as Average,
    Sum(Floating) over (order by Position) as Doubles,
    Count(Amount) over (partition by Partition) as Values
from #schema.first() order by Position
*/

// === Logical Plan ===
/*
MultiStatement
  Sort [ko3iko.Position]
    Project [ko3iko.Position as Position, WindowRef(0) as Integers, WindowRef(1) as Average, WindowRef(2) as Doubles, WindowRef(3) as Values]
      Window [Sum(idx:0; order: ko3iko.Position; args: ko3iko.Integral), Avg(idx:1; order: ko3iko.Position; args: ko3iko.Integral), Sum(idx:2; order: ko3iko.Position; args: ko3iko.Floating), Count(idx:3; partition: ko3iko.Partition; args: ko3iko.Amount)]
        SchemaScan [#schema.first() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalSort [ko3iko.Position]
    PhysicalProject [ko3iko.Position as Position, WindowRef(0) as Integers, WindowRef(1) as Average, WindowRef(2) as Doubles, WindowRef(3) as Values]
      PhysicalWindow [Sum(idx:0; order: ko3iko.Position; args: ko3iko.Integral), Avg(idx:1; order: ko3iko.Position; args: ko3iko.Integral), Sum(idx:2; order: ko3iko.Position; args: ko3iko.Floating), Count(idx:3; partition: ko3iko.Partition; args: ko3iko.Amount)]
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
      Integers: int? <- field Integers
      Average: decimal? <- field Average
      Doubles: double? <- field Doubles
      Values: long <- field Values

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: LibraryExecutionEntity] -> ko3ikoRows
    MaterializeChunked [ko3ikoRows -> resultWindowRows]
    ComputeSumWindowKernel[BoundedRows] [resultSums0 <- resultWindowRows value ko3iko.Integral order by ko3iko.Position ASC frame range between unbounded preceding and current row]
    ComputeAvgWindowKernel[BoundedRows] [resultAvgs1 <- resultWindowRows value ko3iko.Integral order by ko3iko.Position ASC frame range between unbounded preceding and current row]
    ComputeSumWindowKernel[BoundedRows] [resultSums2 <- resultWindowRows value ko3iko.Floating order by ko3iko.Position ASC frame range between unbounded preceding and current row]
    ComputeCountWindowKernel[WholePartition] [resultCounts3 <- resultWindowRows value ko3iko.Amount partition by ko3iko.Partition]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, ko3iko in resultWindowRows]
      AppendShape [result <- ResultShape0(Position: ko3iko.Position, Integers: resultSums0[windowIndex], Average: resultAvgs1[windowIndex], Doubles: resultSums2[windowIndex], Values: resultCounts3[windowIndex])]
    SortShapeRows [result -> resultSorted by Position ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P35_TypedNumericWindows_Full
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
            new Column("Integers", typeof(int?), 1),
            new Column("Average", typeof(decimal?), 2),
            new Column("Doubles", typeof(double?), 3),
            new Column("Values", typeof(long), 4)
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

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Integers, __musoqShapeRow.Average, __musoqShapeRow.Doubles, __musoqShapeRow.Values);
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return new ResultRow0(__musoqShapeRow.Position, __musoqShapeRow.Integers, __musoqShapeRow.Average, __musoqShapeRow.Doubles, __musoqShapeRow.Values);
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
                var resultSums0 = new int? [resultWindowRows.Count];
                for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                {
                    var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                    var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                    var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<int>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                    Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                    for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount;)
                    {
                        var resultSums0PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums0CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums0State, (int?)ko3iko.Integral);
                        }

                        var resultSums0PeerValue = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums0State);
                        for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                        {
                            var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                            resultSums0[resultSums0CurrentIndex] = resultSums0PeerValue;
                        }

                        resultSums0PartitionIndex = resultSums0PeerEnd + 1;
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
                    for (int resultAvgs1PartitionIndex = 0; resultAvgs1PartitionIndex < resultAvgs1PartitionCount;)
                    {
                        var resultAvgs1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultAvgs1PartitionIndices, resultAvgs1PartitionStart, resultAvgs1PartitionCount, resultAvgs1PartitionIndex);
                        for (int resultAvgs1PeerIndex = resultAvgs1PartitionIndex; resultAvgs1PeerIndex <= resultAvgs1PeerEnd; ++resultAvgs1PeerIndex)
                        {
                            var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PeerIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultAvgs1CurrentIndex];
                            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgs1State, (int?)ko3iko.Integral);
                        }

                        var resultAvgs1PeerValue = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgs1State);
                        for (int resultAvgs1PeerIndex = resultAvgs1PartitionIndex; resultAvgs1PeerIndex <= resultAvgs1PeerEnd; ++resultAvgs1PeerIndex)
                        {
                            var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PeerIndex];
                            resultAvgs1[resultAvgs1CurrentIndex] = resultAvgs1PeerValue;
                        }

                        resultAvgs1PartitionIndex = resultAvgs1PeerEnd + 1;
                    }
                }

                var resultSums2 = new double? [resultWindowRows.Count];
                for (int resultSums2PartitionSetIndex = 0; resultSums2PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums2PartitionSetIndex)
                {
                    var resultSums2PartitionStart = resultSums0Partitions.GetStart(resultSums2PartitionSetIndex);
                    var resultSums2PartitionCount = resultSums0Partitions.GetLength(resultSums2PartitionSetIndex);
                    var resultSums2PartitionIndices = resultSums0Partitions.Indices;
                    Musoq.Plugins.SumAggregateKernel<double>.State resultSums2State = default(Musoq.Plugins.SumAggregateKernel<double>.State)!;
                    Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSums2State);
                    for (int resultSums2PartitionIndex = 0; resultSums2PartitionIndex < resultSums2PartitionCount;)
                    {
                        var resultSums2PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums2PartitionIndices, resultSums2PartitionStart, resultSums2PartitionCount, resultSums2PartitionIndex);
                        for (int resultSums2PeerIndex = resultSums2PartitionIndex; resultSums2PeerIndex <= resultSums2PeerEnd; ++resultSums2PeerIndex)
                        {
                            var resultSums2CurrentIndex = resultSums2PartitionIndices[resultSums2PartitionStart + resultSums2PeerIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums2CurrentIndex];
                            Musoq.Plugins.SumAggregateKernel<double>.Set(ref resultSums2State, (double?)ko3iko.Floating);
                        }

                        var resultSums2PeerValue = Musoq.Plugins.SumAggregateKernel<double>.Get(in resultSums2State);
                        for (int resultSums2PeerIndex = resultSums2PartitionIndex; resultSums2PeerIndex <= resultSums2PeerEnd; ++resultSums2PeerIndex)
                        {
                            var resultSums2CurrentIndex = resultSums2PartitionIndices[resultSums2PartitionStart + resultSums2PeerIndex];
                            resultSums2[resultSums2CurrentIndex] = resultSums2PeerValue;
                        }

                        resultSums2PartitionIndex = resultSums2PeerEnd + 1;
                    }
                }

                var resultCounts3PartitionBuilder = new Musoq.Evaluator.Helpers.WindowPartitionBuilder<string>(resultWindowRows.Count);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                    string resultCounts3PartitionKeysValue = (string)ko3iko.Partition;
                    resultCounts3PartitionBuilder.Add(resultCounts3PartitionKeysValue, windowIndex);
                }

                var resultCounts3Partitions = resultCounts3PartitionBuilder.ToPartitionSet();
                var resultCounts3 = new long[resultWindowRows.Count];
                for (int resultCounts3PartitionSetIndex = 0; resultCounts3PartitionSetIndex < resultCounts3Partitions.PartitionCount; ++resultCounts3PartitionSetIndex)
                {
                    var resultCounts3PartitionStart = resultCounts3Partitions.GetStart(resultCounts3PartitionSetIndex);
                    var resultCounts3PartitionCount = resultCounts3Partitions.GetLength(resultCounts3PartitionSetIndex);
                    var resultCounts3PartitionIndices = resultCounts3Partitions.Indices;
                    Musoq.Plugins.CountNullableAggregateKernel<decimal>.State resultCounts3State = default(Musoq.Plugins.CountNullableAggregateKernel<decimal>.State)!;
                    Musoq.Plugins.CountNullableAggregateKernel<decimal>.Reset(ref resultCounts3State);
                    for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                    {
                        var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCounts3CurrentIndex];
                        Musoq.Plugins.CountNullableAggregateKernel<decimal>.Set(ref resultCounts3State, (decimal?)ko3iko.Amount);
                    }

                    var resultCounts3FinalValue = Musoq.Plugins.CountNullableAggregateKernel<decimal>.Get(in resultCounts3State);
                    for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                    {
                        var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                        resultCounts3[resultCounts3CurrentIndex] = resultCounts3FinalValue;
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
                    result.Add(new ResultShape0(ko3iko.Position, (int?)resultSums0[windowIndex], (decimal?)resultAvgs1[windowIndex], (double?)resultSums2[windowIndex], (long)resultCounts3[windowIndex]));
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
                    var __op16Handle = profileRecorder?.GetOperatorHandle("op16", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op17Handle = profileRecorder?.GetOperatorHandle("op17", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op18Handle = profileRecorder?.GetOperatorHandle("op18", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op19Handle = profileRecorder?.GetOperatorHandle("op19", "MaterializeChunked") ?? OperatorProfileHandle.None;
                    var __op20Handle = profileRecorder?.GetOperatorHandle("op20", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op21Handle = profileRecorder?.GetOperatorHandle("op21", "ComputeAvgWindowKernel") ?? OperatorProfileHandle.None;
                    var __op22Handle = profileRecorder?.GetOperatorHandle("op22", "ComputeSumWindowKernel") ?? OperatorProfileHandle.None;
                    var __op23Handle = profileRecorder?.GetOperatorHandle("op23", "ComputeCountWindowKernel") ?? OperatorProfileHandle.None;
                    var __op24Handle = profileRecorder?.GetOperatorHandle("op24", "CreateShapeRows") ?? OperatorProfileHandle.None;
                    var __op25Handle = profileRecorder?.GetOperatorHandle("op25", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op26Handle = profileRecorder?.GetOperatorHandle("op26", "AppendShape") ?? OperatorProfileHandle.None;
                    var __op27Handle = profileRecorder?.GetOperatorHandle("op27", "SortShapeRows") ?? OperatorProfileHandle.None;
                    var __op29Handle = profileRecorder?.GetOperatorHandle("op29", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    long __op26OutputRows = 0L;
                    var __op16Scope = profileRecorder?.BeginOperatorValue(__op16Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op16Scope.Dispose();
                    var __op17Scope = profileRecorder?.BeginOperatorValue(__op17Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op17Scope.Dispose();
                    var __op18Scope = profileRecorder?.BeginOperatorValue(__op18Handle) ?? OperatorProfileValueScope.None;
                    var __ko3ikoSchema = provider.GetSchema("#schema");
                    var ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Open(__ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, ko3ikoRowsProfile == null ? SourceDiagnostics.None : ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var ko3ikoRows = ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks, ko3ikoRowsProfile);
                    __op18Scope.Dispose();
                    var __op19Scope = profileRecorder?.BeginOperatorValue(__op19Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeChunkedRowsList(ko3ikoRows);
                    __op19Scope.AddOutputRows(resultWindowRows.Count);
                    __op19Scope.Dispose();
                    var __op20Scope = profileRecorder?.BeginOperatorValue(__op20Handle) ?? OperatorProfileValueScope.None;
                    var resultSums0OrderKeys = new WindowResultSums0OrderKeysKey[resultWindowRows.Count];
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                        resultSums0OrderKeys[windowIndex] = new WindowResultSums0OrderKeysKey(ko3iko.Position);
                    }

                    var resultSums0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    WindowFunctionHelpers.SortStructPartitionSetInPlace(resultSums0Partitions, resultSums0OrderKeys, false);
                    var resultSums0 = new int? [resultWindowRows.Count];
                    for (int resultSums0PartitionSetIndex = 0; resultSums0PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums0PartitionSetIndex)
                    {
                        var resultSums0PartitionStart = resultSums0Partitions.GetStart(resultSums0PartitionSetIndex);
                        var resultSums0PartitionCount = resultSums0Partitions.GetLength(resultSums0PartitionSetIndex);
                        var resultSums0PartitionIndices = resultSums0Partitions.Indices;
                        Musoq.Plugins.SumAggregateKernel<int>.State resultSums0State = default(Musoq.Plugins.SumAggregateKernel<int>.State)!;
                        Musoq.Plugins.SumAggregateKernel<int>.Reset(ref resultSums0State);
                        for (int resultSums0PartitionIndex = 0; resultSums0PartitionIndex < resultSums0PartitionCount;)
                        {
                            var resultSums0PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums0PartitionIndices, resultSums0PartitionStart, resultSums0PartitionCount, resultSums0PartitionIndex);
                            for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                            {
                                var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums0CurrentIndex];
                                Musoq.Plugins.SumAggregateKernel<int>.Set(ref resultSums0State, (int?)ko3iko.Integral);
                            }

                            var resultSums0PeerValue = Musoq.Plugins.SumAggregateKernel<int>.Get(in resultSums0State);
                            for (int resultSums0PeerIndex = resultSums0PartitionIndex; resultSums0PeerIndex <= resultSums0PeerEnd; ++resultSums0PeerIndex)
                            {
                                var resultSums0CurrentIndex = resultSums0PartitionIndices[resultSums0PartitionStart + resultSums0PeerIndex];
                                resultSums0[resultSums0CurrentIndex] = resultSums0PeerValue;
                            }

                            resultSums0PartitionIndex = resultSums0PeerEnd + 1;
                        }
                    }

                    __op20Scope.Dispose();
                    var __op21Scope = profileRecorder?.BeginOperatorValue(__op21Handle) ?? OperatorProfileValueScope.None;
                    var resultAvgs1 = new decimal? [resultWindowRows.Count];
                    for (int resultAvgs1PartitionSetIndex = 0; resultAvgs1PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultAvgs1PartitionSetIndex)
                    {
                        var resultAvgs1PartitionStart = resultSums0Partitions.GetStart(resultAvgs1PartitionSetIndex);
                        var resultAvgs1PartitionCount = resultSums0Partitions.GetLength(resultAvgs1PartitionSetIndex);
                        var resultAvgs1PartitionIndices = resultSums0Partitions.Indices;
                        Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgs1State = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                        Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgs1State);
                        for (int resultAvgs1PartitionIndex = 0; resultAvgs1PartitionIndex < resultAvgs1PartitionCount;)
                        {
                            var resultAvgs1PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultAvgs1PartitionIndices, resultAvgs1PartitionStart, resultAvgs1PartitionCount, resultAvgs1PartitionIndex);
                            for (int resultAvgs1PeerIndex = resultAvgs1PartitionIndex; resultAvgs1PeerIndex <= resultAvgs1PeerEnd; ++resultAvgs1PeerIndex)
                            {
                                var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PeerIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultAvgs1CurrentIndex];
                                Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgs1State, (int?)ko3iko.Integral);
                            }

                            var resultAvgs1PeerValue = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgs1State);
                            for (int resultAvgs1PeerIndex = resultAvgs1PartitionIndex; resultAvgs1PeerIndex <= resultAvgs1PeerEnd; ++resultAvgs1PeerIndex)
                            {
                                var resultAvgs1CurrentIndex = resultAvgs1PartitionIndices[resultAvgs1PartitionStart + resultAvgs1PeerIndex];
                                resultAvgs1[resultAvgs1CurrentIndex] = resultAvgs1PeerValue;
                            }

                            resultAvgs1PartitionIndex = resultAvgs1PeerEnd + 1;
                        }
                    }

                    __op21Scope.Dispose();
                    var __op22Scope = profileRecorder?.BeginOperatorValue(__op22Handle) ?? OperatorProfileValueScope.None;
                    var resultSums2 = new double? [resultWindowRows.Count];
                    for (int resultSums2PartitionSetIndex = 0; resultSums2PartitionSetIndex < resultSums0Partitions.PartitionCount; ++resultSums2PartitionSetIndex)
                    {
                        var resultSums2PartitionStart = resultSums0Partitions.GetStart(resultSums2PartitionSetIndex);
                        var resultSums2PartitionCount = resultSums0Partitions.GetLength(resultSums2PartitionSetIndex);
                        var resultSums2PartitionIndices = resultSums0Partitions.Indices;
                        Musoq.Plugins.SumAggregateKernel<double>.State resultSums2State = default(Musoq.Plugins.SumAggregateKernel<double>.State)!;
                        Musoq.Plugins.SumAggregateKernel<double>.Reset(ref resultSums2State);
                        for (int resultSums2PartitionIndex = 0; resultSums2PartitionIndex < resultSums2PartitionCount;)
                        {
                            var resultSums2PeerEnd = WindowFunctionHelpers.ResolveRangePeerFrameEnd(resultSums0OrderKeys, resultSums2PartitionIndices, resultSums2PartitionStart, resultSums2PartitionCount, resultSums2PartitionIndex);
                            for (int resultSums2PeerIndex = resultSums2PartitionIndex; resultSums2PeerIndex <= resultSums2PeerEnd; ++resultSums2PeerIndex)
                            {
                                var resultSums2CurrentIndex = resultSums2PartitionIndices[resultSums2PartitionStart + resultSums2PeerIndex];
                                Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultSums2CurrentIndex];
                                Musoq.Plugins.SumAggregateKernel<double>.Set(ref resultSums2State, (double?)ko3iko.Floating);
                            }

                            var resultSums2PeerValue = Musoq.Plugins.SumAggregateKernel<double>.Get(in resultSums2State);
                            for (int resultSums2PeerIndex = resultSums2PartitionIndex; resultSums2PeerIndex <= resultSums2PeerEnd; ++resultSums2PeerIndex)
                            {
                                var resultSums2CurrentIndex = resultSums2PartitionIndices[resultSums2PartitionStart + resultSums2PeerIndex];
                                resultSums2[resultSums2CurrentIndex] = resultSums2PeerValue;
                            }

                            resultSums2PartitionIndex = resultSums2PeerEnd + 1;
                        }
                    }

                    __op22Scope.Dispose();
                    var __op23Scope = profileRecorder?.BeginOperatorValue(__op23Handle) ?? OperatorProfileValueScope.None;
                    var resultCounts3PartitionBuilder = new Musoq.Evaluator.Helpers.WindowPartitionBuilder<string>(resultWindowRows.Count);
                    for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                    {
                        Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                        string resultCounts3PartitionKeysValue = (string)ko3iko.Partition;
                        resultCounts3PartitionBuilder.Add(resultCounts3PartitionKeysValue, windowIndex);
                    }

                    var resultCounts3Partitions = resultCounts3PartitionBuilder.ToPartitionSet();
                    var resultCounts3 = new long[resultWindowRows.Count];
                    for (int resultCounts3PartitionSetIndex = 0; resultCounts3PartitionSetIndex < resultCounts3Partitions.PartitionCount; ++resultCounts3PartitionSetIndex)
                    {
                        var resultCounts3PartitionStart = resultCounts3Partitions.GetStart(resultCounts3PartitionSetIndex);
                        var resultCounts3PartitionCount = resultCounts3Partitions.GetLength(resultCounts3PartitionSetIndex);
                        var resultCounts3PartitionIndices = resultCounts3Partitions.Indices;
                        Musoq.Plugins.CountNullableAggregateKernel<decimal>.State resultCounts3State = default(Musoq.Plugins.CountNullableAggregateKernel<decimal>.State)!;
                        Musoq.Plugins.CountNullableAggregateKernel<decimal>.Reset(ref resultCounts3State);
                        for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                        {
                            var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[resultCounts3CurrentIndex];
                            Musoq.Plugins.CountNullableAggregateKernel<decimal>.Set(ref resultCounts3State, (decimal?)ko3iko.Amount);
                        }

                        var resultCounts3FinalValue = Musoq.Plugins.CountNullableAggregateKernel<decimal>.Get(in resultCounts3State);
                        for (int resultCounts3PartitionIndex = 0; resultCounts3PartitionIndex < resultCounts3PartitionCount; ++resultCounts3PartitionIndex)
                        {
                            var resultCounts3CurrentIndex = resultCounts3PartitionIndices[resultCounts3PartitionStart + resultCounts3PartitionIndex];
                            resultCounts3[resultCounts3CurrentIndex] = resultCounts3FinalValue;
                        }
                    }

                    __op23Scope.Dispose();
                    var __op24Scope = profileRecorder?.BeginOperatorValue(__op24Handle) ?? OperatorProfileValueScope.None;
                    var result = new List<ResultShape0>(resultWindowRows.Count);
                    __op24Scope.Dispose();
                    long __op25InputRows = 0L;
                    long __op25OutputRows = 0L;
                    var __op25Scope = profileRecorder?.BeginOperatorValue(__op25Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Musoq.Evaluator.Tests.Schema.LibraryExecution.LibraryExecutionEntity ko3iko = resultWindowRows[windowIndex];
                            __op25InputRows++;
                            __op25OutputRows++;
                            result.Add(new ResultShape0(ko3iko.Position, (int?)resultSums0[windowIndex], (decimal?)resultAvgs1[windowIndex], (double?)resultSums2[windowIndex], (long)resultCounts3[windowIndex]));
                            __op26OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op25Scope.AddInputRows(__op25InputRows);
                        __op25Scope.AddOutputRows(__op25OutputRows);
                        __op25Scope.Dispose();
                    }

                    var __op27Scope = profileRecorder?.BeginOperatorValue(__op27Handle) ?? OperatorProfileValueScope.None;
                    __op27Scope.AddInputRows(result.Count);
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

                    __op27Scope.AddOutputRows(__musoqFinalShapeRows.Count);
                    __op27Scope.Dispose();
                    var __op29Scope = profileRecorder?.BeginOperatorValue(__op29Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op29Scope.Dispose();
                    if (__op26OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op26Handle, __op26OutputRows);
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
            public ResultRow0(int __value0, int? __value1, decimal? __value2, double? __value3, long __value4)
            {
                Position = __value0;
                Integers = __value1;
                Average = __value2;
                Doubles = __value3;
                Values = __value4;
            }

            public decimal? Average { get; private set; }
            public override int Count => 5;
            public double? Doubles { get; private set; }
            public int? Integers { get; private set; }
            public int Position { get; private set; }
            public long Values { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Position),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Integers),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double?>(this.Doubles),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Values),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Position" => true,
                "Integers" => true,
                "Average" => true,
                "Doubles" => true,
                "Values" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Position, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Integers, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.Doubles, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.Values, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Position, int? Integers, decimal? Average, double? Doubles, long Values)
            {
                this.Position = Position;
                this.Integers = Integers;
                this.Average = Average;
                this.Doubles = Doubles;
                this.Values = Values;
            }

            public decimal? Average { get; }
            public double? Doubles { get; }
            public int? Integers { get; }
            public int Position { get; }
            public long Values { get; }
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
