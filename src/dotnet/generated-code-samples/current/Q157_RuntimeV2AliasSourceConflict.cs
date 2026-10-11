// === Parsed Query ===
/*
SELECT City as Department, Department as SourceDepartment, Count(*) as cnt
                  FROM #features.items()
                  GROUP BY Department, City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, ko3iko.Department as ko3iko.Department, AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    Aggregate [keys: Department, City] [aggs: Count(*)]
      SchemaScan [#features.items() as ko3iko]
  Project [ko3iko.City as Department, ko3iko.Department as SourceDepartment, ko3iko.Count(*) as cnt]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, ko3iko.Department as ko3iko.Department, AggRef(ko3iko.Count(*)) as ko3iko.Count(*)]
    PhysicalValueTupleAggregate [keys: Department, City] [aggs: Count(*)]
      PhysicalSchemaScan [#features.items() as ko3iko]
  PhysicalProject [ko3iko.City as Department, ko3iko.Department as SourceDepartment, ko3iko.Count(*) as cnt]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeV2CastGroupingFeatureEntity]
      City: string <- property City
      Department: string <- property Department
    AggregateGroup [ResultAggregateGroup; keys: 2; typed aggs: 1]
    Generated [ResultRow0]
      Department: string <- field Department
      SourceDepartment: string <- field SourceDepartment
      cnt: long <- field cnt

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeV2CastGroupingFeatureEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateValueTupleAggregateContext [groups: (string, string) -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      GetOrAddValueTupleAggregateGroup [group = groups[(ko3iko.Department, ko3iko.City)] by Department, City; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Department: finalGroup.City, SourceDepartment: finalGroup.Department, cnt: ko3iko.Count(*))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q157_RuntimeV2AliasSourceConflict
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Department", typeof(string), 0),
            new Column("SourceDepartment", typeof(string), 1),
            new Column("cnt", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Department", typeof(string), 1) });
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
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
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#features");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>.Open(__ko3ikoSchema, "items", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.RuntimeV2CastGroupingFeatureEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
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
                                string groupKey0 = ko3iko.Department;
                                string groupKey1 = ko3iko.City;
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
                                string groupKey0 = ko3iko.Department;
                                string groupKey1 = ko3iko.City;
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
                        string groupKey0 = ko3iko.Department;
                        string groupKey1 = ko3iko.City;
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

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__key1, finalGroup.__key0, finalGroup.__agg0.Count));
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
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public readonly string __key0;
            public readonly string __key1;
            public ResultAggregateGroup(string __key0, string __key1)
            {
                this.__key0 = __key0;
                this.__key1 = __key1;
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg0);
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
                Department = __value0;
                SourceDepartment = __value1;
                cnt = __value2;
            }

            public override int Count => 3;
            public string Department { get; private set; }
            public string SourceDepartment { get; private set; }
            public long cnt { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Department = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.SourceDepartment = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.cnt = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Department),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.SourceDepartment),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.cnt),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Department" => true,
                "SourceDepartment" => true,
                "cnt" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Department, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.SourceDepartment, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.cnt, ref visitor);
            }
        }
    }
}
