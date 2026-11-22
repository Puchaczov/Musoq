// === Parsed Query ===
/*
select r.Id as Id from perf.rows() r where r.Id % 100 < 1
*/

// === Logical Plan ===
/*
MultiStatement
  Project [r.Id as Id]
    Filter [((r.Id % 100) < 1)]
      SchemaScan [#perf.rows() as r]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [r.Id as Id]
    PhysicalFilter [((r.Id % 100) < 1)]
      PhysicalSchemaScan [#perf.rows() as r]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [r: PerformanceRow]
      Id: int <- property Id
    Generated [ResultRow0]
      Id: int <- field Id

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [r: PerformanceRow] -> rRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [r in rRows]
      Let [id: int = r.Id]
      If [((id % 100) < 1)]
        AppendShape [result <- ResultRow0(Id: id)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_B02_Filter_Selectivity1_BenchmarkTable
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
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_r_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Id", typeof(int), 0) });
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
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
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        public Table Run(QueryRunContext queryContext)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(queryContext.Provider!, queryContext.SourceRuntimeSettingsBySourceContextId, queryContext.SourceExecutionPlans, queryContext.Logger!, queryContext), queryContext.CancellationToken);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, QueryRunContext queryContext)
        {
            var token = queryContext.CancellationToken;
            var __musoqProgressContext = queryContext.QueryProgress == null ? null : queryContext;
            Action<string, QueryPhase> OnPhaseChanged = queryContext.NotifyPhaseChanged;
            DataSourceEventHandler OnDataSourceProgress = queryContext.NotifyDataSourceProgress;
            QueryProgressEventHandler OnQueryProgress = queryContext.QueryProgress;
            var __musoqExecutionState = ExecutionState.Capture(queryContext.TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            var __rSchema = provider.GetSchema("#perf");
            var rRowsSource = SourceInvocation<Musoq.Benchmarks.Scenarios.PerformanceRow>.Open(__rSchema, "rows", new SourceExecutionContext("r:1", sourceExecutionPlans["r:1"], token, __schemaColumns_compiled_r_0, sourceRuntimeSettingsBySourceContextId["r:1"], logger, OnDataSourceProgress) { WarningReporter = global::Musoq.Schema.Diagnostics.DataSourceWarningCollector.GetReporter(provider, sourceExecutionPlans["r:1"].Identity) }, global::Musoq.Schema.SourceArguments.Empty);
            var rRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Benchmarks.Scenarios.PerformanceRow>(rRowsSource.Chunks, __musoqProgressContext, "r:1") : rRowsSource.Chunks;
            var __musoqTableSourceRows = rRows;
            OnPhaseChanged("compiled", QueryPhase.Where);
            OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectOptionalRowsSerial<Musoq.Benchmarks.Scenarios.PerformanceRow, ResultRow0>(__musoqTableSourceRows, (r) =>
            {
                int id = r.Id;
                if (((id % ((int?)100).GetValueOrDefault()) < 1))
                {
                    return new ResultRow0(id);
                }

                return null;
            }, token), token, onCompleted: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onException: (Exception _) =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            }, onDisposed: () =>
            {
                try
                {
                    __musoqProgressContext?.CompleteQueryProgress();
                }
                finally
                {
                    OnPhaseChanged("compiled", QueryPhase.End);
                }
            });
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
    }
}
