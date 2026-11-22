// === Parsed Query ===
/*
select r.Key as Id, Count(*) as Count, Sum(r.Amount) as Total
from bench.skewed() r
group by r.Key
order by Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount), AggRef(r.Count(*)) as r.Count(*)]
    Aggregate [keys: r.Key] [aggs: Sum(Amount), Count(*)]
      SchemaScan [#bench.skewed() as r]
  Sort [r.Key]
    Project [r.Key as Id, r.Count(*) as Count, r.Sum(r.Amount) as Total]
      CteRef [rScore as rScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Key as r.Key, AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount), AggRef(r.Count(*)) as r.Count(*)]
    PhysicalSingleKeyAggregate [key: r.Key (Int32)] [aggs: Sum(Amount), Count(*)]
      PhysicalSchemaScan [#bench.skewed() as r]
  PhysicalSort [r.Key]
    PhysicalProject [r.Key as Id, r.Count(*) as Count, r.Sum(r.Amount) as Total]
      PhysicalCteRef [rScore as rScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      Id: int <- field Id
      Count: long <- field Count_
      Total: decimal? <- field Total

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [GroupBy]
    CreateSingleKeyAggregateContext [groups: int -> ResultAggregateGroup]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      GetOrAddSingleKeyAggregateGroup [group = groups[r.Key] by r.Key; typed: ResultAggregateGroup]
      Let [amount: decimal = r.Amount]
      TypedAggregateSet [Set(group.__agg0, amount)]
      TypedAggregateSet [Set(group.__agg1)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultShape0(Id: finalGroup.r.Key, Count: r.Count(*), Total: r.Sum(r.Amount))]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B14_Skewed_BenchmarkTable
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IContextTableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0), new Column("Amount", typeof(decimal), 1) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Count", typeof(long), 1),
            new Column("Total", typeof(decimal?), 2)
        };
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public ILogger Logger { get; set; }
        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();

        public event QueryPhaseEventHandler PhaseChanged;
        public event DataSourceEventHandler DataSourceProgress;
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "skewed", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var result = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.GroupBy);
                var groupsToFinalize = new List<ResultAggregateGroup>();
                var groups = new Dictionary<int, ResultAggregateGroup>();
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkView)
                    {
                        if (rChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                int groupKey = r.Key;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(groupKey);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                decimal amount = r.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                group.__agg1.Count = checked(group.__agg1.Count + 1L);
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                int groupKey = r.Key;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(groupKey);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                decimal amount = r.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                group.__agg1.Count = checked(group.__agg1.Count + 1L);
                            }

                            continue;
                        }
                    }

                    for (int rIndex = 0, rIndexCount = rChunk.Count; rIndex < rIndexCount; ++rIndex)
                    {
                        if ((rIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var r = rChunk[rIndex];
                        int groupKey = r.Key;
                        ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                        if (!groupExists)
                        {
                            groupRef = new ResultAggregateGroup(groupKey);
                            groupsToFinalize.Add(groupRef);
                        }

                        ResultAggregateGroup group = groupRef;
                        decimal amount = r.Amount;
                        {
                            var __agg0Input = (decimal?)amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                group.__agg0.HasValue = true;
                            }
                        }

                        group.__agg1.Count = checked(group.__agg1.Count + 1L);
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg1.Count, (finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null)) { __musoqSequence = result.Count });
                }

                result.Sort(Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return left.__musoqSequence.CompareTo(right.__musoqSequence);
                }));
                return result;
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

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        private sealed class ResultAggregateGroup
        {
            public readonly int __key0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg1;
            public ResultAggregateGroup(int __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg1);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, long Count_, decimal? Total)
            {
                this.Id = Id;
                this.Count_ = Count_;
                this.Total = Total;
            }

            public int Id { get; private set; }
            public long Count_ { get; private set; }
            public decimal? Total { get; private set; }
            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Count_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Total, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Count" => true,
                "Count_" => true,
                "Total" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public int __musoqSequence { get; init; }
        }
    }
}
