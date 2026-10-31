// === Parsed Query ===
/*
SELECT a.City,
                     (
                         SELECT Sum(b.Population)
                         FROM #B.entities() b
                         WHERE b.Country = a.Country
                     ) AS CountryPopulation
              FROM #A.entities() a
*/

// === Logical Plan ===
/*
Cte
  Definition [_sq_1]
    MultiStatement
      Project [b.Country as b.Country, AggRef(b.Sum(b.Population)) as b.Sum(b.Population)]
        Aggregate [keys: b.Country] [aggs: Sum(Population)]
          SchemaScan [#B.entities() as b]
      Project [b.Country as _sq_1_corr_0, b.Sum(b.Population) as _sq_1_value]
        CteRef [bScore as bScore]
  Query
    MultiStatement
      Project [a.City as a.City, a.Country as a.Country, _sq_1._sq_1_corr_0 as _sq_1._sq_1_corr_0, _sq_1._sq_1_value as _sq_1._sq_1_value]
        Join [LeftSingle] [(_sq_1._sq_1_corr_0 = a.Country)]
          SchemaScan [#A.entities() as a]
          CteRef [_sq_1 as _sq_1]
      Project [a.City as City, _sq_1._sq_1_value as CountryPopulation]
        CteRef [a_sq_1 as a_sq_1]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [_sq_1]
    PhysicalMultiStatement
      PhysicalProject [b.Country as b.Country, AggRef(b.Sum(b.Population)) as b.Sum(b.Population)]
        PhysicalSingleKeyAggregate [key: b.Country (String)] [aggs: Sum(Population)]
          PhysicalSchemaScan [#B.entities() as b]
      PhysicalProject [b.Country as _sq_1_corr_0, b.Sum(b.Population) as _sq_1_value]
        PhysicalCteRef [bScore as bScore]
  Query
    PhysicalMultiStatement
      PhysicalProject [a.City as a.City, _sq_1._sq_1_value as _sq_1._sq_1_value]
        PhysicalHashJoin [LeftSingle] [build: _sq_1._sq_1_corr_0] [probe: a.Country]
          PhysicalSchemaScan [#A.entities() as a]
          PhysicalCteRef [_sq_1 as _sq_1]
      PhysicalProject [a.City as City, _sq_1._sq_1_value as CountryPopulation]
        PhysicalCteRef [a_sq_1 as a_sq_1]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [b: BasicEntity]
      Country: string <- property Country
      Population: decimal <- property Population
    AggregateGroup [Cte0AggregateGroup; keys: 1; typed aggs: 1]
    Generated [Cte0Row0]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    SourceEntity [a: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    TableRow [_sq_1]
      _sq_1_corr_0: string <- field _sq_1_corr_0
      _sq_1_value: decimal? <- field _sq_1_value
    Generated [ResultRow0]
      City: string <- field City
      CountryPopulation: decimal? <- field CountryPopulation

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [From:cte0]
    SourceScan [b: BasicEntity] -> cte0_bRows
    CreateTable [cte0: Cte0Row0]
    PhaseBoundary [GroupBy:cte0]
    CreateSingleKeyAggregateContext [cte0Groups: string -> Cte0AggregateGroup]
    ChunkedForEach [b in cte0_bRows]
      GetOrAddSingleKeyAggregateGroup [cte0Group = cte0Groups[b.Country] by b.Country; typed: Cte0AggregateGroup]
      Let [population: decimal = b.Population]
      TypedAggregateSet [Set(cte0Group.__agg0, population)]
    EnsureCapacity [cte0 <- cte0GroupsToFinalize.Count]
    PhaseBoundary [Select:cte0]
    ForEach [cte0FinalGroup in cte0GroupsToFinalize]
      AppendRow [cte0 <- Cte0Row0(_sq_1_corr_0: cte0FinalGroup.b.Country, _sq_1_value: b.Sum(b.Population))]
    StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]
    PhaseBoundary [End:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:cte1]
    SourceScan [a: BasicEntity] -> aRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateHash [_sq_1Hash: string -> Row; capacity: _cteRowResults.Slot0.Count]
    ForEach [_sq_1 in _cteRowResults.Slot0]
      HashAdd [_sq_1Hash[_sq_1._sq_1_corr_0] += _sq_1]
    ChunkedForEach [a in aRows]
      HashProbe [_sq_1Hash[a.Country] -> _sq_1HashMatches] [match: _sq_1HashHasMatch]
        ForEach [_sq_1 in _sq_1HashMatches]
          Assign [_sq_1HashHasMatch = TRUE]
          AppendShape [result <- ResultRow0(City: a.City, CountryPopulation: _sq_1._sq_1_value)]
      HashProbeNoMatch
        AppendShape [result <- ResultRow0(City: a.City, CountryPopulation: NULL)]
    PhaseBoundary [End:cte1]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q140_CorrelatedScalarAggregateSubquery
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
            new Column("_sq_1_corr_0", typeof(string), 0),
            new Column("_sq_1_value", typeof(decimal?), 1)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("CountryPopulation", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Country", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                OnPhaseChanged("compiled", QueryPhase.From);
                _cteRowResults.Slot0 = BuildCte0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, __musoqProgressContext, OnDataSourceProgress, OnQueryProgress, OnPhaseChanged, _cteRowResults);
                OnPhaseChanged("compiled", QueryPhase.Select);
                OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
                try
                {
                    var __aSchema = provider.GetSchema("#A");
                    var aRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__aSchema, "entities", new SourceExecutionContext("a:2", sourceExecutionPlans["a:2"], token, __schemaColumns_compiled_a_2, sourceRuntimeSettingsBySourceContextId["a:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                    var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(aRowsSource.Chunks, __musoqProgressContext, "a:2") : aRowsSource.Chunks;
                    var _sq_1Hash = new Dictionary<string, HashJoinBucket<Cte0Row0>>(_cteRowResults.Slot0.Count);
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
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(_sq_1Hash, key, out var matchesExists);
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
                                    string key = a.Country;
                                    if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                    {
                                        foreach (var _sq_1 in _sq_1HashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            _sq_1HashHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultRow0(a.City, _sq_1._sq_1_value));
                                        }
                                    }

                                    if (!_sq_1HashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(a.City, null));
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
                                    string key = a.Country;
                                    if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                                    {
                                        foreach (var _sq_1 in _sq_1HashMatches)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            _sq_1HashHasMatch = true;
                                            __musoqFinalShapeRows.Add(new ResultRow0(a.City, _sq_1._sq_1_value));
                                        }
                                    }

                                    if (!_sq_1HashHasMatch)
                                    {
                                        __musoqFinalShapeRows.Add(new ResultRow0(a.City, null));
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
                            string key = a.Country;
                            if (key != null && _sq_1Hash.TryGetValue(key, out var _sq_1HashMatches))
                            {
                                foreach (var _sq_1 in _sq_1HashMatches)
                                {
                                    token.ThrowIfCancellationRequested();
                                    _sq_1HashHasMatch = true;
                                    __musoqFinalShapeRows.Add(new ResultRow0(a.City, _sq_1._sq_1_value));
                                }
                            }

                            if (!_sq_1HashHasMatch)
                            {
                                __musoqFinalShapeRows.Add(new ResultRow0(a.City, null));
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte1", QueryPhase.End);
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
                var __cte0_bSchema = provider.GetSchema("#B");
                var cte0_bRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__cte0_bSchema, "entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_0, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var cte0_bRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(cte0_bRowsSource.Chunks, __musoqProgressContext, "b:2") : cte0_bRowsSource.Chunks;
                var cte0 = new List<Cte0Row0>();
                var cte0GroupsToFinalize = new List<Cte0AggregateGroup>();
                var cte0Groups = new Dictionary<string, Cte0AggregateGroup>();
                Cte0AggregateGroup cte0NullGroup = null;
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
                                string groupKey = b.Country;
                                Cte0AggregateGroup cte0Group = null;
                                if (groupKey != null)
                                {
                                    ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                    if (!cte0GroupExists)
                                    {
                                        cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                        cte0GroupsToFinalize.Add(cte0GroupRef);
                                    }

                                    cte0Group = cte0GroupRef;
                                }
                                else
                                {
                                    if (cte0NullGroup == null)
                                    {
                                        cte0NullGroup = new Cte0AggregateGroup(null);
                                        cte0GroupsToFinalize.Add(cte0NullGroup);
                                    }

                                    cte0Group = cte0NullGroup;
                                }

                                decimal population = b.Population;
                                {
                                    var __agg0Input = (decimal?)population;
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
                                string groupKey = b.Country;
                                Cte0AggregateGroup cte0Group = null;
                                if (groupKey != null)
                                {
                                    ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                                    if (!cte0GroupExists)
                                    {
                                        cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                        cte0GroupsToFinalize.Add(cte0GroupRef);
                                    }

                                    cte0Group = cte0GroupRef;
                                }
                                else
                                {
                                    if (cte0NullGroup == null)
                                    {
                                        cte0NullGroup = new Cte0AggregateGroup(null);
                                        cte0GroupsToFinalize.Add(cte0NullGroup);
                                    }

                                    cte0Group = cte0NullGroup;
                                }

                                decimal population = b.Population;
                                {
                                    var __agg0Input = (decimal?)population;
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

                    for (int bIndex = 0, bIndexCount = bChunk.Count; bIndex < bIndexCount; ++bIndex)
                    {
                        if ((bIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var b = bChunk[bIndex];
                        string groupKey = b.Country;
                        Cte0AggregateGroup cte0Group = null;
                        if (groupKey != null)
                        {
                            ref var cte0GroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte0Groups, groupKey, out var cte0GroupExists);
                            if (!cte0GroupExists)
                            {
                                cte0GroupRef = new Cte0AggregateGroup(groupKey);
                                cte0GroupsToFinalize.Add(cte0GroupRef);
                            }

                            cte0Group = cte0GroupRef;
                        }
                        else
                        {
                            if (cte0NullGroup == null)
                            {
                                cte0NullGroup = new Cte0AggregateGroup(null);
                                cte0GroupsToFinalize.Add(cte0NullGroup);
                            }

                            cte0Group = cte0NullGroup;
                        }

                        decimal population = b.Population;
                        {
                            var __agg0Input = (decimal?)population;
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
                    cte0.Add(new Cte0Row0(cte0FinalGroup.__key0, cte0FinalGroup.__agg0.HasValue ? (decimal?)cte0FinalGroup.__agg0.Value : null));
                }

                return cte0;
            }
            finally
            {
                OnPhaseChanged("compiled:cte0", QueryPhase.End);
            }
        }

        private sealed class Cte0AggregateGroup
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public Cte0AggregateGroup(string __key0)
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
            public Cte0Row0(string __value0, decimal? __value1)
            {
                _sq_1_corr_0 = __value0;
                _sq_1_value = __value1;
            }

            public string _sq_1_corr_0 { get; }
            public decimal? _sq_1_value { get; }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                CountryPopulation = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? CountryPopulation { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.CountryPopulation = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.CountryPopulation),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "CountryPopulation" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.CountryPopulation, ref visitor);
            }
        }
    }
}
