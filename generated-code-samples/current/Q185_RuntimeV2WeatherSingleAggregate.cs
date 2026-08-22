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
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Avg(ko3iko.Temperature::Single)) as ko3iko.Avg(ko3iko.Temperature::Single), AggRef(ko3iko.Max(ko3iko.Temperature::Single)) as ko3iko.Max(ko3iko.Temperature::Single), AggRef(ko3iko.Min(ko3iko.Temperature::Single)) as ko3iko.Min(ko3iko.Temperature::Single)]
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
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group ResultAggregateGroup]
      ParallelAccumulate
        Let [temperature: double = ko3iko.Temperature]
        Let [temperatureSingle: float? = temperature::Single]
        TypedAggregateSet [Set(group.__agg0, temperatureSingle)]
        TypedAggregateSet [Set(group.__agg1, temperatureSingle)]
        TypedAggregateSet [Set(group.__agg2, temperatureSingle)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(MinTemperature: ko3iko.Min(ko3iko.Temperature::Single), MaxTemperature: ko3iko.Max(ko3iko.Temperature::Single), AvgTemperature: ko3iko.Avg(ko3iko.Temperature::Single))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
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
                yield return new ResultRow0(__musoqShapeRow.MinTemperature, __musoqShapeRow.MaxTemperature, __musoqShapeRow.AvgTemperature);
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
                var __ko3ikoSchema = provider.GetSchema("#weather");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>("measurements", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                groupsToFinalize = ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__agg2.HasValue ? (float?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (float?)finalGroup.__agg1.Value : null, finalGroup.__agg0.HasValue ? (float?)finalGroup.__agg0.Sum / float.CreateChecked(finalGroup.__agg0.Count) : null));
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
        private static void ParallelSingleKeyAggregateChunk_0(IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity> chunk, Dictionary<string, ResultAggregateGroup> groups, List<ResultAggregateGroup> orderedGroups, ref ResultAggregateGroup nullGroup, CancellationToken cancellationToken)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity ko3iko = chunk[index];
                string groupKey = ko3iko.City;
                ResultAggregateGroup group = null;
                if (groupKey != null)
                {
                    ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                    if (!groupExists)
                    {
                        groupRef = new ResultAggregateGroup(groupKey);
                        orderedGroups.Add(groupRef);
                    }

                    group = groupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new ResultAggregateGroup(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    group = nullGroup;
                }

                double temperature = ko3iko.Temperature;
                float? temperatureSingle = global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToSingle(temperature);
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
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<ResultAggregateGroup> ParallelSingleKeyAggregate_0(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<ResultAggregateGroup>>();
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            Parallel.ForEach<IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>, ParallelSingleKeyAggregateChunkWorker_0>(rows, options, () => new ParallelSingleKeyAggregateChunkWorker_0(cancellationToken), (chunk, _, worker) =>
            {
                worker.ProcessChunk(chunk ?? Array.Empty<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity>());
                return worker;
            }, worker =>
            {
                if (worker.OrderedGroups.Count != 0)
                {
                    shards.Enqueue(worker.OrderedGroups);
                }
            });
            var mergedGroups = new Dictionary<string, ResultAggregateGroup>();
            var groupsToFinalize = new List<ResultAggregateGroup>();
            ResultAggregateGroup nullGroup = null;
            foreach (var shard in shards)
            {
                foreach (var sourceGroup in shard)
                {
                    string groupKey = sourceGroup.__key0;
                    if (groupKey != null)
                    {
                        ref var mergedGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(mergedGroups, groupKey, out var mergedGroupExists);
                        if (!mergedGroupExists)
                        {
                            mergedGroupRef = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            mergedGroupRef.MergeFrom(sourceGroup);
                        }
                    }
                    else
                    {
                        if (nullGroup == null)
                        {
                            nullGroup = sourceGroup;
                            groupsToFinalize.Add(sourceGroup);
                        }
                        else
                        {
                            nullGroup.MergeFrom(sourceGroup);
                        }
                    }
                }
            }

            return groupsToFinalize;
        }

        private sealed class ParallelSingleKeyAggregateChunkWorker_0
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Dictionary<string, ResultAggregateGroup> _groups = new Dictionary<string, ResultAggregateGroup>();
            private ResultAggregateGroup _nullGroup;
            private readonly List<ResultAggregateGroup> _orderedGroups = new List<ResultAggregateGroup>();
            public ParallelSingleKeyAggregateChunkWorker_0(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<ResultAggregateGroup> OrderedGroups => _orderedGroups;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.RuntimeV2.WeatherMeasurementEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_0(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken);
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        MinTemperature = (float?)value;
                        break;
                    case 1:
                        MaxTemperature = (float?)value;
                        break;
                    case 2:
                        AvgTemperature = (float?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "MinTemperature" => true,
                "MaxTemperature" => true,
                "AvgTemperature" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)MinTemperature,
                1 => (object)MaxTemperature,
                2 => (object)AvgTemperature,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "MinTemperature" => (object)MinTemperature,
                "MaxTemperature" => (object)MaxTemperature,
                "AvgTemperature" => (object)AvgTemperature,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(float? MinTemperature, float? MaxTemperature, float? AvgTemperature)
            {
                this.MinTemperature = MinTemperature;
                this.MaxTemperature = MaxTemperature;
                this.AvgTemperature = AvgTemperature;
            }

            public float? AvgTemperature { get; }
            public float? MaxTemperature { get; }
            public float? MinTemperature { get; }
        }
    }
}
