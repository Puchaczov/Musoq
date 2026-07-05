/*
raw query string

SELECT a.City, b.City
              FROM #A.entities() a
              INNER JOIN #B.entities() b ON b.City = (
                  SELECT c.City
                  FROM #C.entities() c
                  WHERE c.Country = a.Country
              )
*/

/*
logical plan representation string

Cte
  Definition [_sq_1]
    MultiStatement
      Project [c.Country as c.Country, AggRef(c.__ScalarSubqueryValue(c.City)) as c.__ScalarSubqueryValue(c.City)]
        Aggregate [keys: c.Country] [aggs: __ScalarSubqueryValue(City)]
          SchemaScan [#C.entities() as c]
      Project [c.Country as _sq_1_corr_0, c.__ScalarSubqueryValue(c.City) as _sq_1_value]
        CteRef [cScore as cScore]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftOuter] [(_sq_1._sq_1_corr_0 = a.Country)]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1._sq_1._sq_1_value as _sq_1._sq_1_value, b.City as City]
        Join [Inner] [(b.City = a_sq_1._sq_1._sq_1_value)]
          CteRef [a_sq_1 as a_sq_1]
          SchemaScan [#B.entities() as b]
      Project [a.City as a.City, b.City as b.City]
        CteRef [a_sq_1b as a_sq_1b]
*/

