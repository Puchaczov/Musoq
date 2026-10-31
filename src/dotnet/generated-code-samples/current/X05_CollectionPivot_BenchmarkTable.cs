// === Parsed Query ===
/*
with expanded as (select r.Key as Id, i.Ordinal as Bucket, r.Amount as Amount
from bench.rows() r
outer apply r.Items i with ordinality
left join bench.lookup() d on i.Value = d.Id) pivot expanded e on e.Bucket in (0 as Even, 1 as Odd) using Sum(e.Amount) as Total
group by e.Id
order by Id
*/

// === Logical Plan ===
/*
Cte
  Definition [expanded]
    MultiStatement
      Project [r.Key as r.Key, r.Amount as r.Amount, r.Items as r.Items, i.Value as i.Value, i.Ordinal as i.Ordinal]
        Apply [Outer, with ordinality]
          SchemaScan [#bench.rows() as r]
          PropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
      Project [ri.r.Key as r.Key, ri.r.Amount as r.Amount, ri.r.Items as r.Items, ri.i.Value as i.Value, ri.i.Ordinal as i.Ordinal, d.Id as Id]
        Join [LeftOuter] [(ri.i.Value = d.Id)]
          CteRef [ri as ri]
          SchemaScan [#bench.lookup() as d]
      Project [r.Key as Id, i.Ordinal as Bucket, r.Amount as Amount]
        CteRef [rid as rid]
  Query
    MultiStatement
      Project [e.Id as e.Id, AggRef(e.Sum(e.Amount) filter (where e.Bucket = 1)) as e.Sum(e.Amount) filter (where e.Bucket = 1), AggRef(e.Sum(e.Amount) filter (where e.Bucket = 0)) as e.Sum(e.Amount) filter (where e.Bucket = 0)]
        Aggregate [keys: e.Id] [aggs: Sum(Amount) filter (where Bucket = 1), Sum(Amount) filter (where Bucket = 0)]
          CteRef [expanded as e]
      Sort [e.Id]
        Project [e.Id as Id, e.Sum(e.Amount) filter (where e.Bucket = 0) as Even, e.Sum(e.Amount) filter (where e.Bucket = 1) as Odd]
          CteRef [eScore as eScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [expanded]
    PhysicalMultiStatement
      PhysicalProject [r.Key as r.Key, r.Amount as r.Amount, r.Items as r.Items, i.Value as i.Value, i.Ordinal as i.Ordinal]
        PhysicalNestedLoopApply [Outer, with ordinality]
          PhysicalSchemaScan [#bench.rows() as r]
          PhysicalPropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
      PhysicalProject [ri.r.Key as r.Key, ri.r.Amount as r.Amount, ri.r.Items as r.Items, ri.i.Value as i.Value, ri.i.Ordinal as i.Ordinal, d.Id as Id]
        PhysicalHashJoin [LeftOuter] [build: d.Id] [probe: ri.i.Value]
          PhysicalCteRef [ri as ri]
          PhysicalSchemaScan [#bench.lookup() as d]
      PhysicalProject [r.Key as Id, i.Ordinal as Bucket, r.Amount as Amount]
        PhysicalCteRef [rid as rid]
  Query
    PhysicalMultiStatement
      PhysicalProject [e.Id as e.Id, AggRef(e.Sum(e.Amount) filter (where e.Bucket = 1)) as e.Sum(e.Amount) filter (where e.Bucket = 1), AggRef(e.Sum(e.Amount) filter (where e.Bucket = 0)) as e.Sum(e.Amount) filter (where e.Bucket = 0)]
        PhysicalSingleKeyAggregate [key: e.Id (Int32)] [aggs: Sum(Amount) filter (where Bucket = 1), Sum(Amount) filter (where Bucket = 0)]
          PhysicalCteRef [expanded as e]
      PhysicalSort [e.Id]
        PhysicalProject [e.Id as Id, e.Sum(e.Amount) filter (where e.Bucket = 0) as Even, e.Sum(e.Amount) filter (where e.Bucket = 1) as Odd]
          PhysicalCteRef [eScore as eScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: BenchmarkSampleRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
      Items: int[] <- property Items
    SourceEntity [i: PrimitiveTypeEntity<int>]
      Value: int <- property Value
      Ordinal: int <- apply ordinality iOrdinal
    Generated [Cte0Statement0Row0]
      r.Key: int <- field r_Key
      r.Amount: decimal <- field r_Amount
      r.Items: int[] <- field r_Items
      i.Value: int? <- field i_Value
      i.Ordinal: int? <- field i_Ordinal
    TableRow [ri]
      r.Key: int <- field r_Key
      r.Amount: decimal <- field r_Amount
      r.Items: int[] <- field r_Items
      i.Value: int? <- field i_Value
      i.Ordinal: int? <- field i_Ordinal
    SourceEntity [d: BenchmarkSampleRow]
      Id: int <- property Id
    Generated [Cte0Row0]
      Id: int <- field Id
      Bucket: int? <- field Bucket
      Amount: decimal <- field Amount
    TableRow [e]
      Id: int <- field Id
      Bucket: int? <- field Bucket
      Amount: decimal <- field Amount
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      Id: int <- field Id
      Even: decimal? <- field Even
      Odd: decimal? <- field Odd

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Select]
    PhaseBoundary [From:cte0]
    SourceScan [r: BenchmarkSampleRow] -> cte0_statement0_rRows
    CreateTable [cte0_statement0: Cte0Statement0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [r in cte0_statement0_rRows]
      Let [rAmount: decimal = r.Amount]
      Let [rKey: int = r.Key]
      EnumerableSource [r.Items -> cte0_statement0_iRows]
      Let [iHasMatch: bool = FALSE]
      ChunkedForEachWithOrdinality [iOrdinal, i in cte0_statement0_iRows]
        Assign [iHasMatch = TRUE]
        AppendRow [cte0_statement0 <- Cte0Statement0Row0(r.Key: rKey, r.Amount: rAmount, r.Items: r.Items, i.Value: i.Value, i.Ordinal: i.Ordinal)]
      If [NOT iHasMatch]
        AppendRow [cte0_statement0 <- Cte0Statement0Row0(r.Key: rKey, r.Amount: rAmount, r.Items: r.Items, i.Value: NULL, i.Ordinal: NULL)]
    StoreTable [cte0_statement0 -> _cteRowResults.Slot1: List<Cte0Statement0Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Begin:cte2]
    SourceScan [d: BenchmarkSampleRow] -> cte0_dRows
    CreateTable [cte0: Cte0Row0]
    CreateHash [cte0DHash: int? -> BenchmarkSampleRow]
    ChunkedForEach [d in cte0_dRows]
      HashAdd [cte0DHash[d.Id] += d]
    ForEach [ri in _cteRowResults.Slot1]
      HashProbe [cte0DHash[ri.i.Value] -> cte0DHashMatches] [match: cte0DHashHasMatch]
        ForEach [d in cte0DHashMatches]
          Assign [cte0DHashHasMatch = TRUE]
          AppendRow [cte0 <- Cte0Row0(Id: ri.r.Key, Bucket: ri.i.Ordinal, Amount: ri.r.Amount)]
      HashProbeNoMatch
        AppendRow [cte0 <- Cte0Row0(Id: ri.r.Key, Bucket: ri.i.Ordinal, Amount: ri.r.Amount)]
    PhaseBoundary [End:cte2]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: int -> ResultAggregateGroup]
    ForEach [e in _cteRowResults.Slot0]
      Let [bucket: int? = e.Bucket]
      GetOrAddSingleKeyAggregateGroup [group = groups[e.Id] by e.Id; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0, e.Amount) filter (bucket = 1)]
      TypedAggregateSet [Set(group.__agg1, e.Amount) filter (bucket = 0)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Id: finalGroup.e.Id, Even: e.Sum(e.Amount) filter (where e.Bucket = 0), Odd: e.Sum(e.Amount) filter (where e.Bucket = 1))]
    SortShapeRows [result -> resultSorted by Id ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X05_CollectionPivot_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_3 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Bucket", typeof(int?), 1),
            new Column("Amount", typeof(decimal), 2)
        };
        private static readonly Column[] __columns_compiled_cte0_statement0_1 = new Column[]
        {
            new Column("r.Key", typeof(int), 0),
            new Column("r.Amount", typeof(decimal), 1),
            new Column("r.Items", typeof(int[]), 2),
            new Column("i.Value", typeof(int?), 3),
            new Column("i.Ordinal", typeof(int?), 4)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Even", typeof(decimal?), 1),
            new Column("Odd", typeof(decimal?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_d_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Amount", typeof(decimal), 1), new Column("Items", typeof(int[]), 2) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Id, __musoqShapeRow.Even, __musoqShapeRow.Odd);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.Select);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<int, ResultAggregateGroup>();
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 e = __storedTable0Rows[__storedTable0Index];
                    UpdateGroupsAggregates(groupsToFinalize, groups, e);
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null));
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultSortedRowsRow in resultSortedRows)
                {
                    __musoqFinalShapeRows.Add(resultSortedRowsRow);
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
                var __cte0_dSchema = provider.GetSchema("#bench");
                var cte0_dRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__cte0_dSchema, "lookup", new SourceExecutionContext("d:1", sourceExecutionPlans["d:1"], token, __schemaColumns_compiled_d_2, sourceRuntimeSettingsBySourceContextId["d:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(cte0_dRowsSource.Chunks, __musoqProgressContext, "d:1") : cte0_dRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>(_cteRowResults.Slot1.Count);
                var cte0DHash = new Dictionary<int?, HashJoinBucket<Musoq.Evaluator.Tests.BenchmarkSampleRow>>();
                foreach (var dChunk in cte0_dRows)
                {
                    if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> dChunkView)
                    {
                        if (dChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] dChunkViewArray)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                int? key = d.Id;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.BenchmarkSampleRow>(d);
                                    }
                                    else
                                    {
                                        matches.Add(d);
                                    }
                                }
                            }

                            continue;
                        }

                        if (dChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> dChunkViewList)
                        {
                            int dChunkViewOffset = dChunkView.Offset;
                            for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                            {
                                if ((dIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var d = dChunkViewList[dChunkViewOffset + dIndex];
                                int? key = d.Id;
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0DHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.BenchmarkSampleRow>(d);
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
                        int? key = d.Id;
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0DHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.BenchmarkSampleRow>(d);
                            }
                            else
                            {
                                matches.Add(d);
                            }
                        }
                    }
                }

                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Statement0Row0 ri = __storedTable1Rows[__storedTable1Index];
                    bool cte0DHashHasMatch = false;
                    int? key = ri.i_Value;
                    if (key != null && cte0DHash.TryGetValue(key, out var cte0DHashMatches))
                    {
                        foreach (var d in cte0DHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            cte0DHashHasMatch = true;
                            cte0.Add(new Cte0Row0(ri.r_Key, ri.i_Ordinal, ri.r_Amount));
                        }
                    }

                    if (!cte0DHashHasMatch)
                    {
                        cte0.Add(new Cte0Row0(ri.r_Key, ri.i_Ordinal, ri.r_Amount));
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
        private static List<Cte0Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte0_statement0_rSchema = provider.GetSchema("#bench");
                var cte0_statement0_rRowsSource = SourceInvocation<Musoq.Evaluator.Tests.BenchmarkSampleRow>.Open(__cte0_statement0_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_statement0_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.BenchmarkSampleRow>(cte0_statement0_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0_statement0_rRowsSource.Chunks;
                var cte0_statement0 = new List<Cte0Statement0Row0>();
                foreach (var rChunk in cte0_statement0_rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Evaluator.Tests.BenchmarkSampleRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                decimal rAmount = r.Amount;
                                int rKey = r.Key;
                                var cte0_statement0_iRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(r.Items);
                                bool iHasMatch = false;
                                {
                                    int iOrdinal = 0;
                                    foreach (var iChunk in cte0_statement0_iRows)
                                    {
                                        if (iChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkView)
                                        {
                                            if (iChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] iChunkViewArray)
                                            {
                                                int iChunkViewOffset = iChunkView.Offset;
                                                for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                                {
                                                    if ((iIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var i = iChunkViewArray[iChunkViewOffset + iIndex];
                                                    iHasMatch = true;
                                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                                    ++iOrdinal;
                                                }

                                                continue;
                                            }

                                            if (iChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkViewList)
                                            {
                                                int iChunkViewOffset = iChunkView.Offset;
                                                for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                                {
                                                    if ((iIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var i = iChunkViewList[iChunkViewOffset + iIndex];
                                                    iHasMatch = true;
                                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                                    ++iOrdinal;
                                                }

                                                continue;
                                            }
                                        }

                                        for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var i = iChunk[iIndex];
                                            iHasMatch = true;
                                            cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                            ++iOrdinal;
                                        }
                                    }
                                }

                                if ((!iHasMatch))
                                {
                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, null, null));
                                }
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Evaluator.Tests.BenchmarkSampleRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                decimal rAmount = r.Amount;
                                int rKey = r.Key;
                                var cte0_statement0_iRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(r.Items);
                                bool iHasMatch = false;
                                {
                                    int iOrdinal = 0;
                                    foreach (var iChunk in cte0_statement0_iRows)
                                    {
                                        if (iChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkView)
                                        {
                                            if (iChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] iChunkViewArray)
                                            {
                                                int iChunkViewOffset = iChunkView.Offset;
                                                for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                                {
                                                    if ((iIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var i = iChunkViewArray[iChunkViewOffset + iIndex];
                                                    iHasMatch = true;
                                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                                    ++iOrdinal;
                                                }

                                                continue;
                                            }

                                            if (iChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkViewList)
                                            {
                                                int iChunkViewOffset = iChunkView.Offset;
                                                for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                                {
                                                    if ((iIndex & 1023) == 0)
                                                    {
                                                        token.ThrowIfCancellationRequested();
                                                    }

                                                    var i = iChunkViewList[iChunkViewOffset + iIndex];
                                                    iHasMatch = true;
                                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                                    ++iOrdinal;
                                                }

                                                continue;
                                            }
                                        }

                                        for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var i = iChunk[iIndex];
                                            iHasMatch = true;
                                            cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                            ++iOrdinal;
                                        }
                                    }
                                }

                                if ((!iHasMatch))
                                {
                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, null, null));
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
                        decimal rAmount = r.Amount;
                        int rKey = r.Key;
                        var cte0_statement0_iRows = EvaluationHelper.ConvertScalarEnumerableToTypedChunks<int>(r.Items);
                        bool iHasMatch = false;
                        {
                            int iOrdinal = 0;
                            foreach (var iChunk in cte0_statement0_iRows)
                            {
                                if (iChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkView)
                                {
                                    if (iChunkView.Source is Musoq.Plugins.PrimitiveTypeEntity<int>[] iChunkViewArray)
                                    {
                                        int iChunkViewOffset = iChunkView.Offset;
                                        for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var i = iChunkViewArray[iChunkViewOffset + iIndex];
                                            iHasMatch = true;
                                            cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                            ++iOrdinal;
                                        }

                                        continue;
                                    }

                                    if (iChunkView.Source is List<Musoq.Plugins.PrimitiveTypeEntity<int>> iChunkViewList)
                                    {
                                        int iChunkViewOffset = iChunkView.Offset;
                                        for (int iIndex = 0, iIndexCount = iChunkView.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            var i = iChunkViewList[iChunkViewOffset + iIndex];
                                            iHasMatch = true;
                                            cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                            ++iOrdinal;
                                        }

                                        continue;
                                    }
                                }

                                for (int iIndex = 0, iIndexCount = iChunk.Count; iIndex < iIndexCount; ++iIndex)
                                {
                                    if ((iIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var i = iChunk[iIndex];
                                    iHasMatch = true;
                                    cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, i.Value, iOrdinal));
                                    ++iOrdinal;
                                }
                            }
                        }

                        if ((!iHasMatch))
                        {
                            cte0_statement0.Add(new Cte0Statement0Row0(rKey, rAmount, r.Items, null, null));
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
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<int, ResultAggregateGroup> groups, Cte0Row0 e)
        {
            int? bucket = e.Bucket;
            int groupKey = e.Id;
            ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
            if (!groupExists)
            {
                groupRef = new ResultAggregateGroup(groupKey);
                groupsToFinalize.Add(groupRef);
            }

            ResultAggregateGroup group = groupRef;
            if ((Operators.SqlCompare<int?, int>(bucket, 1, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight))) == true)
            {
                {
                    var __agg0Input = (decimal?)e.Amount;
                    if (__agg0Input.HasValue)
                    {
                        var __agg0Current = __agg0Input.GetValueOrDefault();
                        group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                        group.__agg0.HasValue = true;
                    }
                }
            }

            if ((Operators.SqlCompare<int?, int>(bucket, 0, (int? __sqlLeft, int __sqlRight) => (__sqlLeft == __sqlRight))) == true)
            {
                {
                    var __agg1Input = (decimal?)e.Amount;
                    if (__agg1Input.HasValue)
                    {
                        var __agg1Current = __agg1Input.GetValueOrDefault();
                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                        group.__agg1.HasValue = true;
                    }
                }
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, int? __value1, decimal __value2)
            {
                Id = __value0;
                Bucket = __value1;
                Amount = __value2;
            }

            public decimal Amount { get; }
            public int? Bucket { get; }
            public int Id { get; }
        }

        private sealed class Cte0Statement0Row0
        {
            public Cte0Statement0Row0(int __value0, decimal __value1, int[] __value2, int? __value3, int? __value4)
            {
                r_Key = __value0;
                r_Amount = __value1;
                r_Items = __value2;
                i_Value = __value3;
                i_Ordinal = __value4;
            }

            public int? i_Ordinal { get; }
            public int? i_Value { get; }
            public decimal r_Amount { get; }
            public int[] r_Items { get; }
            public int r_Key { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte0Statement0Row0> Slot1;
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public readonly int __key0;
            public ResultAggregateGroup(int __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg1);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1, decimal? __value2)
            {
                Id = __value0;
                Even = __value1;
                Odd = __value2;
            }

            public override int Count => 3;
            public decimal? Even { get; private set; }
            public int Id { get; private set; }
            public decimal? Odd { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Even = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Odd = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Even),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Odd),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Even" => true,
                "Odd" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Even, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Odd, ref visitor);
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id, decimal? Even, decimal? Odd)
            {
                this.Id = Id;
                this.Even = Even;
                this.Odd = Odd;
            }

            public decimal? Even { get; }
            public int Id { get; }
            public decimal? Odd { get; }
        }
    }
}
