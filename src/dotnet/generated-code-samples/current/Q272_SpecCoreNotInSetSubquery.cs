// === Parsed Query ===
/*
select Count(a.City) as NotMemberCount from #A.entities() a where a.City NOT IN (select b.City from #B.entities() b)
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [b.City as City]
        SchemaScan [#B.entities() as b]
  Definition [_sq_2]
    MultiStatement
      Project [1 as _sq_2_key]
        Filter [_sq_1.City IS NULL]
          CteRef [_sq_1 as _sq_1]
  Definition [_sq_3]
    MultiStatement
      Project [1 as _sq_3_key]
        CteRef [_sq_1 as _sq_1]
  Query
    MultiStatement
      Project [1 as 1, AggRef(a.Count(a.City)) as a.Count(a.City)]
        Aggregate [keys: 1] [aggs: Count(City)]
          Filter [(a.City IS NOT NULL OR NOT _sq_3._sq_3_key IS NOT NULL)]
            Join [LeftMark] [(1 = _sq_3._sq_3_key)]
              Join [LeftAntiSemi] [(1 = _sq_2._sq_2_key)]
                Join [LeftAntiSemi] [(a.City = _sq_1.City)]
                  SchemaScan [#A.entities() as a]
                  CteRef [_sq_1 as _sq_1]
                CteRef [_sq_2 as _sq_2]
              CteRef [_sq_3 as _sq_3]
      Project [a.Count(a.City) as NotMemberCount]
        CteRef [a_sq_1_sq_2_sq_3Score as a_sq_1_sq_2_sq_3Score]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.City as City]
        PhysicalSchemaScan [#B.entities() as b]
  Definition [_sq_2]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_2_key]
        PhysicalFilter [_sq_1.City IS NULL]
          PhysicalCteRef [_sq_1 as _sq_1]
  Definition [_sq_3]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_3_key]
        PhysicalCteRef [_sq_1 as _sq_1]
  Query
    PhysicalMultiStatement
      PhysicalProject [AggRef(a.Count(a.City)) as a.Count(a.City)]
        PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(City)]
          PhysicalFilter [(a.City IS NOT NULL OR NOT _sq_3._sq_3_key IS NOT NULL)]
            PhysicalHashJoin [LeftMark] [build: _sq_3._sq_3_key] [probe: 1]
              PhysicalHashJoin [LeftAntiSemi] [build: _sq_2._sq_2_key] [probe: 1]
                PhysicalHashJoin [LeftAntiSemi] [build: _sq_1.City] [probe: a.City]
                  PhysicalSchemaScan [#A.entities() as a]
                  PhysicalCteRef [_sq_1 as _sq_1]
                PhysicalCteRef [_sq_2 as _sq_2]
              PhysicalCteRef [_sq_3 as _sq_3]
      PhysicalProject [a.Count(a.City) as NotMemberCount]
        PhysicalCteRef [a_sq_1_sq_2_sq_3Score as a_sq_1_sq_2_sq_3Score]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      City: string <- property City
    Generated [Cte0Row0]
      City: string <- field City
    TableRow [_sq_1]
      City: string <- field City
    Generated [Cte1Row0]
      _sq_2_key: int <- field _sq_2_key
    TableRow [_sq_1]
      City: string <- field City
    Generated [Cte2Row0]
      _sq_3_key: int <- field _sq_3_key
    SourceEntity [a: BasicEntity]
      City: string <- property City
    TableRow [_sq_1]
      City: string <- field City
    Generated [join_0_a__sq_1Row0]
      a.City: string <- field a_City
    TableRow [join_0_a__sq_1]
      a.City: string <- field a_City
    TableRow [_sq_2]
      _sq_2_key: int <- field _sq_2_key
    Generated [join_0_a__sq_1__sq_2Row0]
      a.City: string <- field a_City
    TableRow [join_0_a__sq_1__sq_2]
      a.City: string <- field a_City
    TableRow [_sq_3]
      _sq_3_key: int <- field _sq_3_key
    Generated [join_0_a__sq_1__sq_2__sq_3Row0]
      a.City: string <- field a_City
      _sq_3._sq_3_key: int? <- field _sq_3__sq_3_key
    TableRow [join_0_a__sq_1__sq_2__sq_3]
      a.City: string <- field a_City
      _sq_3._sq_3_key: int? <- field _sq_3__sq_3_key
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 1]
    Generated [ResultRow0]
      NotMemberCount: long <- field NotMemberCount

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [Select:cte0]
    ChunkedForEach [b in cte0_bRows]
      AppendRow [cte0 <- Cte0Row0(City: b.City)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    ParallelBlock [cte-level-1, tasks 2, maxDegree 2]
      ParallelTask [_sq_2 -> __parallelCteLevel1Task0Result]
        PhaseBoundary [Begin:cte1]
        CreateTable [cte1: Cte1Row0]
        PhaseBoundary [From:cte1]
        ForEach [_sq_1 in _cteRowResults.Slot0]
          If [_sq_1.City IS NULL]
            AppendRow [cte1 <- Cte1Row0(_sq_2_key: 1)]
        PhaseBoundary [Where:cte1]
        PhaseBoundary [Select:cte1]
        Assign [__parallelCteLevel1Task0Result = cte1]
        PhaseBoundary [End:cte1]
      ParallelTask [_sq_3 -> __parallelCteLevel1Task1Result]
        PhaseBoundary [Begin:cte2]
        CreateTable [cte2: Cte2Row0]
        PhaseBoundary [From:cte2]
        ForEach [_sq_1 in _cteRowResults.Slot0]
          AppendRow [cte2 <- Cte2Row0(_sq_3_key: 1)]
        PhaseBoundary [Select:cte2]
        Assign [__parallelCteLevel1Task1Result = cte2]
        PhaseBoundary [End:cte2]
      ParallelMerge
        StoreTable [__parallelCteLevel1Task0Result -> _cteRowResults.Slot1: List<Cte1Row0>]
        StoreTable [__parallelCteLevel1Task1Result -> _cteRowResults.Slot2: List<Cte2Row0>]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> join_0_a__sq_1Table_aRows
    CreateTable [join_0_a__sq_1Table: join_0_a__sq_1Row0]
    CreateKeySet [join_0_a__sq_1Table_sq_1Keys: string; capacity: _cteRowResults.Slot0.Count]
    PhaseBoundary [Where]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      KeySetAdd [join_0_a__sq_1Table_sq_1Keys += _sq_1.City]
    ChunkedForEach [a in join_0_a__sq_1Table_aRows]
      KeySetProbe [join_0_a__sq_1Table_sq_1Keys[a.City]] [match: join_0_a__sq_1Table_sq_1KeysHasMatch]
        Assign [join_0_a__sq_1Table_sq_1KeysHasMatch = TRUE]
      KeySetProbeNoMatch
        AppendRow [join_0_a__sq_1Table <- join_0_a__sq_1Row0(a.City: a.City)]
    CreateTable [join_0_a__sq_1__sq_2Table: join_0_a__sq_1__sq_2Row0]
    CreateKeySet [join_0_a__sq_1__sq_2Table_sq_2Keys: int; capacity: _cteRowResults.Slot1.Count]
    ForEach [_sq_2 in _cteRowResults.Slot1]
      KeySetAdd [join_0_a__sq_1__sq_2Table_sq_2Keys += _sq_2._sq_2_key]
    ForEach [join_0_a__sq_1 in join_0_a__sq_1Table.Rows]
      KeySetProbe [join_0_a__sq_1__sq_2Table_sq_2Keys[1]] [match: join_0_a__sq_1__sq_2Table_sq_2KeysHasMatch]
        Assign [join_0_a__sq_1__sq_2Table_sq_2KeysHasMatch = TRUE]
      KeySetProbeNoMatch
        AppendRow [join_0_a__sq_1__sq_2Table <- join_0_a__sq_1__sq_2Row0(a.City: join_0_a__sq_1.a.City)]
    CreateTable [join_0_a__sq_1__sq_2__sq_3Table: join_0_a__sq_1__sq_2__sq_3Row0]
    CreateKeySet [join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys: int?; capacity: _cteRowResults.Slot2.Count]
    ForEach [_sq_3 in _cteRowResults.Slot2]
      KeySetAdd [join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys += _sq_3._sq_3_key]
    ForEach [join_0_a__sq_1__sq_2 in join_0_a__sq_1__sq_2Table.Rows]
      KeySetProbe [join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys[1]]
        AppendRow [join_0_a__sq_1__sq_2__sq_3Table <- join_0_a__sq_1__sq_2__sq_3Row0(a.City: join_0_a__sq_1__sq_2.a.City, _sq_3._sq_3_key: 1)]
      KeySetProbeNoMatch
        AppendRow [join_0_a__sq_1__sq_2__sq_3Table <- join_0_a__sq_1__sq_2__sq_3Row0(a.City: join_0_a__sq_1__sq_2.a.City, _sq_3._sq_3_key: NULL)]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ForEach [join_0_a__sq_1__sq_2__sq_3 in join_0_a__sq_1__sq_2__sq_3Table.Rows]
      Let [a_City: string = join_0_a__sq_1__sq_2__sq_3.a.City]
      If [(a_City IS NOT NULL OR NOT join_0_a__sq_1__sq_2__sq_3._sq_3._sq_3_key IS NOT NULL)]
        EnsureAggregateGroup [group; typed: ResultAggregateGroup]
        TypedAggregateSet [Set(group.__agg0, a_City)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(NotMemberCount: a.Count(a.City))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q272_SpecCoreNotInSetSubquery
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
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("City", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_cte1_2 = new Column[]
        {
            new Column("_sq_2_key", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_cte2_3 = new Column[]
        {
            new Column("_sq_3_key", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_join_0_a__sq_1Table_4 = new Column[]
        {
            new Column("a.City", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_join_0_a__sq_1__sq_2__sq_3Table_5 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("_sq_3._sq_3_key", typeof(int?), 1)
        };
        private static readonly Column[] __columns_compiled_result_6 = new Column[]
        {
            new Column("NotMemberCount", typeof(long), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_6, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
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
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                List<Cte1Row0> __parallelCteLevel1Task0Result = null;
                List<Cte2Row0> __parallelCteLevel1Task1Result = null;
                var cteLevel1Runner = new CteLevel1Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel1Runner.RunCteLevel1Task0, cteLevel1Runner.RunCteLevel1Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel1Task0Result = cteLevel1Runner.Task0Result;
                __parallelCteLevel1Task1Result = cteLevel1Runner.Task1Result;
                _cteRowResults.Slot1 = __parallelCteLevel1Task0Result;
                _cteRowResults.Slot2 = __parallelCteLevel1Task1Result;
                OnPhaseChanged("compiled", QueryPhase.From);
                var __join_0_a__sq_1Table_aSchema = provider.GetSchema("#A");
                var join_0_a__sq_1Table_aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__join_0_a__sq_1Table_aSchema, "entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["a:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var join_0_a__sq_1Table_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(join_0_a__sq_1Table_aRowsSource.Chunks, __musoqProgressContext, "a:1") : join_0_a__sq_1Table_aRowsSource.Chunks;
                var join_0_a__sq_1Table = new Table("join_0_a__sq_1Table", __columns_compiled_join_0_a__sq_1Table_4);
                var join_0_a__sq_1Table_sq_1Keys = new HashSet<string>(_cteRowResults.Slot0.Count);
                OnPhaseChanged("compiled", QueryPhase.Where);
                BuildJoin0ASq1TableSq1Keys(_cteRowResults.Slot0, join_0_a__sq_1Table_sq_1Keys, token);
                AppendLeftJoinRows(join_0_a__sq_1Table_aRows, join_0_a__sq_1Table_sq_1Keys, join_0_a__sq_1Table, token);
                var join_0_a__sq_1__sq_2Table = new Table("join_0_a__sq_1__sq_2Table", __columns_compiled_join_0_a__sq_1Table_4);
                join_0_a__sq_1__sq_2Table.EnsureCapacity(join_0_a__sq_1Table.Count);
                var join_0_a__sq_1__sq_2Table_sq_2Keys = new HashSet<int>(_cteRowResults.Slot1.Count);
                BuildJoin0ASq1Sq2TableSq2Keys(_cteRowResults.Slot1, join_0_a__sq_1__sq_2Table_sq_2Keys, token);
                AppendLeftJoinRows1(join_0_a__sq_1Table.Rows, join_0_a__sq_1__sq_2Table_sq_2Keys, join_0_a__sq_1__sq_2Table, token);
                var join_0_a__sq_1__sq_2__sq_3Table = new Table("join_0_a__sq_1__sq_2__sq_3Table", __columns_compiled_join_0_a__sq_1__sq_2__sq_3Table_5);
                join_0_a__sq_1__sq_2__sq_3Table.EnsureCapacity(join_0_a__sq_1__sq_2Table.Count);
                var join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys = new HashSet<int?>(_cteRowResults.Slot2.Count);
                BuildJoin0ASq1Sq2Sq3TableSq3Keys(_cteRowResults.Slot2, join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys, token);
                AppendLeftJoinRows2(join_0_a__sq_1__sq_2Table.Rows, join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys, join_0_a__sq_1__sq_2__sq_3Table, token);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                foreach (join_0_a__sq_1__sq_2__sq_3Row0 join_0_a__sq_1__sq_2__sq_3 in join_0_a__sq_1__sq_2__sq_3Table.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    string a_City = join_0_a__sq_1__sq_2__sq_3.a_City;
                    if (((a_City != null) || (!(join_0_a__sq_1__sq_2__sq_3._sq_3__sq_3_key != null))))
                    {
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        if ((string)a_City != null)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg0.Count));
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
        private static void AppendLeftJoinRows(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, HashSet<string> join_0_a__sq_1Table_sq_1Keys, Musoq.Evaluator.Tables.Table join_0_a__sq_1Table, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (var aChunk in rows)
            {
                if (aChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkView)
                {
                    if (aChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] aChunkViewArray)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewArray[aChunkViewOffset + aIndex];
                            bool join_0_a__sq_1Table_sq_1KeysHasMatch = false;
                            string key = a.City;
                            if (key != null && join_0_a__sq_1Table_sq_1Keys.Contains(key))
                            {
                                join_0_a__sq_1Table_sq_1KeysHasMatch = true;
                            }

                            if (!join_0_a__sq_1Table_sq_1KeysHasMatch)
                            {
                                join_0_a__sq_1Table.AddDirect(new join_0_a__sq_1Row0(a.City));
                            }
                        }

                        continue;
                    }

                    if (aChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> aChunkViewList)
                    {
                        int aChunkViewOffset = aChunkView.Offset;
                        for (int aIndex = 0, aIndexCount = aChunkView.Count; aIndex < aIndexCount; ++aIndex)
                        {
                            if ((aIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var a = aChunkViewList[aChunkViewOffset + aIndex];
                            bool join_0_a__sq_1Table_sq_1KeysHasMatch = false;
                            string key = a.City;
                            if (key != null && join_0_a__sq_1Table_sq_1Keys.Contains(key))
                            {
                                join_0_a__sq_1Table_sq_1KeysHasMatch = true;
                            }

                            if (!join_0_a__sq_1Table_sq_1KeysHasMatch)
                            {
                                join_0_a__sq_1Table.AddDirect(new join_0_a__sq_1Row0(a.City));
                            }
                        }

                        continue;
                    }
                }

                for (int aIndex = 0, aIndexCount = aChunk.Count; aIndex < aIndexCount; ++aIndex)
                {
                    if ((aIndex & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var a = aChunk[aIndex];
                    bool join_0_a__sq_1Table_sq_1KeysHasMatch = false;
                    string key = a.City;
                    if (key != null && join_0_a__sq_1Table_sq_1Keys.Contains(key))
                    {
                        join_0_a__sq_1Table_sq_1KeysHasMatch = true;
                    }

                    if (!join_0_a__sq_1Table_sq_1KeysHasMatch)
                    {
                        join_0_a__sq_1Table.AddDirect(new join_0_a__sq_1Row0(a.City));
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void AppendLeftJoinRows1(IEnumerable<Musoq.Evaluator.Tables.Row> rows, HashSet<int> join_0_a__sq_1__sq_2Table_sq_2Keys, Musoq.Evaluator.Tables.Table join_0_a__sq_1__sq_2Table, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (join_0_a__sq_1Row0 join_0_a__sq_1 in rows)
            {
                token.ThrowIfCancellationRequested();
                bool join_0_a__sq_1__sq_2Table_sq_2KeysHasMatch = false;
                int key = 1;
                if (join_0_a__sq_1__sq_2Table_sq_2Keys.Contains(key))
                {
                    join_0_a__sq_1__sq_2Table_sq_2KeysHasMatch = true;
                }

                if (!join_0_a__sq_1__sq_2Table_sq_2KeysHasMatch)
                {
                    join_0_a__sq_1__sq_2Table.AddDirect(new join_0_a__sq_1__sq_2Row0(join_0_a__sq_1.a_City));
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void AppendLeftJoinRows2(IEnumerable<Musoq.Evaluator.Tables.Row> rows, HashSet<int?> join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys, Musoq.Evaluator.Tables.Table join_0_a__sq_1__sq_2__sq_3Table, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (join_0_a__sq_1__sq_2Row0 join_0_a__sq_1__sq_2 in rows)
            {
                token.ThrowIfCancellationRequested();
                int? key = 1;
                if (key != null && join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys.Contains(key))
                {
                    join_0_a__sq_1__sq_2__sq_3Table.AddDirect(new join_0_a__sq_1__sq_2__sq_3Row0(join_0_a__sq_1__sq_2.a_City, 1));
                }
                else
                {
                    join_0_a__sq_1__sq_2__sq_3Table.AddDirect(new join_0_a__sq_1__sq_2__sq_3Row0(join_0_a__sq_1__sq_2.a_City, null));
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            try
            {
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_bSchema, "entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["b:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                foreach (var bChunk in cte0_bRows)
                {
                    if (bChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkView)
                    {
                        if (bChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] bChunkViewArray)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewArray[bChunkViewOffset + bIndex];
                                cte0.Add(new Cte0Row0(b.City));
                            }

                            continue;
                        }

                        if (bChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> bChunkViewList)
                        {
                            int bChunkViewOffset = bChunkView.Offset;
                            for (int bIndex = 0, bIndexCount = bChunkView.Count; bIndex < bIndexCount; ++bIndex)
                            {
                                if ((bIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var b = bChunkViewList[bChunkViewOffset + bIndex];
                                cte0.Add(new Cte0Row0(b.City));
                            }

                            continue;
                        }
                    }

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        cte0.Add(new Cte0Row0(b.City));
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
        private static List<Cte1Row0> BuildCteLevel1Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            List<Cte1Row0> __parallelCteLevel1Task0Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            List<Cte1Row0> cte1 = null!;
            try
            {
                cte1 = new List<Cte1Row0>(_cteRowResults.Slot0.Count);
                OnPhaseChanged("compiled:cte1", QueryPhase.From);
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                    if ((_sq_1.City == null))
                    {
                        cte1.Add(new Cte1Row0(1));
                    }
                }

                OnPhaseChanged("compiled:cte1", QueryPhase.Where);
                OnPhaseChanged("compiled:cte1", QueryPhase.Select);
                __parallelCteLevel1Task0Result = cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }

            return __parallelCteLevel1Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte2Row0> BuildCteLevel1Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            List<Cte2Row0> __parallelCteLevel1Task1Result = null;
            token.ThrowIfCancellationRequested();
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            List<Cte2Row0> cte2 = null!;
            try
            {
                cte2 = new List<Cte2Row0>(_cteRowResults.Slot0.Count);
                OnPhaseChanged("compiled:cte2", QueryPhase.From);
                var __storedTable0Rows1 = _cteRowResults.Slot0;
                for (int __storedTable0Rows1Index = 0; __storedTable0Rows1Index < __storedTable0Rows1.Count; ++__storedTable0Rows1Index)
                {
                    if ((__storedTable0Rows1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 _sq_1 = __storedTable0Rows1[__storedTable0Rows1Index];
                    cte2.Add(new Cte2Row0(1));
                }

                OnPhaseChanged("compiled:cte2", QueryPhase.Select);
                __parallelCteLevel1Task1Result = cte2;
            }
            finally
            {
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
            }

            return __parallelCteLevel1Task1Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void BuildJoin0ASq1Sq2Sq3TableSq3Keys(IReadOnlyList<Cte2Row0> rows, HashSet<int?> join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int rowsIndex = 0; rowsIndex < rows.Count; ++rowsIndex)
            {
                if ((rowsIndex & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Cte2Row0 _sq_3 = (Cte2Row0)rows[rowsIndex];
                int? key = _sq_3._sq_3_key;
                if (key == null)
                    continue;
                join_0_a__sq_1__sq_2__sq_3Table_sq_3Keys.Add(key);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void BuildJoin0ASq1Sq2TableSq2Keys(IReadOnlyList<Cte1Row0> rows, HashSet<int> join_0_a__sq_1__sq_2Table_sq_2Keys, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int rowsIndex = 0; rowsIndex < rows.Count; ++rowsIndex)
            {
                if ((rowsIndex & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Cte1Row0 _sq_2 = (Cte1Row0)rows[rowsIndex];
                int key = _sq_2._sq_2_key;
                join_0_a__sq_1__sq_2Table_sq_2Keys.Add(key);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void BuildJoin0ASq1TableSq1Keys(IReadOnlyList<Cte0Row0> rows, HashSet<string> join_0_a__sq_1Table_sq_1Keys, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            for (int rowsIndex = 0; rowsIndex < rows.Count; ++rowsIndex)
            {
                if ((rowsIndex & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Cte0Row0 _sq_1 = (Cte0Row0)rows[rowsIndex];
                string key = _sq_1.City;
                if (key == null)
                    continue;
                join_0_a__sq_1Table_sq_1Keys.Add(key);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0)
            {
                City = __value0;
            }

            public string City { get; }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int __value0)
            {
                _sq_2_key = __value0;
            }

            public int _sq_2_key { get; }
        }

        private sealed class Cte2Row0
        {
            public Cte2Row0(int __value0)
            {
                _sq_3_key = __value0;
            }

            public int _sq_3_key { get; }
        }

        private sealed class CteLevel1Runner
        {
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly QueryRunContext? _musoqProgressContext;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Evaluator.QueryProgressEventHandler _onQueryProgress;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel1Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _musoqProgressContext = __musoqProgressContext;
                _onDataSourceProgress = OnDataSourceProgress;
                _onQueryProgress = OnQueryProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
            }

            public List<Cte1Row0> Task0Result { get; private set; }
            public List<Cte2Row0> Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel1Task0()
            {
                Task0Result = BuildCteLevel1Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel1Task1()
            {
                Task1Result = BuildCteLevel1Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _musoqProgressContext, _onDataSourceProgress, _onQueryProgress, _onPhaseChanged, _cteRowResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Cte1Row0> Slot1;
            public List<Cte2Row0> Slot2;
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Reset(ref this.__agg0);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(long __value0)
            {
                NotMemberCount = __value0;
            }

            public override int Count => 1;
            public long NotMemberCount { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.NotMemberCount = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.NotMemberCount),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "NotMemberCount" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.NotMemberCount, ref visitor);
            }
        }

        private sealed class join_0_a__sq_1Row0 : Row
        {
            public join_0_a__sq_1Row0(string __value0)
            {
                a_City = __value0;
            }

            public override int Count => 1;
            public string a_City { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.a_City, ref visitor);
            }
        }

        private sealed class join_0_a__sq_1__sq_2Row0 : Row
        {
            public join_0_a__sq_1__sq_2Row0(string __value0)
            {
                a_City = __value0;
            }

            public override int Count => 1;
            public string a_City { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.a_City, ref visitor);
            }
        }

        private sealed class join_0_a__sq_1__sq_2__sq_3Row0 : Row
        {
            public join_0_a__sq_1__sq_2__sq_3Row0(string __value0, int? __value1)
            {
                a_City = __value0;
                _sq_3__sq_3_key = __value1;
            }

            public override int Count => 2;
            public int? _sq_3__sq_3_key { get; private set; }
            public string a_City { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this._sq_3__sq_3_key = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.a_City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int?>(this._sq_3__sq_3_key),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                "_sq_3._sq_3_key" => true,
                "_sq_3__sq_3_key" => true,
                "_sq_3_key" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.a_City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this._sq_3__sq_3_key, ref visitor);
            }
        }
    }
}
