// === Parsed Query ===
/*
select Count(*) as Count, Sum(r.Amount) as Total, Avg(r.Amount) as Average from bench.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(r.Avg(r.Amount)) as r.Avg(r.Amount), AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount), AggRef(r.Count(*)) as r.Count(*)]
    Aggregate [keys: 1] [aggs: Avg(Amount), Sum(Amount), Count(*)]
      SchemaScan [#bench.rows() as r]
  Project [r.Count(*) as Count, r.Sum(r.Amount) as Total, r.Avg(r.Amount) as Average]
    CteRef [rScore as rScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(r.Avg(r.Amount)) as r.Avg(r.Amount), AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount), AggRef(r.Count(*)) as r.Count(*)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Avg(Amount), Sum(Amount), Count(*)]
      PhysicalSchemaScan [#bench.rows() as r]
  PhysicalProject [r.Count(*) as Count, r.Sum(r.Amount) as Total, r.Avg(r.Amount) as Average]
    PhysicalCteRef [rScore as rScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Amount: decimal <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 3]
    Generated [ResultRow0]
      Count: long <- field Count_
      Total: decimal? <- field Total
      Average: decimal? <- field Average

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [r in rRows]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      Let [amount: decimal = r.Amount]
      TypedAggregateSet [Set(group.__agg0, amount)]
      Let [amount1: decimal = r.Amount]
      TypedAggregateSet [Set(group.__agg1, amount1)]
      TypedAggregateSet [Set(group.__agg2)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Count: r.Count(*), Total: r.Sum(r.Amount), Average: r.Avg(r.Amount))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B14_WholeAggregate_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Amount", typeof(decimal), 0) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Count", typeof(long), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Average", typeof(decimal?), 2)
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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
                var __musoqFinalShapeRows = new List<ResultRow0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#bench");
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
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
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal amount = r.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Sum = group.__agg0.HasValue ? checked(group.__agg0.Sum + __agg0Current) : __agg0Current;
                                        group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal amount1 = r.Amount;
                                {
                                    var __agg1Input = (decimal?)amount1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                group.__agg2.Count = checked(group.__agg2.Count + 1L);
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
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal amount = r.Amount;
                                {
                                    var __agg0Input = (decimal?)amount;
                                    if (__agg0Input.HasValue)
                                    {
                                        var __agg0Current = __agg0Input.GetValueOrDefault();
                                        group.__agg0.Sum = group.__agg0.HasValue ? checked(group.__agg0.Sum + __agg0Current) : __agg0Current;
                                        group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                        group.__agg0.HasValue = true;
                                    }
                                }

                                decimal amount1 = r.Amount;
                                {
                                    var __agg1Input = (decimal?)amount1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                group.__agg2.Count = checked(group.__agg2.Count + 1L);
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
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        decimal amount = r.Amount;
                        {
                            var __agg0Input = (decimal?)amount;
                            if (__agg0Input.HasValue)
                            {
                                var __agg0Current = __agg0Input.GetValueOrDefault();
                                group.__agg0.Sum = group.__agg0.HasValue ? checked(group.__agg0.Sum + __agg0Current) : __agg0Current;
                                group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                group.__agg0.HasValue = true;
                            }
                        }

                        decimal amount1 = r.Amount;
                        {
                            var __agg1Input = (decimal?)amount1;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                group.__agg1.HasValue = true;
                            }
                        }

                        group.__agg2.Count = checked(group.__agg2.Count + 1L);
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg2.Count, (finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null), (finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Sum / decimal.CreateChecked(finalGroup.__agg0.Count) : null)));
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

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
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
            public Musoq.Plugins.AvgAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.CountAllAggregateKernel.State __agg2;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.AvgAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg1);
                Musoq.Plugins.CountAllAggregateKernel.Reset(ref this.__agg2);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.AvgAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.CountAllAggregateKernel.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public long Count_ { get; private set; }
            public decimal? Total { get; private set; }
            public decimal? Average { get; private set; }

            public ResultRow0(long __value0, decimal? __value1, decimal? __value2)
            {
                Count_ = __value0;
                Total = __value1;
                Average = __value2;
            }

            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Count_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Average, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Count" => true,
                "Count_" => true,
                "Total" => true,
                "Average" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }
    }
}
