// === Parsed Query ===
/*
select r.Id as Id, i.Value as Item, r.StableWork(r.Text) as Work from perf.rows() r cross apply r.Items i
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as r.Id, r.Text as r.Text, r.Items as r.Items, i.Value as i.Value]
    Apply [Cross]
      SchemaScan [#perf.rows() as r]
      PropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
  Project [r.Id as Id, i.Value as Item, StableWork(r.Text) as Work]
    CteRef [ri as ri]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as r.Id, r.Text as r.Text, i.Value as i.Value]
    PhysicalNestedLoopApply [Cross]
      PhysicalSchemaScan [#perf.rows() as r]
      PhysicalPropertySource [r.Items as i] [apply: Cross] [type: Int32[]]
  PhysicalProject [r.Id as Id, i.Value as Item, StableWork(r.Text) as Work]
    PhysicalCteRef [ri as ri]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
      Text: string <- property Text
      Items: int[] <- property Items
    SourceEntity [i: int]
      Value: int <- direct scalar value
    Generated [ResultRow0]
      Id: int <- field Id
      Item: int <- field Item_
      Work: long <- field Work

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    SourceScan [r: PerformanceRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    CreateObject [__resultPerformanceLibrary0: PerformanceLibrary]
    ChunkedForEach [r in rRows]
      Let [rId: int = r.Id]
      Let [stableWork: long = StableWork(r.Text)]
      EnumerableSource [r.Items -> iRows]
      ForEach [i in iRows]
        AppendShape [result <- ResultRow0(Id: rId, Item: i.Value, Work: stableWork)]
    PhaseBoundary [End:cte0]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B13_ApplyInvariant_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0), new Column("Text", typeof(string), 1), new Column("Items", typeof(int[]), 2) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Id", typeof(int), 0),
            new Column("Item", typeof(int), 1),
            new Column("Work", typeof(long), 2)
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
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Benchmarks.Scenarios.PerformanceLibrary __resultPerformanceLibrary0 = default!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    var __rSchema = provider.GetSchema("#perf");
                    var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                    __resultPerformanceLibrary0 = new Musoq.Benchmarks.Scenarios.PerformanceLibrary();
                    foreach (var rChunk in rRows)
                    {
                        if (rChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkView)
                        {
                            if (rChunkView.Source is Musoq.Benchmarks.Scenarios.PerformanceRow[] rChunkViewArray)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    long stableWork = (long)__resultPerformanceLibrary0.StableWork(r.Text);
                                    var iRows = (IEnumerable<int>)(r.Items) ?? Array.Empty<int>();
                                    var iIndexRows = iRows;
                                    if (iIndexRows is int[] iIndexArray)
                                        for (int iIndex = 0, iIndexCount = iIndexArray.Length; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            int i = iIndexArray[iIndex];
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                        }
                                    else if (iIndexRows is List<int> iIndexConcreteList)
                                        for (int iIndex = 0, iIndexCount = iIndexConcreteList.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            int i = iIndexConcreteList[iIndex];
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                        }
                                    else
                                    {
                                        int iIndex = 0;
                                        foreach (var i in iIndexRows)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                            ++iIndex;
                                        }
                                    }
                                }

                                continue;
                            }

                            if (rChunkView.Source is List<Musoq.Benchmarks.Scenarios.PerformanceRow> rChunkViewList)
                            {
                                int rChunkViewOffset = rChunkView.Offset;
                                for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                                {
                                    if ((rIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var r = rChunkViewList[rChunkViewOffset + rIndex];
                                    int rId = r.Id;
                                    long stableWork = (long)__resultPerformanceLibrary0.StableWork(r.Text);
                                    var iRows = (IEnumerable<int>)(r.Items) ?? Array.Empty<int>();
                                    var iIndexRows = iRows;
                                    if (iIndexRows is int[] iIndexArray)
                                        for (int iIndex = 0, iIndexCount = iIndexArray.Length; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            int i = iIndexArray[iIndex];
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                        }
                                    else if (iIndexRows is List<int> iIndexConcreteList)
                                        for (int iIndex = 0, iIndexCount = iIndexConcreteList.Count; iIndex < iIndexCount; ++iIndex)
                                        {
                                            if ((iIndex & 1023) == 0)
                                            {
                                                token.ThrowIfCancellationRequested();
                                            }

                                            int i = iIndexConcreteList[iIndex];
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                        }
                                    else
                                    {
                                        int iIndex = 0;
                                        foreach (var i in iIndexRows)
                                        {
                                            token.ThrowIfCancellationRequested();
                                            __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                            ++iIndex;
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
                            int rId = r.Id;
                            long stableWork = (long)__resultPerformanceLibrary0.StableWork(r.Text);
                            var iRows = (IEnumerable<int>)(r.Items) ?? Array.Empty<int>();
                            var iIndexRows = iRows;
                            if (iIndexRows is int[] iIndexArray)
                                for (int iIndex = 0, iIndexCount = iIndexArray.Length; iIndex < iIndexCount; ++iIndex)
                                {
                                    if ((iIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iIndexArray[iIndex];
                                    __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                }
                            else if (iIndexRows is List<int> iIndexConcreteList)
                                for (int iIndex = 0, iIndexCount = iIndexConcreteList.Count; iIndex < iIndexCount; ++iIndex)
                                {
                                    if ((iIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    int i = iIndexConcreteList[iIndex];
                                    __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                }
                            else
                            {
                                int iIndex = 0;
                                foreach (var i in iIndexRows)
                                {
                                    token.ThrowIfCancellationRequested();
                                    __musoqFinalShapeRows.Add(new ResultRow0(rId, i, stableWork));
                                    ++iIndex;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
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

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }
            public int Item_ { get; private set; }
            public long Work { get; private set; }

            public ResultRow0(int __value0, int __value1, long __value2)
            {
                Id = __value0;
                Item_ = __value1;
                Work = __value2;
            }

            public override int Count => 3;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Item_, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Work, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Item_),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Work),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Item" => true,
                "Item_" => true,
                "Work" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Item_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long>(this.Work = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }
    }
}
