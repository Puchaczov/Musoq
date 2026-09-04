// === Parsed Query ===
/*
select Country, City, Count(City, 1), Count(City) as CountOfCities from #A.entities() group by Country, City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, ko3iko.Country as ko3iko.Country, AggRef(ko3iko.Count(ko3iko.City)) as ko3iko.Count(ko3iko.City), AggRef(ko3iko.Count(ko3iko.City, 1)) as ko3iko.Count(ko3iko.City, 1)]
    Aggregate [keys: Country, City] [aggs: Count(City), Count(City, 1)]
      SchemaScan [#A.entities() as ko3iko]
  Project [ko3iko.Country as Country, ko3iko.City as City, ko3iko.Count(ko3iko.City, 1) as Count(City, 1), ko3iko.Count(ko3iko.City) as CountOfCities]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, ko3iko.Country as ko3iko.Country, AggRef(ko3iko.Count(ko3iko.City)) as ko3iko.Count(ko3iko.City), AggRef(ko3iko.Count(ko3iko.City, 1)) as ko3iko.Count(ko3iko.City, 1)]
    PhysicalValueTupleAggregate [keys: Country, City] [aggs: Count(City), Count(City, 1)]
      PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalProject [ko3iko.Country as Country, ko3iko.City as City, ko3iko.Count(ko3iko.City, 1) as Count(City, 1), ko3iko.Count(ko3iko.City) as CountOfCities]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Country: string <- property Country
    AggregateGroup [ResultAggregateGroupPrefix1; keys: 1; typed aggs: 1]
    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 1]
    Generated [ResultRow0]
      Country: string <- field Country
      City: string <- field City
      Count(City, 1): long <- field Count_City__1_
      CountOfCities: long <- field CountOfCities

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateValueTupleAggregateContext [groups: (string, string) -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddValueTupleAggregateGroup [group = groups[(ko3iko.Country, ko3iko.City)] by Country, City; typed: ResultAggregateGroup]
      Let [city: string = ko3iko.City]
      TypedAggregateSet [Set(group.__agg0, city)]
      Let [city1: string = ko3iko.City]
      TypedAggregateSet [Set(group.__owner1.__agg1, city1)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Country: finalGroup.Country, City: finalGroup.City, Count(City, 1): ko3iko.Count(ko3iko.City, 1), CountOfCities: ko3iko.Count(ko3iko.City))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q286_SpecCoreParentLevelAggregate
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
            new Column("Country", typeof(string), 0),
            new Column("City", typeof(string), 1),
            new Column("Count(City, 1)", typeof(long), 2),
            new Column("CountOfCities", typeof(long), 3)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Country", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.Country, __musoqShapeRow.City, __musoqShapeRow.Count_City__1_, __musoqShapeRow.CountOfCities);
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
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groupsLevel_0 = new Dictionary<ValueTuple<string>, ResultAggregateGroupPrefix1>();
                var groups = new Dictionary<(string, string), ResultAggregateGroup>();
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
                                string groupKey0 = ko3iko.Country;
                                string groupKey1 = ko3iko.City;
                                ref var levelGroup_0Ref = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groupsLevel_0, ValueTuple.Create(groupKey0), out var levelGroup_0Exists);
                                if (!levelGroup_0Exists)
                                {
                                    levelGroup_0Ref = new ResultAggregateGroupPrefix1(groupKey0);
                                }

                                ResultAggregateGroupPrefix1 levelGroup_0 = levelGroup_0Ref;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(levelGroup_0, groupKey0, groupKey1);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                string city = ko3iko.City;
                                if ((string)city != null)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                string city1 = ko3iko.City;
                                if ((string)city1 != null)
                                {
                                    group.__owner1.__agg1.Count = checked(group.__owner1.__agg1.Count + 1L);
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
                                string groupKey0 = ko3iko.Country;
                                string groupKey1 = ko3iko.City;
                                ref var levelGroup_0Ref = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groupsLevel_0, ValueTuple.Create(groupKey0), out var levelGroup_0Exists);
                                if (!levelGroup_0Exists)
                                {
                                    levelGroup_0Ref = new ResultAggregateGroupPrefix1(groupKey0);
                                }

                                ResultAggregateGroupPrefix1 levelGroup_0 = levelGroup_0Ref;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(levelGroup_0, groupKey0, groupKey1);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                string city = ko3iko.City;
                                if ((string)city != null)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                string city1 = ko3iko.City;
                                if ((string)city1 != null)
                                {
                                    group.__owner1.__agg1.Count = checked(group.__owner1.__agg1.Count + 1L);
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
                        string groupKey0 = ko3iko.Country;
                        string groupKey1 = ko3iko.City;
                        ref var levelGroup_0Ref = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groupsLevel_0, ValueTuple.Create(groupKey0), out var levelGroup_0Exists);
                        if (!levelGroup_0Exists)
                        {
                            levelGroup_0Ref = new ResultAggregateGroupPrefix1(groupKey0);
                        }

                        ResultAggregateGroupPrefix1 levelGroup_0 = levelGroup_0Ref;
                        ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                        if (!groupExists)
                        {
                            groupRef = new ResultAggregateGroup(levelGroup_0, groupKey0, groupKey1);
                            groupsToFinalize.Add(groupRef);
                        }

                        ResultAggregateGroup group = groupRef;
                        string city = ko3iko.City;
                        if ((string)city != null)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        string city1 = ko3iko.City;
                        if ((string)city1 != null)
                        {
                            group.__owner1.__agg1.Count = checked(group.__owner1.__agg1.Count + 1L);
                        }
                    }
                }

                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__key1, finalGroup.__owner1.__agg1.Count, finalGroup.__agg0.Count));
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
            public readonly string __key1;
            public readonly ResultAggregateGroupPrefix1 __owner1;
            public ResultAggregateGroup(ResultAggregateGroupPrefix1 __owner1, string __key0, string __key1)
            {
                this.__owner1 = __owner1;
                this.__key0 = __key0;
                this.__key1 = __key1;
            }
        }

        private sealed class ResultAggregateGroupPrefix1
        {
            public Musoq.Plugins.CountReferenceAggregateKernel<string>.State __agg1;
            public readonly string __key0;
            public ResultAggregateGroupPrefix1(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroupPrefix1 source)
            {
                Musoq.Plugins.CountReferenceAggregateKernel<string>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, long __value2, long __value3)
            {
                Country = __value0;
                City = __value1;
                Count_City__1_ = __value2;
                CountOfCities = __value3;
            }

            public string City { get; private set; }
            public override int Count => 4;
            public long CountOfCities { get; private set; }
            public long Count_City__1_ { get; private set; }
            public string Country { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Country = (string)value;
                        break;
                    case 1:
                        City = (string)value;
                        break;
                    case 2:
                        Count_City__1_ = (long)value;
                        break;
                    case 3:
                        CountOfCities = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Country" => true,
                "City" => true,
                "Count(City, 1)" => true,
                "Count_City__1_" => true,
                "CountOfCities" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Country,
                1 => (object)City,
                2 => (object)Count_City__1_,
                3 => (object)CountOfCities,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "Country" => (object)Country,
                "City" => (object)City,
                "Count(City, 1)" => (object)Count_City__1_,
                "Count_City__1_" => (object)Count_City__1_,
                "CountOfCities" => (object)CountOfCities,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string Country, string City, long Count_City__1_, long CountOfCities)
            {
                this.Country = Country;
                this.City = City;
                this.Count_City__1_ = Count_City__1_;
                this.CountOfCities = CountOfCities;
            }

            public string City { get; }
            public long CountOfCities { get; }
            public long Count_City__1_ { get; }
            public string Country { get; }
        }
    }
}
