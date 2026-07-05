/*
raw query string

with leftCte as (select a.City as City from #A.entities() a), rightCte as (select b.City as City, b.Name as Name from #B.entities() b) select l.City as City, Count(r.Name) as MatchCount from leftCte l inner join rightCte r on l.City = r.City group by l.City
*/

/*
logical plan representation string

Cte
  Definition [leftCte]
    MultiStatement
      Project [a.City as City]
        SchemaScan [#A.entities() as a]
  Definition [rightCte]
    MultiStatement
      Project [b.City as City, b.Name as Name]
        SchemaScan [#B.entities() as b]
  Query
    MultiStatement
      Project [l.City as l.City, r.City as r.City, r.Name as r.Name]
        Join [Inner] [(l.City = r.City)]
          CteRef [leftCte as l]
          CteRef [rightCte as r]
      Project [l.City as l.City, AggRef(l.Count(r.Name)) as l.Count(r.Name)]
        Aggregate [keys: l.City] [aggs: Count(Name)]
          CteRef [lr as lr]
      Project [l.City as City, l.Count(r.Name) as MatchCount]
        CteRef [lrScore as lrScore]
*/

/*
physical plan representation string

PhysicalCte
  Definition [leftCte]
    PhysicalMultiStatement
      PhysicalProject [a.City as City]
        PhysicalSchemaScan [#A.entities() as a]
  Definition [rightCte]
    PhysicalMultiStatement
      PhysicalProject [b.City as City, b.Name as Name]
        PhysicalSchemaScan [#B.entities() as b]
  Query
    PhysicalMultiStatement
      PhysicalProject [l.City as l.City, r.City as r.City, r.Name as r.Name]
        PhysicalHashJoin [Inner] [build: r.City] [probe: l.City]
          PhysicalCteRef [leftCte as l]
          PhysicalCteRef [rightCte as r]
      PhysicalProject [l.City as l.City, AggRef(l.Count(r.Name)) as l.Count(r.Name)]
        PhysicalSingleKeyAggregate [key: l.City (String)] [aggs: Count(Name)]
          PhysicalCteRef [lr as lr]
      PhysicalProject [l.City as City, l.Count(r.Name) as MatchCount]
        PhysicalCteRef [lrScore as lrScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      City: string <- property City
    Generated [Cte0Row0]
      City: string <- field City
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    HashPayload [Cte1HashPayload0]
      City: string <- field City
      Name: string <- field Name
    TableRow [l]
      City: string <- field City
    HashPayload [Cte1HashPayload0]
      City: string <- field City
      Name: string <- field Name
    TableRow [r]
      City: string <- field City
      Name: string <- field Name
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      City: string <- field City
      MatchCount: long <- field MatchCount

  Body
    ParallelBlock [cte-level-0, tasks 2, maxDegree 2]
      ParallelTask [leftCte -> __parallelCteLevel0Task0Result]
        SourceScan [a: BasicEntity] -> cte0_aRows
        CreateTable [cte0: Cte0Row0]
        ChunkedForEach [a in cte0_aRows]
          AppendRow [cte0 <- Cte0Row0(City: a.City)]
        Assign [__parallelCteLevel0Task0Result = cte0]
      ParallelTask [rightCte -> __parallelCteLevel0Task1Result]
        SourceScan [b: BasicEntity] -> cte1_bRows
        CreateHash [cte1HashSidecar0City: string -> Row]
        ChunkedForEach [b in cte1_bRows]
          CreateHashPayload [cte1SidecarPayload0 <- Cte1HashPayload0(City: b.City, Name: b.Name)]
          HashAdd [cte1HashSidecar0City[b.City] += cte1SidecarPayload0]
        StoreCteIndex [cte1HashSidecar0City -> _cteIndexResults.Slot0 Hash]
      ParallelMerge
        StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]
    LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ForEach [l in _cteRowResults.Slot0]
      HashProbe [rHash[l.City] -> rHashMatches]
        ForEach [r in rHashMatches]
          Let [name: string = r.Name]
          GetOrAddSingleKeyAggregateGroup [group = groups[l.City] by l.City; typed: ResultAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, name)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.l.City, MatchCount: l.Count(r.Name))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q66_CteBackedAggregateOverHashJoin
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
            new Column("City", typeof(string), 0)
        };
        private static readonly Column[] __columns_compiled_result_3 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("MatchCount", typeof(long), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_2 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_3, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.MatchCount);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var _cteRowResults = new CteRowResults();
                var _cteIndexResults = new CteIndexResults();
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                List<Cte0Row0> __parallelCteLevel0Task0Result = null;
                object __parallelCteLevel0Task1Result = null;
                var cteLevel0Runner = new CteLevel0Runner(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, OnDataSourceProgress, OnPhaseChanged, _cteRowResults, _cteIndexResults);
                Parallel.Invoke(new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 2 }, cteLevel0Runner.RunCteLevel0Task0, cteLevel0Runner.RunCteLevel0Task1);
                token.ThrowIfCancellationRequested();
                __parallelCteLevel0Task0Result = cteLevel0Runner.Task0Result;
                __parallelCteLevel0Task1Result = cteLevel0Runner.Task1Result;
                _cteRowResults.Slot0 = __parallelCteLevel0Task0Result;
                var rHash = _cteIndexResults.Slot0;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                var __storedTable0Rows = _cteRowResults.Slot0;
                for (int __storedTable0Index = 0; __storedTable0Index < __storedTable0Rows.Count; ++__storedTable0Index)
                {
                    if ((__storedTable0Index & 1023) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    Cte0Row0 l = __storedTable0Rows[__storedTable0Index];
                    string key = l.City;
                    if (key != null && rHash.TryGetValue(key, out var rHashMatches))
                    {
                        foreach (var r in rHashMatches)
                        {
                            token.ThrowIfCancellationRequested();
                            UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, r, l);
                        }
                    }
                }

                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.Count));
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
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
        private static List<Cte0Row0> BuildCteLevel0Task0(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            List<Cte0Row0> __parallelCteLevel0Task0Result = null;
            token.ThrowIfCancellationRequested();
            var __cte0_aSchema = provider.GetSchema("#A");
            var cte0_aRowsSource = __cte0_aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var cte0_aRows = cte0_aRowsSource.Chunks;
            var cte0 = new List<Cte0Row0>();
            foreach (var aChunk in cte0_aRows)
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
                            cte0.Add(new Cte0Row0(a.City));
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
                            cte0.Add(new Cte0Row0(a.City));
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
                    cte0.Add(new Cte0Row0(a.City));
                }
            }

            __parallelCteLevel0Task0Result = cte0;
            return __parallelCteLevel0Task0Result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static object BuildCteLevel0Task1(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
        {
            OnPhaseChanged("compiled:cte1", QueryPhase.Begin);
            try
            {
                object __parallelCteLevel0Task1Result = null;
                token.ThrowIfCancellationRequested();
                var __cte1_bSchema = provider.GetSchema("#B");
                var cte1_bRowsSource = __cte1_bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:2", sourceExecutionPlans["b:2"], token, __schemaColumns_compiled_b_2, sourceRuntimeSettingsBySourceContextId["b:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var cte1_bRows = cte1_bRowsSource.Chunks;
                var cte1HashSidecar0City = new Dictionary<string, HashJoinBucket<Cte1HashPayload0>>();
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
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(b.City, b.Name);
                                string cte1HashSidecar0CityKey0 = b.City;
                                if (cte1HashSidecar0CityKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0CityBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0City, cte1HashSidecar0CityKey0, out var cte1HashSidecar0CityBucket0Exists);
                                        if (!cte1HashSidecar0CityBucket0Exists)
                                        {
                                            cte1HashSidecar0CityBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0CityBucket0.Add(cte1SidecarPayload0);
                                        }
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
                                Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(b.City, b.Name);
                                string cte1HashSidecar0CityKey0 = b.City;
                                if (cte1HashSidecar0CityKey0 != null)
                                {
                                    {
                                        ref var cte1HashSidecar0CityBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0City, cte1HashSidecar0CityKey0, out var cte1HashSidecar0CityBucket0Exists);
                                        if (!cte1HashSidecar0CityBucket0Exists)
                                        {
                                            cte1HashSidecar0CityBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                        }
                                        else
                                        {
                                            cte1HashSidecar0CityBucket0.Add(cte1SidecarPayload0);
                                        }
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
                        Cte1HashPayload0 cte1SidecarPayload0 = new Cte1HashPayload0(b.City, b.Name);
                        string cte1HashSidecar0CityKey0 = b.City;
                        if (cte1HashSidecar0CityKey0 != null)
                        {
                            {
                                ref var cte1HashSidecar0CityBucket0 = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(cte1HashSidecar0City, cte1HashSidecar0CityKey0, out var cte1HashSidecar0CityBucket0Exists);
                                if (!cte1HashSidecar0CityBucket0Exists)
                                {
                                    cte1HashSidecar0CityBucket0 = new HashJoinBucket<Cte1HashPayload0>(cte1SidecarPayload0);
                                }
                                else
                                {
                                    cte1HashSidecar0CityBucket0.Add(cte1SidecarPayload0);
                                }
                            }
                        }
                    }
                }

                _cteIndexResults.Slot0 = cte1HashSidecar0City;
                return __parallelCteLevel0Task1Result;
            }
            finally
            {
                OnPhaseChanged("compiled:cte1", QueryPhase.End);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, Cte1HashPayload0 r, Cte0Row0 l)
        {
            string name = r.Name;
            string groupKey = l.City;
            ResultAggregateGroup group = null;
            if (groupKey != null)
            {
                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                if (!groupExists)
                {
                    groupRef = new ResultAggregateGroup(groupKey);
                    groupsToFinalize.Add(groupRef);
                }

                group = groupRef;
            }
            else
            {
                if (nullGroup == null)
                {
                    nullGroup = new ResultAggregateGroup(null);
                    groupsToFinalize.Add(nullGroup);
                }

                group = nullGroup;
            }

            if ((string)name != null)
            {
                group.__agg0.Count = checked(group.__agg0.Count + 1L);
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

        private readonly struct Cte1HashPayload0
        {
            public readonly string City;
            public readonly string Name;
            public Cte1HashPayload0(string City, string Name)
            {
                this.City = City;
                this.Name = Name;
            }
        }

        private sealed class CteIndexResults
        {
            public Dictionary<string, HashJoinBucket<Cte1HashPayload0>> Slot0;
        }

        private sealed class CteLevel0Runner
        {
            private readonly CteIndexResults _cteIndexResults;
            private readonly CteRowResults _cteRowResults;
            private readonly Microsoft.Extensions.Logging.ILogger _logger;
            private readonly Musoq.Schema.DataSourceEventHandler _onDataSourceProgress;
            private readonly Action<string, QueryPhase> _onPhaseChanged;
            private readonly Musoq.Schema.ISchemaProvider _provider;
            private readonly IReadOnlyDictionary<string, SourceExecutionPlan> _sourceExecutionPlans;
            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _sourceRuntimeSettingsBySourceContextId;
            private readonly CancellationToken _token;
            public CteLevel0Runner(Musoq.Schema.ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, Microsoft.Extensions.Logging.ILogger logger, CancellationToken token, Musoq.Schema.DataSourceEventHandler OnDataSourceProgress, Action<string, QueryPhase> OnPhaseChanged, CteRowResults _cteRowResults, CteIndexResults _cteIndexResults)
            {
                _provider = provider;
                _sourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
                _sourceExecutionPlans = sourceExecutionPlans;
                _logger = logger;
                _token = token;
                _onDataSourceProgress = OnDataSourceProgress;
                _onPhaseChanged = OnPhaseChanged;
                this._cteRowResults = _cteRowResults;
                this._cteIndexResults = _cteIndexResults;
            }

            public List<Cte0Row0> Task0Result { get; private set; }
            public object Task1Result { get; private set; }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task0()
            {
                Task0Result = BuildCteLevel0Task0(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void RunCteLevel0Task1()
            {
                Task1Result = BuildCteLevel0Task1(_provider, _sourceRuntimeSettingsBySourceContextId, _sourceExecutionPlans, _logger, _token, _onDataSourceProgress, _onPhaseChanged, _cteRowResults, _cteIndexResults);
            }
        }

        private sealed class CteRowResults
        {
            public List<Cte0Row0> Slot0;
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg0;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, long __value1)
            {
                City = __value0;
                MatchCount = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public long MatchCount { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        MatchCount = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "MatchCount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)MatchCount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "MatchCount" => (object)MatchCount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, long MatchCount)
            {
                this.City = City;
                this.MatchCount = MatchCount;
            }

            public string City { get; }
            public long MatchCount { get; }
        }
    }
}
