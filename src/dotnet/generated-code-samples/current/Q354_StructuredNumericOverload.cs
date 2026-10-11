// === Parsed Query ===
/*
select o.Kind, o.Value, o.Count
from #inputs.overloaded(42.5) o
*/

// === Logical Plan ===
/*
MultiStatement
  Project [o.Kind as Kind, o.Value as Value, o.Count as Count]
    SchemaScan [#inputs.overloaded(42,5) as o]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [o.Kind as Kind, o.Value as Value, o.Count as Count]
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
      Kind: string <- field Kind
      Value: int <- field Value
      Count: int <- field Count_

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [o: OverloadRow] -> oRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [o in oRows]
      AppendShape [result <- ResultRow0(Kind: o.Kind, Value: o.Value, Count: o.Count)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Kind", typeof(string), 0),
            new Column("Value", typeof(int), 1),
            new Column("Count", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_o_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Kind", typeof(string), 0), new Column("Value", typeof(int), 1), new Column("Count", typeof(int), 2) });
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
            var __oSchema = provider.GetSchema("#inputs");
            var oRowsSourceContext = new SourceExecutionContext("o:1", sourceExecutionPlans["o:1"], token, __schemaColumns_compiled_o_0, sourceRuntimeSettingsBySourceContextId["o:1"], logger, OnDataSourceProgress);
            Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.OverloadRow> oRowsSource;
            try
            {
                Musoq.Examples.DataSources.StructuredInputs.OverloadSource oRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.OverloadSource(__musoqCheckStructural_cd6a9f347ba8495b(42.5m, token), oRowsSourceContext);
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

        private static decimal __musoqCheckStructural_cd6a9f347ba8495b(decimal value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "o.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "o:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, int __value1, int __value2)
            {
                Kind = __value0;
                Value = __value1;
                Count_ = __value2;
            }

            public override int Count => 3;
            public int Count_ { get; private set; }
            public string Kind { get; private set; }
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Kind = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<string, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Count_ = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, string>(this.Kind),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                2 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Count_),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Kind" => true,
                "Value" => true,
                "Count" => true,
                "Count_" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Kind, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.Value, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(2, this.Count_, ref visitor);
            }
        }
    }
}
