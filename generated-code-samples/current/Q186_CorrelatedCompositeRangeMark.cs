// === Parsed Query ===
/*
SELECT a.City,
                     CASE WHEN EXISTS (
                         SELECT b.City
                         FROM #B.entities() b
                         WHERE b.Country = a.Country
                           AND b.City = a.City
                           AND b.Population < a.Population
                     ) THEN 'Y' ELSE 'N' END AS HasEarlierPeer
              FROM #A.entities() a
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [1 as _sq_1_key, b.Country as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Population as _sq_1_corr_2]
        SchemaScan [#B.entities() as b]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, a.Population as a.Population, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2]
        Join [LeftMark] [((1 = _sq_1._sq_1_key) AND (((_sq_1._sq_1_corr_0 = a.Country) AND (_sq_1._sq_1_corr_1 = a.City)) AND (_sq_1._sq_1_corr_2 < a.Population)))]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a.City as a.City, CASE WHEN _sq_1._sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END as HasEarlierPeer]
        CteRef [a_sq_1 as a_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_1_key, b.Country as _sq_1_corr_0, b.City as _sq_1_corr_1, b.Population as _sq_1_corr_2]
        PhysicalSchemaScan [#B.entities() as b]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, a.Country as a.Country, a.Population as a.Population, _sq_1._sq_1_key as _sq_1._sq_1_key, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_corr_1 as _sq_1._sq_1_corr_1, _sq_1._sq_1_corr_2 as _sq_1._sq_1_corr_2]
        PhysicalSortMergeJoin [LeftMark] [left: a.Population] [right: _sq_1._sq_1_corr_2] [op: >] [partitions: a.Country = _sq_1._sq_1_corr_0, a.City = _sq_1._sq_1_corr_1] [residual: ((1 = _sq_1._sq_1_key) AND (((_sq_1._sq_1_corr_0 = a.Country) AND (_sq_1._sq_1_corr_1 = a.City)) AND (_sq_1._sq_1_corr_2 < a.Population)))]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a.City as a.City, CASE WHEN _sq_1._sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END as HasEarlierPeer]
        PhysicalCteRef [a_sq_1 as a_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    Generated [Cte0Row0]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: decimal <- field _sq_1_corr_2
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    TableRow [_sq_1]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: string <- field _sq_1_corr_1
      _sq_1_corr_2: decimal <- field _sq_1_corr_2
    Generated [ResultRow0]
      a.City: string <- field a_City
      HasEarlierPeer: string <- field HasEarlierPeer

  Body
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    ChunkedForEach [b in cte0_bRows]
      AppendRow [cte0 <- Cte0Row0(_sq_1_key: 1, _sq_1_corr_0: b.Country, _sq_1_corr_1: b.City, _sq_1_corr_2: b.Population)]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    CtePhase [cte1]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateRangeIndex [resultRangeIndex <- _cteRowResults.Slot0 by _sq_1RangeCandidate._sq_1_corr_0, _sq_1RangeCandidate._sq_1_corr_1, _sq_1RangeCandidate._sq_1_corr_2 >]
    ChunkedForEach [a in aRows]
      RangeProbe [_sq_1 <- resultRangeIndex where a.Country = _sq_1RangeCandidate._sq_1_corr_0 and a.City = _sq_1RangeCandidate._sq_1_corr_1 and a.Population] [match: resultRangeHasMatch]
        Let [_sq_1_key: int? = _sq_1._sq_1_key]
        Let [city: string = a.City]
        If [((1 = _sq_1_key) AND (((_sq_1._sq_1_corr_0 = a.Country) AND (_sq_1._sq_1_corr_1 = city)) AND (_sq_1._sq_1_corr_2 < a.Population)))]
          Assign [resultRangeHasMatch = TRUE]
          AppendShape [result <- ResultShape0(a.City: city, HasEarlierPeer: CASE WHEN _sq_1_key IS NOT NULL THEN 'Y' ELSE 'N' END)]
          Break
      RangeProbeNoMatch
        AppendShape [result <- ResultShape0(a.City: a.City, HasEarlierPeer: CASE WHEN FALSE THEN 'Y' ELSE 'N' END)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q186_CorrelatedCompositeRangeMark
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
            new Column("_sq_1_key", typeof(int), 0),
            new Column("_sq_1_corr_0", typeof(string), 1),
            new Column("_sq_1_corr_1", typeof(string), 2),
            new Column("_sq_1_corr_2", typeof(decimal), 3)
        };
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("a.City", typeof(string), 0),
            new Column("HasEarlierPeer", typeof(string), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Country", typeof(string), 12), new Column("Population", typeof(decimal), 13) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City, __musoqShapeRow.HasEarlierPeer);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, _cteRowResults);
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = aRowsSource.Chunks;
                var resultRangeIndex = EvaluationHelper.CreateRangeJoinIndex<Cte0Row0, ValueTuple<string, string>?, decimal?>(_cteRowResults.Slot0, (_sq_1RangeCandidate) => _sq_1RangeCandidate._sq_1_corr_0 == null || _sq_1RangeCandidate._sq_1_corr_1 == null ? default(ValueTuple<string, string>?) : (_sq_1RangeCandidate._sq_1_corr_0, _sq_1RangeCandidate._sq_1_corr_1), (_sq_1RangeCandidate) => _sq_1RangeCandidate._sq_1_corr_2, Musoq.Evaluator.IR.Expressions.BinaryOpKind.GreaterThan);
                foreach (var aChunk in aRows)
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
                                {
                                    bool resultRangeHasMatch = false;
                                    foreach (var _sq_1 in resultRangeIndex.Find(a.Country == null || a.City == null ? default(ValueTuple<string, string>?) : (a.Country, a.City), a.Population))
                                    {
                                        int? _sq_1_key = _sq_1._sq_1_key;
                                        string city = a.City;
                                        if (((1 == _sq_1_key) && (((_sq_1._sq_1_corr_0 == a.Country) && (_sq_1._sq_1_corr_1 == city)) && (_sq_1._sq_1_corr_2 < a.Population))))
                                        {
                                            resultRangeHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(city, (_sq_1_key != null) ? (string)"Y" : (string)"N"));
                                            break;
                                        }
                                    }

                                    if (!resultRangeHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, false ? (string)"Y" : (string)"N"));
                                    }
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
                                {
                                    bool resultRangeHasMatch = false;
                                    foreach (var _sq_1 in resultRangeIndex.Find(a.Country == null || a.City == null ? default(ValueTuple<string, string>?) : (a.Country, a.City), a.Population))
                                    {
                                        int? _sq_1_key = _sq_1._sq_1_key;
                                        string city = a.City;
                                        if (((1 == _sq_1_key) && (((_sq_1._sq_1_corr_0 == a.Country) && (_sq_1._sq_1_corr_1 == city)) && (_sq_1._sq_1_corr_2 < a.Population))))
                                        {
                                            resultRangeHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultShape0(city, (_sq_1_key != null) ? (string)"Y" : (string)"N"));
                                            break;
                                        }
                                    }

                                    if (!resultRangeHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultShape0(a.City, false ? (string)"Y" : (string)"N"));
                                    }
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
                        {
                            bool resultRangeHasMatch = false;
                            foreach (var _sq_1 in resultRangeIndex.Find(a.Country == null || a.City == null ? default(ValueTuple<string, string>?) : (a.Country, a.City), a.Population))
                            {
                                int? _sq_1_key = _sq_1._sq_1_key;
                                string city = a.City;
                                if (((1 == _sq_1_key) && (((_sq_1._sq_1_corr_0 == a.Country) && (_sq_1._sq_1_corr_1 == city)) && (_sq_1._sq_1_corr_2 < a.Population))))
                                {
                                    resultRangeHasMatch = true;
                                    __musoqFinalShapeRows.Add(new ResultShape0(city, (_sq_1_key != null) ? (string)"Y" : (string)"N"));
                                    break;
                                }
                            }

                            if (!resultRangeHasMatch)
                            {
                                __musoqFinalShapeRows.Add(new ResultShape0(a.City, false ? (string)"Y" : (string)"N"));
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
            var __cte0_bSchema = provider.GetSchema("#B");
            var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_bRows = cte0_bRowsSource.Chunks;
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
                            cte0.Add(new Cte0Row0(1, b.Country, b.City, b.Population));
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
                            cte0.Add(new Cte0Row0(1, b.Country, b.City, b.Population));
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
                    cte0.Add(new Cte0Row0(1, b.Country, b.City, b.Population));
                }
            }

            return cte0;
        }

        private sealed class Cte0Row0
        {
            public Cte0Row0(int __value0, string __value1, string __value2, decimal __value3)
            {
                _sq_1_key = __value0;
                _sq_1_corr_0 = __value1;
                _sq_1_corr_1 = __value2;
                _sq_1_corr_2 = __value3;
            }

            public string _sq_1_corr_0 { get; }
            public string _sq_1_corr_1 { get; }
            public decimal _sq_1_corr_2 { get; }
            public int _sq_1_key { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1)
            {
                a_City = __value0;
                HasEarlierPeer = __value1;
            }

            public override int Count => 2;
            public string HasEarlierPeer { get; private set; }
            public string a_City { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_City = (string)value;
                        break;
                    case 1:
                        HasEarlierPeer = (string)value;
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
                "HasEarlierPeer" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_City,
                1 => (object)HasEarlierPeer,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.City" => (object)a_City,
                "a_City" => (object)a_City,
                "City" => (object)a_City,
                "HasEarlierPeer" => (object)HasEarlierPeer,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City, string HasEarlierPeer)
            {
                this.a_City = a_City;
                this.HasEarlierPeer = HasEarlierPeer;
            }

            public string HasEarlierPeer { get; }
            public string a_City { get; }
        }
    }
}
