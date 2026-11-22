// === Parsed Query ===
/*
pivot bench.rows() r on r.Key in (0 as K0, 1 as K1, 2 as K2, 3 as K3) using Sum(r.Amount) as Total
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(r.Sum(r.Amount) filter (where r.Key = 3)) as r.Sum(r.Amount) filter (where r.Key = 3), AggRef(r.Sum(r.Amount) filter (where r.Key = 2)) as r.Sum(r.Amount) filter (where r.Key = 2), AggRef(r.Sum(r.Amount) filter (where r.Key = 1)) as r.Sum(r.Amount) filter (where r.Key = 1), AggRef(r.Sum(r.Amount) filter (where r.Key = 0)) as r.Sum(r.Amount) filter (where r.Key = 0)]
    Aggregate [keys: 1] [aggs: Sum(Amount) filter (where Key = 3), Sum(Amount) filter (where Key = 2), Sum(Amount) filter (where Key = 1), Sum(Amount) filter (where Key = 0)]
      SchemaScan [#bench.rows() as r]
  Project [r.Sum(r.Amount) filter (where r.Key = 0) as K0, r.Sum(r.Amount) filter (where r.Key = 1) as K1, r.Sum(r.Amount) filter (where r.Key = 2) as K2, r.Sum(r.Amount) filter (where r.Key = 3) as K3]
    CteRef [rScore as rScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(r.Sum(r.Amount) filter (where r.Key = 3)) as r.Sum(r.Amount) filter (where r.Key = 3), AggRef(r.Sum(r.Amount) filter (where r.Key = 2)) as r.Sum(r.Amount) filter (where r.Key = 2), AggRef(r.Sum(r.Amount) filter (where r.Key = 1)) as r.Sum(r.Amount) filter (where r.Key = 1), AggRef(r.Sum(r.Amount) filter (where r.Key = 0)) as r.Sum(r.Amount) filter (where r.Key = 0)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Sum(Amount) filter (where Key = 3), Sum(Amount) filter (where Key = 2), Sum(Amount) filter (where Key = 1), Sum(Amount) filter (where Key = 0)]
      PhysicalSchemaScan [#bench.rows() as r]
  PhysicalProject [r.Sum(r.Amount) filter (where r.Key = 0) as K0, r.Sum(r.Amount) filter (where r.Key = 1) as K1, r.Sum(r.Amount) filter (where r.Key = 2) as K2, r.Sum(r.Amount) filter (where r.Key = 3) as K3]
    PhysicalCteRef [rScore as rScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
      Amount: decimal <- property Amount
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 4]
    Generated [ResultRow0]
      K0: decimal? <- field K0
      K1: decimal? <- field K1
      K2: decimal? <- field K2
      K3: decimal? <- field K3

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: FixtureRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [r in rRows]
      Let [key: int = r.Key]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      TypedAggregateSet [Set(group.__agg0, r.Amount) filter (key = 3)]
      TypedAggregateSet [Set(group.__agg1, r.Amount) filter (key = 2)]
      TypedAggregateSet [Set(group.__agg2, r.Amount) filter (key = 1)]
      TypedAggregateSet [Set(group.__agg3, r.Amount) filter (key = 0)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(K0: r.Sum(r.Amount) filter (where r.Key = 0), K1: r.Sum(r.Amount) filter (where r.Key = 1), K2: r.Sum(r.Amount) filter (where r.Key = 2), K3: r.Sum(r.Amount) filter (where r.Key = 3))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B18_Pivot_BenchmarkTable
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
            new Column("K0", typeof(decimal?), 0),
            new Column("K1", typeof(decimal?), 1),
            new Column("K2", typeof(decimal?), 2),
            new Column("K3", typeof(decimal?), 3)
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
                                int key = r.Key;
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                if ((key == 3))
                                {
                                    {
                                        var __agg0Input = (decimal?)r.Amount;
                                        if (__agg0Input.HasValue)
                                        {
                                            var __agg0Current = __agg0Input.GetValueOrDefault();
                                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                            group.__agg0.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 2))
                                {
                                    {
                                        var __agg1Input = (decimal?)r.Amount;
                                        if (__agg1Input.HasValue)
                                        {
                                            var __agg1Current = __agg1Input.GetValueOrDefault();
                                            group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                            group.__agg1.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 1))
                                {
                                    {
                                        var __agg2Input = (decimal?)r.Amount;
                                        if (__agg2Input.HasValue)
                                        {
                                            var __agg2Current = __agg2Input.GetValueOrDefault();
                                            group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                            group.__agg2.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 0))
                                {
                                    {
                                        var __agg3Input = (decimal?)r.Amount;
                                        if (__agg3Input.HasValue)
                                        {
                                            var __agg3Current = __agg3Input.GetValueOrDefault();
                                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                            group.__agg3.HasValue = true;
                                        }
                                    }
                                }
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
                                int key = r.Key;
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                if ((key == 3))
                                {
                                    {
                                        var __agg0Input = (decimal?)r.Amount;
                                        if (__agg0Input.HasValue)
                                        {
                                            var __agg0Current = __agg0Input.GetValueOrDefault();
                                            group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                            group.__agg0.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 2))
                                {
                                    {
                                        var __agg1Input = (decimal?)r.Amount;
                                        if (__agg1Input.HasValue)
                                        {
                                            var __agg1Current = __agg1Input.GetValueOrDefault();
                                            group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                            group.__agg1.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 1))
                                {
                                    {
                                        var __agg2Input = (decimal?)r.Amount;
                                        if (__agg2Input.HasValue)
                                        {
                                            var __agg2Current = __agg2Input.GetValueOrDefault();
                                            group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                            group.__agg2.HasValue = true;
                                        }
                                    }
                                }

                                if ((key == 0))
                                {
                                    {
                                        var __agg3Input = (decimal?)r.Amount;
                                        if (__agg3Input.HasValue)
                                        {
                                            var __agg3Current = __agg3Input.GetValueOrDefault();
                                            group.__agg3.Value = group.__agg3.HasValue ? checked(group.__agg3.Value + __agg3Current) : __agg3Current;
                                            group.__agg3.HasValue = true;
                                        }
                                    }
                                }
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
                        int key = r.Key;
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        if ((key == 3))
                        {
                            {
                                var __agg0Input = (decimal?)r.Amount;
                                if (__agg0Input.HasValue)
                                {
                                    var __agg0Current = __agg0Input.GetValueOrDefault();
                                    group.__agg0.Value = group.__agg0.HasValue ? checked(group.__agg0.Value + __agg0Current) : __agg0Current;
                                    group.__agg0.HasValue = true;
                                }
                            }
                        }

                        if ((key == 2))
                        {
                            {
                                var __agg1Input = (decimal?)r.Amount;
                                if (__agg1Input.HasValue)
                                {
                                    var __agg1Current = __agg1Input.GetValueOrDefault();
                                    group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                    group.__agg1.HasValue = true;
                                }
                            }
                        }

                        if ((key == 1))
                        {
                            {
                                var __agg2Input = (decimal?)r.Amount;
                                if (__agg2Input.HasValue)
                                {
                                    var __agg2Current = __agg2Input.GetValueOrDefault();
                                    group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                    group.__agg2.HasValue = true;
                                }
                            }
                        }

                        if ((key == 0))
                        {
                            {
                                var __agg3Input = (decimal?)r.Amount;
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

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0((finalGroup.__agg3.HasValue ? (decimal?)finalGroup.__agg3.Value : null), (finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null), (finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null), (finalGroup.__agg0.HasValue ? (decimal?)finalGroup.__agg0.Value : null)));
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
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg2;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg3;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg2);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg3);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg2, in source.__agg2);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg3, in source.__agg3);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public decimal? K0 { get; private set; }
            public decimal? K1 { get; private set; }
            public decimal? K2 { get; private set; }
            public decimal? K3 { get; private set; }

            public ResultRow0(decimal? __value0, decimal? __value1, decimal? __value2, decimal? __value3)
            {
                K0 = __value0;
                K1 = __value1;
                K2 = __value2;
                K3 = __value3;
            }

            public override int Count => 4;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.K0, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.K1, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.K2, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(3, this.K3, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K0),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K1),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K2),
                3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K3),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "K0" => true,
                "K1" => true,
                "K2" => true,
                "K3" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K0 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K1 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K2 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    3 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.K3 = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }
    }
}
