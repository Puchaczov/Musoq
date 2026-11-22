// === Parsed Query ===
/*
with classified as (select case when r is missing then 'right-only' when d is missing then 'missing' else 'matched' end as State, r.Optional ?? -1 as Fallback
from bench.rows() r
full outer join bench.reconcile() d on r.Key = d.Key) select State, Fallback, Count(*) as Count
from classified
group by State, Fallback
order by State, Fallback
*/

// === Logical Plan ===
/*
Cte
  Definition [classified]
    MultiStatement
      Project [r.Key as r.Key, r.Optional as r.Optional, d.Key as d.Key]
        Join [FullOuter] [(r.Key = d.Key)]
          SchemaScan [#bench.rows() as r]
          SchemaScan [#bench.reconcile() as d]
      Project [CASE WHEN r IS MISSING THEN 'right-only' WHEN d IS MISSING THEN 'missing' ELSE 'matched' END as State, COALESCE(r.Optional, -1) as Fallback]
        CteRef [rd as rd]
  Query
    MultiStatement
      Project [classified.Fallback as classified.Fallback, classified.State as classified.State, AggRef(classified.Count(*)) as classified.Count(*)]
        Aggregate [keys: State, Fallback] [aggs: Count(*)]
          CteRef [classified as classified]
      Sort [classified.State, classified.Fallback]
        Project [classified.State as State, classified.Fallback as Fallback, classified.Count(*) as Count]
          CteRef [classifiedScore as classifiedScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [classified]
    PhysicalMultiStatement
      PhysicalProject [r.Key as r.Key, r.Optional as r.Optional, d.Key as d.Key]
        PhysicalHashJoin [FullOuter] [build: d.Key] [probe: r.Key]
          PhysicalSchemaScan [#bench.rows() as r]
          PhysicalSchemaScan [#bench.reconcile() as d]
      PhysicalProject [CASE WHEN r IS MISSING THEN 'right-only' WHEN d IS MISSING THEN 'missing' ELSE 'matched' END as State, COALESCE(r.Optional, -1) as Fallback]
        PhysicalCteRef [rd as rd]
  Query
    PhysicalMultiStatement
      PhysicalProject [classified.Fallback as classified.Fallback, classified.State as classified.State, AggRef(classified.Count(*)) as classified.Count(*)]
        PhysicalValueTupleAggregate [keys: State, Fallback] [aggs: Count(*)]
          PhysicalCteRef [classified as classified]
      PhysicalSort [classified.State, classified.Fallback]
        PhysicalProject [classified.State as State, classified.Fallback as Fallback, classified.Count(*) as Count]
          PhysicalCteRef [classifiedScore as classifiedScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
      Optional: int? <- property Optional
    SourceEntity [d: FixtureRow]
      Key: int <- property Key
    Generated [Cte0Row0]
      State: string <- field State
      Fallback: int <- field Fallback
    TableRow [classified]
      State: string <- field State
      Fallback: int <- field Fallback
    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 1]
    Generated [ResultRow0]
      State: string <- field State
      Fallback: int <- field Fallback
      Count: long <- field Count_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [r: FixtureRow] -> cte0_rRows
    SourceScan [d: FixtureRow] -> cte0_dRows
    CreateTable [cte0: Cte0Row0]
    MaterializeChunked [cte0_dRows -> dRowsBuffer]
    CreateHash [cte0DHash: int? -> IndexedHashJoinRow<FixtureRow>; capacity: dRowsBuffer.Count]
    CreateBooleanArray [cte0DHashBuildMatched <- dRowsBuffer.Count]
    ForEachIndexed [dIndex, d in dRowsBuffer]
      Let [dIndexed: IndexedHashJoinRow<FixtureRow> = IndexedHashRow(d, dIndex)]
      HashAdd [cte0DHash[d.Key] += dIndexed]
    ChunkedForEach [r in cte0_rRows]
      HashProbe [cte0DHash[r.Key] -> cte0DHashMatches] [match: cte0DHashHasMatch]
        ForEach [dIndexed in cte0DHashMatches]
          Let [d: FixtureRow = dIndexed.Row]
          Let [dIndex: int = dIndexed.Index]
          Assign [cte0DHashHasMatch = TRUE]
          ArrayAssign [cte0DHashBuildMatched[dIndex] = TRUE]
          AppendRow [cte0 <- Cte0Row0(State: CASE WHEN FALSE THEN 'right-only' WHEN FALSE THEN 'missing' ELSE 'matched' END, Fallback: COALESCE(r.Optional, -1))]
      HashProbeNoMatch
        AppendRow [cte0 <- Cte0Row0(State: CASE WHEN FALSE THEN 'right-only' WHEN TRUE THEN 'missing' ELSE 'matched' END, Fallback: COALESCE(r.Optional, -1))]
    ForEachIndexed [dIndex, d in dRowsBuffer]
      If [NOT cte0DHashBuildMatched[dIndex]]
        AppendRow [cte0 <- Cte0Row0(State: CASE WHEN TRUE THEN 'right-only' WHEN FALSE THEN 'missing' ELSE 'matched' END, Fallback: -1)]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Select:cte0]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateValueTupleAggregateContext [groups: (string, int) -> ResultAggregateGroup]
    ForEach [classified in _cteRowResults.Slot0]
      GetOrAddValueTupleAggregateGroup [group = groups[(classified.State, classified.Fallback)] by State, Fallback; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(State: finalGroup.State, Fallback: finalGroup.Fallback, Count: classified.Count(*))]
    SortOwnedBuffer [result by State ASC, Fallback ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X03_Reconciliation_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Optional", typeof(int?), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_cte0_2 = new Column[]
        {
            new Column("State", typeof(string), 0),
            new Column("Fallback", typeof(int), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("State", typeof(string), 0),
            new Column("Fallback", typeof(int), 1),
            new Column("Count", typeof(long), 2)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<(string, int), ResultAggregateGroup>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 classified = __storedTable0Rows[__storedTable0Index];
                    string groupKey0 = classified.State;
                    int groupKey1 = classified.Fallback;
                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                    if (!groupExists)
                    {
                        groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                        groupsToFinalize.Add(groupRef);
                    }

                    ResultAggregateGroup group = groupRef;
                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__key1, finalGroup.__agg0.Count) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.State, right.State);
                    if (comparison != 0)
                        return comparison;
                    comparison = left.Fallback.CompareTo(right.Fallback);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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
                var __cte0_rSchema = provider.GetSchema("#bench");
                var cte0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_rRowsSource.Chunks;
                var __cte0_dSchema = provider.GetSchema("#bench");
                var cte0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_dSchema, "reconcile", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_1, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : cte0_dRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var dRowsBuffer = EvaluationHelper.MaterializeChunkedRows(cte0_dRows);
                var cte0DHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Benchmarks.Scenarios.FixtureRow>>>(dRowsBuffer.Count);
                bool[] cte0DHashBuildMatched = new bool[dRowsBuffer.Count];
                for (int dIndex = 0; dIndex < dRowsBuffer.Count; ++dIndex)
                {
                    if ((dIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Benchmarks.Scenarios.FixtureRow d = dRowsBuffer[dIndex];
                    Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Benchmarks.Scenarios.FixtureRow> dIndexed = new Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Benchmarks.Scenarios.FixtureRow>(d, dIndex);
                    int? key = d.Key;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0DHash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Musoq.Evaluator.Helpers.IndexedHashJoinRow<Musoq.Benchmarks.Scenarios.FixtureRow>>(dIndexed);
                        }
                        else
                        {
                            matches.Add(dIndexed);
                        }
                    }
                }

                foreach (var rChunk in cte0_rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                bool cte0DHashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && cte0DHash.TryGetValue(key, out var cte0DHashMatches))
                                {
                                    foreach (var dIndexed in cte0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        Musoq.Benchmarks.Scenarios.FixtureRow d = dIndexed.Row;
                                        int dIndex = dIndexed.Index;
                                        cte0DHashHasMatch = true;
                                        cte0DHashBuildMatched[dIndex] = (bool)(true);
                                        cte0.Add(new Cte0Row0((false ? (string)"right-only" : (false ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
                                    }
                                }

                                if (!cte0DHashHasMatch)
                                {
                                    cte0.Add(new Cte0Row0((false ? (string)"right-only" : (true ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                bool cte0DHashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && cte0DHash.TryGetValue(key, out var cte0DHashMatches))
                                {
                                    foreach (var dIndexed in cte0DHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        Musoq.Benchmarks.Scenarios.FixtureRow d = dIndexed.Row;
                                        int dIndex = dIndexed.Index;
                                        cte0DHashHasMatch = true;
                                        cte0DHashBuildMatched[dIndex] = (bool)(true);
                                        cte0.Add(new Cte0Row0((false ? (string)"right-only" : (false ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
                                    }
                                }

                                if (!cte0DHashHasMatch)
                                {
                                    cte0.Add(new Cte0Row0((false ? (string)"right-only" : (true ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
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
                        bool cte0DHashHasMatch = false;
                        int? key = r.Key;
                        if (key != null && cte0DHash.TryGetValue(key, out var cte0DHashMatches))
                        {
                            foreach (var dIndexed in cte0DHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                Musoq.Benchmarks.Scenarios.FixtureRow d = dIndexed.Row;
                                int dIndex = dIndexed.Index;
                                cte0DHashHasMatch = true;
                                cte0DHashBuildMatched[dIndex] = (bool)(true);
                                cte0.Add(new Cte0Row0((false ? (string)"right-only" : (false ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
                            }
                        }

                        if (!cte0DHashHasMatch)
                        {
                            cte0.Add(new Cte0Row0((false ? (string)"right-only" : (true ? (string)"missing" : (string)"matched")), (r.Optional ?? -1)));
                        }
                    }
                }

                for (int dIndex = 0; dIndex < dRowsBuffer.Count; ++dIndex)
                {
                    if ((dIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Musoq.Benchmarks.Scenarios.FixtureRow d = dRowsBuffer[dIndex];
                    if ((!(bool)SafeArrayAccess.GetArrayElement<bool>(cte0DHashBuildMatched, dIndex)))
                    {
                        cte0.Add(new Cte0Row0((true ? (string)"right-only" : (false ? (string)"missing" : (string)"matched")), -1));
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
            public string State { get; }
            public int Fallback { get; }

            public Cte0Row0(string __value0, int __value1)
            {
                State = __value0;
                Fallback = __value1;
            }
        }

        private sealed class ResultAggregateGroup
        {
            public readonly string __key0;
            public readonly int __key1;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public ResultAggregateGroup(string __key0, int __key1)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg0);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(string State, int Fallback, long Count_)
            {
                this.State = State;
                this.Fallback = Fallback;
                this.Count_ = Count_;
            }

            public string State { get; private set; }
            public int Fallback { get; private set; }
            public long Count_ { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.State, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Fallback, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Count_, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.State),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Fallback),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "State" => true,
                "Fallback" => true,
                "Count" => true,
                "Count_" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.State = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Fallback = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }
    }
}
