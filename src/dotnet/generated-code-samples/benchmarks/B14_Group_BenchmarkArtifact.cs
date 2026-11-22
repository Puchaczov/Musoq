// === Parsed Query ===
/*
select r.Key as Id, Sum(r.Amount) as Total, Count(distinct r.Optional) filter (where r.Id % 2 = 0) as Present
from bench.rows() r
group by r.Key
having Sum(r.Amount) > 0
order by Id
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Key as r.Key, AggRef(r.Count(distinct r.Optional) filter (where r.Id % 2 = 0)) as r.Count(distinct r.Optional) filter (where r.Id % 2 = 0), AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
    Having [(AggRef(r.Sum(r.Amount)) > 0)]
      Aggregate [keys: r.Key] [aggs: Count(distinct Optional) filter (where Id % 2 = 0), Sum(Amount)]
        SchemaScan [#bench.rows() as r]
  Sort [r.Key]
    Project [r.Key as Id, r.Sum(r.Amount) as Total, r.Count(distinct r.Optional) filter (where r.Id % 2 = 0) as Present]
      CteRef [rScore as rScore]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Key as r.Key, AggRef(r.Count(distinct r.Optional) filter (where r.Id % 2 = 0)) as r.Count(distinct r.Optional) filter (where r.Id % 2 = 0), AggRef(r.Sum(r.Amount)) as r.Sum(r.Amount)]
    PhysicalHaving [(AggRef(r.Sum(r.Amount)) > 0)]
      PhysicalSingleKeyAggregate [key: r.Key (Int32)] [aggs: Count(distinct Optional) filter (where Id % 2 = 0), Sum(Amount)]
        PhysicalSchemaScan [#bench.rows() as r]
  PhysicalSort [r.Key]
    PhysicalProject [r.Key as Id, r.Sum(r.Amount) as Total, r.Count(distinct r.Optional) filter (where r.Id % 2 = 0) as Present]
      PhysicalCteRef [rScore as rScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Id: int <- property Id
      Key: int <- property Key
      Amount: decimal <- property Amount
      Optional: int? <- property Optional
    AggregateGroup [ResultAggregateGroup; keys: 1; typed aggs: 2]
    Generated [ResultRow0]
      Id: int <- field Id
      Total: decimal? <- field Total
      Present: long <- field Present

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
      TypedAggregateSet [Set(group.__agg0, r.Optional) filter ((r.Id % 2) = 0)]
      Let [amount: decimal = r.Amount]
      TypedAggregateSet [Set(group.__agg1, amount)]
    EnsureShapeCapacity [result <- groupsToFinalize.Count]
    ForEach [finalGroup in groupsToFinalize]
      If [(r.Sum(r.Amount) > 0)]
        AppendShape [result <- ResultShape0(Id: finalGroup.r.Key, Total: r.Sum(r.Amount), Present: r.Count(distinct r.Optional) filter (where r.Id % 2 = 0))]
    SortOwnedBuffer [result by Id ASC, stable sequence __musoqSequence]
    ReturnDeferredTable [resultSorted: ResultShape0 from owned buffer result]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B14_Group_BenchmarkArtifact
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
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Total", typeof(decimal?), 1),
            new Column("Present", typeof(long), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Key", typeof(int), 1), new Column("Amount", typeof(decimal), 2), new Column("Optional", typeof(int?), 3) });
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
            var queryContext = new QueryRunContext(token, TypedParameters, PhaseChanged, DataSourceProgress, this, queryId: "compiled", provider: Provider, sourceRuntimeSettingsBySourceContextId: SourceRuntimeSettingsBySourceContextId, sourceRuntimeSettingDescriptionsBySourceContextId: SourceRuntimeSettingDescriptionsBySourceContextId, sourceExecutionPlans: SourceExecutionPlans, logger: Logger, queryProgress: QueryProgress);
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultShape0>("resultSorted", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultShape0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext))
            {
                yield return __musoqShapeRow;
            }
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
                var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
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
                                if (((r.Id % ((int?)2).GetValueOrDefault()) == 0))
                                {
                                    Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Set(ref group.__agg0, (int?)r.Optional);
                                }

                                decimal amount = r.Amount;
                                {
                                    var __agg1Input = (decimal?)amount;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
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
                                int groupKey = r.Key;
                                ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                                if (!groupExists)
                                {
                                    groupRef = new ResultAggregateGroup(groupKey);
                                    groupsToFinalize.Add(groupRef);
                                }

                                ResultAggregateGroup group = groupRef;
                                if (((r.Id % ((int?)2).GetValueOrDefault()) == 0))
                                {
                                    Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Set(ref group.__agg0, (int?)r.Optional);
                                }

                                decimal amount = r.Amount;
                                {
                                    var __agg1Input = (decimal?)amount;
                                    if (__agg1Input.HasValue)
                                    {
                                        var __agg1Current = __agg1Input.GetValueOrDefault();
                                        group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                        group.__agg1.HasValue = true;
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
                        int groupKey = r.Key;
                        ref var groupRef = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(groups, groupKey, out var groupExists);
                        if (!groupExists)
                        {
                            groupRef = new ResultAggregateGroup(groupKey);
                            groupsToFinalize.Add(groupRef);
                        }

                        ResultAggregateGroup group = groupRef;
                        if (((r.Id % ((int?)2).GetValueOrDefault()) == 0))
                        {
                            Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Set(ref group.__agg0, (int?)r.Optional);
                        }

                        decimal amount = r.Amount;
                        {
                            var __agg1Input = (decimal?)amount;
                            if (__agg1Input.HasValue)
                            {
                                var __agg1Current = __agg1Input.GetValueOrDefault();
                                group.__agg1.Value = group.__agg1.HasValue ? checked(group.__agg1.Value + __agg1Current) : __agg1Current;
                                group.__agg1.HasValue = true;
                            }
                        }
                    }
                }

                result.EnsureCapacity(groupsToFinalize.Count);
                foreach (var finalGroup in groupsToFinalize)
                {
                    token.ThrowIfCancellationRequested();
                    if (((Operators.SqlCompare<decimal?, int>(finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, 0, (decimal? __sqlLeft, int __sqlRight) => (__sqlLeft > __sqlRight)))) == true)
                    {
                        result.Add(new ResultShape0(finalGroup.__key0, finalGroup.__agg1.HasValue ? (decimal?)finalGroup.__agg1.Value : null, Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Get(in finalGroup.__agg0)) { __musoqSequence = result.Count });
                    }
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

        private sealed class ResultAggregateGroup
        {
            public Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.State __agg0;
            public Musoq.Plugins.SumAggregateKernel<decimal>.State __agg1;
            public readonly int __key0;
            public ResultAggregateGroup(int __key0)
            {
                this.__key0 = __key0;
                Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Reset(ref this.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Reset(ref this.__agg1);
            }

            public void MergeFrom(ResultAggregateGroup source)
            {
                Musoq.Plugins.CountDistinctNullableAggregateKernel<int>.Merge(ref this.__agg0, in source.__agg0);
                Musoq.Plugins.SumAggregateKernel<decimal>.Merge(ref this.__agg1, in source.__agg1);
            }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal? __value1, long __value2)
            {
                Id = __value0;
                Total = __value1;
                Present = __value2;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public long Present { get; private set; }
            public decimal? Total { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                "Present" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Present, ref visitor);
            }
        }

        private sealed class ResultShape0 : Row
        {
            public ResultShape0(int Id, decimal? Total, long Present)
            {
                this.Id = Id;
                this.Total = Total;
                this.Present = Present;
            }

            public override int Count => 3;
            public int Id { get; private set; }
            public long Present { get; private set; }
            public decimal? Total { get; private set; }
            public int __musoqSequence { get; init; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<decimal?, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, decimal?>(this.Total),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Present),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Total" => true,
                "Present" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Total, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Present, ref visitor);
            }
        }
    }
}
