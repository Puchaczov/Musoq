// === Parsed Query ===
/*
select City, Sum(Population) from #A.Entities() group by City except (City) select City, Sum(Population) from #B.Entities() group by City except (City) select City, Sum(Population) from #C.Entities() group by City
*/

// === Logical Plan ===
/*
SetOp [Except]
  SetOp [Except]
    MultiStatement
      Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population)]
        Aggregate [keys: City] [aggs: Sum(Population)]
          SchemaScan [#A.Entities() as ko3iko]
      Project [ko3iko.City as City, ko3iko.Sum(ko3iko.Population) as Sum(Population)]
        CteRef [ko3ikoScore as ko3ikoScore]
    MultiStatement
      Project [vo04qt.City as vo04qt.City, AggRef(vo04qt.Sum(vo04qt.Population)) as vo04qt.Sum(vo04qt.Population)]
        Aggregate [keys: City] [aggs: Sum(Population)]
          SchemaScan [#B.Entities() as vo04qt]
      Project [vo04qt.City as City, vo04qt.Sum(vo04qt.Population) as Sum(Population)]
        CteRef [vo04qtScore as vo04qtScore]
  MultiStatement
    Project [gougbq.City as gougbq.City, AggRef(gougbq.Sum(gougbq.Population)) as gougbq.Sum(gougbq.Population)]
      Aggregate [keys: City] [aggs: Sum(Population)]
        SchemaScan [#C.Entities() as gougbq]
    Project [gougbq.City as City, gougbq.Sum(gougbq.Population) as Sum(Population)]
      CteRef [gougbqScore as gougbqScore]
*/

