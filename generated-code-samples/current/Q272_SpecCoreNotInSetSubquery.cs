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
        Filter [b.City IS NULL]
          SchemaScan [#B.entities() as b]
  Query
    MultiStatement
      Project [1 as 1, AggRef(a.Count(a.City)) as a.Count(a.City)]
        Aggregate [keys: 1] [aggs: Count(City)]
          Filter [a.City IS NOT NULL]
            Join [LeftAntiSemi] [(1 = _sq_2._sq_2_key)]
              Join [LeftAntiSemi] [(a.City = _sq_1.City)]
                SchemaScan [#A.entities() as a]
                CteRef [_sq_1 as _sq_1]
              CteRef [_sq_2 as _sq_2]
      Project [a.Count(a.City) as NotMemberCount]
        CteRef [a_sq_1_sq_2Score as a_sq_1_sq_2Score]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.City as City]
        PhysicalSchemaScan [#B.entities() as b] [pushdown: b.City IS NULL]
  Definition [_sq_2]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_2_key]
        PhysicalFilter [b.City IS NULL]
          PhysicalSchemaScan [#B.entities() as b] [pushdown: b.City IS NULL]
  Query
    PhysicalMultiStatement
      PhysicalProject [1 as 1, AggRef(a.Count(a.City)) as a.Count(a.City)]
        PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(City)]
          PhysicalFilter [a.City IS NOT NULL]
            PhysicalHashJoin [LeftAntiSemi] [build: _sq_2._sq_2_key] [probe: 1]
              PhysicalHashJoin [LeftAntiSemi] [build: _sq_1.City] [probe: a.City]
                PhysicalSchemaScan [#A.entities() as a]
                PhysicalCteRef [_sq_1 as _sq_1]
              PhysicalCteRef [_sq_2 as _sq_2]
      PhysicalProject [a.Count(a.City) as NotMemberCount]
        PhysicalCteRef [a_sq_1_sq_2Score as a_sq_1_sq_2Score]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      City: string <- property City
    Generated [Cte1Row0]
      _sq_2_key: int <- field _sq_2_key
    SourceEntity [a: BasicEntity]
      City: string <- property City
    SourceEntity [b: BasicEntity]
      City: string <- property City
    TableRow [_sq_1]
      City: string <- field City
    Generated [join_0_a__sq_1Row0]
      a.City: string <- field a_City
    TableRow [join_0_a__sq_1]
      a.City: string <- generated row join_0_a__sq_1Row0.a_City
    TableRow [_sq_2]
      _sq_2_key: int <- field _sq_2_key
    Generated [join_0_a__sq_1__sq_2Row0]
      a.City: string <- field a_City
    TableRow [join_0_a__sq_1__sq_2]
      a.City: string <- generated row join_0_a__sq_1__sq_2Row0.a_City
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 1]
    Generated [ResultRow0]
      NotMemberCount: long <- field NotMemberCount

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [End:cte0]
    PhaseBoundary [From:cte1]
    SourceScan [b: BasicEntity] -> cte1_bRows
    CreateTable [cte1: Cte1Row0]
    PhaseBoundary [Where:cte1]
    PhaseBoundary [Select:cte1]
    ChunkedForEach [b in cte1_bRows]
      If [b.City IS NULL]
        AppendRow [cte1 <- Cte1Row0(_sq_2_key: 1)]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> join_0_a__sq_1Table_aRows
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [join_0_a__sq_1Table: join_0_a__sq_1Row0]
    CreateKeySet [join_0_a__sq_1Table_sq_1Keys: string]
    PhaseBoundary [Where]
    ChunkedForEach [b in cte0_bRows]
      KeySetAdd [join_0_a__sq_1Table_sq_1Keys += b.City]
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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ForEach [join_0_a__sq_1__sq_2 in join_0_a__sq_1__sq_2Table.Rows]
      Let [a_City: string = join_0_a__sq_1__sq_2.a.City]
      If [a_City IS NOT NULL]
        EnsureAggregateGroup [group; typed: ResultAggregateGroup]
        TypedAggregateSet [Set(group.__agg0, a_City)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(NotMemberCount: a.Count(a.City))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte1_1 = new Column[]
        {
            new Column("_sq_2_key", typeof(int), 0)
        };
        private static readonly Column[] __columns_compiled_join_0_a__sq_1Table_2 = new Column[]
        {
            new Column("a.City", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("NotMemberCount", typeof(long), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

        public event DataSourceEventHandler DataSourceProgress;
        public event QueryPhaseEventHandler PhaseChanged;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.NotMemberCount);
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __join_0_a__sq_1Table_aSchema = provider.GetSchema("#A");
                var join_0_a__sq_1Table_aRowsSource = __join_0_a__sq_1Table_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var join_0_a__sq_1Table_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(join_0_a__sq_1Table_aRowsSource.Chunks, __musoqProgressContext, "a:1") : join_0_a__sq_1Table_aRowsSource.Chunks;
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var join_0_a__sq_1Table = new Table("join_0_a__sq_1Table", __columns_compiled_join_0_a__sq_1Table_2);
                var join_0_a__sq_1Table_sq_1Keys = new HashSet<string>();
                OnPhaseChanged("compiled", QueryPhase.Where);
                BuildJoin0ASq1TableSq1Keys(cte0_bRows, join_0_a__sq_1Table_sq_1Keys, token);
                AppendLeftJoinRows(join_0_a__sq_1Table_aRows, join_0_a__sq_1Table_sq_1Keys, join_0_a__sq_1Table, token);
                var join_0_a__sq_1__sq_2Table = new Table("join_0_a__sq_1__sq_2Table", __columns_compiled_join_0_a__sq_1Table_2);
                join_0_a__sq_1__sq_2Table.EnsureCapacity(join_0_a__sq_1Table.Count);
                var join_0_a__sq_1__sq_2Table_sq_2Keys = new HashSet<int>(_cteRowResults.Slot1.Count);
                BuildJoin0ASq1Sq2TableSq2Keys(_cteRowResults.Slot1, join_0_a__sq_1__sq_2Table_sq_2Keys, token);
                AppendLeftJoinRows1(join_0_a__sq_1Table.Rows, join_0_a__sq_1__sq_2Table_sq_2Keys, join_0_a__sq_1__sq_2Table, token);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                foreach (var join_0_a__sq_1__sq_2 in join_0_a__sq_1__sq_2Table.Rows)
                {
                    token.ThrowIfCancellationRequested();
                    string a_City = ((join_0_a__sq_1__sq_2Row0)join_0_a__sq_1__sq_2).a_City;
                    if ((a_City != null))
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

                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__agg0.Count));
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
            foreach (var join_0_a__sq_1 in rows)
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
                    join_0_a__sq_1__sq_2Table.AddDirect(new join_0_a__sq_1__sq_2Row0(((join_0_a__sq_1Row0)join_0_a__sq_1).a_City));
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte1_bSchema = provider.GetSchema("#B");
                var cte1_bRowsSource = __cte1_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte1_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte1_bRowsSource.Chunks;
                var cte1 = new List<Cte1Row0>();
                foreach (var bChunk in cte1_bRows)
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
                                if ((b.City == null))
                                {
                                    cte1.Add(new Cte1Row0(1));
                                }
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
                                if ((b.City == null))
                                {
                                    cte1.Add(new Cte1Row0(1));
                                }
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
                        if ((b.City == null))
                        {
                            cte1.Add(new Cte1Row0(1));
                        }
                    }
                }

                return cte1;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
        private static void BuildJoin0ASq1TableSq1Keys(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, HashSet<string> join_0_a__sq_1Table_sq_1Keys, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (var bChunk in rows)
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
                            string key = b.City;
                            if (key == null)
                                continue;
                            join_0_a__sq_1Table_sq_1Keys.Add(key);
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
                            string key = b.City;
                            if (key == null)
                                continue;
                            join_0_a__sq_1Table_sq_1Keys.Add(key);
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
                    string key = b.City;
                    if (key == null)
                        continue;
                    join_0_a__sq_1Table_sq_1Keys.Add(key);
                }
            }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int __value0)
            {
                _sq_2_key = __value0;
            }

            public int _sq_2_key { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte1Row0> Slot1;
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0;
            public ResultAggregateGroup()
            {
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        NotMemberCount = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "NotMemberCount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)NotMemberCount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "NotMemberCount" => (object)NotMemberCount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(long NotMemberCount)
            {
                this.NotMemberCount = NotMemberCount;
            }

            public long NotMemberCount { get; }
        }

        private sealed class join_0_a__sq_1Row0 : Row
        {
            public join_0_a__sq_1Row0(string __value0)
            {
                a_City = __value0;
            }

            public override int Count => 1;
            public string a_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "City" => (object)a_City,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class join_0_a__sq_1__sq_2Row0 : Row
        {
            public join_0_a__sq_1__sq_2Row0(string __value0)
            {
                a_City = __value0;
            }

            public override int Count => 1;
            public string a_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "City" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "City" => (object)a_City,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
