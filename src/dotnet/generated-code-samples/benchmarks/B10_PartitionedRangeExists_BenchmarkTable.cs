// === Parsed Query ===
/*
select r.Id as Id from perf.rows() r where exists (select d.Id from perf.right() d where d.Partition = r.Partition and d.Id > r.Id)
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [1 as _sq_1_key, d.Partition as _sq_1_corr_0, d.Id as _sq_1_corr_1]
        SchemaScan [#perf.right() as d]
  Query
    MultiStatement
      Project [r.Id as Id]
        Join [LeftSemi] [((1 = _sq_1._sq_1_key) AND ((_sq_1._sq_1_corr_0 = r.Partition) AND (_sq_1._sq_1_corr_1 > r.Id)))]
          SchemaScan [#perf.rows() as r]
          CteRef [_sq_1 as _sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_1_key, d.Partition as _sq_1_corr_0, d.Id as _sq_1_corr_1]
        PhysicalSchemaScan [#perf.right() as d]
  Query
    PhysicalMultiStatement
      PhysicalProject [r.Id as Id]
        PhysicalSortMergeJoin [LeftSemi] [left: r.Id] [right: _sq_1._sq_1_corr_1] [op: <] [partitions: r.Partition = _sq_1._sq_1_corr_0] [residual: ((1 = _sq_1._sq_1_key) AND ((_sq_1._sq_1_corr_0 = r.Partition) AND (_sq_1._sq_1_corr_1 > r.Id)))]
          PhysicalSchemaScan [#perf.rows() as r]
          PhysicalCteRef [_sq_1 as _sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [d: PerformanceRow]
      Id: int <- property Id
      Partition: int <- property Partition
    Generated [Cte0Row0]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: int <- field _sq_1_corr_0
      _sq_1_corr_1: int <- field _sq_1_corr_1
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
      Partition: int <- property Partition
    TableRow [_sq_1]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: int <- field _sq_1_corr_0
      _sq_1_corr_1: int <- field _sq_1_corr_1
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [d: PerformanceRow] -> cte0_dRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [d in cte0_dRows]
      AppendRow [cte0 <- Cte0Row0(_sq_1_key: 1, _sq_1_corr_0: d.Partition, _sq_1_corr_1: d.Id)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [From]
    SourceScan [r: PerformanceRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateRangeIndex [resultRangeIndex <- _cteRowResults.Slot0 by _sq_1RangeCandidate._sq_1_corr_0, _sq_1RangeCandidate._sq_1_corr_1 <]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      RangeProbe [_sq_1 <- resultRangeIndex where r.Partition = _sq_1RangeCandidate._sq_1_corr_0 and r.Id]
        Let [id: int = r.Id]
        If [((1 = _sq_1._sq_1_key) AND ((_sq_1._sq_1_corr_0 = r.Partition) AND (_sq_1._sq_1_corr_1 > id)))]
          AppendShape [result <- ResultRow0(Id: id)]
          Break
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B10_PartitionedRangeExists_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Partition", typeof(int), 1) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("_sq_1_key", typeof(int), 0),
            new Column("_sq_1_corr_0", typeof(int), 1),
            new Column("_sq_1_corr_1", typeof(int), 2)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("Id", typeof(int), 0)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#perf");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var resultRangeIndex = EvaluationHelper.CreateRangeJoinIndex<Cte0Row0, int, int>(_cteRowResults.Slot0, (_sq_1RangeCandidate) => _sq_1RangeCandidate._sq_1_corr_0, (_sq_1RangeCandidate) => _sq_1RangeCandidate._sq_1_corr_1, Musoq.Evaluator.IR.Expressions.BinaryOpKind.LessThan);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Benchmarks.Scenarios.PerformanceRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                foreach (var _sq_1 in resultRangeIndex.Find<int>(r.Partition, r.Id))
                                {
                                    int id = r.Id;
                                    if (((1 == _sq_1._sq_1_key) && ((_sq_1._sq_1_corr_0 == r.Partition) && (_sq_1._sq_1_corr_1 > id))))
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(id));
                                        break;
                                    }
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                foreach (var _sq_1 in resultRangeIndex.Find<int>(r.Partition, r.Id))
                                {
                                    int id = r.Id;
                                    if (((1 == _sq_1._sq_1_key) && ((_sq_1._sq_1_corr_0 == r.Partition) && (_sq_1._sq_1_corr_1 > id))))
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(id));
                                        break;
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        foreach (var _sq_1 in resultRangeIndex.Find<int>(r.Partition, r.Id))
                        {
                            int id = r.Id;
                            if (((1 == _sq_1._sq_1_key) && ((_sq_1._sq_1_corr_0 == r.Partition) && (_sq_1._sq_1_corr_1 > id))))
                            {
                                __musoqFinalShapeRows.Add(new ResultRow0(id));
                                break;
                            }
                        }
                    }
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_dSchema = provider.GetSchema("#perf");
                var cte0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__cte0_dSchema, "right", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(cte0_dRowsSource.Chunks, __musoqProgressContext, "d:2") : cte0_dRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var dChunk in cte0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.PerformanceRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Benchmarks.Scenarios.PerformanceRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                cte0.Add(new Cte0Row0(1, d.Partition, d.Id));
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.PerformanceRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                cte0.Add(new Cte0Row0(1, d.Partition, d.Id));
                            }

                            continue;
                        }
                    }

                    for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                    {
                        if ((dIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var d = dChunk[dIndex];
                        cte0.Add(new Cte0Row0(1, d.Partition, d.Id));
                    }
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0Row0
        {
            public int _sq_1_key { get; }
            public int _sq_1_corr_0 { get; }
            public int _sq_1_corr_1 { get; }

            public Cte0Row0(int __value0, int __value1, int __value2)
            {
                _sq_1_key = __value0;
                _sq_1_corr_0 = __value1;
                _sq_1_corr_1 = __value2;
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }

            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }
    }
}
