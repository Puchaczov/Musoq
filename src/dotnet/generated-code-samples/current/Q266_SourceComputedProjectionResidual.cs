// === Parsed Query ===
/*
select r.Id, r.Id::long * 2 as Value from #computedtest.rows() r
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as Id, (r.Id::Int64 * 2) as Value]
    SchemaScan [#computedtest.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as Id, (r.Id::Int64 * 2) as Value]
    PhysicalSchemaScan [#computedtest.rows() as r] [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    Generated [QueryRow_3D4EDDB8B07D_S]
      Id: int <- field Field0
    Generated [ResultRow0]
      Id: int <- field Id
      Value: long? <- field Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: QueryRow_3D4EDDB8B07D_S] -> rRows [query-row:ReadonlyStruct;lifetime=ScanLocal;shape=3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B]
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      Let [id: int = r.Id]
      AppendShape [result <- ResultRow0(Id: id, Value: (id::Int64 * 2))]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q266_SourceComputedProjectionResidual
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
            new Column("Id", typeof(int), 0),
            new Column("Value", typeof(long?), 1)
        };
        private static readonly QueryRowShape __queryRowShape_3D4EDDB8B07D = new QueryRowShape(new QueryRowField[] { new QueryRowField(0, 0, "Id", typeof(int), false) });
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
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
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __rSchema = provider.GetSchema("#computedtest");
                var __rSchemaQueryRows = __rSchema as Musoq.Schema.IQueryScopedRowSourceSchema ?? throw new InvalidOperationException("Source '#computedtest.rows' advertised QueryScopedRows but its runtime schema does not implement IQueryScopedRowSourceSchema (shape 3D4EDDB8B07DA3CD9EFD8F267D363EC943ADE8917A3F2B49DF8EE4CBED47227B).");
                var rRowsSource = QuerySourceInvocation<QueryRow_3D4EDDB8B07D_S, QueryRowMaterializer_3D4EDDB8B07D_S>.Open(__rSchemaQueryRows, "rows", new QueryScopedRowSourceRequest(new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, __queryRowShape_3D4EDDB8B07D), global::Musoq.Schema.SourceArguments.Empty);
                var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<QueryRow_3D4EDDB8B07D_S>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var rChunk in rRows)
                {
                    if (rChunk is global::Musoq.Schema.DataSources.RowChunk<QueryRow_3D4EDDB8B07D_S> rChunkView)
                    {
                        if (rChunkView.Source is QueryRow_3D4EDDB8B07D_S[] rChunkViewArray)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewArray[rChunkViewOffset + rIndex];
                                int id = r.Field0;
                                yield return new ResultRow0(id, (long?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64(id) * 2));
                            }

                            continue;
                        }

                        if (rChunkView.Source is List<QueryRow_3D4EDDB8B07D_S> rChunkViewList)
                        {
                            int rChunkViewOffset = rChunkView.Offset;
                            for (int rIndex = 0, rIndexCount = rChunkView.Count; rIndex < rIndexCount; ++rIndex)
                            {
                                if ((rIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var r = rChunkViewList[rChunkViewOffset + rIndex];
                                int id = r.Field0;
                                yield return new ResultRow0(id, (long?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64(id) * 2));
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
                        int id = r.Field0;
                        yield return new ResultRow0(id, (long?)(global::Musoq.Evaluator.Helpers.StrictCastRuntime.ToInt64(id) * 2));
                    }
                }
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

        private readonly struct QueryRowMaterializer_3D4EDDB8B07D_S : IQueryRowMaterializer<QueryRow_3D4EDDB8B07D_S>
        {
            public static QueryRow_3D4EDDB8B07D_S Materialize<TReader>(scoped ref TReader reader)
                where TReader : IQuerySourceFieldReader, allows ref struct => new QueryRow_3D4EDDB8B07D_S(reader.Read<int>(0));
        }

        private readonly struct QueryRow_3D4EDDB8B07D_S
        {
            public QueryRow_3D4EDDB8B07D_S(int Field0)
            {
                this.Field0 = Field0;
            }

            public int Field0 { get; }
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, long? __value1)
            {
                Id = __value0;
                Value = __value1;
            }

            public override int Count => 2;
            public int Id { get; private set; }
            public long? Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<long?, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Id),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, long?>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Id" => true,
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Id, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
            }
        }
    }
}
