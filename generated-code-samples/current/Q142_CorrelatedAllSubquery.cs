// === Parsed Query ===
/*
SELECT a.City
              FROM #A.entities() a
              WHERE a.Population > ALL (
                  SELECT b.Population
                  FROM #B.entities() b
                  WHERE b.Country = a.Country
              )
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [1 as _sq_1_key, b.Country as _sq_1_corr_0, b.Population as _sq_1_corr_1]
        SchemaScan [#B.entities() as b]
  Query
    MultiStatement
      Project [a.City as a.City]
        Join [LeftAntiSemi] [((1 = _sq_1._sq_1_key) AND ((_sq_1._sq_1_corr_0 = a.Country) AND ((a.Population IS NULL OR _sq_1._sq_1_corr_1 IS NULL) OR (a.Population <= _sq_1._sq_1_corr_1))))]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [1 as _sq_1_key, b.Country as _sq_1_corr_0, b.Population as _sq_1_corr_1]
        PhysicalSchemaScan [#B.entities() as b]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City]
        PhysicalHashJoin [LeftAntiSemi] [build: _sq_1._sq_1_key, _sq_1._sq_1_corr_0] [probe: 1, a.Country] [residual: ((a.Population IS NULL OR _sq_1._sq_1_corr_1 IS NULL) OR (a.Population <= _sq_1._sq_1_corr_1))]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
      Population: decimal <- property Population
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
      Population: decimal <- property Population
    HashPayload [_sq_1HashPayload0]
      _sq_1_corr_1: decimal <- field _sq_1_corr_1
    TableRow [_sq_1]
      _sq_1_key: int <- field _sq_1_key
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_corr_1: decimal <- field _sq_1_corr_1
    Generated [ResultRow0]
      a.City: string <- field a_City

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [End:cte0]
    PhaseBoundary [From]
    SourceScan [a: BasicEntity] -> aRows
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateHash [_sq_1Hash: ValueTuple<int, string> -> Row]
    ChunkedForEach [b in cte0_bRows]
      Let [key0: int = 1]
      Let [key1: string = b.Country]
      ContinueIf [(key1 = NULL)]
      Let [key: ValueTuple<int, string> = (key0, key1)]
      CreateHashPayload [_sq_1 <- _sq_1HashPayload0(_sq_1_corr_1: b.Population)]
      HashAdd [_sq_1Hash[key] += _sq_1]
    PhaseBoundary [Select]
    ChunkedForEach [a in aRows]
      HashProbe [_sq_1Hash[(1, a.Country)] -> _sq_1HashMatches] [match: _sq_1HashHasMatch]
        ForEach [_sq_1 in _sq_1HashMatches]
          Let [population: decimal = a.Population]
          Let [_sq_1_corr_1: decimal = _sq_1._sq_1_corr_1]
          If [((population IS NULL OR _sq_1_corr_1 IS NULL) OR (population <= _sq_1_corr_1))]
            Assign [_sq_1HashHasMatch = TRUE]
            Break
      HashProbeNoMatch
        AppendShape [result <- ResultShape0(a.City: a.City)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q142_CorrelatedAllSubquery
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("a.City", typeof(string), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1), new Column("Population", typeof(decimal), 2) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_2, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.a_City);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                try
                {
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                OnPhaseChanged("compiled", QueryPhase.From);
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:1") : aRowsSource.Chunks;
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = __cte0_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var _sq_1Hash = new Dictionary<ValueTuple<int, string>, HashJoinBucket<_sq_1HashPayload0>>();
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
                                int key0 = 1;
                                string key1 = b.Country;
                                if ((key1 == null))
                                {
                                    continue;
                                }

                                ValueTuple<int, string> key = (key0, key1);
                                _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(b.Population);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_sq_1Hash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                                    }
                                    else
                                    {
                                        matches.Add(_sq_1);
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
                                int key0 = 1;
                                string key1 = b.Country;
                                if ((key1 == null))
                                {
                                    continue;
                                }

                                ValueTuple<int, string> key = (key0, key1);
                                _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(b.Population);
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_sq_1Hash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                                    }
                                    else
                                    {
                                        matches.Add(_sq_1);
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
                        int key0 = 1;
                        string key1 = b.Country;
                        if ((key1 == null))
                        {
                            continue;
                        }

                        ValueTuple<int, string> key = (key0, key1);
                        _sq_1HashPayload0 _sq_1 = new _sq_1HashPayload0(b.Population);
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_sq_1Hash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<_sq_1HashPayload0>(_sq_1);
                            }
                            else
                            {
                                matches.Add(_sq_1);
                            }
                        }
                    }
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
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
                                bool _sq_1HashHasMatch = false;
                                var key0 = 1;
                                var key1 = a.Country;
                                var key = (key0, key1);
                                if (key1 != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in _sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        decimal population = a.Population;
                                        decimal _sq_1_corr_1 = _sq_1._sq_1_corr_1;
                                        if ((((population == null) || (_sq_1_corr_1 == null)) || (population <= _sq_1_corr_1)))
                                        {
                                            _sq_1HashHasMatch = true;
                                            break;
                                        }
                                    }
                                }

                                if (!_sq_1HashHasMatch)
                                {
                                    __musoqFinalShapeRows.Add(new ResultShape0(a.City));
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
                                bool _sq_1HashHasMatch = false;
                                var key0 = 1;
                                var key1 = a.Country;
                                var key = (key0, key1);
                                if (key1 != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                {
                                    foreach (var _sq_1 in _sq_1HashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        decimal population = a.Population;
                                        decimal _sq_1_corr_1 = _sq_1._sq_1_corr_1;
                                        if ((((population == null) || (_sq_1_corr_1 == null)) || (population <= _sq_1_corr_1)))
                                        {
                                            _sq_1HashHasMatch = true;
                                            break;
                                        }
                                    }
                                }

                                if (!_sq_1HashHasMatch)
                                {
                                    __musoqFinalShapeRows.Add(new ResultShape0(a.City));
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
                        bool _sq_1HashHasMatch = false;
                        var key0 = 1;
                        var key1 = a.Country;
                        var key = (key0, key1);
                        if (key1 != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                        {
                            foreach (var _sq_1 in _sq_1HashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                decimal population = a.Population;
                                decimal _sq_1_corr_1 = _sq_1._sq_1_corr_1;
                                if ((((population == null) || (_sq_1_corr_1 == null)) || (population <= _sq_1_corr_1)))
                                {
                                    _sq_1HashHasMatch = true;
                                    break;
                                }
                            }
                        }

                        if (!_sq_1HashHasMatch)
                        {
                            __musoqFinalShapeRows.Add(new ResultShape0(a.City));
                        }
                    }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0)
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

        private sealed class ResultShape0
        {
            public ResultShape0(string a_City)
            {
                this.a_City = a_City;
            }

            public string a_City { get; }
        }

        private readonly struct _sq_1HashPayload0
        {
            public readonly decimal _sq_1_corr_1;
            public _sq_1HashPayload0(decimal _sq_1_corr_1)
            {
                this._sq_1_corr_1 = _sq_1_corr_1;
            }
        }
    }
}
