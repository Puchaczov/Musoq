// === Parsed Query ===
/*
select Min(Temperature::Single) as MinTemperature,
                         Max(Temperature::Single) as MaxTemperature,
                         Avg(Temperature::Single) as AvgTemperature
                  from #weather.measurements()
                  group by City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Avg(ko3iko.Temperature::Single)) as ko3iko.Avg(ko3iko.Temperature::Single), AggRef(ko3iko.Max(ko3iko.Temperature::Single)) as ko3iko.Max(ko3iko.Temperature::Single), AggRef(ko3iko.Min(ko3iko.Temperature::Single)) as ko3iko.Min(ko3iko.Temperature::Single)]
    Aggregate [keys: City] [aggs: Avg(Temperature::Single), Max(Temperature::Single), Min(Temperature::Single)]
      SchemaScan [#weather.measurements() as ko3iko]
  Project [ko3iko.Min(ko3iko.Temperature::Single) as MinTemperature, ko3iko.Max(ko3iko.Temperature::Single) as MaxTemperature, ko3iko.Avg(ko3iko.Temperature::Single) as AvgTemperature]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(ko3iko.Avg(ko3iko.Temperature::Single)) as ko3iko.Avg(ko3iko.Temperature::Single), AggRef(ko3iko.Max(ko3iko.Temperature::Single)) as ko3iko.Max(ko3iko.Temperature::Single), AggRef(ko3iko.Min(ko3iko.Temperature::Single)) as ko3iko.Min(ko3iko.Temperature::Single)]
    PhysicalSingleKeyAggregate [key: City (String)] [aggs: Avg(Temperature::Single), Max(Temperature::Single), Min(Temperature::Single)]
      PhysicalSchemaScan [#weather.measurements() as ko3iko]
  PhysicalProject [ko3iko.Min(ko3iko.Temperature::Single) as MinTemperature, ko3iko.Max(ko3iko.Temperature::Single) as MaxTemperature, ko3iko.Avg(ko3iko.Temperature::Single) as AvgTemperature]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: WeatherMeasurementEntity]
      City: string <- property City
      Temperature: double <- property Temperature
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 3]
    Generated [ResultRow0]
      MinTemperature: float? <- field MinTemperature
      MaxTemperature: float? <- field MaxTemperature
      AvgTemperature: float? <- field AvgTemperature

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: WeatherMeasurementEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [temperature: double = ko3iko.Temperature]
      Let [temperatureSingle: float? = temperature::Single]
      GetOrAddSingleKeyAggregateGroup [group = groups[ko3iko.City] by City; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0, temperatureSingle)]
      TypedAggregateSet [Set(group.__agg1, temperatureSingle)]
      TypedAggregateSet [Set(group.__agg2, temperatureSingle)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(MinTemperature: ko3iko.Min(ko3iko.Temperature::Single), MaxTemperature: ko3iko.Max(ko3iko.Temperature::Single), AvgTemperature: ko3iko.Avg(ko3iko.Temperature::Single))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q185_RuntimeV2WeatherSingleAggregate
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
            new Column("MinTemperature", typeof(float?), 0),
            new Column("MaxTemperature", typeof(float?), 1),
            new Column("AvgTemperature", typeof(float?), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 0), new Column("Temperature", typeof(double), 1) });
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
                var __ko3ikoSchema = provider.GetSchema("#weather");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>.Open(__ko3ikoSchema, "measurements", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                ResultAggregateGroup nullGroup = null;
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
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
                        UpdateGroupsAggregates(groupsToFinalize, groups, ref nullGroup, ko3iko);
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg2.HasValue ? (float?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (float?)finalGroup.__agg1.Value : null, finalGroup.__agg0.HasValue ? (float?)finalGroup.__agg0.Sum / float.CreateChecked(finalGroup.__agg0.Count) : null));
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
        private static void UpdateGroupsAggregates(List<ResultAggregateGroup> groupsToFinalize, Dictionary<string, ResultAggregateGroup> groups, ref ResultAggregateGroup nullGroup, Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity ko3iko)
        {
            double temperature = ko3iko.Temperature;
            float? temperatureSingle = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToSingle(temperature);
            string groupKey = ko3iko.City;
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

            {
                var __agg0Input = (float?)temperatureSingle;
                if (__agg0Input.HasValue)
                {
                    var __agg0Current = __agg0Input.GetValueOrDefault();
                    group.__agg0.Sum = group.__agg0.HasValue ? checked(group.__agg0.Sum + __agg0Current) : __agg0Current;
                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                    group.__agg0.HasValue = true;
                    if (!group.__agg1.HasValue || __agg0Current > group.__agg1.Value)
                    {
                        group.__agg1.Value = __agg0Current;
                    }

                    group.__agg1.HasValue = true;
                    if (!group.__agg2.HasValue || __agg0Current < group.__agg2.Value)
                    {
                        group.__agg2.Value = __agg0Current;
                    }

                    group.__agg2.HasValue = true;
                }
            }
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.AvgAggregateKernel<float>.State __agg0;
            public Musoq.Plugins.MaxAggregateKernel<float>.State __agg1;
            public Musoq.Plugins.MinAggregateKernel<float>.State __agg2;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.AvgAggregateKernel<float>.Reset(ref this.__agg0);
                Musoq.Plugins.MaxAggregateKernel<float>.Reset(ref this.__agg1);
                Musoq.Plugins.MinAggregateKernel<float>.Reset(ref this.__agg2);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.AvgAggregateKernel<float>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.MaxAggregateKernel<float>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.MinAggregateKernel<float>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(float? __value0, float? __value1, float? __value2)
            {
                MinTemperature = __value0;
                MaxTemperature = __value1;
                AvgTemperature = __value2;
            }

            public float? AvgTemperature { get; private set; }
            public override int Count => 3;
            public float? MaxTemperature { get; private set; }
            public float? MinTemperature { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.MinTemperature = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<float?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.MaxTemperature = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<float?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.AvgTemperature = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<float?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.MinTemperature),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.MaxTemperature),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, float?>(this.AvgTemperature),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "MinTemperature" => true,
                "MaxTemperature" => true,
                "AvgTemperature" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.MinTemperature, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.MaxTemperature, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.AvgTemperature, ref visitor);
            }
        }
    }
}
