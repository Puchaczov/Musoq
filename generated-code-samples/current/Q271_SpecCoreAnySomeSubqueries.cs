// === Parsed Query ===
/*
select a.City, a.Country = ANY (select b.Country from #B.entities() b) as AnyMatch, a.Population > SOME (select c.Population from #C.entities() c) as SomeMatch from #A.entities() a
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [b.Country as Country]
        SchemaScan [#B.entities() as b]
  Definition [_sq_2]
    MultiStatement
      Project [1 as _sq_2_key, c.Population as _sq_2_corr_0]
        Filter [c.Population IS NOT NULL]
          SchemaScan [#C.entities() as c]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, a.Population as a.Population, _sq_1.Country as _sq_1.Country]
        Join [LeftMark] [(a.Country = _sq_1.Country)]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1.a.Population as a.Population, a_sq_1._sq_1.Country as _sq_1.Country, _sq_2._sq_2_key as _sq_2_key, _sq_2._sq_2_corr_0 as _sq_2_corr_0]
        Join [LeftMark] [((1 = _sq_2._sq_2_key) AND (a_sq_1.a.Population IS NOT NULL AND (a_sq_1.a.Population > _sq_2._sq_2_corr_0)))]
          CteRef [a_sq_1 as a_sq_1]
          CteRef [_sq_2 as _sq_2]
      Project [a.City as a.City, _sq_1.Country IS NOT NULL as AnyMatch, _sq_2._sq_2_key IS NOT NULL as SomeMatch]
        CteRef [a_sq_1_sq_2 as a_sq_1_sq_2]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.Country as Country]
        PhysicalSchemaScan [#B.entities() as b]
  Definition [_sq_2]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_2_key, c.Population as _sq_2_corr_0]
        PhysicalFilter [c.Population IS NOT NULL]
          PhysicalSchemaScan [#C.entities() as c]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, a.Country as a.Country, a.Population as a.Population, _sq_1.Country as _sq_1.Country]
        PhysicalHashJoin [LeftMark] [build: _sq_1.Country] [probe: a.Country]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a_sq_1.a.City as a.City, a_sq_1.a.Country as a.Country, a_sq_1.a.Population as a.Population, a_sq_1._sq_1.Country as _sq_1.Country, _sq_2._sq_2_key as _sq_2_key, _sq_2._sq_2_corr_0 as _sq_2_corr_0]
        PhysicalSortMergeJoin [LeftMark] [left: a_sq_1.a.Population] [right: _sq_2._sq_2_corr_0] [op: >] [residual: ((1 = _sq_2._sq_2_key) AND (a_sq_1.a.Population IS NOT NULL AND (a_sq_1.a.Population > _sq_2._sq_2_corr_0)))]
          PhysicalCteRef [a_sq_1 as a_sq_1]
          PhysicalCteRef [_sq_2 as _sq_2]
      PhysicalProject [a.City as a.City, _sq_1.Country IS NOT NULL as AnyMatch, _sq_2._sq_2_key IS NOT NULL as SomeMatch]
        PhysicalCteRef [a_sq_1_sq_2 as a_sq_1_sq_2]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [c: BasicEntity]
      Population: decimal <- property Population
    Generated [Cte1Row0]
      _sq_2_key: int <- field _sq_2_key
      _sq_2_corr_0: decimal <- field _sq_2_corr_0
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
    TableRow [_sq_1]
      Country: string <- field Country
    Generated [Statement0Row0]
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      _sq_1.Country: string <- field _sq_1_Country
    TableRow [a_sq_1]
      a.City: string <- field a_City
      a.Country: string <- field a_Country
      a.Population: decimal <- field a_Population
      _sq_1.Country: string <- field _sq_1_Country
    TableRow [_sq_2]
      _sq_2_key: int <- field _sq_2_key
      _sq_2_corr_0: decimal <- field _sq_2_corr_0
    Generated [ResultRow0]
      a.City: string <- field a_City
      AnyMatch: bool <- field AnyMatch
      SomeMatch: bool <- field SomeMatch

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte1]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [End:cte0]
    PhaseBoundary [From:cte1]
    SourceScan [c: BasicEntity] -> cte1_cRows
    CreateTable [cte1: Cte1Row0]
    PhaseBoundary [Where:cte1]
    PhaseBoundary [Select:cte1]
    ChunkedForEach [c in cte1_cRows]
      Let [population: decimal = c.Population]
      If [population IS NOT NULL]
        AppendRow [cte1 <- Cte1Row0(_sq_2_key: 1, _sq_2_corr_0: population)]
    StoreTable [cte1 -> _cteRowResults.Slot1: List<Cte1Row0>]
    PhaseBoundary [End:cte1]
    PhaseBoundary [Begin:cte2]
    SourceScan [a: BasicEntity] -> statement0_aRows
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [statement0: Statement0Row0]
    CreateKeySet [statement0_sq_1Keys: string]
    ChunkedForEach [b in cte0_bRows]
      KeySetAdd [statement0_sq_1Keys += b.Country]
    ChunkedForEach [a in statement0_aRows]
      KeySetProbe [statement0_sq_1Keys[a.Country]]
        Let [country: string = a.Country]
        AppendRow [statement0 <- Statement0Row0(a.City: a.City, a.Country: country, a.Population: a.Population, _sq_1.Country: country)]
      KeySetProbeNoMatch
        AppendRow [statement0 <- Statement0Row0(a.City: a.City, a.Country: a.Country, a.Population: a.Population, _sq_1.Country: NULL)]
    StoreTable [statement0 -> _cteRowResults.Slot2: List<Statement0Row0>]
    PhaseBoundary [End:cte2]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte3]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateRangeIndex [resultRangeIndex <- _cteRowResults.Slot1 by _sq_2RangeCandidate._sq_2_corr_0 >]
    ForEach [a_sq_1 in _cteRowResults.Slot2]
      RangeProbe [_sq_2 <- resultRangeIndex where a_sq_1.a.Population] [match: resultRangeHasMatch]
        Let [_sq_2_key: int? = _sq_2._sq_2_key]
        Let [a_Population: decimal = a_sq_1.a.Population]
        If [((1 = _sq_2_key) AND (a_Population IS NOT NULL AND (a_Population > _sq_2._sq_2_corr_0)))]
          Assign [resultRangeHasMatch = TRUE]
          AppendShape [result <- ResultShape0(a.City: a_sq_1.a.City, AnyMatch: a_sq_1._sq_1.Country IS NOT NULL, SomeMatch: _sq_2_key IS NOT NULL)]
          Break
      RangeProbeNoMatch
        AppendShape [result <- ResultShape0(a.City: a_sq_1.a.City, AnyMatch: a_sq_1._sq_1.Country IS NOT NULL, SomeMatch: FALSE)]
    PhaseBoundary [End:cte3]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q271_SpecCoreAnySomeSubqueries
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
            new Column("_sq_2_key", typeof(int), 0),
            new Column("_sq_2_corr_0", typeof(decimal), 1)
        };
        private static readonly Column[] __columns_compiled_result_5 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("AnyMatch", typeof(bool), 1),
            new Column("SomeMatch", typeof(bool), 2)
        };
        private static readonly Column[] __columns_compiled_statement0_4 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("a.Country", typeof(string), 1),
            new Column("a.Population", typeof(decimal), 2),
            new Column("_sq_1.Country", typeof(string), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_3 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 12) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Population", typeof(decimal), 13) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_5, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City, __musoqShapeRow.AnyMatch, __musoqShapeRow.SomeMatch);
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
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot1 = BuildCte1(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                _cteRowResults.Slot2 = BuildCte2(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte3", QueryPhase.Begin);
                try
                {
                    var resultRangeIndex = EvaluationHelper.CreateRangeJoinIndex<Cte1Row0, decimal?>(_cteRowResults.Slot1, (_sq_2RangeCandidate) => _sq_2RangeCandidate._sq_2_corr_0, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterThan);
                    var __storedTable2Rows = _cteRowResults.Slot2;
                    for (int __storedTable2Index = 0; __storedTable2Index < __storedTable2Rows.Count; ++__storedTable2Index)
                    {
                        if ((__storedTable2Index & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        Statement0Row0 a_sq_1 = __storedTable2Rows[__storedTable2Index];
                        {
                            bool resultRangeHasMatch = false;
                            foreach (var _sq_2 in resultRangeIndex.Find(a_sq_1.a_Population))
                            {
                                int? _sq_2_key = _sq_2._sq_2_key;
                                decimal a_Population = a_sq_1.a_Population;
                                if (((Operators.SqlCompare<int, int?>(1, _sq_2_key, (int __sqlLeft, int? __sqlRight) => (__sqlLeft == __sqlRight)) & ((a_Population != null) & Operators.SqlCompare<decimal, decimal?>(a_Population, _sq_2._sq_2_corr_0, (decimal __sqlLeft, decimal? __sqlRight) => (__sqlLeft > __sqlRight))))) == true)
                                {
                                    resultRangeHasMatch = true;
                                    __musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, (a_sq_1._sq_1_Country != null), (_sq_2_key != null)));
                                    break;
                                }
                            }

                            if (!resultRangeHasMatch)
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(a_sq_1.a_City, (a_sq_1._sq_1_Country != null), false));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte3", QueryPhase.End);
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
        private static List<Cte1Row0> BuildCte1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                var __cte1_cSchema = provider.GetSchema("#C");
                var cte1_cRowsSource = __cte1_cSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("c:3", sourceExecutionPlans["c:3"], token, __schemaColumns_compiled_c_0, sourceRuntimeSettingsBySourceContextId["c:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte1_cRowsSource.Chunks, __musoqProgressContext, "c:3") : cte1_cRowsSource.Chunks;
                var cte1 = new List<Cte1Row0>();
                foreach (var cChunk in cte1_cRows)
                {
                    if (cChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> cChunkView)
                    {
                        if (cChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] cChunkViewArray)
                        {
                            int cChunkViewOffset = cChunkView.Offset;
                            for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                            {
                                if ((cIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var c = cChunkViewArray[cChunkViewOffset + cIndex];
                                decimal population = c.Population;
                                if ((population != null))
                                {
                                    cte1.Add(new Cte1Row0(1, population));
                                }
                            }

                            continue;
                        }

                        if (cChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> cChunkViewList)
                        {
                            int cChunkViewOffset = cChunkView.Offset;
                            for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                            {
                                if ((cIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var c = cChunkViewList[cChunkViewOffset + cIndex];
                                decimal population = c.Population;
                                if ((population != null))
                                {
                                    cte1.Add(new Cte1Row0(1, population));
                                }
                            }

                            continue;
                        }
                    }

                    for (int cIndex = 0, cIndexCount = cChunk.Count; cIndex < cIndexCount; ++cIndex)
                    {
                        if ((cIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var c = cChunk[cIndex];
                        decimal population = c.Population;
                        if ((population != null))
                        {
                            cte1.Add(new Cte1Row0(1, population));
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
        private static List<Statement0Row0> BuildCte2(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, QueryRunContext? __musoqProgressContext, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Musoq.Evaluator.QueryProgressEventHandler OnQueryProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults)
        {
            OnPhaseChanged("compiled:cte2", QueryPhase.Begin);
            try
            {
                var __statement0_aSchema = provider.GetSchema("#A");
                var statement0_aRowsSource = __statement0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:3", sourceExecutionPlans["a:3"], token, __schemaColumns_compiled_a_2, sourceRuntimeSettingsBySourceContextId["a:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var statement0_aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(statement0_aRowsSource.Chunks, __musoqProgressContext, "a:3") : statement0_aRowsSource.Chunks;
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_3, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var statement0 = new List<Statement0Row0>();
                var statement0_sq_1Keys = new HashSet<string>();
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
                                string key = b.Country;
                                if (key == null)
                                    continue;
                                statement0_sq_1Keys.Add(key);
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
                                string key = b.Country;
                                if (key == null)
                                    continue;
                                statement0_sq_1Keys.Add(key);
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
                        string key = b.Country;
                        if (key == null)
                            continue;
                        statement0_sq_1Keys.Add(key);
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
                                string key = a.Country;
                                if (key != null && statement0_sq_1Keys.Contains(key))
                                {
                                    string country = a.Country;
                                    statement0.Add(new Statement0Row0(a.City, country, a.Population, country));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(a.City, a.Country, a.Population, null));
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
                                string key = a.Country;
                                if (key != null && statement0_sq_1Keys.Contains(key))
                                {
                                    string country = a.Country;
                                    statement0.Add(new Statement0Row0(a.City, country, a.Population, country));
                                }
                                else
                                {
                                    statement0.Add(new Statement0Row0(a.City, a.Country, a.Population, null));
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
                        string key = a.Country;
                        if (key != null && statement0_sq_1Keys.Contains(key))
                        {
                            string country = a.Country;
                            statement0.Add(new Statement0Row0(a.City, country, a.Population, country));
                        }
                        else
                        {
                            statement0.Add(new Statement0Row0(a.City, a.Country, a.Population, null));
                        }
                    }
                }

                return statement0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte2", QueryPhase.End);
            }
        }

        private sealed class Cte1Row0
        {
            public Cte1Row0(int __value0, decimal __value1)
            {
                _sq_2_key = __value0;
                _sq_2_corr_0 = __value1;
            }

            public decimal _sq_2_corr_0 { get; }
            public int _sq_2_key { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte1Row0> Slot1;
            public List<Statement0Row0> Slot2;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, bool __value1, bool __value2)
            {
                a_City = __value0;
                AnyMatch = __value1;
                SomeMatch = __value2;
            }

            public bool AnyMatch { get; private set; }
            public override int Count => 3;
            public bool SomeMatch { get; private set; }
            public string a_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    case 1:
                        AnyMatch = (bool)value;
                        break;
                    case 2:
                        SomeMatch = (bool)value;
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
                "AnyMatch" => true,
                "SomeMatch" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                1 => (object)AnyMatch,
                2 => (object)SomeMatch,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "City" => (object)a_City,
                "AnyMatch" => (object)AnyMatch,
                "SomeMatch" => (object)SomeMatch,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City, bool AnyMatch, bool SomeMatch)
            {
                this.a_City = a_City;
                this.AnyMatch = AnyMatch;
                this.SomeMatch = SomeMatch;
            }

            public bool AnyMatch { get; }
            public bool SomeMatch { get; }
            public string a_City { get; }
        }

        private sealed class Statement0Row0
        {
            public Statement0Row0(string __value0, string __value1, decimal __value2, string __value3)
            {
                a_City = __value0;
                a_Country = __value1;
                a_Population = __value2;
                _sq_1_Country = __value3;
            }

            public string _sq_1_Country { get; }
            public string a_City { get; }
            public string a_Country { get; }
            public decimal a_Population { get; }
        }
    }
}
