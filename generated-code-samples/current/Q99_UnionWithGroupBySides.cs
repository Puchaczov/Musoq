/*
raw query string

select City, Sum(Population) from #A.Entities() group by City union (City) select City, Sum(Population) from #A.Entities() group by City
*/

/*
logical plan representation string

SetOp [Union]
  MultiStatement
    Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population)]
      Aggregate [keys: City] [aggs: Sum(Population)]
        SchemaScan [#A.Entities() as ko3iko]
    Project [ko3iko.City as City, ko3iko.Sum(ko3iko.Population) as Sum(Population)]
      CteRef [ko3ikoScore as ko3ikoScore]
  MultiStatement
    Project [vo04qt.City as vo04qt.City, AggRef(vo04qt.Sum(vo04qt.Population)) as vo04qt.Sum(vo04qt.Population)]
      Aggregate [keys: City] [aggs: Sum(Population)]
        SchemaScan [#A.Entities() as vo04qt]
    Project [vo04qt.City as City, vo04qt.Sum(vo04qt.Population) as Sum(Population)]
      CteRef [vo04qtScore as vo04qtScore]
*/

/*
physical plan representation string

PhysicalSetOp [Union]
  PhysicalMultiStatement
    PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Sum(ko3iko.Population)) as ko3iko.Sum(ko3iko.Population)]
      PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Population)]
        PhysicalSchemaScan [#A.Entities() as ko3iko]
    PhysicalProject [ko3iko.City as City, ko3iko.Sum(ko3iko.Population) as Sum(Population)]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
  PhysicalMultiStatement
    PhysicalProject [vo04qt.City as vo04qt.City, AggRef(vo04qt.Sum(vo04qt.Population)) as vo04qt.Sum(vo04qt.Population)]
      PhysicalSingleKeyAggregate [key: City (String)] [aggs: Sum(Population)]
        PhysicalSchemaScan [#A.Entities() as vo04qt]
    PhysicalProject [vo04qt.City as City, vo04qt.Sum(vo04qt.Population) as Sum(Population)]
      PhysicalCteRef [vo04qtScore as vo04qtScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    AggregateGroup [LeftAggregateGroup; keys: 1; typed aggs: 1]
    Generated [LeftRow0]
      City: string <- field City
      Sum(Population): decimal? <- field Sum_Population_
    SourceEntity [vo04qt: BasicEntity]
      City: string <- property City
      Population: decimal <- property Population
    AggregateGroup [RightAggregateGroup; keys: 1; typed aggs: 1]
    Generated [RightRow0]
      City: string <- field City
      Sum(Population): decimal? <- field Sum_Population_

  Body
    SourceScan [ko3iko: BasicEntity] -> left_ko3ikoRows
    CreateRowBuffer [left: List<LeftRow0>]
    CreateSingleKeyAggregateContext [leftGroups: string -> LeftAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in left_ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group LeftAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = ko3iko.Population]
        TypedAggregateSet [Set(leftGroup.__agg0, population)]
    EnsureRowBufferCapacity [left <- leftGroupsToFinalize.Count]
    ForEach [leftFinalGroup in leftGroupsToFinalize]
      AppendRowBuffer [left <- LeftRow0(City: leftFinalGroup.City, Sum(Population): ko3iko.Sum(ko3iko.Population))]
    SourceScan [vo04qt: BasicEntity] -> right_vo04qtRows
    CreateRowBuffer [right: List<RightRow0>]
    CreateSingleKeyAggregateContext [rightGroups: string -> RightAggregateGroup]
    ParallelSingleKeyAggregateLoop [vo04qt in right_vo04qtRows by vo04qt.City; threshold 4096, sample 8192/6144, maxDegree 24, group RightAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = vo04qt.Population]
        TypedAggregateSet [Set(rightGroup.__agg0, population)]
    EnsureRowBufferCapacity [right <- rightGroupsToFinalize.Count]
    ForEach [rightFinalGroup in rightGroupsToFinalize]
      AppendRowBuffer [right <- RightRow0(City: rightFinalGroup.City, Sum(Population): vo04qt.Sum(vo04qt.Population))]
    SetOperation [result = left Union right, HashSet]
    ReturnDeferredTable [result: LeftRow0 <- LeftShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q99_UnionWithGroupBySides
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
        private static readonly Column[] __columns_compiled_left_1 = new Column[]
        {
            new Column("City", typeof(string), 0),
            new Column("Sum(Population)", typeof(decimal?), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Population", typeof(decimal), 13) });
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
            return QueryRows.DeferredTable<LeftRow0>("result", __columns_compiled_left_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<LeftRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new LeftRow0(__musoqShapeRow.City, __musoqShapeRow.Sum_Population_);
            }
        }

        private IEnumerable<LeftShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.GroupBy);
            OnPhaseChanged("compiled:left", QueryPhase.Begin);
            OnPhaseChanged("compiled:right", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.Select);
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<LeftShape0>();
                var __left_ko3ikoSchema = provider.GetSchema("#A");
                var left_ko3ikoRowsSource = __left_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var left_ko3ikoRows = left_ko3ikoRowsSource.Chunks;
                var left = new List<LeftRow0>();
                var leftGroupsToFinalize = new List<AggregateGroup0>();
                var leftGroups = new Dictionary<string, AggregateGroup0>();
                leftGroupsToFinalize = ParallelSingleKeyAggregate_0(left_ko3ikoRows, 24, token);
                left.EnsureCapacity(leftGroupsToFinalize.Count);
                foreach (var leftFinalGroup in leftGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    left.Add(new LeftRow0(leftFinalGroup.__key0, leftFinalGroup.__agg0.HasValue ? (decimal?)leftFinalGroup.__agg0.Value : null));
                }

                var __right_vo04qtSchema = provider.GetSchema("#A");
                var right_vo04qtRowsSource = __right_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_vo04qtRows = right_vo04qtRowsSource.Chunks;
                var right = new List<RightRow0>();
                var rightGroupsToFinalize = new List<AggregateGroup0>();
                var rightGroups = new Dictionary<string, AggregateGroup0>();
                rightGroupsToFinalize = ParallelSingleKeyAggregate_0(right_vo04qtRows, 24, token);
                right.EnsureCapacity(rightGroupsToFinalize.Count);
                foreach (var rightFinalGroup in rightGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    right.Add(new RightRow0(rightFinalGroup.__key0, rightFinalGroup.__agg0.HasValue ? (decimal?)rightFinalGroup.__agg0.Value : null));
                }

                var resultKeys = new HashSet<string>(left.Count + right.Count);
                foreach (var resultLeftRow in left)
                {
                    resultKeys.Add((string)resultLeftRow.City);
                    __musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.City, (decimal?)resultLeftRow.Sum_Population_));
                }

                foreach (var resultRightRow in right)
                {
                    if (resultKeys.Add((string)resultRightRow.City))
                    {
                        __musoqFinalShapeRows.Add(new LeftShape0((string)resultRightRow.City, (decimal?)resultRightRow.Sum_Population_));
                    }
                }

                return __musoqFinalShapeRows;
            }
            finally
            {
                OnPhaseChanged("compiled:left", QueryPhase.End);
                OnPhaseChanged("compiled:right", QueryPhase.End);
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
        private static void ParallelSingleKeyAggregateChunk_0(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk, Dictionary<string, AggregateGroup0> groups, List<AggregateGroup0> orderedGroups, ref AggregateGroup0 nullGroup, CancellationToken cancellationToken)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = chunk[index];
                string groupKey = ko3iko.City;
                AggregateGroup0 leftGroup = null;
                if (groupKey != null)
                {
                    ref var leftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var leftGroupExists);
                    if (!leftGroupExists)
                    {
                        leftGroupRef = new AggregateGroup0(groupKey);
                        orderedGroups.Add(leftGroupRef);
                    }

                    leftGroup = leftGroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new AggregateGroup0(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    leftGroup = nullGroup;
                }

                decimal population = ko3iko.Population;
                {
                    var __agg0Input = (decimal?)population;
                    if (__agg0Input.HasValue)
                    {
                        var __agg0Current = __agg0Input.GetValueOrDefault();
                        leftGroup.__agg0.Value = leftGroup.__agg0.HasValue ? checked(leftGroup.__agg0.Value + __agg0Current) : __agg0Current;
                        leftGroup.__agg0.HasValue = true;
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<AggregateGroup0> ParallelSingleKeyAggregate_0(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<AggregateGroup0>>();
            var options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = workerCount
            };
            Parallel.ForEach<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>, ParallelSingleKeyAggregateChunkWorker_0>(rows, options, () => new ParallelSingleKeyAggregateChunkWorker_0(cancellationToken), (chunk, _, worker) =>
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
            var mergedGroups = new Dictionary<string, AggregateGroup0>();
            var groupsToFinalize = new List<AggregateGroup0>();
            AggregateGroup0 nullGroup = null;
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

        private sealed class AggregateGroup0
        {
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public readonly string __key0;
            public AggregateGroup0(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(AggregateGroup0 source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
            }
        }

        private sealed class LeftRow0 : Row
        {
            public LeftRow0(string __value0, decimal? __value1)
            {
                City = __value0;
                Sum_Population_ = __value1;
            }

            public string City { get; private set; }
            public override int Count => 2;
            public decimal? Sum_Population_ { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        Sum_Population_ = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)Sum_Population_,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "Sum(Population)" => (object)Sum_Population_,
                "Sum_Population_" => (object)Sum_Population_,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class LeftShape0
        {
            public LeftShape0(string City, decimal? Sum_Population_)
            {
                this.City = City;
                this.Sum_Population_ = Sum_Population_;
            }

            public string City { get; }
            public decimal? Sum_Population_ { get; }
        }

        private sealed class ParallelSingleKeyAggregateChunkWorker_0
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Dictionary<string, AggregateGroup0> _groups = new Dictionary<string, AggregateGroup0>();
            private AggregateGroup0 _nullGroup;
            private readonly List<AggregateGroup0> _orderedGroups = new List<AggregateGroup0>();
            public ParallelSingleKeyAggregateChunkWorker_0(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public List<AggregateGroup0> OrderedGroups => _orderedGroups;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_0(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken);
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

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        Sum_Population_ = (decimal?)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Sum(Population)" => true,
                "Sum_Population_" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)Sum_Population_,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "Sum(Population)" => (object)Sum_Population_,
                "Sum_Population_" => (object)Sum_Population_,
                _ => throw new KeyNotFoundException(name)
            };
        }
    }
}
