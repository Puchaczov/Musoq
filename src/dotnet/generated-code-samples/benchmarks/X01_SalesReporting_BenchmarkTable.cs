// === Parsed Query ===
/*
with totals as (select r.Key as Id, Sum(r.Amount) as Total
from bench.rows() r
inner join bench.lookup() d on r.Key = d.Key
where r.Amount >= 2
group by r.Key
having Sum(r.Amount) > 0) select Id, Total, RowNumber() over (order by Total desc) as Position
from totals
qualify Position <= 2
order by Id
take 2
*/

// === Logical Plan ===
/*
Cte
  Definition [totals]
    MultiStatement
      Project [r.Key as r.Key, r.Amount as r.Amount, d.Key as d.Key]
        Join [Inner] [(r.Key = d.Key)]
          SchemaScan [#bench.rows() as r]
          SchemaScan [#bench.lookup() as d]
      Project [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
        Having [(AggRef(r.Sum(r.Amount)) > 0)]
          Aggregate [keys: r.Key] [aggs: Sum(Amount)]
            Filter [(rd.r.Amount >= 2)]
              CteRef [rd as rd]
      Project [r.Key as Id, r.Sum(r.Amount) as Total]
        CteRef [rdScore as rdScore]
  Query
    MultiStatement
      Take [2]
        Sort [totals.Id]
          Project [totals.Id as Id, totals.Total as Total, WindowRef(0) as Position]
            Qualify [(WindowRef(0) <= 2)]
              Window [RowNumber(idx:0; order: totals.Total DESC)]
                CteRef [totals as totals]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [totals]
    PhysicalMultiStatement
      PhysicalProject [r.Key as r.Key, r.Amount as r.Amount]
        PhysicalHashJoin [Inner] [build: d.Key] [probe: r.Key]
          PhysicalFilter [(r.Amount >= 2)]
            PhysicalSchemaScan [#bench.rows() as r] [pushdown: (r.Amount >= 2)]
          PhysicalSchemaScan [#bench.lookup() as d]
      PhysicalProject [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
        PhysicalHaving [(AggRef(r.Sum(r.Amount)) > 0)]
          PhysicalSingleKeyAggregate [key: r.Key (Int32)] [aggs: Sum(Amount)]
            PhysicalFilter [(rd.r.Amount >= 2)]
              PhysicalCteRef [rd as rd]
      PhysicalProject [r.Key as Id, r.Sum(r.Amount) as Total]
        PhysicalCteRef [rdScore as rdScore]
  Query
    PhysicalMultiStatement
      PhysicalTopN [2] [totals.Id]
        PhysicalProject [totals.Id as Id, totals.Total as Total, WindowRef(0) as Position]
          PhysicalQualify [(WindowRef(0) <= 2)]
            PhysicalWindow [RowNumber(idx:0; order: totals.Total DESC) bounded winners]
              PhysicalMaterialize
                PhysicalCteRef [totals as totals]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
    SourceEntity [d: FixtureRow]
      Key: int <- property Key
    Generated [Cte0Statement0Row0]
      r.Key: int <- field r_Key
      r.Amount: decimal <- field r_Amount
    TableRow [rd]
      r.Key: int <- field r_Key
      r.Amount: decimal <- field r_Amount
    AggregateGroup [Cte0Statement1AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Statement1Row0]
      r.Key: int <- field r_Key
      r.Sum(r.Amount): decimal? <- field r_Sum_r_Amount_
    TableRow [rdScore]
      r.Key: int <- field r_Key
      r.Sum(r.Amount): decimal? <- field r_Sum_r_Amount_
    Generated [Cte0Row0]
      Id: int <- field Id
      Total: decimal? <- field Total
    TableRow [totals]
      Id: int <- field Id
      Total: decimal? <- field Total
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
      Position: long <- field Position

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [r: FixtureRow] -> cte0_statement0_rRows
    MaterializeFilteredChunked [cte0_statement0_rRows where (r.Amount >= 2) -> rPrefilteredRows]
    SourceScan [d: FixtureRow] -> cte0_statement0_dRows
    CreateTable [cte0_statement0: Cte0Statement0Row0]
    CreateHash [cte0_statement0DHash: int -> FixtureRow]
    PhaseBoundary [Where:cte0]
    ChunkedForEach [d in cte0_statement0_dRows]
      HashAdd [cte0_statement0DHash[d.Key] += d]
    PhaseBoundary [Select:cte0]
    ForEach [r in rPrefilteredRows]
      HashProbe [cte0_statement0DHash[r.Key] -> cte0_statement0DHashMatches]
        ForEach [d in cte0_statement0DHashMatches]
          AppendRow [cte0_statement0 <- Cte0Statement0Row0(r.Key: r.Key, r.Amount: r.Amount)]
    StoreTable [cte0_statement0 -> _cteRowResults.Slot1: List<Cte0Statement0Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:cte2]
    CreateTable [cte0_statement1: Cte0Statement1Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0_statement1Groups: int -> Cte0Statement1AggregateGroup]
    ForEach [rd in _cteRowResults.Slot1]
      Let [r_Amount: decimal = rd.r.Amount]
      If [(r_Amount >= 2)]
        GetOrAddSingleKeyAggregateGroup [cte0_statement1Group = cte0_statement1Groups[rd.r.Key] by r.Key; typed: Cte0Statement1AggregateGroup]
        TypedAggregateSet [Set(cte0_statement1Group.__agg0, r_Amount)]
    EnsureCapacity [cte0_statement1 <- cte0_statement1GroupsToFinalize.Count]
    ForEach [cte0_statement1FinalGroup in cte0_statement1GroupsToFinalize]
      If [(r.Sum(r.Amount) > 0)]
        AppendRow [cte0_statement1 <- Cte0Statement1Row0(r.Key: cte0_statement1FinalGroup.r.Key, r.Sum(r.Amount): r.Sum(r.Amount))]
    StoreTable [cte0_statement1 -> _cteRowResults.Slot2: List<Cte0Statement1Row0>]
    PhaseBoundary [End:cte2]
    PhaseBoundary [Begin:cte0]
    CreateTable [cte0: Cte0Row0]
    ForEach [rdScore in _cteRowResults.Slot2]
      AppendRow [cte0 <- Cte0Row0(Id: rdScore.r.Key, Total: rdScore.r.Sum(r.Amount))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    Materialize [_cteRowResults.Slot0 -> resultWindowRows]
    ComputeRowNumberWindow [resultRowNumbers <- resultWindowRows order by totals.Total DESC qualify <= 2] [bounded winners]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ForEachIndexed [windowIndex, totals in resultWindowRows] [positions: resultRowNumbersWinnerPositions]
      If [((resultRowNumbers[windowIndex] > 0) AND (resultRowNumbers[windowIndex] <= 2))]
        AppendShape [result <- ResultShape0(Id: totals.Id, Total: totals.Total, Position: resultRowNumbers[windowIndex])]
    TopNShapeRows [result -> resultTopN by Id ASC, 2]
    ReturnDeferredTable [resultTopN: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X01_SalesReporting_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_cte0_statement0_2 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("r.Amount", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_cte0_statement1_3 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("r.Sum(r.Amount)", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_cte0_4 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Position", typeof(long), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("resultTopN", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultTopN", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot2 = BuildCte2(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var resultWindowRows = EvaluationHelper.MaterializeGeneratedRows<Cte0Row0>(_cteRowResults.Slot0);
                var resultRowNumbersOrderKeys = new WindowResultRowNumbersOrderKeysKey[resultWindowRows.Count];
                ExtractResultRowNumbersWindowKeys(resultWindowRows, resultRowNumbersOrderKeys);
                var resultRowNumbersPartitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count);
                static long[] resultRowNumbersWinnerPositionsSelect<TKey>(Musoq.Evaluator.Helpers.WindowPartitionSet partitions, TKey[] keys, long bound, System.Threading.CancellationToken cancellation, out int[] positions)
                    where TKey : struct, IComparable<TKey>
                {
                    int limit = (int)Math.Clamp(bound, 0L, (long)int.MaxValue);
                    int total = 0;
                    for (int p = 0; p < partitions.PartitionCount; p++)
                        total += Math.Min(limit, partitions.GetLength(p));
                    var ranks = new long[total];
                    bool allSelected = total == keys.Length;
                    var selected = allSelected ? Array.Empty<int>() : new int[total];
                    positions = selected;
                    if (total == 0)
                        return ranks;
                    int next = 0;
                    var comparer = Comparer<int>.Create((a, b) => keys[a].CompareTo(keys[b]));
                    int[] indices = partitions.Indices;
                    for (int p = 0; p < partitions.PartitionCount; p++)
                    {
                        if ((p & 1023) == 0)
                            cancellation.ThrowIfCancellationRequested();
                        int start = partitions.GetStart(p);
                        int length = partitions.GetLength(p);
                        if (length <= limit)
                        {
                            if (length == 2)
                            {
                                int first = indices[start];
                                int second = indices[start + 1];
                                bool reverse = keys[first].CompareTo(keys[second]) > 0;
                                Record(first, reverse ? 2 : 1);
                                Record(second, reverse ? 1 : 2);
                            }
                            else
                            {
                                if (length > 2)
                                    Array.Sort(indices, start, length, comparer);
                                for (int i = 0; i < length; i++)
                                    Record(indices[start + i], i + 1L);
                            }

                            continue;
                        }

                        var best = new Musoq.Evaluator.Helpers.EvaluationHelper.BoundedTopRecordList<int>(limit, comparer);
                        for (int i = 0; i < length; i++)
                        {
                            if ((i & 1023) == 0)
                                cancellation.ThrowIfCancellationRequested();
                            best.Add(indices[start + i]);
                        }

                        long rank = 0;
                        foreach (int input in best)
                        {
                            Record(input, ++rank);
                        }
                    }

                    if (!allSelected)
                        Array.Sort(selected, ranks);
                    return ranks;
                    void Record(int input, long rank)
                    {
                        if (allSelected)
                            ranks[input] = rank;
                        else
                        {
                            ranks[next] = rank;
                            selected[next++] = input;
                        }
                    }
                }

                var resultRowNumbers = resultRowNumbersWinnerPositionsSelect(resultRowNumbersPartitions, resultRowNumbersOrderKeys, 2L, token, out var resultRowNumbersWinnerPositions);
                var result = new List<ResultShape0>(resultRowNumbers.Length);
                for (int windowIndex = 0; windowIndex < resultRowNumbers.Length; ++windowIndex)
                {
                    if ((windowIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 totals = resultWindowRows[resultRowNumbersWinnerPositions.Length == 0 ? windowIndex : resultRowNumbersWinnerPositions[windowIndex]];
                    if ((((long)resultRowNumbers[windowIndex] > 0L) && ((long)resultRowNumbers[windowIndex] <= 2)))
                    {
                        result.Add(new ResultShape0(totals.Id, totals.Total, (long)resultRowNumbers[windowIndex]));
                    }
                }

                var resultTopNRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                })).Take(2);
                foreach (var resultTopNRowsRow in resultTopNRows)
                {
                    __musoqFinalShapeRows.Add(resultTopNRowsRow);
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
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Total, __musoqShapeRow.Position);
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
        private static List<Cte0Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte0_statement0_rSchema = provider.GetSchema("#bench");
                var cte0_statement0_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_statement0_rRowsSource.Chunks;
                var rPrefilteredRows = new List<Musoq.Benchmarks.Scenarios.FixtureRow>();
                foreach (var rChunk in cte0_statement0_rRows)
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
                                {
                                    if ((r.Amount >= 2))
                                    {
                                        rPrefilteredRows.Add(r);
                                    }
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
                                {
                                    if ((r.Amount >= 2))
                                    {
                                        rPrefilteredRows.Add(r);
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
                        {
                            if ((r.Amount >= 2))
                            {
                                rPrefilteredRows.Add(r);
                            }
                        }
                    }
                }

                var __cte0_statement0_dSchema = provider.GetSchema("#bench");
                var cte0_statement0_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0_statement0_dSchema, "lookup", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_1, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_statement0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0_statement0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : cte0_statement0_dRowsSource.Chunks;
                var cte0_statement0 = new List<Cte0Statement0Row0>();
                var cte0_statement0DHash = new Dictionary<int, HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>>();
                foreach (var dChunk in cte0_statement0_dRows)
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
                                int key = d.Key;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0_statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
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
                                int key = d.Key;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0_statement0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
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
                        int key = d.Key;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0_statement0DHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Benchmarks.Scenarios.FixtureRow>(d);
                            }
                            else
                            {
                                matches.Add(d);
                            }
                        }
                    }
                }

                foreach (var r in rPrefilteredRows)
                {
                    token.ThrowIfCancellationRequested();
                    int key = r.Key;
                    if (cte0_statement0DHash.TryGetValue(key, out var cte0_statement0DHashMatches))
                    {
                        foreach (var d in cte0_statement0DHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            cte0_statement0.Add(new Cte0Statement0Row0(r.Key, r.Amount));
                        }
                    }
                }

                return cte0_statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Statement1Row0> BuildCte2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            try
            {
                var cte0_statement1 = new List<Cte0Statement1Row0>();
                var cte0_statement1GroupsToFinalize = new List<Cte0Statement1AggregateGroup>();
                var cte0_statement1Groups = new Dictionary<int, Cte0Statement1AggregateGroup>();
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Statement0Row0 rd = __storedTable1Rows[__storedTable1Index];
                    decimal r_Amount = rd.r_Amount;
                    if ((r_Amount >= 2))
                    {
                        int groupKey = rd.r_Key;
                        ref var cte0_statement1GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0_statement1Groups, groupKey, out var cte0_statement1GroupExists);
                        if (!cte0_statement1GroupExists)
                        {
                            cte0_statement1GroupRef = new Cte0Statement1AggregateGroup(groupKey);
                            cte0_statement1GroupsToFinalize.Add(cte0_statement1GroupRef);
                        }

                        Cte0Statement1AggregateGroup cte0_statement1Group = cte0_statement1GroupRef;
                        {
                            var __agg0Input = (decimal?)r_Amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                cte0_statement1Group.__agg0.Value = cte0_statement1Group.__agg0.HasValue ? checked(cte0_statement1Group.__agg0.Value + __agg0Current) : __agg0Current;
                                cte0_statement1Group.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                cte0_statement1.EnsureCapacity(cte0_statement1GroupsToFinalize.Count);
                foreach (var cte0_statement1FinalGroup in cte0_statement1GroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if (((Operators.SqlCompare<decimal?, int>((cte0_statement1FinalGroup.__agg0.HasValue ? (decimal?)cte0_statement1FinalGroup.__agg0.Value : null), 0, (decimal? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight)))) == true)
                    {
                        cte0_statement1.Add(new Cte0Statement1Row0(cte0_statement1FinalGroup.__key0, (cte0_statement1FinalGroup.__agg0.HasValue ? (decimal?)cte0_statement1FinalGroup.__agg0.Value : null)));
                    }
                }

                return cte0_statement1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var cte0 = new List<Cte0Row0>(_cteRowResults.Slot2.Count);
                var __storedTable2Rows = _cteRowResults.Slot2;
                for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
                {
                    if ((__storedTable2Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Statement1Row0 rdScore = __storedTable2Rows[__storedTable2Index];
                    cte0.Add(new Cte0Row0(rdScore.r_Key, rdScore.r_Sum_r_Amount_));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ExtractResultRowNumbersWindowKeys(IReadOnlyList<Cte0Row0> resultWindowRows, WindowResultRowNumbersOrderKeysKey[] resultRowNumbersOrderKeys)
        {
            for (int windowIndex = 0; windowIndex < resultWindowRows.Count; ++windowIndex)
            {
                Cte0Row0 totals = resultWindowRows[windowIndex];
                resultRowNumbersOrderKeys[windowIndex] = new WindowResultRowNumbersOrderKeysKey(totals.Total);
            }
        }

        private sealed class Cte0Statement0Row0
        {
            public int r_Key { get; }
            public decimal r_Amount { get; }

            public Cte0Statement0Row0(int __value0, decimal __value1)
            {
                r_Key = __value0;
                r_Amount = __value1;
            }
        }

        private sealed class Cte0Statement1AggregateGroup
        {
            public readonly int __key0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Cte0Statement1AggregateGroup(int __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(Cte0Statement1AggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Statement1Row0
        {
            public int r_Key { get; }
            public decimal? r_Sum_r_Amount_ { get; }

            public Cte0Statement1Row0(int __value0, decimal? __value1)
            {
                r_Key = __value0;
                r_Sum_r_Amount_ = __value1;
            }
        }

        private sealed class Cte0Row0
        {
            public int Id { get; }
            public decimal? Total { get; }

            public Cte0Row0(int __value0, decimal? __value1)
            {
                Id = __value0;
                Total = __value1;
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public decimal? Total { get; private set; }
            public long Position { get; private set; }

            public ResultRow0(int __value0, decimal? __value1, long __value2)
            {
                Id = __value0;
                Total = __value1;
                Position = __value2;
            }

            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Position, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                "Position" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Position = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, decimal? Total, long Position)
            {
                this.Id = Id;
                this.Total = Total;
                this.Position = Position;
            }

            public int Id { get; }
            public decimal? Total { get; }
            public long Position { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte0Statement0Row0> Slot1;
            public List<Cte0Statement1Row0> Slot2;
        }

        private readonly struct WindowResultRowNumbersOrderKeysKey : System.IEquatable<WindowResultRowNumbersOrderKeysKey>, System.IComparable<WindowResultRowNumbersOrderKeysKey>
        {
            private readonly decimal? _value0;
            public WindowResultRowNumbersOrderKeysKey(decimal? value0)
            {
                _value0 = value0;
            }

            public decimal? RawValues => _value0;

            public int CompareTo(WindowResultRowNumbersOrderKeysKey other)
            {
                var comparison0 = CompareValue0(_value0, other._value0);
                if (comparison0 != 0)
                    return comparison0;
                return 0;
            }

            public bool Equals(WindowResultRowNumbersOrderKeysKey other)
            {
                return System.Collections.Generic.EqualityComparer<decimal?>.Default.Equals(_value0, other._value0);
            }

            public override bool Equals(object obj)
            {
                return obj is WindowResultRowNumbersOrderKeysKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                var hash = new System.HashCode();
                hash.Add(_value0);
                return hash.ToHashCode();
            }

            private static int CompareValue0(decimal? left, decimal? right)
            {
                if (!left.HasValue)
                    return !right.HasValue ? 0 : 1;
                if (!right.HasValue)
                    return -1;
                var comparison = left.Value.CompareTo(right.Value);
                return -comparison;
            }
        }
    }
}