/*
physical plan representation string

PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [c.Country as c.Country, AggRef(c.__ScalarSubqueryValue(c.City)) as c.__ScalarSubqueryValue(c.City)]
        PhysicalSingleKeyAggregate [key: c.Country (String)] [aggs: __ScalarSubqueryValue(City)]
          PhysicalSchemaScan [#C.entities() as c]
      PhysicalProject [c.Country as _sq_1_corr_0, c.__ScalarSubqueryValue(c.City) as _sq_1_value]
        PhysicalCteRef [cScore as cScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, a.Country as a.Country, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftOuter] [build: _sq_1._sq_1_corr_0] [probe: a.Country]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1._sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, a_sq_1._sq_1._sq_1_value as _sq_1._sq_1_value, b.City as City]
        PhysicalHashJoin [Inner] [build: a_sq_1._sq_1._sq_1_value] [probe: b.City]
          PhysicalCteRef [a_sq_1 as a_sq_1]
          PhysicalSchemaScan [#B.entities() as b]
      PhysicalProject [a.City as a.City, b.City as b.City]
        PhysicalCteRef [a_sq_1b as a_sq_1b]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [c: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: string <- field _sq_1_value
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    TableRow [_sq_1]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: string <- field _sq_1_value
    Generated [Statement0Row0]
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_value: string <- field _sq_1__sq_1_value
    TableRow [a_sq_1]
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      _sq_1._sq_1_corr_0: string <- field _sq_1__sq_1_corr_0
      _sq_1._sq_1_value: string <- field _sq_1__sq_1_value
    SourceEntity [b: BasicEntity]
      City: string <- property City
    Generated [ResultRow0]
      a.City: string <- field a_City
      b.City: string <- field b_City

  Body
    SourceScan [c: BasicEntity] -> cte0_cRows
    CreateTable [cte0: Cte0Row0]
    CreateSingleKeyAggregateContext [cte0Groups: string -> Cte0AggregateGroup]
    ParallelSingleKeyAggregateLoop [c in cte0_cRows by c.Country; threshold 4096, sample 8192/6144, maxDegree 24, group Cte0AggregateGroup]
      ParallelAccumulate
        Let [city: string = c.City]
        TypedAggregateSet [Set(cte0Group.__agg0, city)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(_sq_1_corr_0: cte0FinalGroup.c.Country, _sq_1_value: c.__ScalarSubqueryValue(c.City))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    SourceScan [a: BasicEntity] -> statement0_aRows
    CreateTable [statement0: Statement0Row0]
    CreateHash [statement0_sq_1Hash: string -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      HashAdd [statement0_sq_1Hash[_sq_1._sq_1_corr_0] += _sq_1]
    ChunkedForEach [a in statement0_aRows]
      HashProbe [statement0_sq_1Hash[a.Country] -> statement0_sq_1HashMatches] [match: statement0_sq_1HashHasMatch]
        ForEach [_sq_1 in statement0_sq_1HashMatches]
          Assign [statement0_sq_1HashHasMatch = TRUE]
          AppendRow [statement0 <- Statement0Row0(a.City: a.City, a.Country: a.Country, _sq_1._sq_1_corr_0: _sq_1._sq_1_corr_0, _sq_1._sq_1_value: _sq_1._sq_1_value)]
      HashProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.City: a.City, a.Country: a.Country, _sq_1._sq_1_corr_0: NULL, _sq_1._sq_1_value: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot1: List<Statement0Row0>]
    CtePhase [cte2]
    SourceScan [b: BasicEntity] -> bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [a_sq_1Hash: string -> Row; capacity: _cteRowResults.Slot1.Count]
    ForEach [a_sq_1 in _cteRowResults.Slot1]
      HashAdd [a_sq_1Hash[a_sq_1._sq_1._sq_1_value] += a_sq_1]
    ChunkedForEach [b in bRows]
      HashProbe [a_sq_1Hash[b.City] -> a_sq_1HashMatches]
        ForEach [a_sq_1 in a_sq_1HashMatches]
          AppendShape [result <- ResultShape0(a.City: a_sq_1.a.City, b.City: b.City)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q141_ScalarSubqueryJoinOn
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_cte0_1 = new Column[]
        {
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sq_1_value", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_result_4 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("b.City", typeof(string), 1)
        };
        private static readonly Column[] __columns_compiled_statement0_2 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("a.Country", typeof(string), 1),
            new Column("_sq_1._sq_1_corr_0", typeof(string), 2),
            new Column("_sq_1._sq_1_value", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_3 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_4, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City, __musoqShapeRow.b_City);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_3, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var bRows = bRowsSource.Chunks;
                var a_sq_1Hash = new Dictionary<string, HashJoinBucket<Statement0Row0>>(_cteRowResults.Slot1.Count);
                var __storedTable1Rows = _cteRowResults.Slot1;
                for (int __storedTable1Index = 0; __storedTable1Index < __storedTable1Rows.Count; ++__storedTable1Index)
                {
                    if ((__storedTable1Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Statement0Row0 a_sq_1 = __storedTable1Rows[__storedTable1Index];
                    string key = a_sq_1._sq_1__sq_1_value;
                    if (key == null)
                        continue;
                    {
                        ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(a_sq_1Hash, key, out var matchesExists);
                        if (!matchesExists)
                        {
                            matches = new HashJoinBucket<Statement0Row0>(a_sq_1);
                        }
                        else
                        {
                            matches.Add(a_sq_1);
                        }
                    }
                }

                foreach (var bChunk in bRows)
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
                                if (key != null && a_sq_1Hash.TryGetValue(key, out var a_sq_1HashMatches))
                                {
                                    foreach (var a_sq_1 in a_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, b.City));
                                    }
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
                                string key = b.City;
                                if (key != null && a_sq_1Hash.TryGetValue(key, out var a_sq_1HashMatches))
                                {
                                    foreach (var a_sq_1 in a_sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        __musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, b.City));
                                    }
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
                        string key = b.City;
                        if (key != null && a_sq_1Hash.TryGetValue(key, out var a_sq_1HashMatches))
                        {
                            foreach (var a_sq_1 in a_sq_1HashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                __musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, b.City));
                            }
                        }
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
                OnPhaseChanged("compiled", QueryPhase.End);
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
        private static List<Cte0Row0> BuildCte0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __cte0_cSchema = provider.GetSchema("#C");
            var cte0_cRowsSource = __cte0_cSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("c:2", sourceExecutionPlans["c:2"], token, __schemaColumns_compiled_c_0, sourceRuntimeSettingsBySourceContextId["c:2"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_cRows = cte0_cRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
            var cte0Groups = new Dictionary<string, Cte0AggregateGroup>();
            cte0GroupsToFinalize = ParallelSingleKeyAggregate_0(cte0_cRows, 24, token);
            cte0.EnsureCapacity(cte0GroupsToFinalize.Count);
            foreach (var cte0FinalGroup in cte0GroupsToFinalize)
            {
                token.ThrowIfCancellationRequested();
                cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, Musoq.Plugins.ScalarSubqueryAggregateKernel<string>.Get(in cte0FinalGroup.__agg0)));
            }

            return cte0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Statement0Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, CteRowResults _cteRowResults)
        {
            var __statement0_aSchema = provider.GetSchema("#A");
            var statement0_aRowsSource = __statement0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_c_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var statement0_aRows = statement0_aRowsSource.Chunks;
            var statement0 = new List<Statement0Row0>();
            var statement0_sq_1Hash = new Dictionary<string, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
            var __storedTable0Rows = _cteRowResults.Slot0;
            for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
            {
                if ((__storedTable0Index & 1023) == 0)
                {
                    token.ThrowIfCancellationRequested();
                }

                Cte0Row0 _sq_1 = __storedTable0Rows[__storedTable0Index];
                string key = _sq_1._sq_1_corr_0;
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

            foreach (var aChunk in statement0_aRows)
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
                            bool statement0_sq_1HashHasMatch = false;
                            string key = a.Country;
                            if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                            {
                                foreach (var _sq_1 in statement0_sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0_sq_1HashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.City, a.Country, _sq_1._sq_1_corr_0, _sq_1._sq_1_value));
                                }
                            }

                            if (!statement0_sq_1HashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.City, a.Country, null, null));
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
                            bool statement0_sq_1HashHasMatch = false;
                            string key = a.Country;
                            if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                            {
                                foreach (var _sq_1 in statement0_sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    statement0_sq_1HashHasMatch = true;
                                    statement0.Add(new Statement0Row0(a.City, a.Country, _sq_1._sq_1_corr_0, _sq_1._sq_1_value));
                                }
                            }

                            if (!statement0_sq_1HashHasMatch)
                            {
                                statement0.Add(new Statement0Row0(a.City, a.Country, null, null));
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
                    bool statement0_sq_1HashHasMatch = false;
                    string key = a.Country;
                    if (key != null && statement0_sq_1Hash.TryGetValue(key, out var statement0_sq_1HashMatches))
                    {
                        foreach (var _sq_1 in statement0_sq_1HashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            statement0_sq_1HashHasMatch = true;
                            statement0.Add(new Statement0Row0(a.City, a.Country, _sq_1._sq_1_corr_0, _sq_1._sq_1_value));
                        }
                    }

                    if (!statement0_sq_1HashHasMatch)
                    {
                        statement0.Add(new Statement0Row0(a.City, a.Country, null, null));
                    }
                }
            }

            return statement0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ParallelSingleKeyAggregateChunk_0(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk, Dictionary<string, Cte0AggregateGroup> groups, List<Cte0AggregateGroup> orderedGroups, ref Cte0AggregateGroup nullGroup, CancellationToken cancellationToken)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity c = chunk[index];
                string groupKey = c.Country;
                Cte0AggregateGroup cte0Group = null;
                if (groupKey != null)
                {
                    ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var cte0GroupExists);
                    if (!cte0GroupExists)
                    {
                        cte0GroupRef = new Cte0AggregateGroup(groupKey);
                        orderedGroups.Add(cte0GroupRef);
                    }

                    cte0Group = cte0GroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new Cte0AggregateGroup(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    cte0Group = nullGroup;
                }

                string city = c.City;
                Musoq.Plugins.ScalarSubqueryAggregateKernel<string>.Set(ref cte0Group.__agg0, (string)city);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<Cte0AggregateGroup> ParallelSingleKeyAggregate_0(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<Cte0AggregateGroup>>();
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            Parallel.ForEach<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>, ParallelSingleKeyAggregateChunkWorker_0>(rows, options, () => new ParallelSingleKeyAggregateChunkWorker_0(cancellationToken), (chunk, _, worker) =>
            {
                worker.ProcessChunk(chunk ?? Array.Empty<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>());
                return worker;
            }, worker =>
            {
                if (worker.OrderedGroups.Count != 0)
                {
                    shards.Enqueue(worker.OrderedGroups);
                }
            });
            var mergedGroups = new Dictionary<string, Cte0AggregateGroup>();
            var groupsToFinalize = new List<Cte0AggregateGroup>();
            Cte0AggregateGroup nullGroup = null;
            foreach (var shard in shards)
            {
                foreach (var sourceGroup in shard)
                {
                    string groupKey = sourceGroup.__key0;
                    if (groupKey != null)
                    {
                        ref var mergedGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(mergedGroups, groupKey, out var mergedGroupExists);
                        if (!mergedGroupExists)
                        {
                            mergedGroupRef = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            mergedGroupRef.MergeFrom(sourceGroup);
                        }
                    }
                    else
                    {
                        if (nullGroup == null)
                        {
                            nullGroup = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            nullGroup.MergeFrom(sourceGroup);
                        }
                    }
                }
            }

            return groupsToFinalize;
        }

        private sealed class Cte0AggregateGroup
        {
            public Musoq.Plugins.ScalarSubqueryAggregateKernel<string>.State __agg0;
            public readonly string __key0;
            public Cte0AggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(Cte0AggregateGroup source)
            {
                Musoq.Plugins.ScalarSubqueryAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(string __value0, string __value1)
            {
                _sq_1_corr_0 = __value0;
                _sq_1_value = __value1;
            }

            public string _sq_1_corr_0 { get; }
            public string _sq_1_value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
            public List<Statement0Row0> Slot1;
        }

        private sealed class ParallelSingleKeyAggregateChunkWorker_0
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Dictionary<string, Cte0AggregateGroup> _groups = new Dictionary<string, Cte0AggregateGroup>();
            private Cte0AggregateGroup _nullGroup;
            private readonly List<Cte0AggregateGroup> _orderedGroups = new List<Cte0AggregateGroup>();
            public ParallelSingleKeyAggregateChunkWorker_0(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<Cte0AggregateGroup> OrderedGroups => _orderedGroups;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_0(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                a_City = __value0;
                b_City = __value1;
            }

            public override int Count => 2;
            public string a_City { get; private set; }
            public string b_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    case 1:
                        b_City = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.City" => true,
                "a_City" => true,
                "b.City" => true,
                "b_City" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                1 => (object)b_City,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "b.City" => (object)b_City,
                "b_City" => (object)b_City,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City, string b_City)
            {
                this.a_City = a_City;
                this.b_City = b_City;
            }

            public string a_City { get; }
            public string b_City { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, string __value2, string __value3)
            {
                a_City = __value0;
                a_Country = __value1;
                _sq_1__sq_1_corr_0 = __value2;
                _sq_1__sq_1_value = __value3;
            }

            public string _sq_1__sq_1_corr_0 { get; }
            public string _sq_1__sq_1_value { get; }
            public string a_City { get; }
            public string a_Country { get; }
        }
    }
}
