/*
raw query string

SELECT City, Department, Count(*) as Cnt
                  FROM #features.items()
                  GROUP BY 1, 2
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.Department as ko3iko.Department, ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    Aggregate [keys: ko3iko.City, ko3iko.Department] [aggs: Count(*)]
      SchemaScan [#features.items() as ko3iko]
  Project [ko3iko.City as City, ko3iko.Department as Department, ko3iko.Count(*) as Cnt]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.Department as ko3iko.Department, ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    PhysicalValueTupleAggregate [keys: ko3iko.City, ko3iko.Department] [aggs: Count(*)]
      PhysicalSchemaScan [#features.items() as ko3iko]
  PhysicalProject [ko3iko.City as City, ko3iko.Department as Department, ko3iko.Count(*) as Cnt]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      City: string <- property City
      Department: string <- property Department
    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 1]
    Generated [ResultRow0]
      City: string <- field City
      Department: string <- field Department
      Cnt: long <- field Cnt

  Body
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateValueTupleAggregateContext [groups: (string, string) -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddValueTupleAggregateGroup [group = groups[(ko3iko.City, ko3iko.Department)] by ko3iko.City, ko3iko.Department; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.ko3iko.City, Department: finalGroup.ko3iko.Department, Cnt: ko3iko.Count(*))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q153_RuntimeV2GroupByOrdinal
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("Department", typeof(string), 1),
            new Column("Cnt", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Department", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.Department, __musoqShapeRow.Cnt);
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
                var __ko3ikoSchema = provider.GetSchema("#features");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>("items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<(string, string), ResultAggregateGroup>();
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey0 = ko3iko.City;
                                string groupKey1 = ko3iko.Department;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                group.__agg0.Count = checked(group.__agg0.Count + 1L);
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                string groupKey0 = ko3iko.City;
                                string groupKey1 = ko3iko.Department;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                group.__agg0.Count = checked(group.__agg0.Count + 1L);
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
                        string groupKey0 = ko3iko.City;
                        string groupKey1 = ko3iko.Department;
                        ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, (groupKey0, groupKey1), out var groupExists);
                        if (!groupExists)
                        {
                            groupRef = new ResultAggregateGroup(groupKey0, groupKey1);
                            groupsToFinalize.Add(groupRef);
                        }

                        ResultAggregateGroup group = groupRef;
                        group.__agg0.Count = checked(group.__agg0.Count + 1L);
                    }
                }

                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__key1, finalGroup.__agg0.Count));
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

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public readonly string __key0;
            public readonly string __key1;
            public ResultAggregateGroup(string __key0, string __key1)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, long __value2)
            {
                City = __value0;
                Department = __value1;
                Cnt = __value2;
            }

            public string City { get; private set; }
            public long Cnt { get; private set; }
            public override int Count => 3;
            public string Department { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        Department = (string)value;
                        break;
                    case 2:
                        Cnt = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Department" => true,
                "Cnt" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)Department,
                2 => (object)Cnt,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "Department" => (object)Department,
                "Cnt" => (object)Cnt,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, string Department, long Cnt)
            {
                this.City = City;
                this.Department = Department;
                this.Cnt = Cnt;
            }

            public string City { get; }
            public long Cnt { get; }
            public string Department { get; }
        }
    }
}
