// === Parsed Query ===
/*
with doubled as (
    select Partition, Value from #schema.first()
    union all select Partition, Value from #schema.first() where Position % 2 = 0),
totals as (
    select Partition, Sum(Value) as Total, Count(Value) as Present,
        Sum(distinct Value) as UniqueTotal from doubled group by Partition)
select Partition, Total, Present, UniqueTotal, Avg(Total) over () as AverageTotal
from totals
*/

// === Logical Plan ===
/*
Cte
  Definition [doubled]
    SetOp [UnionAll]
      MultiStatement
        Project [ko3iko.Partition as Partition, ko3iko.Value as Value]
          SchemaScan [#schema.first() as ko3iko]
      MultiStatement
        Project [vo04qt.Partition as Partition, vo04qt.Value as Value]
          Filter [((vo04qt.Position % 2) = 0)]
            SchemaScan [#schema.first() as vo04qt]
  Definition [totals]
    MultiStatement
      Project [doubled.Partition as doubled.Partition, AggRef(doubled.Sum(distinct doubled.Value)) as doubled.Sum(distinct doubled.Value), AggRef(doubled.Count(doubled.Value)) as doubled.Count(doubled.Value), AggRef(doubled.Sum(doubled.Value)) as doubled.Sum(doubled.Value)]
        Aggregate [keys: Partition] [aggs: Sum(distinct Value), Count(Value), Sum(Value)]
          CteRef [doubled as doubled]
      Project [doubled.Partition as Partition, doubled.Sum(doubled.Value) as Total, doubled.Count(doubled.Value) as Present, doubled.Sum(distinct doubled.Value) as UniqueTotal]
        CteRef [doubledScore as doubledScore]
  Query
    MultiStatement
      Project [totals.Partition as Partition, totals.Total as Total, totals.Present as Present, totals.UniqueTotal as UniqueTotal, WindowRef(0) as AverageTotal]
        Window [Avg(idx:0; args: totals.Total)]
          CteRef [totals as totals]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [doubled]
    PhysicalSetOp [UnionAll]
      PhysicalMultiStatement
        PhysicalProject [ko3iko.Partition as Partition, ko3iko.Value as Value]
          PhysicalSchemaScan [#schema.first() as ko3iko]
      PhysicalMultiStatement
        PhysicalProject [vo04qt.Partition as Partition, vo04qt.Value as Value]
          PhysicalFilter [((vo04qt.Position % 2) = 0)]
            PhysicalSchemaScan [#schema.first() as vo04qt]
  Definition [totals]
    PhysicalMultiStatement
      PhysicalProject [doubled.Partition as doubled.Partition, AggRef(doubled.Sum(distinct doubled.Value)) as doubled.Sum(distinct doubled.Value), AggRef(doubled.Count(doubled.Value)) as doubled.Count(doubled.Value), AggRef(doubled.Sum(doubled.Value)) as doubled.Sum(doubled.Value)]
        PhysicalSingleKeyAggregate [key: Partition (Nullable`1)] [aggs: Sum(distinct Value), Count(Value), Sum(Value)]
          PhysicalCteRef [doubled as doubled]
      PhysicalProject [doubled.Partition as Partition, doubled.Sum(doubled.Value) as Total, doubled.Count(doubled.Value) as Present, doubled.Sum(distinct doubled.Value) as UniqueTotal]
        PhysicalCteRef [doubledScore as doubledScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [totals.Partition as Partition, totals.Total as Total, totals.Present as Present, totals.UniqueTotal as UniqueTotal, WindowRef(0) as AverageTotal]
        PhysicalWindow [Avg(idx:0; args: totals.Total)]
          PhysicalMaterialize
            PhysicalCteRef [totals as totals]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: CoverageRow]
      Partition: int? <- property Partition
      Value: int? <- property Value
    SourceEntity [vo04qt: CoverageRow]
      Position: int <- property Position
      Partition: int? <- property Partition
      Value: int? <- property Value
    Generated [Cte0Row0]
      Partition: int? <- field Partition
      Value: int? <- field Value
    TableRow [doubled]
      Partition: int? <- field Partition
      Value: int? <- field Value
    AggregateGroup [Cte1AggregateGroup; keys: 1; typed aggs: 3]
    Generated [Cte1Row0]
      Partition: int? <- field Partition
      Total: int? <- field Total
      Present: long <- field Present
      UniqueTotal: int? <- field UniqueTotal
    TableRow [totals]
      Partition: int? <- field Partition
      Total: int? <- field Total
      Present: long <- field Present
      UniqueTotal: int? <- field UniqueTotal
    Generated [ResultRow0]
      Partition: int? <- field Partition
      Total: int? <- field Total
      Present: long <- field Present
      UniqueTotal: int? <- field UniqueTotal
      AverageTotal: decimal? <- field AverageTotal

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From:cte0]
    SourceScan [ko3iko: CoverageRow] -> left_ko3ikoRows
    PhaseBoundary [Select:left]
    PhaseBoundary [Where:cte0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [ko3iko in left_ko3ikoRows]
      AppendRow [cte0 <- Cte0Row0(Partition: ko3iko.Partition, Value: ko3iko.Value)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [vo04qt: CoverageRow] -> right_vo04qtRows
    PhaseBoundary [Where:right]
    PhaseBoundary [Select:right]
    ChunkedForEach [vo04qt in right_vo04qtRows]
      If [((vo04qt.Position % 2) = 0)]
        AppendRow [cte0 <- Cte0Row0(Partition: vo04qt.Partition, Value: vo04qt.Value)]
    PhaseBoundary [End:right]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    CreateTable [cte1: Cte1Row0]
    CreateSingleKeyAggregateContext [cte1Groups: int? -> Cte1AggregateGroup]
    PhaseBoundary [From:cte1]
    ForEach [doubled in _cteRowResults.Slot0]
      GetOrAddSingleKeyAggregateGroup [cte1Group = cte1Groups[doubled.Partition] by Partition; typed: Cte1AggregateGroup]
      Let [value: int? = doubled.Value]
      TypedAggregateSet [Set(cte1Group.__agg0, value)]
      Let [value1: int? = doubled.Value]
      TypedAggregateSet [Set(cte1Group.__agg1, value1)]
      Let [value2: int? = doubled.Value]
      TypedAggregateSet [Set(cte1Group.__agg2, value2)]
    PhaseBoundary [GroupBy:cte1]
    EnsureCapacity [cte1 <- cte1GroupsToFinalize.Count]
    PhaseBoundary [Select:cte1]
    ForEach [cte1FinalGroup in cte1GroupsToFinalize]
      AppendRow [cte1 <- Cte1Row0(Partition: cte1FinalGroup.Partition, Total: doubled.Sum(doubled.Value), Present: doubled.Count(doubled.Value), UniqueTotal: doubled.Sum(distinct doubled.Value))]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    Materialize [_cteRowResults.Slot1 -> resultWindowRows]
    ComputeAvgWindowKernel[WholePartition] [resultAvgs <- resultWindowRows value totals.Total]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ForEachIndexed [windowIndex, totals in resultWindowRows]
      AppendShape [result <- ResultRow0(Partition: totals.Partition, Total: totals.Total, Present: totals.Present, UniqueTotal: totals.UniqueTotal, AverageTotal: resultAvgs[windowIndex])]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_P90_NativeSetAggregateWindow_Full
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
        private static readonly Column[] __columns_compiled_cte0_0 = new Column[]
        {
            new Column("Partition", typeof(int?), 0),
            new Column("Value", typeof(int?), 1)
        };
        private static readonly Column[] __columns_compiled_cte1_3 = new Column[]
        {
            new Column("Partition", typeof(int?), 0),
            new Column("Total", typeof(int?), 1),
            new Column("Present", typeof(long), 2),
            new Column("UniqueTotal", typeof(int?), 3)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("Partition", typeof(int?), 0),
            new Column("Total", typeof(int?), 1),
            new Column("Present", typeof(long), 2),
            new Column("UniqueTotal", typeof(int?), 3),
            new Column("AverageTotal", typeof(decimal?), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Partition", typeof(int?), 0), new Column("Value", typeof(int?), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_vo04qt_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Position", typeof(int), 0), new Column("Partition", typeof(int?), 1), new Column("Value", typeof(int?), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            ArgumentNullException.ThrowIfNull(profileRecorder);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0_Profiled(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken, profileRecorder), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0_Profiled(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder))
            {
                yield return __musoqShapeRow;
            }
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte1Row0>(_cteRowResults.Slot1);
                var resultAvgsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                var resultAvgs = new decimal? [resultWindowRows.Count];
                for (int resultAvgsPartitionSetIndex = 0; resultAvgsPartitionSetIndex < resultAvgsPartitions.PartitionCount; ++resultAvgsPartitionSetIndex)
                {
                    var resultAvgsPartitionStart = resultAvgsPartitions.GetStart(resultAvgsPartitionSetIndex);
                    var resultAvgsPartitionCount = resultAvgsPartitions.GetLength(resultAvgsPartitionSetIndex);
                    var resultAvgsPartitionIndices = resultAvgsPartitions.Indices;
                    Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgsState = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                    Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgsState);
                    for (int resultAvgsPartitionIndex = 0; resultAvgsPartitionIndex < resultAvgsPartitionCount; ++resultAvgsPartitionIndex)
                    {
                        var resultAvgsCurrentIndex = resultAvgsPartitionIndices[resultAvgsPartitionStart + resultAvgsPartitionIndex];
                        Cte1Row0 totals = resultWindowRows[resultAvgsCurrentIndex];
                        Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgsState, (int?)totals.Total);
                    }

                    var resultAvgsFinalValue = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgsState);
                    for (int resultAvgsPartitionIndex = 0; resultAvgsPartitionIndex < resultAvgsPartitionCount; ++resultAvgsPartitionIndex)
                    {
                        var resultAvgsCurrentIndex = resultAvgsPartitionIndices[resultAvgsPartitionStart + resultAvgsPartitionIndex];
                        resultAvgs[resultAvgsCurrentIndex] = resultAvgsFinalValue;
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(resultWindowRows.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte1Row0 totals = resultWindowRows[windowIndex];
                    __musoqFinalShapeRows.Add(new ResultRow0(totals.Partition, totals.Total, totals.Present, totals.UniqueTotal, (decimal?)resultAvgs[windowIndex]));
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

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0_Profiled(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token, QueryProfileRecorder profileRecorder)
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
                    var __musoqFinalShapeRows = new List<ResultRow0>();
                    var __op34Handle = profileRecorder?.GetOperatorHandle("op34", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op35Handle = profileRecorder?.GetOperatorHandle("op35", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op78Handle = profileRecorder?.GetOperatorHandle("op78", "Materialize") ?? OperatorProfileHandle.None;
                    var __op79Handle = profileRecorder?.GetOperatorHandle("op79", "ComputeAvgWindowKernel") ?? OperatorProfileHandle.None;
                    var __op81Handle = profileRecorder?.GetOperatorHandle("op81", "PhaseBoundary") ?? OperatorProfileHandle.None;
                    var __op82Handle = profileRecorder?.GetOperatorHandle("op82", "ForEachIndexed") ?? OperatorProfileHandle.None;
                    var __op83Handle = profileRecorder?.GetOperatorHandle("op83", "AppendShape") ?? OperatorProfileHandle.None;
                    long __op83OutputRows = 0L;
                    var __op34Scope = profileRecorder?.BeginOperatorValue(__op34Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Begin);
                    __op34Scope.Dispose();
                    var __op35Scope = profileRecorder?.BeginOperatorValue(__op35Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.From);
                    __op35Scope.Dispose();
                    _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, profileRecorder, _cteRowResults);
                    var __op78Scope = profileRecorder?.BeginOperatorValue(__op78Handle) ?? OperatorProfileValueScope.None;
                    var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte1Row0>(_cteRowResults.Slot1);
                    __op78Scope.AddOutputRows(resultWindowRows.Count);
                    __op78Scope.Dispose();
                    var __op79Scope = profileRecorder?.BeginOperatorValue(__op79Handle) ?? OperatorProfileValueScope.None;
                    var resultAvgsPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                    var resultAvgs = new decimal? [resultWindowRows.Count];
                    for (int resultAvgsPartitionSetIndex = 0; resultAvgsPartitionSetIndex < resultAvgsPartitions.PartitionCount; ++resultAvgsPartitionSetIndex)
                    {
                        var resultAvgsPartitionStart = resultAvgsPartitions.GetStart(resultAvgsPartitionSetIndex);
                        var resultAvgsPartitionCount = resultAvgsPartitions.GetLength(resultAvgsPartitionSetIndex);
                        var resultAvgsPartitionIndices = resultAvgsPartitions.Indices;
                        Musoq.Plugins.AverageKernel<int, decimal>.State resultAvgsState = default(Musoq.Plugins.AverageKernel<int, decimal>.State)!;
                        Musoq.Plugins.AverageKernel<int, decimal>.Reset(ref resultAvgsState);
                        for (int resultAvgsPartitionIndex = 0; resultAvgsPartitionIndex < resultAvgsPartitionCount; ++resultAvgsPartitionIndex)
                        {
                            var resultAvgsCurrentIndex = resultAvgsPartitionIndices[resultAvgsPartitionStart + resultAvgsPartitionIndex];
                            Cte1Row0 totals = resultWindowRows[resultAvgsCurrentIndex];
                            Musoq.Plugins.AverageKernel<int, decimal>.Set(ref resultAvgsState, (int?)totals.Total);
                        }

                        var resultAvgsFinalValue = Musoq.Plugins.AverageKernel<int, decimal>.Get(in resultAvgsState);
                        for (int resultAvgsPartitionIndex = 0; resultAvgsPartitionIndex < resultAvgsPartitionCount; ++resultAvgsPartitionIndex)
                        {
                            var resultAvgsCurrentIndex = resultAvgsPartitionIndices[resultAvgsPartitionStart + resultAvgsPartitionIndex];
                            resultAvgs[resultAvgsCurrentIndex] = resultAvgsFinalValue;
                        }
                    }

                    __op79Scope.Dispose();
                    __musoqFinalShapeRows.EnsureCapacity(resultWindowRows.Count);
                    var __op81Scope = profileRecorder?.BeginOperatorValue(__op81Handle) ?? OperatorProfileValueScope.None;
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    __op81Scope.Dispose();
                    long __op82InputRows = 0L;
                    long __op82OutputRows = 0L;
                    var __op82Scope = profileRecorder?.BeginOperatorValue(__op82Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
                        {
                            if ((windowIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte1Row0 totals = resultWindowRows[windowIndex];
                            __op82InputRows++;
                            __op82OutputRows++;
                            __musoqFinalShapeRows.Add(new ResultRow0(totals.Partition, totals.Total, totals.Present, totals.UniqueTotal, (decimal?)resultAvgs[windowIndex]));
                            __op83OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op82Scope.AddInputRows(__op82InputRows);
                        __op82Scope.AddOutputRows(__op82OutputRows);
                        __op82Scope.Dispose();
                    }

                    if (__op83OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op83Handle, __op83OutputRows);
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
                var cte0 = new List<Cte0Row0>();
                var __left_ko3ikoSchema = provider.GetSchema("#schema");
                var left_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__left_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_1, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var left_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(left_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : left_ko3ikoRowsSource.Chunks;
                foreach (var ko3ikoChunk in left_ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                    }
                }

                var __right_vo04qtSchema = provider.GetSchema("#schema");
                var right_vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__right_vo04qtSchema, "first", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_vo04qt_2, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var right_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(right_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : right_vo04qtRowsSource.Chunks;
                foreach (var vo04qtChunk in right_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                if (((vo04qt.Position % 2) == 0))
                                {
                                    cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                if (((vo04qt.Position % 2) == 0))
                                {
                                    cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
                                }
                            }

                            continue;
                        }
                    }

                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                    {
                        if ((vo04qtIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vo04qt = vo04qtChunk[vo04qtIndex];
                        if (((vo04qt.Position % 2) == 0))
                        {
                            cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
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
                    var __op37Handle = profileRecorder?.GetOperatorHandle("op37", "CreateTable") ?? OperatorProfileHandle.None;
                    var __op41Handle = profileRecorder?.GetOperatorHandle("op41", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op45Handle = profileRecorder?.GetOperatorHandle("op45", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op46Handle = profileRecorder?.GetOperatorHandle("op46", "AppendRow") ?? OperatorProfileHandle.None;
                    var __op50Handle = profileRecorder?.GetOperatorHandle("op50", "SourceScan") ?? OperatorProfileHandle.None;
                    var __op53Handle = profileRecorder?.GetOperatorHandle("op53", "ChunkedForEach") ?? OperatorProfileHandle.None;
                    var __op55Handle = profileRecorder?.GetOperatorHandle("op55", "AppendRow") ?? OperatorProfileHandle.None;
                    long __op46OutputRows = 0L;
                    long __op55OutputRows = 0L;
                    var __op37Scope = profileRecorder?.BeginOperatorValue(__op37Handle) ?? OperatorProfileValueScope.None;
                    var cte0 = new List<Cte0Row0>();
                    __op37Scope.Dispose();
                    var __op41Scope = profileRecorder?.BeginOperatorValue(__op41Handle) ?? OperatorProfileValueScope.None;
                    var __left_ko3ikoSchema = provider.GetSchema("#schema");
                    var left_ko3ikoRowsProfile = profileRecorder?.CreateSourceRecorder("ko3iko");
                    var left_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__left_ko3ikoSchema, "first", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_1, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress, left_ko3ikoRowsProfile == null ? SourceDiagnostics.None : left_ko3ikoRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var left_ko3ikoRows = left_ko3ikoRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(left_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : left_ko3ikoRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(left_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : left_ko3ikoRowsSource.Chunks, left_ko3ikoRowsProfile);
                    __op41Scope.Dispose();
                    long __op45InputRows = 0L;
                    long __op45OutputRows = 0L;
                    var __op45Scope = profileRecorder?.BeginOperatorValue(__op45Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var ko3ikoChunk in left_ko3ikoRows)
                        {
                            if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> ko3ikoChunkView)
                            {
                                if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] ko3ikoChunkViewArray)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op45InputRows++;
                                        __op45OutputRows++;
                                        cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                                        __op46OutputRows += 1;
                                    }

                                    continue;
                                }

                                if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> ko3ikoChunkViewList)
                                {
                                    int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                                    {
                                        if ((ko3ikoIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                        __op45InputRows++;
                                        __op45OutputRows++;
                                        cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                                        __op46OutputRows += 1;
                                    }

                                    continue;
                                }
                            }

                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunk[ko3ikoIndex];
                                __op45InputRows++;
                                __op45OutputRows++;
                                cte0.Add(new Cte0Row0(ko3iko.Partition, ko3iko.Value));
                                __op46OutputRows += 1;
                            }
                        }
                    }
                    finally
                    {
                        __op45Scope.AddInputRows(__op45InputRows);
                        __op45Scope.AddOutputRows(__op45OutputRows);
                        __op45Scope.Dispose();
                    }

                    var __op50Scope = profileRecorder?.BeginOperatorValue(__op50Handle) ?? OperatorProfileValueScope.None;
                    var __right_vo04qtSchema = provider.GetSchema("#schema");
                    var right_vo04qtRowsProfile = profileRecorder?.CreateSourceRecorder("vo04qt");
                    var right_vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Open(__right_vo04qtSchema, "first", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_vo04qt_2, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress, right_vo04qtRowsProfile == null ? SourceDiagnostics.None : right_vo04qtRowsProfile.CreateDiagnostics()), global::Musoq.Schema.SourceArguments.Empty);
                    var right_vo04qtRows = right_vo04qtRowsProfile == null ? __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(right_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : right_vo04qtRowsSource.Chunks : ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>.Create(__musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow>(right_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : right_vo04qtRowsSource.Chunks, right_vo04qtRowsProfile);
                    __op50Scope.Dispose();
                    long __op53InputRows = 0L;
                    long __op53OutputRows = 0L;
                    var __op53Scope = profileRecorder?.BeginOperatorValue(__op53Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var vo04qtChunk in right_vo04qtRows)
                        {
                            if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> vo04qtChunkView)
                            {
                                if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow[] vo04qtChunkViewArray)
                                {
                                    int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                                    {
                                        if ((vo04qtIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                        __op53InputRows++;
                                        __op53OutputRows++;
                                        if (((vo04qt.Position % 2) == 0))
                                        {
                                            cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
                                            __op55OutputRows += 1;
                                        }
                                    }

                                    continue;
                                }

                                if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.LibraryExecutionCoverageQueryTests.CoverageRow> vo04qtChunkViewList)
                                {
                                    int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                                    {
                                        if ((vo04qtIndex & 1023) == 0)
                                        {
                                            token.ThrowIfCancellationRequested();
                                        }

                                        var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                        __op53InputRows++;
                                        __op53OutputRows++;
                                        if (((vo04qt.Position % 2) == 0))
                                        {
                                            cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
                                            __op55OutputRows += 1;
                                        }
                                    }

                                    continue;
                                }
                            }

                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunk[vo04qtIndex];
                                __op53InputRows++;
                                __op53OutputRows++;
                                if (((vo04qt.Position % 2) == 0))
                                {
                                    cte0.Add(new Cte0Row0(vo04qt.Partition, vo04qt.Value));
                                    __op55OutputRows += 1;
                                }
                            }
                        }
                    }
                    finally
                    {
                        __op53Scope.AddInputRows(__op53InputRows);
                        __op53Scope.AddOutputRows(__op53OutputRows);
                        __op53Scope.Dispose();
                    }

                    if (__op46OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op46Handle, __op46OutputRows);
                    if (__op55OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op55Handle, __op55OutputRows);
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
        private static List<Cte1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var cte1 = new List<Cte1Row0>();
                var cte1GroupsToFinalize = new List<Cte1AggregateGroup>();
                var cte1Groups = new Dictionary<int?, Cte1AggregateGroup>();
                Cte1AggregateGroup cte1NullGroup = null;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 doubled = __storedTable0Rows[__storedTable0Index];
                    int? groupKey = doubled.Partition;
                    Cte1AggregateGroup cte1Group = null;
                    if (groupKey != null)
                    {
                        ref var cte1GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1Groups, groupKey, out var cte1GroupExists);
                        if (!cte1GroupExists)
                        {
                            cte1GroupRef = new Cte1AggregateGroup(groupKey);
                            cte1GroupsToFinalize.Add(cte1GroupRef);
                        }

                        cte1Group = cte1GroupRef;
                    }
                    else
                    {
                        if (cte1NullGroup == null)
                        {
                            cte1NullGroup = new Cte1AggregateGroup(null);
                            cte1GroupsToFinalize.Add(cte1NullGroup);
                        }

                        cte1Group = cte1NullGroup;
                    }

                    int? value = doubled.Value;
                    Musoq.Plugins.SumDistinctAggregateKernel<int>.Set(ref cte1Group.__agg0, (int?)value);
                    int? value1 = doubled.Value;
                    if (((int?)value1).HasValue)
                    {
                        cte1Group.__agg1.Count = checked(cte1Group.__agg1.Count + 1L);
                    }

                    int? value2 = doubled.Value;
                    {
                        var __agg2Input = (int?)value2;
                        if (__agg2Input.HasValue)
                        {
                            var __agg2Current = __agg2Input.GetValueOrDefault();
                            cte1Group.__agg2.Value = cte1Group.__agg2.HasValue ? checked(cte1Group.__agg2.Value + __agg2Current) : __agg2Current;
                            cte1Group.__agg2.HasValue = true;
                        }
                    }
                }

                cte1.EnsureCapacity(cte1GroupsToFinalize.Count);
                foreach (var cte1FinalGroup in cte1GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte1.Add(new Cte1Row0(cte1FinalGroup.__key0, cte1FinalGroup.__agg2.HasValue ? (int?)cte1FinalGroup.__agg2.Value : null, cte1FinalGroup.__agg1.Count, Musoq.Plugins.SumDistinctAggregateKernel<int>.Get(in cte1FinalGroup.__agg0)));
                }

                return cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, Musoq.Evaluator.Diagnostics.QueryProfileRecorder profileRecorder, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __profileScopeDepth = profileRecorder?.GetCurrentOperatorScopeDepth() ?? 0;
                try
                {
                    var __op60Handle = profileRecorder?.GetOperatorHandle("op60", "CreateTable") ?? OperatorProfileHandle.None;
                    var __op61Handle = profileRecorder?.GetOperatorHandle("op61", "CreateSingleKeyAggregateContext") ?? OperatorProfileHandle.None;
                    var __op63Handle = profileRecorder?.GetOperatorHandle("op63", "ForEach") ?? OperatorProfileHandle.None;
                    var __op64Handle = profileRecorder?.GetOperatorHandle("op64", "GetOrAddSingleKeyAggregateGroup") ?? OperatorProfileHandle.None;
                    var __op66Handle = profileRecorder?.GetOperatorHandle("op66", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op68Handle = profileRecorder?.GetOperatorHandle("op68", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op70Handle = profileRecorder?.GetOperatorHandle("op70", "TypedAggregateSet") ?? OperatorProfileHandle.None;
                    var __op72Handle = profileRecorder?.GetOperatorHandle("op72", "EnsureCapacity") ?? OperatorProfileHandle.None;
                    var __op74Handle = profileRecorder?.GetOperatorHandle("op74", "ForEach") ?? OperatorProfileHandle.None;
                    var __op75Handle = profileRecorder?.GetOperatorHandle("op75", "AppendRow") ?? OperatorProfileHandle.None;
                    long __op64InputRows = 0L;
                    long __op64OutputRows = 0L;
                    long __op66OutputRows = 0L;
                    long __op68OutputRows = 0L;
                    long __op70OutputRows = 0L;
                    long __op75OutputRows = 0L;
                    var __op60Scope = profileRecorder?.BeginOperatorValue(__op60Handle) ?? OperatorProfileValueScope.None;
                    var cte1 = new List<Cte1Row0>();
                    __op60Scope.Dispose();
                    var __op61Scope = profileRecorder?.BeginOperatorValue(__op61Handle) ?? OperatorProfileValueScope.None;
                    var cte1GroupsToFinalize = new List<Cte1AggregateGroup>();
                    var cte1Groups = new Dictionary<int?, Cte1AggregateGroup>();
                    Cte1AggregateGroup cte1NullGroup = null;
                    __op61Scope.Dispose();
                    long __op63InputRows = 0L;
                    long __op63OutputRows = 0L;
                    var __op63Scope = profileRecorder?.BeginOperatorValue(__op63Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        var __storedTable0Rows = _cteRowResults.Slot0;
                        for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                        {
                            if ((__storedTable0Index & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            Cte0Row0 doubled = __storedTable0Rows[__storedTable0Index];
                            __op63InputRows++;
                            __op63OutputRows++;
                            __op64InputRows += 1;
                            int? groupKey = doubled.Partition;
                            Cte1AggregateGroup cte1Group = null;
                            if (groupKey != null)
                            {
                                ref var cte1GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1Groups, groupKey, out var cte1GroupExists);
                                if (!cte1GroupExists)
                                {
                                    cte1GroupRef = new Cte1AggregateGroup(groupKey);
                                    cte1GroupsToFinalize.Add(cte1GroupRef);
                                }

                                cte1Group = cte1GroupRef;
                            }
                            else
                            {
                                if (cte1NullGroup == null)
                                {
                                    cte1NullGroup = new Cte1AggregateGroup(null);
                                    cte1GroupsToFinalize.Add(cte1NullGroup);
                                }

                                cte1Group = cte1NullGroup;
                            }

                            __op64OutputRows += 1;
                            int? value = doubled.Value;
                            Musoq.Plugins.SumDistinctAggregateKernel<int>.Set(ref cte1Group.__agg0, (int?)value);
                            __op66OutputRows += 1;
                            int? value1 = doubled.Value;
                            if (((int?)value1).HasValue)
                            {
                                cte1Group.__agg1.Count = checked(cte1Group.__agg1.Count + 1L);
                            }

                            __op68OutputRows += 1;
                            int? value2 = doubled.Value;
                            {
                                var __agg2Input = (int?)value2;
                                if (__agg2Input.HasValue)
                                {
                                    var __agg2Current = __agg2Input.GetValueOrDefault();
                                    cte1Group.__agg2.Value = cte1Group.__agg2.HasValue ? checked(cte1Group.__agg2.Value + __agg2Current) : __agg2Current;
                                    cte1Group.__agg2.HasValue = true;
                                }
                            }

                            __op70OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op63Scope.AddInputRows(__op63InputRows);
                        __op63Scope.AddOutputRows(__op63OutputRows);
                        __op63Scope.Dispose();
                    }

                    var __op72Scope = profileRecorder?.BeginOperatorValue(__op72Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        cte1.EnsureCapacity(cte1GroupsToFinalize.Count);
                    }
                    finally
                    {
                        __op72Scope.Dispose();
                    }

                    long __op74InputRows = 0L;
                    long __op74OutputRows = 0L;
                    var __op74Scope = profileRecorder?.BeginOperatorValue(__op74Handle) ?? OperatorProfileValueScope.None;
                    try
                    {
                        foreach (var cte1FinalGroup in cte1GroupsToFinalize)
                        {
                            token.ThrowIfCancellationRequested();
                            __op74InputRows++;
                            __op74OutputRows++;
                            cte1.Add(new Cte1Row0(cte1FinalGroup.__key0, cte1FinalGroup.__agg2.HasValue ? (int?)cte1FinalGroup.__agg2.Value : null, cte1FinalGroup.__agg1.Count, Musoq.Plugins.SumDistinctAggregateKernel<int>.Get(in cte1FinalGroup.__agg0)));
                            __op75OutputRows += 1;
                        }
                    }
                    finally
                    {
                        __op74Scope.AddInputRows(__op74InputRows);
                        __op74Scope.AddOutputRows(__op74OutputRows);
                        __op74Scope.Dispose();
                    }

                    if (__op64InputRows > 0L)
                        profileRecorder?.AddOperatorInputRows(__op64Handle, __op64InputRows);
                    if (__op64OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op64Handle, __op64OutputRows);
                    if (__op66OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op66Handle, __op66OutputRows);
                    if (__op68OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op68Handle, __op68OutputRows);
                    if (__op70OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op70Handle, __op70OutputRows);
                    if (__op75OutputRows > 0L)
                        profileRecorder?.AddOperatorOutputRows(__op75Handle, __op75OutputRows);
                    return cte1;
                }
                catch (Exception __profileException)when (profileRecorder != null && profileRecorder.RecordActiveOperatorException(__profileException, __profileScopeDepth))
                {
                    profileRecorder.DisposeActiveOperatorScopes(__profileScopeDepth);
                    throw;
                }
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int? __value0, int? __value1)
            {
                Partition = __value0;
                Value = __value1;
            }

            public int? Partition { get; }
            public int? Value { get; }
        }

        private sealed class Cte1AggregateGroup
        {
            public Musoq.Plugins.SumDistinctAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.CountNullableAggregateKernel<int>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<int>.State __agg2;
            public readonly int? __key0;
            public Cte1AggregateGroup(int? __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumDistinctAggregateKernel<int>.Reset(ref this.__agg0);
                Musoq.Plugins.CountNullableAggregateKernel<int>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<int>.Reset(ref this.__agg2);
            }

            public void MergeFrom(Cte1AggregateGroup source)
            {
                Musoq.Plugins.SumDistinctAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountNullableAggregateKernel<int>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<int>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int? __value0, int? __value1, long __value2, int? __value3)
            {
                Partition = __value0;
                Total = __value1;
                Present = __value2;
                UniqueTotal = __value3;
            }

            public int? Partition { get; }
            public long Present { get; }
            public int? Total { get; }
            public int? UniqueTotal { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int? __value0, int? __value1, long __value2, int? __value3, decimal? __value4)
            {
                Partition = __value0;
                Total = __value1;
                Present = __value2;
                UniqueTotal = __value3;
                AverageTotal = __value4;
            }

            public decimal? AverageTotal { get; private set; }
            public override int Count => 5;
            public int? Partition { get; private set; }
            public long Present { get; private set; }
            public int? Total { get; private set; }
            public int? UniqueTotal { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Partition = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.UniqueTotal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AverageTotal = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Partition),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this.UniqueTotal),
                4 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.AverageTotal),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Partition" => true,
                "Total" => true,
                "Present" => true,
                "UniqueTotal" => true,
                "AverageTotal" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Partition, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Present, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.UniqueTotal, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(4, this.AverageTotal, ref visitor);
            }
        }
    }
}
