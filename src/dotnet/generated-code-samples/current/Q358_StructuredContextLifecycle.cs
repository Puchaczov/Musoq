// === Parsed Query ===
/*
select c.QueryId, c.SourceContextId, c.Alias
from #inputs.contextprobe() c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [c.QueryId as QueryId, c.SourceContextId as SourceContextId, c.Alias as Alias]
    SchemaScan [#inputs.contextprobe() as c]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [c.QueryId as QueryId, c.SourceContextId as SourceContextId, c.Alias as Alias]
    PhysicalSchemaScan [#inputs.contextprobe() as c]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [c: ContextProbeRow]
      QueryId: string <- property QueryId
      SourceContextId: string <- property SourceContextId
      Alias: string <- property Alias
    Generated [ResultRow0]
      QueryId: string <- field QueryId
      SourceContextId: string <- field SourceContextId
      Alias: string <- field Alias

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [c: ContextProbeRow] -> cRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [c in cRows]
      AppendShape [result <- ResultRow0(QueryId: c.QueryId, SourceContextId: c.SourceContextId, Alias: c.Alias)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q358_StructuredContextLifecycle
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
            new Column("QueryId", typeof(string), 0),
            new Column("SourceContextId", typeof(string), 1),
            new Column("Alias", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("QueryId", typeof(string), 0), new Column("SourceContextId", typeof(string), 1), new Column("Alias", typeof(string), 2) });
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
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(TypedParameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState, Array.Empty<string>());
            this.OnPhaseChanged("compiled", QueryPhase.Begin);
            this.OnPhaseChanged("compiled", QueryPhase.From);
            var __cSchema = provider.GetSchema("#inputs");
            var cRowsSourceContext = new SourceExecutionContext("c:1", sourceExecutionPlans["c:1"], token, __schemaColumns_compiled_c_0, sourceRuntimeSettingsBySourceContextId["c:1"], logger, OnDataSourceProgress);
            Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.ContextProbeRow> cRowsSource;
            try
            {
                Musoq.Examples.DataSources.StructuredInputs.ContextProbeSource cRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.ContextProbeSource(cRowsSourceContext);
                cRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.ContextProbeSource, Musoq.Examples.DataSources.StructuredInputs.ContextProbeRow>(__cSchema, "contextprobe", cRowsSourceInstance, cRowsSourceContext, "#inputs", "c", "c:1");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (global::Musoq.Schema.Exceptions.DataSourceLifecycleException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "contextprobe", "c", "c:1", exception);
            }

            var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.ContextProbeRow>(cRowsSource.Chunks, __musoqProgressContext, "c:1") : cRowsSource.Chunks;
            var __musoqTableSourceRows = cRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Examples.DataSources.StructuredInputs.ContextProbeRow, ResultRow0>(__musoqTableSourceRows, (c) => true, (c) => new ResultRow0(c.QueryId, c.SourceContextId, c.Alias), token), token, onCompleted: () =>
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
        private void OnDataSourceProgress(object sender, DataSourceEventArgs e)
        {
            DataSourceProgress?.Invoke(this, e);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void OnPhaseChanged(string queryId, QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs(queryId, phase));
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, string __value1, string __value2)
            {
                QueryId = __value0;
                SourceContextId = __value1;
                Alias = __value2;
            }

            public string Alias { get; private set; }
            public override int Count => 3;
            public string QueryId { get; private set; }
            public string SourceContextId { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.QueryId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.SourceContextId = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Alias = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.QueryId),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.SourceContextId),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Alias),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "QueryId" => true,
                "SourceContextId" => true,
                "Alias" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.QueryId, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.SourceContextId, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Alias, ref visitor);
            }
        }
    }
}
