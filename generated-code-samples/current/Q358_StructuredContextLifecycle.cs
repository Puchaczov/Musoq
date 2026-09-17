// === Parsed Query ===
/*
select c.QueryId, c.SourceContextId, c.Alias
from #inputs.contextprobe() c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [c.QueryId as c.QueryId, c.SourceContextId as c.SourceContextId, c.Alias as c.Alias]
    SchemaScan [#inputs.contextprobe() as c]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [c.QueryId as c.QueryId, c.SourceContextId as c.SourceContextId, c.Alias as c.Alias]
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
      c.QueryId: string <- field c_QueryId
      c.SourceContextId: string <- field c_SourceContextId
      c.Alias: string <- field c_Alias

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [c: ContextProbeRow] -> cRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [c in cRows]
      AppendShape [result <- ResultShape0(c.QueryId: c.QueryId, c.SourceContextId: c.SourceContextId, c.Alias: c.Alias)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("c.QueryId", typeof(string), 0),
            new Column("c.SourceContextId", typeof(string), 1),
            new Column("c.Alias", typeof(string), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("QueryId", typeof(string), 0), new Column("SourceContextId", typeof(string), 1), new Column("Alias", typeof(string), 2) });
        public ILogger Logger { get; set; }
        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = Array.Empty<ScriptParameterContract>();
        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = Array.Empty<ScriptParameterDefinition>();
        public IDictionary<string, System.Object> Parameters { get; } = new Dictionary<string, System.Object>(StringComparer.Ordinal);
        public ISchemaProvider Provider { get; set; }
        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }

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
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
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
                c_QueryId = __value0;
                c_SourceContextId = __value1;
                c_Alias = __value2;
            }

            public override int Count => 3;
            public string c_Alias { get; private set; }
            public string c_QueryId { get; private set; }
            public string c_SourceContextId { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c_QueryId = (string)value;
                        break;
                    case 1:
                        c_SourceContextId = (string)value;
                        break;
                    case 2:
                        c_Alias = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "c.QueryId" => true,
                "c_QueryId" => true,
                "QueryId" => true,
                "c.SourceContextId" => true,
                "c_SourceContextId" => true,
                "SourceContextId" => true,
                "c.Alias" => true,
                "c_Alias" => true,
                "Alias" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c_QueryId,
                1 => (object)c_SourceContextId,
                2 => (object)c_Alias,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "c.QueryId" => (object)c_QueryId,
                "c_QueryId" => (object)c_QueryId,
                "QueryId" => (object)c_QueryId,
                "c.SourceContextId" => (object)c_SourceContextId,
                "c_SourceContextId" => (object)c_SourceContextId,
                "SourceContextId" => (object)c_SourceContextId,
                "c.Alias" => (object)c_Alias,
                "c_Alias" => (object)c_Alias,
                "Alias" => (object)c_Alias,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string c_QueryId, string c_SourceContextId, string c_Alias)
            {
                this.c_QueryId = c_QueryId;
                this.c_SourceContextId = c_SourceContextId;
                this.c_Alias = c_Alias;
            }

            public string c_Alias { get; }
            public string c_QueryId { get; }
            public string c_SourceContextId { get; }
        }
    }
}
