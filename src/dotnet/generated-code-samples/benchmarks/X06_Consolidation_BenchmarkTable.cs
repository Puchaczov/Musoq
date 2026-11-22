// === Parsed Query ===
/*
with combined as (select r.Key as Id
from bench.rows() r
union (Id) select d.Key as Id
from bench.lookup() d) select distinct Id
from combined
order by Id
skip 1
take 2
*/

// === Logical Plan ===
/*
Cte
  Definition [combined]
    SetOp [Union]
      MultiStatement
        Project [r.Key as Id]
          SchemaScan [#bench.rows() as r]
      MultiStatement
        Project [d.Key as Id]
          SchemaScan [#bench.lookup() as d]
  Query
    MultiStatement
      Project [combined.Id as combined.Id]
        Aggregate [keys: Id] [aggs: ]
          CteRef [combined as combined]
      Take [2]
        Skip [1]
          Sort [combined.Id]
            Project [combined.Id as Id]
              CteRef [combinedScore as combinedScore]
*/

// === Physical Plan ===
/*
PhysicalCte
  Definition [combined]
    PhysicalSetOp [Union]
      PhysicalMultiStatement
        PhysicalProject [r.Key as Id]
          PhysicalSchemaScan [#bench.rows() as r]
      PhysicalMultiStatement
        PhysicalProject [d.Key as Id]
          PhysicalSchemaScan [#bench.lookup() as d]
  Query
    PhysicalMultiStatement
      PhysicalProject [combined.Id as combined.Id]
        PhysicalSingleKeyAggregate [key: Id (Int32)] [aggs: ]
          PhysicalCteRef [combined as combined]
      PhysicalTopOffset [skip 1, take 2] [combined.Id]
        PhysicalProject [combined.Id as Id]
          PhysicalCteRef [combinedScore as combinedScore]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: FixtureRow]
      Key: int <- property Key
    Generated [Cte0LeftRow0]
      Id: int <- field Id
    SourceEntity [d: FixtureRow]
      Key: int <- property Key
    Generated [Cte0RightRow0]
      Id: int <- field Id
    TableRow [combined]
      Id: int <- position 0
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    PhaseBoundary [Begin:cte0]
    PhaseBoundary [Select]
    PhaseBoundary [Begin:left]
    PhaseBoundary [From:left]
    PhaseBoundary [From:cte0]
    SourceScan [r: FixtureRow] -> cte0Left_rRows
    CreateTable [cte0Left: Cte0LeftRow0]
    PhaseBoundary [Select:left]
    PhaseBoundary [Select:cte0]
    PhaseBoundary [GroupBy]
    ChunkedForEach [r in cte0Left_rRows]
      AppendRow [cte0Left <- Cte0LeftRow0(Id: r.Key)]
    PhaseBoundary [End:left]
    PhaseBoundary [Begin:right]
    PhaseBoundary [From:right]
    SourceScan [d: FixtureRow] -> cte0Right_dRows
    CreateTable [cte0Right: Cte0RightRow0]
    PhaseBoundary [Select:right]
    ChunkedForEach [d in cte0Right_dRows]
      AppendRow [cte0Right <- Cte0RightRow0(Id: d.Key)]
    PhaseBoundary [End:right]
    SetOperation [cte0 = cte0Left Union cte0Right, HashSet]
    StoreTable [cte0 -> _tableResults[0]]
    PhaseBoundary [End:cte0]
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    CreateKeySet [distinctKeys: int; capacity: _tableResults[0].Count]
    ForEach [combined in _tableResults[0].Rows]
      Let [id: int = combined.Id]
      If [Add(id)]
        AppendShape [result <- ResultShape0(Id: id)]
    TopOffsetShapeRows [result -> resultTopOffset by Id ASC, skip 1, take 2, BoundedHeap]
    ReturnDeferredTable [resultTopOffset: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_X06_Consolidation_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Key", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_cte0Left_1 = new Column[]
        {
            new Column("Id", typeof(int), 0)
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
            return QueryRows.DeferredTable<ResultRow0>("resultTopOffset", __columns_compiled_cte0Left_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("resultTopOffset", __columns_compiled_cte0Left_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
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
                var _tableResults = new Musoq.Evaluator.Tables.Table[1];
                var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                OnPhaseChanged("compiled:cte0", QueryPhase.Begin);
                Musoq.Evaluator.Tables.Table cte0Left = null!;
                Musoq.Evaluator.Tables.Table cte0Right = null!;
                Musoq.Evaluator.Tables.Table cte0 = null!;
                try
                {
                    OnPhaseChanged("compiled", QueryPhase.Select);
                    OnPhaseChanged("compiled:left", QueryPhase.Begin);
                    OnPhaseChanged("compiled:left", QueryPhase.From);
                    OnPhaseChanged("compiled:cte0", QueryPhase.From);
                    var __cte0Left_rSchema = provider.GetSchema("#bench");
                    var cte0Left_rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0Left_rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var cte0Left_rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0Left_rRowsSource.Chunks, __musoqProgressContext, "r:1") : cte0Left_rRowsSource.Chunks;
                    cte0Left = new Table("cte0Left", __columns_compiled_cte0Left_1);
                    OnPhaseChanged("compiled:left", QueryPhase.Select);
                    OnPhaseChanged("compiled:cte0", QueryPhase.Select);
                    OnPhaseChanged("compiled", QueryPhase.GroupBy);
                    foreach (var rChunk in cte0Left_rRows)
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
                                    cte0Left.AddDirect(new Cte0LeftRow0(r.Key));
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
                                    cte0Left.AddDirect(new Cte0LeftRow0(r.Key));
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
                            cte0Left.AddDirect(new Cte0LeftRow0(r.Key));
                        }
                    }

                    OnPhaseChanged("compiled:left", QueryPhase.End);
                    OnPhaseChanged("compiled:right", QueryPhase.Begin);
                    OnPhaseChanged("compiled:right", QueryPhase.From);
                    var __cte0Right_dSchema = provider.GetSchema("#bench");
                    var cte0Right_dRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.FixtureRow>.Open(__cte0Right_dSchema, "lookup", new SourceExecutionContext("d:2", sourceExecutionPlans["d:2"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["d:2"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["d:2"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
                    var cte0Right_dRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.FixtureRow>(cte0Right_dRowsSource.Chunks, __musoqProgressContext, "d:2") : cte0Right_dRowsSource.Chunks;
                    cte0Right = new Table("cte0Right", __columns_compiled_cte0Left_1);
                    OnPhaseChanged("compiled:right", QueryPhase.Select);
                    foreach (var dChunk in cte0Right_dRows)
                    {
                        if (dChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkView)
                        {
                            if (dChunkView.Source is Musoq.Benchmarks.Scenarios.FixtureRow[] dChunkViewArray)
                            {
                                int dChunkViewOffset = dChunkView.Offset;
                                for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                                {
                                    if ((dIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var d = dChunkViewArray[dChunkViewOffset + dIndex];
                                    cte0Right.AddDirect(new Cte0RightRow0(d.Key));
                                }

                                continue;
                            }

                            if (dChunkView.Source is List<Musoq.Benchmarks.Scenarios.FixtureRow> dChunkViewList)
                            {
                                int dChunkViewOffset = dChunkView.Offset;
                                for (int dIndex = 0, dIndexCount = dChunkView.Count; dIndex < dIndexCount; ++dIndex)
                                {
                                    if ((dIndex & 1023) == 0)
                                    {
                                        token.ThrowIfCancellationRequested();
                                    }

                                    var d = dChunkViewList[dChunkViewOffset + dIndex];
                                    cte0Right.AddDirect(new Cte0RightRow0(d.Key));
                                }

                                continue;
                            }
                        }

                        for (int dIndex = 0, dIndexCount = dChunk.Count; dIndex < dIndexCount; ++dIndex)
                        {
                            if ((dIndex & 1023) == 0)
                            {
                                token.ThrowIfCancellationRequested();
                            }

                            var d = dChunk[dIndex];
                            cte0Right.AddDirect(new Cte0RightRow0(d.Key));
                        }
                    }

                    OnPhaseChanged("compiled:right", QueryPhase.End);
                    cte0 = new Table("cte0", __columns_compiled_cte0Left_1);
                    var cte0Keys = new HashSet<int>(cte0Left.Rows.Count + cte0Right.Rows.Count);
                    foreach (var cte0LeftRow in EvaluationHelper.CastGeneratedRows<Cte0LeftRow0>(cte0Left.Rows))
                    {
                        if (cte0Keys.Add((int)cte0LeftRow.Id))
                        {
                            cte0.AddDirect(cte0LeftRow);
                        }
                    }

                    foreach (var cte0RightRow in EvaluationHelper.CastGeneratedRows<Cte0RightRow0>(cte0Right.Rows))
                    {
                        if (cte0Keys.Add((int)cte0RightRow.Id))
                        {
                            cte0.AddDirect(new Cte0LeftRow0((int)cte0RightRow.Id));
                        }
                    }

                    _tableResults[0] = cte0;
                }
                finally
                {
                    OnPhaseChanged("compiled:cte0", QueryPhase.End);
                }

                var result = new List<ResultShape0>();
                var distinctKeys = new HashSet<int>(_tableResults[0].Count);
                foreach (var combined in _tableResults[0].Rows)
                {
                    token.ThrowIfCancellationRequested();
                    int id = combined.GetValue<int>(0);
                    if ((bool)distinctKeys.Add(id))
                    {
                        result.Add(new ResultShape0(id));
                    }
                }

                var resultTopOffsetRows = EvaluationHelper.SelectTopOffsetRecords(result, 1, 2, Comparer<ResultShape0>.Create((left, right) =>
                {
                    var comparison = left.Id.CompareTo(right.Id);
                    if (comparison != 0)
                        return comparison;
                    return 0;
                }));
                foreach (var resultTopOffsetRowsRow in resultTopOffsetRows)
                {
                    __musoqFinalShapeRows.Add(resultTopOffsetRowsRow);
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
                yield return new ResultRow0(__musoqShapeRow.Id);
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

        private sealed class Cte0LeftRow0 : Row
        {
            public int Id { get; private set; }

            public Cte0LeftRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class Cte0RightRow0 : Row
        {
            public int Id { get; private set; }

            public Cte0RightRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultRow0 : Row
        {
            public int Id { get; private set; }

            public ResultRow0(int __value0)
            {
                Id = __value0;
            }

            public override int Count => 1;

            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                _ => false
            };
            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Id)
            {
                this.Id = Id;
            }

            public int Id { get; }
        }
    }
}
