// === Parsed Query ===
/*
param(suffix: string, minCount: int)
              select Country + $suffix as CountryKey, Count(Name) as NameCount
              from #A.entities()
              group by Country + $suffix
              having Count(Name) >= $minCount
*/

// === Logical Plan ===
/*
MultiStatement
  Project [(ko3iko.Country || $suffix) as ko3iko.Country + $suffix, AggRef(ko3iko.Count(ko3iko.Name)) as ko3iko.Count(ko3iko.Name)]
    Having [(AggRef(ko3iko.Count(ko3iko.Name)) >= $minCount)]
      Aggregate [keys: Country + $suffix] [aggs: Count(Name)]
        SchemaScan [#A.entities() as ko3iko]
  Project [ko3iko.Country + $suffix as CountryKey, ko3iko.Count(ko3iko.Name) as NameCount]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [(ko3iko.Country || $suffix) as ko3iko.Country + $suffix, AggRef(ko3iko.Count(ko3iko.Name)) as ko3iko.Count(ko3iko.Name)]
    PhysicalHaving [(AggRef(ko3iko.Count(ko3iko.Name)) >= $minCount)]
      PhysicalSingleKeyAggregate [key: Country + $suffix (String)] [aggs: Count(Name)]
        PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalProject [ko3iko.Country + $suffix as CountryKey, ko3iko.Count(ko3iko.Name) as NameCount]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      Country: string <- property Country
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 1]
    Generated [ResultRow0]
      CountryKey: string <- field CountryKey
      NameCount: long <- field NameCount

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by (ko3iko.Country || $suffix); threshold 4096, sample 8192/6144, maxDegree 24, group ResultAggregateGroup]
      ParallelAccumulate
        Let [name: string = ko3iko.Name]
        TypedAggregateSet [Set(group.__agg0, name)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      If [(ko3iko.Count(ko3iko.Name) >= $minCount)]
        AppendShape [result <- ResultShape0(CountryKey: finalGroup.Country + $suffix, NameCount: ko3iko.Count(ko3iko.Name))]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q123_ScriptParameterGroupByHelperCapture
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
            new Column("CountryKey", typeof(string), 0),
            new Column("NameCount", typeof(long), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("Country", typeof(string), 1) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = new ScriptParameterContract[]
        {
            new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null),
            new ScriptParameterContract("minCount", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null)
        };
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = new ScriptParameterDefinition[]
        {
            new ScriptParameterDefinition(new ScriptParameterContract("suffix", "string", "string", typeof(string), false, false, null, null, false, ScriptParameterDefaultKind.None, null)),
            new ScriptParameterDefinition(new ScriptParameterContract("minCount", "int", "int", typeof(int), false, false, null, null, false, ScriptParameterDefaultKind.None, null))
        };
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
                yield return new ResultRow0(__musoqShapeRow.CountryKey, __musoqShapeRow.NameCount);
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
                var paramSuffix = ScriptParameterBinder.GetRequired<string>(__musoqExecutionState.Parameters, "suffix");
                var paramMinCount = ScriptParameterBinder.GetRequired<int>(__musoqExecutionState.Parameters, "minCount");
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, new string[] { "suffix", "minCount" });
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                groupsToFinalize = ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token, paramSuffix);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if ((finalGroup.__agg0.Count >= paramMinCount))
                    {
                        __musoqFinalShapeRows.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg0.Count));
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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static void ParallelSingleKeyAggregateChunk_0(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk, Dictionary<string, ResultAggregateGroup> groups, List<ResultAggregateGroup> orderedGroups, ref ResultAggregateGroup nullGroup, CancellationToken cancellationToken, string paramSuffix)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = chunk[index];
                string groupKey = (ko3iko.Country + paramSuffix);
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

                string name = ko3iko.Name;
                if ((string)name != null)
                {
                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<ResultAggregateGroup> ParallelSingleKeyAggregate_0(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken, string paramSuffix)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<ResultAggregateGroup>>();
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            Parallel.ForEach<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>, ParallelSingleKeyAggregateChunkWorker_0>(rows, options, () => new ParallelSingleKeyAggregateChunkWorker_0(cancellationToken, paramSuffix), (chunk, _, worker) =>
            {
                worker.ProcessChunk(chunk ?? Array.Empty<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>());
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
            private readonly string _paramSuffix;
            public ParallelSingleKeyAggregateChunkWorker_0(CancellationToken cancellationToken, string paramSuffix)
            {
                _cancellationToken = cancellationToken;
                _paramSuffix = paramSuffix;
            }

            public List<ResultAggregateGroup> OrderedGroups => _orderedGroups;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_0(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken, _paramSuffix);
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
                CountryKey = __value0;
                NameCount = __value1;
            }

            public override int Count => 2;
            public string CountryKey { get; private set; }
            public long NameCount { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        CountryKey = (string)value;
                        break;
                    case 1:
                        NameCount = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "CountryKey" => true,
                "NameCount" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)CountryKey,
                1 => (object)NameCount,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "CountryKey" => (object)CountryKey,
                "NameCount" => (object)NameCount,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string CountryKey, long NameCount)
            {
                this.CountryKey = CountryKey;
                this.NameCount = NameCount;
            }

            public string CountryKey { get; }
            public long NameCount { get; }
        }
    }
}
