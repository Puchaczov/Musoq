// === Parsed Query ===
/*
select r.Id as Id, d.Id as Match from perf.rows() r inner join perf.right() d on r.StringKey = d.StringKey
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.StringKey as r.StringKey, d.Id as d.Id, d.StringKey as d.StringKey]
    Join [Inner] [(r.StringKey = d.StringKey)]
      SchemaScan [#perf.rows() as r]
      SchemaScan [#perf.right() as d]
  Project [r.Id as Id, d.Id as Match]
    CteRef [rd as rd]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, d.Id as d.Id]
    PhysicalHashJoin [Inner] [build: d.StringKey] [probe: r.StringKey]
      PhysicalSchemaScan [#perf.rows() as r]
      PhysicalSchemaScan [#perf.right() as d]
  PhysicalProject [r.Id as Id, d.Id as Match]
    PhysicalCteRef [rd as rd]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
      StringKey: string <- property StringKey
    SourceEntity [d: PerformanceRow]
      Id: int <- property Id
      StringKey: string <- property StringKey
    Generated [ResultRow0]
      Id: int <- field Id
      Match: int <- field Match

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [r: PerformanceRow] -> rRows
    SourceScan [d: PerformanceRow] -> dRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateHash [dHash: string -> PerformanceRow]
    ChunkedForEach [d in dRows]
      HashAdd [dHash[d.StringKey] += d]
    ChunkedForEach [r in rRows]
      HashProbe [dHash[r.StringKey] -> dHashMatches]
        ForEach [d in dHashMatches]
          AppendShape [result <- ResultRow0(Id: r.Id, Match: d.Id)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B07_StringKeyJoin_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("StringKey", typeof(string), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Match", typeof(int), 1)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __rSchema = provider.GetSchema("#perf");
                    var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                    var __dSchema = provider.GetSchema("#perf");
                    var dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__dSchema, "right", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(dRowsSource.Chunks, __musoqProgressContext, "d:1") : dRowsSource.Chunks;
                    var dHash = new Dictionary<string, HashJoinBucket<Musoq.Benchmarks.Scenarios.PerformanceRow>>();
                    foreach (var dChunk in dRows)
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
                                    string key = d.StringKey;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.PerformanceRow>(d);
                                        }
                                        else
                                        {
                                            matches.Add(d);
                                        }
                                    }
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
                                    string key = d.StringKey;
                                    if (key == null)
                                        continue;
                                    {
                                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                                        if (!matchesExists)
                                        {
                                            matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.PerformanceRow>(d);
                                        }
                                        else
                                        {
                                            matches.Add(d);
                                        }
                                    }
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
                            string key = d.StringKey;
                            if (key == null)
                                continue;
                            {
                                ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(dHash, key, out var matchesExists);
                                if (!matchesExists)
                                {
                                    matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.PerformanceRow>(d);
                                }
                                else
                                {
                                    matches.Add(d);
                                }
                            }
                        }
                    }

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
                                    string key = r.StringKey;
                                    if (key != null && dHash.TryGetValue(key, out var dHashMatches))
                                    {
                                        foreach (var d in dHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(r.Id, d.Id));
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
                                    string key = r.StringKey;
                                    if (key != null && dHash.TryGetValue(key, out var dHashMatches))
                                    {
                                        foreach (var d in dHashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(r.Id, d.Id));
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
                            string key = r.StringKey;
                            if (key != null && dHash.TryGetValue(key, out var dHashMatches))
                            {
                                foreach (var d in dHashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __musoqFinalShapeRows.Add(new ResultRow0(r.Id, d.Id));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public int Match { get; private set; }

            public ResultRow0(int __value0, int __value1)
            {
                Id = __value0;
                Match = __value1;
            }

            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Match, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Match),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Match" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Match = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }
    }
}
