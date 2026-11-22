// === Parsed Query ===
/*
select r.Id as Id, (select Sum(d.Amount)
from bench.lookup() d
where d.Key = r.Key) as Total
from bench.rows() r
order by r.Id
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [d.Key as d.Key, AggRef(d.Sum(d.Amount)) as d.Sum(d.Amount)]
        Aggregate [keys: d.Key] [aggs: Sum(Amount)]
          SchemaScan [#bench.lookup() as d]
      Project [d.Key as _sq_1_corr_0, d.Sum(d.Amount) as _sq_1_value]
        CteRef [dScore as dScore]
  Query
    MultiStatement
      Project [r.Id as r.Id, r.Key as r.Key, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftSingle] [(_sq_1._sq_1_corr_0 = r.Key)]
          SchemaScan [#bench.rows() as r]
          CteRef [_sq_1 as _sq_1]
      Sort [r.Id]
        Project [r.Id as Id, _sq_1._sq_1_value as Total]
          CteRef [r_sq_1 as r_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [d.Key as d.Key, AggRef(d.Sum(d.Amount)) as d.Sum(d.Amount)]
        PhysicalSingleKeyAggregate [key: d.Key (Int32)] [aggs: Sum(Amount)]
          PhysicalSchemaScan [#bench.lookup() as d]
      PhysicalProject [d.Key as _sq_1_corr_0, d.Sum(d.Amount) as _sq_1_value]
        PhysicalCteRef [dScore as dScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [r.Id as r.Id, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_1._sq_1_corr_0] [probe: r.Key]
          PhysicalSchemaScan [#bench.rows() as r]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalSort [r.Id]
        PhysicalProject [r.Id as Id, _sq_1._sq_1_value as Total]
          PhysicalCteRef [r_sq_1 as r_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [d: FixtureRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      _sq_1_corr_0: int <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
    TableRow [_sq_1]
      _sq_1_corr_0: int <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    Generated [Statement0Row0]
      r.Id: int <- field r_Id
      _sq_1._sq_1_value: decimal? <- field _sq_1__sq_1_value
    TableRow [r_sq_1]
      r.Id: int <- field r_Id
      _sq_1._sq_1_value: decimal? <- field _sq_1__sq_1_value
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [d: FixtureRow] -> cte0_dRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: int -> Cte0AggregateGroup]
    ChunkedForEach [d in cte0_dRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[d.Key] by d.Key; typed: Cte0AggregateGroup]
      Let [amount: decimal = d.Amount]
      TypedAggregateSet [Set(cte0Group.__agg0, amount)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(_sq_1_corr_0: cte0FinalGroup.d.Key, _sq_1_value: d.Sum(d.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Begin:cte1]
    SourceScan [r: FixtureRow] -> statement0_rRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0_sq_1Hash: int? -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      HashAdd [statement0_sq_1Hash[_sq_1._sq_1_corr_0] += _sq_1]
    ChunkedForEach [r in statement0_rRows]
      HashProbe [statement0_sq_1Hash[r.Key] -> statement0_sq_1HashMatches] [match: statement0_sq_1HashHasMatch]
        ForEach [_sq_1 in statement0_sq_1HashMatches]
          Assign [statement0_sq_1HashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, _sq_1._sq_1_value: _sq_1._sq_1_value)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(r.Id: r.Id, _sq_1._sq_1_value: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    PhaseBoundary [End:cte1]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEach [r_sq_1 in _cteRowResults.Slot1]
      AppendShape [result <- ResultShape0(Id: r_sq_1.r.Id, Total: r_sq_1._sq_1._sq_1_value)]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B11_ScalarAggregate_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("_sq_1_corr_0", typeof(int), 0),
            new Column("_sq_1_value", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1) });
        private static readonly Column[] __columns_compiled_statement0_3 = new Column[]
        {
            new Column("r.Id", typeof(int), 0),
            new Column("_sq_1._sq_1_value", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
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
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 r_sq_1 = __storedTable1Rows[__storedTable1Index];
                    result.Add(new ResultShape0(r_sq_1.r_Id, r_sq_1._sq_1__sq_1_value) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
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
                var __cte0_dSchema = provider.GetSchema("#bench");
                var cte0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_dSchema, "lookup", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_d_0, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_dRowsSource.Chunks, __musoqProgressContext, "d:2") : cte0_dRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<int, Cte0AggregateGroup>();
                foreach (var dChunk in cte0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                int groupKey = d.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                decimal amount = d.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                int groupKey = d.Key;
                                ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                if (!cte0GroupExists)
                                {
                                    cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                    cte0GroupsToFinalize.Add(cte0GroupRef);
                                }

                                Cte0AggregateGroup cte0Group = cte0GroupRef;
                                decimal amount = d.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                        cte0Group.__agg0.HasValue = true;
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
                        int groupKey = d.Key;
                        ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                        if (!cte0GroupExists)
                        {
                            cte0GroupRef = new Cte0AggregateGroup(groupKey);
                            cte0GroupsToFinalize.Add(cte0GroupRef);
                        }

                        Cte0AggregateGroup cte0Group = cte0GroupRef;
                        decimal amount = d.Amount;
                        {
                            var __agg0Input = (decimal?)amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                cte0Group.__agg0.Value = cte0Group.__agg0.HasValue ? checked(cte0Group.__agg0.Value + __agg0Current) : __agg0Current;
                                cte0Group.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
                foreach (var cte0FinalGroup in cte0GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, (cte0FinalGroup.__agg0.HasValue ? (decimal?)cte0FinalGroup.__agg0.Value : null)));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __statement0_rSchema = provider.GetSchema("#bench");
                var statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__statement0_rSchema, "rows", new SourceExecutionContext("r:2", sourceExecutionPlans["r:2"], token, __schemaColumns_compiled_r_2, sourceRuntimeSettingsBySourceContextId["r:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(statement0_rRowsSource.Chunks, __musoqProgressContext, "r:2") : statement0_rRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                var statement0_sq_1Hash = new Dictionary<int?, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                    int? key = _sq_1._sq_1_corr_0;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(statement0_sq_1Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Cte0Row0>(_sq_1);
                        }
                        else
                        {
                            matches.Add(_sq_1);
                        }
                    }
                }

                foreach (var rChunk in statement0_rRows)
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
                                bool statement0_sq_1HashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(r.Id, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
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
                                bool statement0_sq_1HashHasMatch = false;
                                int? key = r.Key;
                                if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in statement0_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        statement0_sq_1HashHasMatch = true;
                                        statement0.Add(new Statement0Row0(r.Id, _sq_1._sq_1_value));
                                    }
                                }

                                if (!statement0_sq_1HashHasMatch)
                                {
                                    statement0.Add(new Statement0Row0(r.Id, null));
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
                        bool statement0_sq_1HashHasMatch = false;
                        int? key = r.Key;
                        if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                        {
                            foreach (var _sq_1 in statement0_sq_1HashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                statement0_sq_1HashHasMatch = true;
                                statement0.Add(new Statement0Row0(r.Id, _sq_1._sq_1_value));
                            }
                        }

                        if (!statement0_sq_1HashHasMatch)
                        {
                            statement0.Add(new Statement0Row0(r.Id, null));
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public readonly int __key0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Cte0AggregateGroup(int __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public int _sq_1_corr_0 { get; }
            public decimal? _sq_1_value { get; }

            public Cte0Row0(int __value0, decimal? __value1)
            {
                _sq_1_corr_0 = __value0;
                _sq_1_value = __value1;
            }
        }

        private sealed class Statement0Row0
        {
            public int r_Id { get; }
            public decimal? _sq_1__sq_1_value { get; }

            public Statement0Row0(int __value0, decimal? __value1)
            {
                r_Id = __value0;
                _sq_1__sq_1_value = __value1;
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Total)
            {
                this.Id = Id;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 2;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }
    }
}
