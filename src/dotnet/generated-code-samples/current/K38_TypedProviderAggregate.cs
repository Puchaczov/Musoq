// === Parsed Query ===
/*
select Sum(Amount) as Total, Avg(Amount) as Average, Count(Amount) as Present from #typedrows.rows()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as 1, AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Avg(ko3iko.Amount)) as ko3iko.Avg(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
    Aggregate [keys: 1] [aggs: Count(Amount), Avg(Amount), Sum(Amount)]
      SchemaScan [#typedrows.rows() as ko3iko]
  Project [ko3iko.Sum(ko3iko.Amount) as Total, ko3iko.Avg(ko3iko.Amount) as Average, ko3iko.Count(ko3iko.Amount) as Present]
    CteRef [ko3ikoScore as ko3ikoScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [AggRef(ko3iko.Count(ko3iko.Amount)) as ko3iko.Count(ko3iko.Amount), AggRef(ko3iko.Avg(ko3iko.Amount)) as ko3iko.Avg(ko3iko.Amount), AggRef(ko3iko.Sum(ko3iko.Amount)) as ko3iko.Sum(ko3iko.Amount)]
    PhysicalSingleKeyAggregate [key: 1 (Int16)] [aggs: Count(Amount), Avg(Amount), Sum(Amount)]
      PhysicalSchemaScan [#typedrows.rows() as ko3iko] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=5E3ED780A4A305BFD6D5E14D36CE121DFA0BCD3E1656CBE6D0BCAC29544458BF]
  PhysicalProject [ko3iko.Sum(ko3iko.Amount) as Total, ko3iko.Avg(ko3iko.Amount) as Average, ko3iko.Count(ko3iko.Amount) as Present]
    PhysicalCteRef [ko3ikoScore as ko3ikoScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_5E3ED780A4A3_S]
      Amount: decimal? <- field Field0
    AggregateGroup [ResultAggregateGroup; keys: 0; typed aggs: 3]
    Generated [ResultRow0]
      Total: decimal? <- field Total
      Average: decimal? <- field Average
      Present: long <- field Present

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: QueryRow_5E3ED780A4A3_S] -> ko3ikoRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=5E3ED780A4A305BFD6D5E14D36CE121DFA0BCD3E1656CBE6D0BCAC29544458BF]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateAggregateContext [rootGroup, group, groupsToFinalize; typed: ResultAggregateGroup]
    ChunkedForEach [ko3iko in ko3ikoRows]
      EnsureAggregateGroup [group; typed: ResultAggregateGroup]
      Let [amount: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg0, amount)]
      Let [amount1: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg1, amount1)]
      Let [amount2: decimal? = ko3iko.Amount]
      TypedAggregateSet [Set(group.__agg2, amount2)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    PhaseBoundary [Select]
    ForEach [finalGroup in groupsToFinalize]
      AppendShape [result <- ResultRow0(Total: ko3iko.Sum(ko3iko.Amount), Average: ko3iko.Avg(ko3iko.Amount), Present: ko3iko.Count(ko3iko.Amount))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_K38_TypedProviderAggregate
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
            new Column("Total", typeof(decimal?), 0),
            new Column("Average", typeof(decimal?), 1),
            new Column("Present", typeof(long), 2)
        };
        private static readonly QueryRowShape __queryRowShape_5E3ED780A4A3 = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 1, "Amount", typeof(decimal?), true) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Amount", typeof(decimal?), 0) });
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
                var __ko3ikoSchema = provider.GetSchema("#typedrows");
                var __ko3ikoSchemaQueryRows = __ko3ikoSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#typedrows.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 5E3ED780A4A305BFD6D5E14D36CE121DFA0BCD3E1656CBE6D0BCAC29544458BF).");
                var ko3ikoRowsSource = QuerySourceInvocation<QueryRow_5E3ED780A4A3_S, QueryRowMaterializer_5E3ED780A4A3_S>.Open(__ko3ikoSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), __queryRowShape_5E3ED780A4A3), global::Musoq.Schema.SourceArguments.Empty);
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_5E3ED780A4A3_S>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                var groupsToFinalize = new List<ResultAggregateGroup>();
                ResultAggregateGroup group = new ResultAggregateGroup();
                groupsToFinalize.Add(group);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_5E3ED780A4A3_S> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is QueryRow_5E3ED780A4A3_S[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal? amount = ko3iko.Field0;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                decimal? amount1 = ko3iko.Field0;
                                {
                                    var __agg1Input = (decimal?)amount1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Sum = group.__agg1.HasValue ? checked(group.__agg1.Sum + __agg1Current) : __agg1Current;
                                        group.__agg1.Count = checked(group.__agg1.Count + 1L);
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount2 = ko3iko.Field0;
                                {
                                    var __agg2Input = (decimal?)amount2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<QueryRow_5E3ED780A4A3_S> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                if (group == null)
                                {
                                    group = new ResultAggregateGroup();
                                    groupsToFinalize.Add(group);
                                }

                                decimal? amount = ko3iko.Field0;
                                if (((decimal?)amount).HasValue)
                                {
                                    group.__agg0.Count = checked(group.__agg0.Count + 1L);
                                }

                                decimal? amount1 = ko3iko.Field0;
                                {
                                    var __agg1Input = (decimal?)amount1;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Sum = group.__agg1.HasValue ? checked(group.__agg1.Sum + __agg1Current) : __agg1Current;
                                        group.__agg1.Count = checked(group.__agg1.Count + 1L);
                                        group.__agg1.HasValue = true;
                                    }
                                }

                                decimal? amount2 = ko3iko.Field0;
                                {
                                    var __agg2Input = (decimal?)amount2;
                                    if (__agg2Input.HasValue)
                                    {
                                        var __agg2Current = __agg2Input.GetValueOrDefault();
                                        group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                        group.__agg2.HasValue = true;
                                    }
                                }
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
                        if (group == null)
                        {
                            group = new ResultAggregateGroup();
                            groupsToFinalize.Add(group);
                        }

                        decimal? amount = ko3iko.Field0;
                        if (((decimal?)amount).HasValue)
                        {
                            group.__agg0.Count = checked(group.__agg0.Count + 1L);
                        }

                        decimal? amount1 = ko3iko.Field0;
                        {
                            var __agg1Input = (decimal?)amount1;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                group.__agg1.Sum = group.__agg1.HasValue ? checked(group.__agg1.Sum + __agg1Current) : __agg1Current;
                                group.__agg1.Count = checked(group.__agg1.Count + 1L);
                                group.__agg1.HasValue = true;
                            }
                        }

                        decimal? amount2 = ko3iko.Field0;
                        {
                            var __agg2Input = (decimal?)amount2;
                            if (__agg2Input.HasValue)
                            {
                                var __agg2Current = __agg2Input.GetValueOrDefault();
                                group.__agg2.Value = group.__agg2.HasValue ? checked(group.__agg2.Value + __agg2Current) : __agg2Current;
                                group.__agg2.HasValue = true;
                            }
                        }
                    }
                }

                __musoqFinalShapeRows.EnsureCapacity(groupsToFinalize.Count);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    __musoqFinalShapeRows.Add(new ResultRow0(finalGroup.__agg2.HasValue ? (decimal?)finalGroup.__agg2.Value : null, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Sum / decimal.CreateChecked(finalGroup.__agg1.Count) : null, finalGroup.__agg0.Count));
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

        private readonly struct QueryRowMaterializer_5E3ED780A4A3_S : IQueryRowMaterializer<QueryRow_5E3ED780A4A3_S>
        {
            public static QueryRow_5E3ED780A4A3_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_5E3ED780A4A3_S(reader.Read<decimal?>(0));
        }

        private readonly struct QueryRow_5E3ED780A4A3_S
        {
            public QueryRow_5E3ED780A4A3_S(decimal? Field0)
            {
                this.Field0 = Field0;
            }

            public decimal? Field0 { get; }
        }

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountNullableAggregateKernel<decimal>.State __agg0;
            public Musoq.Plugins.AvgAggregateKernel<decimal>.State __agg1;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg2;
            public ResultAggregateGroup()
            {
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Reset(ref this.__agg0);
                Musoq.Plugins.AvgAggregateKernel<decimal>.Reset(ref this.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg2);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountNullableAggregateKernel<decimal>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.AvgAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg2, in source.__agg2);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(decimal? __value0, decimal? __value1, long __value2)
            {
                Total = __value0;
                Average = __value1;
                Present = __value2;
            }

            public decimal? Average { get; private set; }
            public override int Count => 3;
            public long Present { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Average),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Total" => true,
                "Average" => true,
                "Present" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Average, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Present, ref visitor);
            }
        }
    }
}
