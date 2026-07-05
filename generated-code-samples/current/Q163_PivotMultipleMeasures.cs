/*
raw query string

pivot #A.entities()
                  on Month in ('Jan' as Jan, 'Feb' as Feb)
                  using Sum(Money) as Sales, Count(*) as Orders
                  group by City
                  order by City
*/

/*
logical plan representation string

MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*) filter (where Month = 'Feb')) as ko3iko.Count(*) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Count(*) filter (where Month = 'Jan')) as ko3iko.Count(*) filter (where Month = 'Jan'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
    Aggregate [keys: City] [aggs: Count(*) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Feb'), Count(*) filter (where Month = 'Jan'), Sum(Money) filter (where Month = 'Jan')]
      SchemaScan [#A.entities() as ko3iko]
  Sort [ko3iko.City]
    Project [ko3iko.City as City, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan_Sales, ko3iko.Count(*) filter (where Month = 'Jan') as Jan_Orders, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb_Sales, ko3iko.Count(*) filter (where Month = 'Feb') as Feb_Orders]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

/*
physical plan representation string

PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(*) filter (where Month = 'Feb')) as ko3iko.Count(*) filter (where Month = 'Feb'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), AggRef(ko3iko.Count(*) filter (where Month = 'Jan')) as ko3iko.Count(*) filter (where Month = 'Jan'), AggRef(ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')) as ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan')]
    PhysicalSingleKeyAggregate [key: City (String)] [aggs: Count(*) filter (where Month = 'Feb'), Sum(Money) filter (where Month = 'Feb'), Count(*) filter (where Month = 'Jan'), Sum(Money) filter (where Month = 'Jan')]
      PhysicalSchemaScan [#A.entities() as ko3iko]
  PhysicalSort [ko3iko.City]
    PhysicalProject [ko3iko.City as City, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan') as Jan_Sales, ko3iko.Count(*) filter (where Month = 'Jan') as Jan_Orders, ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb') as Feb_Sales, ko3iko.Count(*) filter (where Month = 'Feb') as Feb_Orders]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

/*
intermediate representation

ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      City: string <- property City
      Money: decimal <- property Money
      Month: string <- property Month
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 4]
    Generated [ResultRow0]
      City: string <- field City
      Jan_Sales: decimal? <- field Jan_Sales
      Jan_Orders: long <- field Jan_Orders
      Feb_Sales: decimal? <- field Feb_Sales
      Feb_Orders: long <- field Feb_Orders

  Body
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group ResultAggregateGroup]
      ParallelAccumulate
        Let [money: decimal = ko3iko.Money]
        TypedAggregateSet [Set(group.__agg0) filter (ko3iko.Month = 'Feb')]
        TypedAggregateSet [Set(group.__agg1, money) filter (ko3iko.Month = 'Feb')]
        TypedAggregateSet [Set(group.__agg2) filter (ko3iko.Month = 'Jan')]
        TypedAggregateSet [Set(group.__agg3, money) filter (ko3iko.Month = 'Jan')]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.City, Jan_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Jan'), Jan_Orders: ko3iko.Count(*) filter (where Month = 'Jan'), Feb_Sales: ko3iko.Sum(ko3iko.Money) filter (where Month = 'Feb'), Feb_Orders: ko3iko.Count(*) filter (where Month = 'Feb'))]
    SortShapeRows [result -> resultSorted by City ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === SyntaxTree:  ===
namespace GeneratedSample_Q163_PivotMultipleMeasures
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
            new Column("Jan_Sales", typeof(decimal?), 1),
            new Column("Jan_Orders", typeof(long), 2),
            new Column("Feb_Sales", typeof(decimal?), 3),
            new Column("Feb_Orders", typeof(long), 4)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("City", typeof(string), 11), new Column("Money", typeof(decimal), 15), new Column("Month", typeof(string), 16) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.Jan_Sales, __musoqShapeRow.Jan_Orders, __musoqShapeRow.Feb_Sales, __musoqShapeRow.Feb_Orders);
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
                var __ko3ikoSchema = provider.GetSchema("#A");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                groupsToFinalize = ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token);
                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg3.HasValue ? (decimal?)finalGroup.__agg3.Value : null, finalGroup.__agg2.Count, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, finalGroup.__agg0.Count));
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
        private static void ParallelSingleKeyAggregateChunk_0(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk, Dictionary<string, ResultAggregateGroup> groups, List<ResultAggregateGroup> orderedGroups, ref ResultAggregateGroup nullGroup, CancellationToken cancellationToken)
        {
            for (var index = 0; index < chunk.Count; index++)
            {
                if ((index & 1023) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Musoq.Evaluator.Tests.Schema.Basic.BasicEntity ko3iko = chunk[index];
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

                decimal money = ko3iko.Money;
                if ((ko3iko.Month == "Feb"))
                {
                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                }

                if ((ko3iko.Month == "Feb"))
                {
                    {
                        var __agg1Input = (decimal?)money;
                        if (__agg1Input.HasValue)
                        {
                            var __agg1Current = __agg1Input.GetValueOrDefault();
                            group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                            group.__agg1.HasValue = true;
                        }
                    }
                }

                if ((ko3iko.Month == "Jan"))
                {
                    group.__agg2.Count = checked(group.__agg2.Count + 1L);
                }

                if ((ko3iko.Month == "Jan"))
                {
                    {
                        var __agg3Input = (decimal?)money;
                        if (__agg3Input.HasValue)
                        {
                            var __agg3Current = __agg3Input.GetValueOrDefault();
                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                            group.__agg3.HasValue = true;
                        }
                    }
                }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static List<ResultAggregateGroup> ParallelSingleKeyAggregate_0(IEnumerable<IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>> rows, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var workerCount = Math.Max(1, maxDegreeOfParallelism);
            var shards = new global::System.Collections.Concurrent.ConcurrentQueue<List<ResultAggregateGroup>>();
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
            public void ProcessChunk(IReadOnlyList<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity> chunk)
            {
                ParallelSingleKeyAggregateChunk_0(chunk, _groups, _orderedGroups, ref _nullGroup, _cancellationToken);
            }
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountAllAggregateKernel.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg2;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg3;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, decimal? __value1, long __value2, decimal? __value3, long __value4)
            {
                City = __value0;
                Jan_Sales = __value1;
                Jan_Orders = __value2;
                Feb_Sales = __value3;
                Feb_Orders = __value4;
            }

            public string City { get; private set; }
            public override int Count => 5;
            public long Feb_Orders { get; private set; }
            public decimal? Feb_Sales { get; private set; }
            public long Jan_Orders { get; private set; }
            public decimal? Jan_Sales { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        Jan_Sales = (decimal?)value;
                        break;
                    case 2:
                        Jan_Orders = (long)value;
                        break;
                    case 3:
                        Feb_Sales = (decimal?)value;
                        break;
                    case 4:
                        Feb_Orders = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "Jan_Sales" => true,
                "Jan_Orders" => true,
                "Feb_Sales" => true,
                "Feb_Orders" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)Jan_Sales,
                2 => (object)Jan_Orders,
                3 => (object)Feb_Sales,
                4 => (object)Feb_Orders,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "Jan_Sales" => (object)Jan_Sales,
                "Jan_Orders" => (object)Jan_Orders,
                "Feb_Sales" => (object)Feb_Sales,
                "Feb_Orders" => (object)Feb_Orders,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, decimal? Jan_Sales, long Jan_Orders, decimal? Feb_Sales, long Feb_Orders)
            {
                this.City = City;
                this.Jan_Sales = Jan_Sales;
                this.Jan_Orders = Jan_Orders;
                this.Feb_Sales = Feb_Sales;
                this.Feb_Orders = Feb_Orders;
            }

            public string City { get; }
            public long Feb_Orders { get; }
            public decimal? Feb_Sales { get; }
            public long Jan_Orders { get; }
            public decimal? Jan_Sales { get; }
        }
    }
}
