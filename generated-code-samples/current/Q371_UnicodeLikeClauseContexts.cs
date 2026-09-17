// === Parsed Query ===
/*
select City, City like 'Ł%' as IsPolish, Count(Name) filter (where Name like 'Ż%') as MatchingNames from #A.entities() where Name like '%ó%' or City like 'Ł%' group by City having City like 'Ł%' order by City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%')) as ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%')]
    Having [ko3iko.City LIKE 'Ł%']
      Aggregate [keys: City] [aggs: Count(Name) filter (where Name like 'Ż%')]
        Filter [(ko3iko.Name LIKE '%ó%' OR ko3iko.City LIKE 'Ł%')]
          SchemaScan [#A.entities() as ko3iko]
  Sort [ko3iko.City]
    Project [ko3iko.City as City, ko3iko.City LIKE 'Ł%' as IsPolish, ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%') as MatchingNames]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%')) as ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%')]
    PhysicalHaving [ko3iko.City LIKE 'Ł%']
      PhysicalSingleKeyAggregate [key: City (String)] [aggs: Count(Name) filter (where Name like 'Ż%')]
        PhysicalFilter [(ko3iko.Name LIKE '%ó%' OR ko3iko.City LIKE 'Ł%')]
          PhysicalSchemaScan [#A.entities() as ko3iko] [pushdown: (ko3iko.Name LIKE '%ó%' OR ko3iko.City LIKE 'Ł%')]
  PhysicalSort [ko3iko.City]
    PhysicalProject [ko3iko.City as City, ko3iko.City LIKE 'Ł%' as IsPolish, ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%') as MatchingNames]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      City: string <- field City
      IsPolish: bool <- field IsPolish
      MatchingNames: long <- field MatchingNames

  Body
    PhaseBoundary [Begin]
    Let [__likeMatcher0: PreparedLikeMatcher = PREPARE_LIKE('%ó%', comparison=LikeIgnoreCase)]
    Let [__likeMatcher1: PreparedLikeMatcher = PREPARE_LIKE('Ł%', comparison=LikeIgnoreCase)]
    Let [__likeMatcher2: PreparedLikeMatcher = PREPARE_LIKE('Ż%', comparison=LikeIgnoreCase)]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [name: string = ko3iko.Name]
      Let [city: string = ko3iko.City]
      If [(PREPARED_LIKE(name, __likeMatcher0) OR PREPARED_LIKE(city, __likeMatcher1))]
        GetOrAddSingleKeyAggregateGroup [group = groups[city] by City; typed: ResultAggregateGroup]
        TypedAggregateSet [Set(group.__agg0, name) filter PREPARED_LIKE(name, __likeMatcher2)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      If [PREPARED_LIKE(finalGroup.City, __likeMatcher1)]
        AppendShape [result <- ResultShape0(City: finalGroup.City, IsPolish: PREPARED_LIKE(finalGroup.City, __likeMatcher1), MatchingNames: ko3iko.Count(ko3iko.Name) filter (where Name like 'Ż%'))]
    SortShapeRows [result -> resultSorted by City ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q371_UnicodeLikeClauseContexts
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("IsPolish", typeof(bool), 1),
            new Column("MatchingNames", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.IsPolish, __musoqShapeRow.MatchingNames);
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
                Musoq.Evaluator.PreparedLikeMatcher __likeMatcher0 = Operators.PrepareLike("%ó%");
                Musoq.Evaluator.PreparedLikeMatcher __likeMatcher1 = Operators.PrepareLike("Ł%");
                Musoq.Evaluator.PreparedLikeMatcher __likeMatcher2 = Operators.PrepareLike("Ż%");
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                if ((Operators.LikePrepared(name, __likeMatcher0) || Operators.LikePrepared(city, __likeMatcher1)))
                                {
                                    string groupKey = city;
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

                                    if (Operators.LikePrepared(name, __likeMatcher2))
                                    {
                                        if ((string)name != null)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string name = ko3iko.Name;
                                string city = ko3iko.City;
                                if ((Operators.LikePrepared(name, __likeMatcher0) || Operators.LikePrepared(city, __likeMatcher1)))
                                {
                                    string groupKey = city;
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

                                    if (Operators.LikePrepared(name, __likeMatcher2))
                                    {
                                        if ((string)name != null)
                                        {
                                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        }
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        string name = ko3iko.Name;
                        string city = ko3iko.City;
                        if ((Operators.LikePrepared(name, __likeMatcher0) || Operators.LikePrepared(city, __likeMatcher1)))
                        {
                            string groupKey = city;
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

                            if (Operators.LikePrepared(name, __likeMatcher2))
                            {
                                if ((string)name != null)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if (Operators.LikePrepared(finalGroup.__key0, __likeMatcher1))
                    {
                        result.Add(new ResultShape0(finalGroup.__key0, Operators.LikePrepared(finalGroup.__key0, __likeMatcher1), finalGroup.__agg0.Count));
                    }
                }

                var resultSortedRows = result.OrderBy(static __musoqOrderRow => __musoqOrderRow, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = StringComparer.Ordinal.Compare(left.City, right.City);
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
            public ResultRow0(string __value0, bool __value1, long __value2)
            {
                City = __value0;
                IsPolish = __value1;
                MatchingNames = __value2;
            }

            public string City { get; private set; }
            public override int Count => 3;
            public bool IsPolish { get; private set; }
            public long MatchingNames { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        IsPolish = (bool)value;
                        break;
                    case 2:
                        MatchingNames = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "IsPolish" => true,
                "MatchingNames" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)IsPolish,
                2 => (object)MatchingNames,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "IsPolish" => (object)IsPolish,
                "MatchingNames" => (object)MatchingNames,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, bool IsPolish, long MatchingNames)
            {
                this.City = City;
                this.IsPolish = IsPolish;
                this.MatchingNames = MatchingNames;
            }

            public string City { get; }
            public bool IsPolish { get; }
            public long MatchingNames { get; }
        }
    }
}
