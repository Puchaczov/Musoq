// === Parsed Query ===
/*
pivot #A.Entities() on Id, Country in ((2000, 'NL') as y2000_nl, (0, null) as Missing) using Count(distinct Name) as Customers group by City order by City
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null)) as ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null), AggRef(ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL')) as ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL')]
    Aggregate [keys: City] [aggs: Count(distinct Name) filter (where Id = 0 and Country is null), Count(distinct Name) filter (where Id = 2000 and Country = 'NL')]
      SchemaScan [#A.Entities() as ko3iko]
  Sort [ko3iko.City]
    Project [ko3iko.City as City, ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL') as y2000_nl, ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null) as Missing]
      CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.City as ko3iko.City, AggRef(ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null)) as ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null), AggRef(ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL')) as ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL')]
    PhysicalSingleKeyAggregate [key: City (String)] [aggs: Count(distinct Name) filter (where Id = 0 and Country is null), Count(distinct Name) filter (where Id = 2000 and Country = 'NL')]
      PhysicalSchemaScan [#A.Entities() as ko3iko]
  PhysicalSort [ko3iko.City]
    PhysicalProject [ko3iko.City as City, ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL') as y2000_nl, ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null) as Missing]
      PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: BasicEntity]
      Name: string <- property Name
      City: string <- property City
      Country: string <- property Country
      Id: int <- property Id
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      City: string <- field City
      y2000_nl: long <- field y2000_nl
      Missing: long <- field Missing

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: BasicEntity] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: string -> ResultAggregateGroup]
    ParallelSingleKeyAggregateLoop [ko3iko in ko3ikoRows by ko3iko.City; threshold 4096, sample 8192/6144, maxDegree 24, group ResultAggregateGroup]
      ParallelAccumulate
        Let [name: string = ko3iko.Name]
        Let [id: int = ko3iko.Id]
        Let [country: string = ko3iko.Country]
        TypedAggregateSet [Set(group.__agg0, name) filter ((id = 0) AND country IS NULL)]
        Let [name1: string = ko3iko.Name]
        TypedAggregateSet [Set(group.__agg1, name1) filter ((id = 2000) AND (country = 'NL'))]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(City: finalGroup.City, y2000_nl: ko3iko.Count(distinct ko3iko.Name) filter (where Id = 2000 and Country = 'NL'), Missing: ko3iko.Count(distinct ko3iko.Name) filter (where Id = 0 and Country is null))]
    SortShapeRows [result -> resultSorted by City ASC]
    ReturnDeferredTable [resultSorted: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q289_SpecCorePivotTupleNullDistinct
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
            new Column("City", typeof(string), 0),
            new Column("y2000_nl", typeof(long), 1),
            new Column("Missing", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Name", typeof(string), 0), new Column("City", typeof(string), 1), new Column("Country", typeof(string), 2), new Column("Id", typeof(int), 3) });
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
            return QueryRows.DeferredTable<ResultRow0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.City, __musoqShapeRow.y2000_nl, __musoqShapeRow.Missing);
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
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>("Entities", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<string, ResultAggregateGroup>();
                groupsToFinalize = ParallelSingleKeyAggregate_0(ko3ikoRows, 24, token);
                result.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Get(in finalGroup.__agg1), Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Get(in finalGroup.__agg0)));
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

                string name = ko3iko.Name;
                int id = ko3iko.Id;
                string country = ko3iko.Country;
                if (((id == 0) && (country == null)))
                {
                    Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Set(ref group.__agg0, (string)name);
                }

                string name1 = ko3iko.Name;
                if ((((id == 2000) & Operators.SqlCompare<string, string>(country, "NL", (string __sqlLeft, string __sqlRight) => (__sqlLeft == __sqlRight)))) == true)
                {
                    Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Set(ref group.__agg1, (string)name1);
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
            public Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.State __agg0;
            public Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.State __agg1;
            public readonly string __key0;
            public ResultAggregateGroup(string __key0)
            {
                this.__key0 = __key0;
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountDistinctReferenceAggregateKernel<string>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, long __value1, long __value2)
            {
                City = __value0;
                y2000_nl = __value1;
                Missing = __value2;
            }

            public string City { get; private set; }
            public override int Count => 3;
            public long Missing { get; private set; }
            public long y2000_nl { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        City = (string)value;
                        break;
                    case 1:
                        y2000_nl = (long)value;
                        break;
                    case 2:
                        Missing = (long)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "City" => true,
                "y2000_nl" => true,
                "Missing" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)City,
                1 => (object)y2000_nl,
                2 => (object)Missing,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "City" => (object)City,
                "y2000_nl" => (object)y2000_nl,
                "Missing" => (object)Missing,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string City, long y2000_nl, long Missing)
            {
                this.City = City;
                this.y2000_nl = y2000_nl;
                this.Missing = Missing;
            }

            public string City { get; }
            public long Missing { get; }
            public long y2000_nl { get; }
        }
    }
}
