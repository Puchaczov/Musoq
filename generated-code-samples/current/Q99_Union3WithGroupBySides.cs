/*
raw query string

select City, Sum(Population) from #A.Entities() group by City union (City) select City, Sum(Population) from #B.Entities() group by City union (City) select City, Sum(Population) from #C.Entities() group by City
*/

/*
logical plan representation string

SetOp [Union]
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

/*
physical plan representation string

PhysicalSetOp [Union]
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

/*
intermediate representation

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
    SourceScan [ko3iko: BasicEntity] -> leftLeft_ko3ikoRows
    CreateRowBuffer [leftLeft: List<LeftLeftRow0>]
    CreateSingleKeyAggregateContext [leftLeftGroups: string -> LeftLeftAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in leftLeft_ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group LeftLeftAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = ko3iko.Population]
        TypedAggregateSet [Set(leftLeftGroup.__agg0, population)]
    EnsureRowBufferCapacity [leftLeft <- leftLeftGroupsToFinalize.Count]
    ForEach [leftLeftFinalGroup in leftLeftGroupsToFinalize]
      AppendRowBuffer [leftLeft <- LeftLeftRow0(City: leftLeftFinalGroup.City, Sum(Population): ko3iko.Sum(ko3iko.Population))]
    SourceScan [vo04qt: BasicEntity] -> leftRight_vo04qtRows
    CreateRowBuffer [leftRight: List<LeftRightRow0>]
    CreateSingleKeyAggregateContext [leftRightGroups: string -> LeftRightAggregateGroup]
    ParallelSingleKeyAggregateLoop [vo04qt in leftRight_vo04qtRows by vo04qt.City; threshold 4096, sample 8192/6144, maxDegree 24, group LeftRightAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = vo04qt.Population]
        TypedAggregateSet [Set(leftRightGroup.__agg0, population)]
    EnsureRowBufferCapacity [leftRight <- leftRightGroupsToFinalize.Count]
    ForEach [leftRightFinalGroup in leftRightGroupsToFinalize]
      AppendRowBuffer [leftRight <- LeftRightRow0(City: leftRightFinalGroup.City, Sum(Population): vo04qt.Sum(vo04qt.Population))]
    SetOperation [left = leftLeft Union leftRight, HashSet]
    SourceScan [gougbq: BasicEntity] -> right_gougbqRows
    CreateRowBuffer [right: List<RightRow0>]
    CreateSingleKeyAggregateContext [rightGroups: string -> RightAggregateGroup]
    ParallelSingleKeyAggregateLoop [gougbq in right_gougbqRows by gougbq.City; threshold 4096, sample 8192/6144, maxDegree 24, group RightAggregateGroup]
      ParallelAccumulate
        Let [population: decimal = gougbq.Population]
        TypedAggregateSet [Set(rightGroup.__agg0, population)]
    EnsureRowBufferCapacity [right <- rightGroupsToFinalize.Count]
    ForEach [rightFinalGroup in rightGroupsToFinalize]
      AppendRowBuffer [right <- RightRow0(City: rightFinalGroup.City, Sum(Population): gougbq.Sum(gougbq.Population))]
    SetOperation [result = left Union right, HashSet]
    ReturnDeferredTable [result: LeftLeftRow0 <- LeftLeftShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q99_Union3WithGroupBySides
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
        private static readonly Column[] __columns_compiled_leftLeft_1 = new Column[]
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
                var __musoqFinalShapeRows = new List<LeftLeftShape0>();
                var __leftLeft_ko3ikoSchema = provider.GetSchema("#A");
                var leftLeft_ko3ikoRowsSource = __leftLeft_ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var leftLeft_ko3ikoRows = leftLeft_ko3ikoRowsSource.Chunks;
                var leftLeft = new List<LeftLeftRow0>();
                var leftLeftGroupsToFinalize = new List<AggregateGroup0>();
                var leftLeftGroups = new Dictionary<string, AggregateGroup0>();
                leftLeftGroupsToFinalize = ParallelSingleKeyAggregate_0(leftLeft_ko3ikoRows, 24, token);
                leftLeft.EnsureCapacity(leftLeftGroupsToFinalize.Count);
                foreach (var leftLeftFinalGroup in leftLeftGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    leftLeft.Add(new LeftLeftRow0(leftLeftFinalGroup.__key0, leftLeftFinalGroup.__agg0.HasValue ? (decimal?)leftLeftFinalGroup.__agg0.Value : null));
                }

                var __leftRight_vo04qtSchema = provider.GetSchema("#B");
                var leftRight_vo04qtRowsSource = __leftRight_vo04qtSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("vo04qt:2", sourceExecutionPlans["vo04qt:2"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["vo04qt:2"], logger, OnDataSourceProgress), Array.Empty<object>());
                var leftRight_vo04qtRows = leftRight_vo04qtRowsSource.Chunks;
                var leftRight = new List<LeftRightRow0>();
                var leftRightGroupsToFinalize = new List<AggregateGroup0>();
                var leftRightGroups = new Dictionary<string, AggregateGroup0>();
                leftRightGroupsToFinalize = ParallelSingleKeyAggregate_0(leftRight_vo04qtRows, 24, token);
                leftRight.EnsureCapacity(leftRightGroupsToFinalize.Count);
                foreach (var leftRightFinalGroup in leftRightGroupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    leftRight.Add(new LeftRightRow0(leftRightFinalGroup.__key0, leftRightFinalGroup.__agg0.HasValue ? (decimal?)leftRightFinalGroup.__agg0.Value : null));
                }

                var left = new List<LeftLeftRow0>();
                var leftKeys = new HashSet<string>(leftLeft.Count + leftRight.Count);
                foreach (var leftLeftRow in leftLeft)
                {
                    leftKeys.Add((string)leftLeftRow.City);
                    left.Add(leftLeftRow);
                }

                foreach (var leftRightRow in leftRight)
                {
                    if (leftKeys.Add((string)leftRightRow.City))
                    {
                        left.Add(new LeftLeftRow0((string)leftRightRow.City, (decimal?)leftRightRow.Sum_Population_));
                    }
                }

                var __right_gougbqSchema = provider.GetSchema("#C");
                var right_gougbqRowsSource = __right_gougbqSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("gougbq:3", sourceExecutionPlans["gougbq:3"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["gougbq:3"], logger, OnDataSourceProgress), Array.Empty<object>());
                var right_gougbqRows = right_gougbqRowsSource.Chunks;
                var right = new List<RightRow0>();
                var rightGroupsToFinalize = new List<AggregateGroup0>();
                var rightGroups = new Dictionary<string, AggregateGroup0>();
                rightGroupsToFinalize = ParallelSingleKeyAggregate_0(right_gougbqRows, 24, token);
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
                    __musoqFinalShapeRows.Add(new LeftLeftShape0((string)resultLeftRow.City, (decimal?)resultLeftRow.Sum_Population_));
                }

                foreach (var resultRightRow in right)
                {
                    if (resultKeys.Add((string)resultRightRow.City))
                    {
                        __musoqFinalShapeRows.Add(new LeftLeftShape0((string)resultRightRow.City, (decimal?)resultRightRow.Sum_Population_));
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
                AggregateGroup0 leftLeftGroup = null;
                if (groupKey != null)
                {
                    ref var leftLeftGroupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var leftLeftGroupExists);
                    if (!leftLeftGroupExists)
                    {
                        leftLeftGroupRef = new AggregateGroup0(groupKey);
                        orderedGroups.Add(leftLeftGroupRef);
                    }

                    leftLeftGroup = leftLeftGroupRef;
                }
                else
                {
                    if (nullGroup == null)
                    {
                        nullGroup = new AggregateGroup0(groupKey);
                        orderedGroups.Add(nullGroup);
                    }

                    leftLeftGroup = nullGroup;
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
