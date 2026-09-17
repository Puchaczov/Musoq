// === Parsed Query ===
/*
select o.Kind, o.Value, o.Count
from #inputs.overloaded(42.5) o
*/

// === Logical Plan ===
/*
MultiStatement
  Project [o.Kind as o.Kind, o.Value as o.Value, o.Count as o.Count]
    SchemaScan [#inputs.overloaded(42,5) as o]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [o.Kind as o.Kind, o.Value as o.Value, o.Count as o.Count]
    PhysicalSchemaScan [#inputs.overloaded(42,5) as o]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [o: OverloadRow]
      Kind: string <- property Kind
      Value: int <- property Value
      Count: int <- property Count
    Generated [ResultRow0]
      o.Kind: string <- field o_Kind
      o.Value: int <- field o_Value
      o.Count: int <- field o_Count

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [o: OverloadRow] -> oRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [o in oRows]
      AppendShape [result <- ResultShape0(o.Kind: o.Kind, o.Value: o.Value, o.Count: o.Count)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q354_StructuredNumericOverload
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
            new Column("o.Kind", typeof(string), 0),
            new Column("o.Value", typeof(int), 1),
            new Column("o.Count", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_o_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Kind", typeof(string), 0), new Column("Value", typeof(int), 1), new Column("Count", typeof(int), 2) });
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
            var __oSchema = provider.GetSchema("#inputs");
            var oRowsSourceContext = new SourceExecutionContext("o:1", sourceExecutionPlans["o:1"], token, __schemaColumns_compiled_o_0, sourceRuntimeSettingsBySourceContextId["o:1"], logger, OnDataSourceProgress);
            Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.OverloadRow> oRowsSource;
            try
            {
                Musoq.Examples.DataSources.StructuredInputs.OverloadSource oRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.OverloadSource(42.5m, oRowsSourceContext);
                oRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.OverloadSource, Musoq.Examples.DataSources.StructuredInputs.OverloadRow>(__oSchema, "overloaded", oRowsSourceInstance, oRowsSourceContext, "#inputs", "o", "o:1");
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
                throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "overloaded", "o", "o:1", exception);
            }

            var oRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.OverloadRow>(oRowsSource.Chunks, __musoqProgressContext, "o:1") : oRowsSource.Chunks;
            var __musoqTableSourceRows = oRows;
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Examples.DataSources.StructuredInputs.OverloadRow, ResultRow0>(__musoqTableSourceRows, (o) => true, (o) => new ResultRow0(o.Kind, o.Value, o.Count), token), token, onCompleted: () =>
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
            public ResultRow0(string __value0, int __value1, int __value2)
            {
                o_Kind = __value0;
                o_Value = __value1;
                o_Count = __value2;
            }

            public override int Count => 3;
            public int o_Count { get; private set; }
            public string o_Kind { get; private set; }
            public int o_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        o_Kind = (string)value;
                        break;
                    case 1:
                        o_Value = (int)value;
                        break;
                    case 2:
                        o_Count = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "o.Kind" => true,
                "o_Kind" => true,
                "Kind" => true,
                "o.Value" => true,
                "o_Value" => true,
                "Value" => true,
                "o.Count" => true,
                "o_Count" => true,
                "Count" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)o_Kind,
                1 => (object)o_Value,
                2 => (object)o_Count,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "o.Kind" => (object)o_Kind,
                "o_Kind" => (object)o_Kind,
                "Kind" => (object)o_Kind,
                "o.Value" => (object)o_Value,
                "o_Value" => (object)o_Value,
                "Value" => (object)o_Value,
                "o.Count" => (object)o_Count,
                "o_Count" => (object)o_Count,
                "Count" => (object)o_Count,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string o_Kind, int o_Value, int o_Count)
            {
                this.o_Kind = o_Kind;
                this.o_Value = o_Value;
                this.o_Count = o_Count;
            }

            public int o_Count { get; }
            public string o_Kind { get; }
            public int o_Value { get; }
        }
    }
}