// === Physical Plan ===
/*
PhysicalSetOp [Except]
  PhysicalSetOp [Except]
    PhysicalMultiStatement
      PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population)]
        PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Population)]
          PhysicalSchemaScan [#A.Entities() as ko3iko]
      PhysicalProject [ko3iko.City as City, ko3iko.Sum(ko3iko.Population) as Sum(Population)]
        PhysicalCteRef [ko3ikoScore as ko3ikoScore]
    PhysicalMultiStatement
      PhysicalProject [vo04qt.City as vo04qt.City, AggRef(vo04qt.Sum(vo04qt.Population)) as vo04qt.Sum(vo04qt.Population)]
        PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Population)]
          PhysicalSchemaScan [#B.Entities() as vo04qt]
      PhysicalProject [vo04qt.City as City, vo04qt.Sum(vo04qt.Population) as Sum(Population)]
        PhysicalCteRef [vo04qtScore as vo04qtScore]
  PhysicalMultiStatement
    PhysicalProject [gougbq.City as gougbq.City, AggRef(gougbq.Sum(gougbq.Population)) as gougbq.Sum(gougbq.Population)]
      PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Population)]
        PhysicalSchemaScan [#C.Entities() as gougbq]
    PhysicalProject [gougbq.City as City, gougbq.Sum(gougbq.Population) as Sum(Population)]
      PhysicalCteRef [gougbqScore as gougbqScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    AggregateGroup [LeftLeftAggregateGroup; keys: 1; typed aggs: 1]
    Generated [LeftLeftRow0]
      City: string <- field City
      Sum(Population): decimal? <- field Sum_Population_
    SourceEntity [vo04qt: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    AggregateGroup [LeftRightAggregateGroup; keys: 1; typed aggs: 1]
    Generated [LeftRightRow0]
      City: string <- field City
      Sum(Population): decimal? <- field Sum_Population_
    SourceEntity [gougbq: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    AggregateGroup [RightAggregateGroup; keys: 1; typed aggs: 1]
    Generated [RightRow0]
      City: string <- field City
      Sum(Population): decimal? <- field Sum_Population_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [Begin:left]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> leftLeft_ko3ikoRows
    CreateRowBuffer [leftLeft: List<LeftLeftRow0>]
    PhaseBoundary [GroupBy:left]
    PhaseBoundary [GroupBy:left]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [leftLeftGroups: string -> LeftLeftAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in leftLeft_ko3ikoRows]
      GetOrAddSingleKeyAggregateGroup [leftLeftGroup = leftLeftGroups[ko3iko.City] by City; typed: LeftLeftAggregateGroup]
      Let [population: decimal = ko3iko.Population]
      TypedAggregateSet [Set(leftLeftGroup.__agg0, population)]
    EnsureRowBufferCapacity [leftLeft <- leftLeftGroupsToFinalize.Count]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select:left]
    ForEach [leftLeftFinalGroup in leftLeftGroupsToFinalize]
      AppendRowBuffer [leftLeft <- LeftLeftRow0(City: leftLeftFinalGroup.City, Sum(Population): ko3iko.Sum(ko3iko.Population))]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [vo04qt: BasicEntity] -> leftRight_vo04qtRows
    CreateRowBuffer [leftRight: List<LeftRightRow0>]
    PhaseBoundary [GroupBy:right]
    CreateSingleKeyAggregateContext [leftRightGroups: string -> LeftRightAggregateGroup]
    ChunkedForEach [vo04qt in leftRight_vo04qtRows]
      GetOrAddSingleKeyAggregateGroup [leftRightGroup = leftRightGroups[vo04qt.City] by City; typed: LeftRightAggregateGroup]
      Let [population: decimal = vo04qt.Population]
      TypedAggregateSet [Set(leftRightGroup.__agg0, population)]
    EnsureRowBufferCapacity [leftRight <- leftRightGroupsToFinalize.Count]
    PhaseBoundary [Select:right]
    ForEach [leftRightFinalGroup in leftRightGroupsToFinalize]
      AppendRowBuffer [leftRight <- LeftRightRow0(City: leftRightFinalGroup.City, Sum(Population): vo04qt.Sum(vo04qt.Population))]
    PhaseBoundary [End:right]
    SetOperation [left = leftLeft Except leftRight, HashSet]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [gougbq: BasicEntity] -> right_gougbqRows
    CreateRowBuffer [right: List<RightRow0>]
    PhaseBoundary [GroupBy:right]
    CreateSingleKeyAggregateContext [rightGroups: string -> RightAggregateGroup]
    ChunkedForEach [gougbq in right_gougbqRows]
      GetOrAddSingleKeyAggregateGroup [rightGroup = rightGroups[gougbq.City] by City; typed: RightAggregateGroup]
      Let [population: decimal = gougbq.Population]
      TypedAggregateSet [Set(rightGroup.__agg0, population)]
    EnsureRowBufferCapacity [right <- rightGroupsToFinalize.Count]
    PhaseBoundary [Select:right]
    ForEach [rightFinalGroup in rightGroupsToFinalize]
      AppendRowBuffer [right <- RightRow0(City: rightFinalGroup.City, Sum(Population): gougbq.Sum(gougbq.Population))]
    PhaseBoundary [End:right]
    SetOperation [result = left Except right, HashSet]
    ReturnDeferredTable [result: LeftLeftRow0 <- LeftLeftShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q99_ExceptWithGroupBySides
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
        private static readonly Column[] __columns_compiled_leftLeft_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("Sum(Population)", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Population", typeof(decimal), 1) });
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
            return QueryRows.DeferredTable<LeftLeftRow0>("result", __columns_compiled_leftLeft_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftLeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftLeftRow0(__musoqShapeRow.City, __musoqShapeRow.Sum_Population_);
            }
        }

        private IEnumerable<LeftLeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftLeftShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.Begin);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled:left", QueryPhase.From);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __leftLeft_ko3ikoSchema = provider.GetSchema("#A");
                var leftLeft_ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__leftLeft_ko3ikoSchema, "Entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var leftLeft_ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(leftLeft_ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : leftLeft_ko3ikoRowsSource.Chunks;
                var leftLeft = new List<LeftLeftRow0>();
                OnPhaseChanged("compiled:left", QueryPhase.GroupBy);
                OnPhaseChanged("compiled:left", QueryPhase.GroupBy);
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var leftLeftGroupsToFinalize = new List<AggregateGroup0>();
                var leftLeftGroups = new Dictionary<string, AggregateGroup0>();
                AggregateGroup0 leftLeftNullGroup = null;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in leftLeft_ko3ikoRows)
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
                                string groupKey = ko3iko.City;
                                AggregateGroup0 leftLeftGroup = null;
                                if (groupKey != null)
                                {
                                    ref var leftLeftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftLeftGroups, groupKey, out var leftLeftGroupExists);
                                    if (!leftLeftGroupExists)
                                    {
                                        leftLeftGroupRef = new AggregateGroup0(groupKey);
                                        leftLeftGroupsToFinalize.Add(leftLeftGroupRef);
                                    }

                                    leftLeftGroup = leftLeftGroupRef;
                                }
                                else
                                {
                                    if (leftLeftNullGroup == null)
                                    {
                                        leftLeftNullGroup = new AggregateGroup0(null);
                                        leftLeftGroupsToFinalize.Add(leftLeftNullGroup);
                                    }

                                    leftLeftGroup = leftLeftNullGroup;
                                }

                                decimal population = ko3iko.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        leftLeftGroup.__agg0.Value = leftLeftGroup.__agg0.HasValue ? checked(leftLeftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        leftLeftGroup.__agg0.HasValue = true;
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
                                string groupKey = ko3iko.City;
                                AggregateGroup0 leftLeftGroup = null;
                                if (groupKey != null)
                                {
                                    ref var leftLeftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftLeftGroups, groupKey, out var leftLeftGroupExists);
                                    if (!leftLeftGroupExists)
                                    {
                                        leftLeftGroupRef = new AggregateGroup0(groupKey);
                                        leftLeftGroupsToFinalize.Add(leftLeftGroupRef);
                                    }

                                    leftLeftGroup = leftLeftGroupRef;
                                }
                                else
                                {
                                    if (leftLeftNullGroup == null)
                                    {
                                        leftLeftNullGroup = new AggregateGroup0(null);
                                        leftLeftGroupsToFinalize.Add(leftLeftNullGroup);
                                    }

                                    leftLeftGroup = leftLeftNullGroup;
                                }

                                decimal population = ko3iko.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        leftLeftGroup.__agg0.Value = leftLeftGroup.__agg0.HasValue ? checked(leftLeftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        leftLeftGroup.__agg0.HasValue = true;
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
                        string groupKey = ko3iko.City;
                        AggregateGroup0 leftLeftGroup = null;
                        if (groupKey != null)
                        {
                            ref var leftLeftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftLeftGroups, groupKey, out var leftLeftGroupExists);
                            if (!leftLeftGroupExists)
                            {
                                leftLeftGroupRef = new AggregateGroup0(groupKey);
                                leftLeftGroupsToFinalize.Add(leftLeftGroupRef);
                            }

                            leftLeftGroup = leftLeftGroupRef;
                        }
                        else
                        {
                            if (leftLeftNullGroup == null)
                            {
                                leftLeftNullGroup = new AggregateGroup0(null);
                                leftLeftGroupsToFinalize.Add(leftLeftNullGroup);
                            }

                            leftLeftGroup = leftLeftNullGroup;
                        }

                        decimal population = ko3iko.Population;
                        {
                            var __agg0Input = (decimal?)population;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                leftLeftGroup.__agg0.Value = leftLeftGroup.__agg0.HasValue ? checked(leftLeftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                leftLeftGroup.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                leftLeft.EnsureCapacity(leftLeftGroupsToFinalize.Count);
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                OnPhaseChanged("compiled:left", QueryPhase.Select);
                foreach (var leftLeftFinalGroup in leftLeftGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    leftLeft.Add(new LeftLeftRow0(leftLeftFinalGroup.__key0, leftLeftFinalGroup.__agg0.HasValue ? (decimal?)leftLeftFinalGroup.__agg0.Value : null));
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __leftRight_vo04qtSchema = provider.GetSchema("#B");
                var leftRight_vo04qtRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__leftRight_vo04qtSchema, "Entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var leftRight_vo04qtRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(leftRight_vo04qtRowsSource.Chunks, __musoqProgressContext, "vo04qt:2") : leftRight_vo04qtRowsSource.Chunks;
                var leftRight = new List<LeftRightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.GroupBy);
                var leftRightGroupsToFinalize = new List<AggregateGroup0>();
                var leftRightGroups = new Dictionary<string, AggregateGroup0>();
                AggregateGroup0 leftRightNullGroup = null;
                foreach (var vo04qtChunk in leftRight_vo04qtRows)
                {
                    if (vo04qtChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkView)
                    {
                        if (vo04qtChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] vo04qtChunkViewArray)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewArray[vo04qtChunkViewOffset + vo04qtIndex];
                                string groupKey = vo04qt.City;
                                AggregateGroup0 leftRightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var leftRightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftRightGroups, groupKey, out var leftRightGroupExists);
                                    if (!leftRightGroupExists)
                                    {
                                        leftRightGroupRef = new AggregateGroup0(groupKey);
                                        leftRightGroupsToFinalize.Add(leftRightGroupRef);
                                    }

                                    leftRightGroup = leftRightGroupRef;
                                }
                                else
                                {
                                    if (leftRightNullGroup == null)
                                    {
                                        leftRightNullGroup = new AggregateGroup0(null);
                                        leftRightGroupsToFinalize.Add(leftRightNullGroup);
                                    }

                                    leftRightGroup = leftRightNullGroup;
                                }

                                decimal population = vo04qt.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        leftRightGroup.__agg0.Value = leftRightGroup.__agg0.HasValue ? checked(leftRightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        leftRightGroup.__agg0.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (vo04qtChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> vo04qtChunkViewList)
                        {
                            int vo04qtChunkViewOffset = vo04qtChunkView.Offset;
                            for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunkView.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                            {
                                if ((vo04qtIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var vo04qt = vo04qtChunkViewList[vo04qtChunkViewOffset + vo04qtIndex];
                                string groupKey = vo04qt.City;
                                AggregateGroup0 leftRightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var leftRightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftRightGroups, groupKey, out var leftRightGroupExists);
                                    if (!leftRightGroupExists)
                                    {
                                        leftRightGroupRef = new AggregateGroup0(groupKey);
                                        leftRightGroupsToFinalize.Add(leftRightGroupRef);
                                    }

                                    leftRightGroup = leftRightGroupRef;
                                }
                                else
                                {
                                    if (leftRightNullGroup == null)
                                    {
                                        leftRightNullGroup = new AggregateGroup0(null);
                                        leftRightGroupsToFinalize.Add(leftRightNullGroup);
                                    }

                                    leftRightGroup = leftRightNullGroup;
                                }

                                decimal population = vo04qt.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        leftRightGroup.__agg0.Value = leftRightGroup.__agg0.HasValue ? checked(leftRightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        leftRightGroup.__agg0.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int vo04qtIndex = 0, vo04qtIndexCount = vo04qtChunk.Count; vo04qtIndex < vo04qtIndexCount; ++vo04qtIndex)
                    {
                        if ((vo04qtIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var vo04qt = vo04qtChunk[vo04qtIndex];
                        string groupKey = vo04qt.City;
                        AggregateGroup0 leftRightGroup = null;
                        if (groupKey != null)
                        {
                            ref var leftRightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(leftRightGroups, groupKey, out var leftRightGroupExists);
                            if (!leftRightGroupExists)
                            {
                                leftRightGroupRef = new AggregateGroup0(groupKey);
                                leftRightGroupsToFinalize.Add(leftRightGroupRef);
                            }

                            leftRightGroup = leftRightGroupRef;
                        }
                        else
                        {
                            if (leftRightNullGroup == null)
                            {
                                leftRightNullGroup = new AggregateGroup0(null);
                                leftRightGroupsToFinalize.Add(leftRightNullGroup);
                            }

                            leftRightGroup = leftRightNullGroup;
                        }

                        decimal population = vo04qt.Population;
                        {
                            var __agg0Input = (decimal?)population;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                leftRightGroup.__agg0.Value = leftRightGroup.__agg0.HasValue ? checked(leftRightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                leftRightGroup.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                leftRight.EnsureCapacity(leftRightGroupsToFinalize.Count);
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var leftRightFinalGroup in leftRightGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    leftRight.Add(new LeftRightRow0(leftRightFinalGroup.__key0, leftRightFinalGroup.__agg0.HasValue ? (decimal?)leftRightFinalGroup.__agg0.Value : null));
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var left = new List<LeftLeftRow0>();
                var leftRightKeys = new HashSet<string>(leftRight.Count);
                foreach (var leftRightRow in leftRight)
                {
                    leftRightKeys.Add((string)leftRightRow.City);
                }

                foreach (var leftLeftRow in leftLeft)
                {
                    if (!leftRightKeys.Contains((string)leftLeftRow.City))
                    {
                        left.Add(leftLeftRow);
                    }
                }

                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.Begin);
                OnPhaseChanged("compiled:right", QueryPhase.From);
                var __right_gougbqSchema = provider.GetSchema("#C");
                var right_gougbqRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>.Open(__right_gougbqSchema, "Entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var right_gougbqRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(right_gougbqRowsSource.Chunks, __musoqProgressContext, "gougbq:3") : right_gougbqRowsSource.Chunks;
                var right = new List<RightRow0>();
                OnPhaseChanged("compiled:right", QueryPhase.GroupBy);
                var rightGroupsToFinalize = new List<AggregateGroup0>();
                var rightGroups = new Dictionary<string, AggregateGroup0>();
                AggregateGroup0 rightNullGroup = null;
                foreach (var gougbqChunk in right_gougbqRows)
                {
                    if (gougbqChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkView)
                    {
                        if (gougbqChunkView.Source is Musoq.Evaluator.Tests.Schema.Basic.BasicEntity[] gougbqChunkViewArray)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewArray[gougbqChunkViewOffset + gougbqIndex];
                                string groupKey = gougbq.City;
                                AggregateGroup0 rightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                                    if (!rightGroupExists)
                                    {
                                        rightGroupRef = new AggregateGroup0(groupKey);
                                        rightGroupsToFinalize.Add(rightGroupRef);
                                    }

                                    rightGroup = rightGroupRef;
                                }
                                else
                                {
                                    if (rightNullGroup == null)
                                    {
                                        rightNullGroup = new AggregateGroup0(null);
                                        rightGroupsToFinalize.Add(rightNullGroup);
                                    }

                                    rightGroup = rightNullGroup;
                                }

                                decimal population = gougbq.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        rightGroup.__agg0.Value = rightGroup.__agg0.HasValue ? checked(rightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        rightGroup.__agg0.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (gougbqChunkView.Source is List<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> gougbqChunkViewList)
                        {
                            int gougbqChunkViewOffset = gougbqChunkView.Offset;
                            for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunkView.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                            {
                                if ((gougbqIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var gougbq = gougbqChunkViewList[gougbqChunkViewOffset + gougbqIndex];
                                string groupKey = gougbq.City;
                                AggregateGroup0 rightGroup = null;
                                if (groupKey != null)
                                {
                                    ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                                    if (!rightGroupExists)
                                    {
                                        rightGroupRef = new AggregateGroup0(groupKey);
                                        rightGroupsToFinalize.Add(rightGroupRef);
                                    }

                                    rightGroup = rightGroupRef;
                                }
                                else
                                {
                                    if (rightNullGroup == null)
                                    {
                                        rightNullGroup = new AggregateGroup0(null);
                                        rightGroupsToFinalize.Add(rightNullGroup);
                                    }

                                    rightGroup = rightNullGroup;
                                }

                                decimal population = gougbq.Population;
                                {
                                    var __agg0Input = (decimal?)population;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        rightGroup.__agg0.Value = rightGroup.__agg0.HasValue ? checked(rightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                        rightGroup.__agg0.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    for (int gougbqIndex = 0, gougbqIndexCount = gougbqChunk.Count; gougbqIndex < gougbqIndexCount; ++gougbqIndex)
                    {
                        if ((gougbqIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var gougbq = gougbqChunk[gougbqIndex];
                        string groupKey = gougbq.City;
                        AggregateGroup0 rightGroup = null;
                        if (groupKey != null)
                        {
                            ref var rightGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(rightGroups, groupKey, out var rightGroupExists);
                            if (!rightGroupExists)
                            {
                                rightGroupRef = new AggregateGroup0(groupKey);
                                rightGroupsToFinalize.Add(rightGroupRef);
                            }

                            rightGroup = rightGroupRef;
                        }
                        else
                        {
                            if (rightNullGroup == null)
                            {
                                rightNullGroup = new AggregateGroup0(null);
                                rightGroupsToFinalize.Add(rightNullGroup);
                            }

                            rightGroup = rightNullGroup;
                        }

                        decimal population = gougbq.Population;
                        {
                            var __agg0Input = (decimal?)population;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                rightGroup.__agg0.Value = rightGroup.__agg0.HasValue ? checked(rightGroup.__agg0.Value + __agg0Current) : __agg0Current;
                                rightGroup.__agg0.HasValue = true;
                            }
                        }
                    }
                }

                right.EnsureCapacity(rightGroupsToFinalize.Count);
                OnPhaseChanged("compiled:right", QueryPhase.Select);
                foreach (var rightFinalGroup in rightGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    right.Add(new RightRow0(rightFinalGroup.__key0, rightFinalGroup.__agg0.HasValue ? (decimal?)rightFinalGroup.__agg0.Value : null));
                }

                OnPhaseChanged("compiled:right", QueryPhase.End);
                var resultRightKeys = new HashSet<string>(right.Count);
                foreach (var resultRightRow in right)
                {
                    resultRightKeys.Add((string)resultRightRow.City);
                }

                foreach (var resultLeftRow in left)
                {
                    if (!resultRightKeys.Contains((string)resultLeftRow.City))
                    {
                        __musoqFinalShapeRows.Add(new LeftLeftShape0((string)resultLeftRow.City, (decimal?)resultLeftRow.Sum_Population_));
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

        private sealed class AggregateGroup0
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public AggregateGroup0(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
            }

            public void MergeFrom(AggregateGroup0 source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class LeftLeftRow0 : Row
        {
            public LeftLeftRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                Sum_Population_ = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? Sum_Population_ { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Sum_Population_, ref visitor);
            }
        }

        private sealed class LeftLeftShape0
        {
            public LeftLeftShape0(string City, decimal? Sum_Population_)
            {
                this.City = City;
                this.Sum_Population_ = Sum_Population_;
            }

            public string City { get; }
            public decimal? Sum_Population_ { get; }
        }

        private sealed class LeftRightRow0 : Row
        {
            public LeftRightRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                Sum_Population_ = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? Sum_Population_ { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Sum_Population_, ref visitor);
            }
        }

        private sealed class RightRow0 : Row
        {
            public RightRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                Sum_Population_ = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? Sum_Population_ { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.City),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Sum_Population_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.City, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Sum_Population_, ref visitor);
            }
        }
    }
}
