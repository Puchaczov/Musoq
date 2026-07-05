/*
raw query string

select a.City as City, Count(b.Name) as MatchCount from #A.entities() a inner join #B.entities() b on a.City = b.City group by a.City
*/

/*
logical plan representation string

MultiStatement
  Project [a.City as a.City, b.Name as b.Name, b.City as b.City]
    Join [Inner] [(a.City = b.City)]
      SchemaScan [#A.entities() as a]
      SchemaScan [#B.entities() as b]
  Project [a.City as a.City, AggRef(a.Count(b.Name)) as a.Count(b.Name)]
    Aggregate [keys: a.City] [aggs: Count(Name)]
      CteRef [ab as ab]
  Project [a.City as City, a.Count(b.Name) as MatchCount]
    CteRef [abScore as abScore]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [a.City as a.City, b.Name as b.Name, b.City as b.City]
    PhysicalHashJoin [Inner] [build: b.City] [probe: a.City]
      PhysicalSchemaScan [#A.entities() as a]
      PhysicalSchemaScan [#B.entities() as b]
  PhysicalProject [a.City as a.City, AggRef(a.Count(b.Name)) as a.Count(b.Name)]
    PhysicalSingleKeyAggregate [key: a.City (String)] [aggs: Count(Name)]
      PhysicalCteRef [ab as ab]
  PhysicalProject [a.City as City, a.Count(b.Name) as MatchCount]
    PhysicalCteRef [abScore as abScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: BasicEntity]
      City: string <- property City
    SourceEntity [b: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      City: string <- field City
      MatchCount: long <- field MatchCount

  Body
    SourceScan [a: BasicEntity] -> aRows
    SourceScan [b: BasicEntity] -> bRows
    CreateHash [bHash: string -> BasicEntity]
    ChunkedForEach [b in bRows]
      HashAdd [bHash[b.City] += b]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ChunkedForEach [a in aRows]
      HashProbe [bHash[a.City] -> bHashMatches]
        ForEach [b in bHashMatches]
          Let [name: string = b.Name]
          GetOrAddSingleKeyAggregateGroup [group = groups[a.City] by a.City; typed: ResultAggregateGroup]
          TypedAggregateSet [Set(group.__agg0, name)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.a.City, MatchCount: a.Count(b.Name))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q65_AggregateOverHashJoin
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
        private static readonly Column[] __columns_compiled_result_2 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("MatchCount", typeof(long), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_b_1 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 10), new Column("City", typeof(string), 11) });
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
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                var __aSchema = provider.GetSchema("#A");
                var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var aRows = aRowsSource.Chunks;
                var __bSchema = provider.GetSchema("#B");
                var bRowsSource = __bSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("b:1", sourceExecutionPlans["b:1"], token, __schemaColumns_compiled_b_1, sourceRuntimeSettingsBySourceContextId["b:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var bRows = bRowsSource.Chunks;
                var bHash = new Dictionary<string, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>();
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
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                    }
                                    else
                                    {
                                        matches.Add(b);
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
                                if (key == null)
                                    continue;
                                {
                                    ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                                    if (!matchesExists)
                                    {
                                        matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                                    }
                                    else
                                    {
                                        matches.Add(b);
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
                        if (key == null)
                            continue;
                        {
                            ref var matches = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(bHash, key, out var matchesExists);
                            if (!matchesExists)
                            {
                                matches = new HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(b);
                            }
                            else
                            {
                                matches.Add(b);
                            }
                        }
                    }
                }

                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
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
                                string key = a.City;
                                if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                                {
                                    foreach (var b in bHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, b, a);
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
                                string key = a.City;
                                if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                                {
                                    foreach (var b in bHashMatches)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, b, a);
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
                        string key = a.City;
                        if (key != null && bHash.TryGetValue(key, out var bHashMatches))
                        {
                            foreach (var b in bHashMatches)
                            {
                                token.ThrowIfCancellationRequested();
                                UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, b, a);
                            }
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
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, Musoq.Evaluator.Tests.Schema.Basic.BasicEntity b, Musoq.Evaluator.Tests.Schema.Basic.BasicEntity a)
        {
            string name = b.Name;
            string groupKey = a.City;
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
