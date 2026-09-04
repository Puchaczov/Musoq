// === Parsed Query ===
/*
select a.VolatileValue, a.Value from #licm.outers() a where a.VolatileValue > 0
*/

// === Logical Plan ===
/*
MultiStatement
  Project [a.VolatileValue as a.VolatileValue, a.Value as a.Value]
    Filter [(a.VolatileValue > 0)]
      SchemaScan [#licm.outers() as a]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [a.VolatileValue as a.VolatileValue, a.Value as a.Value]
    PhysicalFilter [(a.VolatileValue > 0)]
      PhysicalSchemaScan [#licm.outers() as a] [pushdown: (a.VolatileValue > 0)]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [a: LoopInvariantSampleOuter]
      Value: int <- property Value
      VolatileValue: int <- property VolatileValue
    Generated [ResultRow0]
      a.VolatileValue: int <- field a_VolatileValue
      a.Value: int <- field a_Value

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [a: LoopInvariantSampleOuter] -> aRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [a in aRows]
      If [(a.VolatileValue > 0)]
        AppendShape [result <- ResultShape0(a.VolatileValue: a.VolatileValue, a.Value: a.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q253_VolatileFilterProjectionReuse
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
            new Column("a.VolatileValue", typeof(int), 0),
            new Column("a.Value", typeof(int), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_a_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0), new Column("VolatileValue", typeof(int), 1) });
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
            var __aSchema = provider.GetSchema("#licm");
            var aRowsSource = __aSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>("outers", new SourceExecutionContext("a:1", sourceExecutionPlans["a:1"], token, __schemaColumns_compiled_a_0, sourceRuntimeSettingsBySourceContextId["a:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var aRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter>(aRowsSource.Chunks, __musoqProgressContext, "a:1") : aRowsSource.Chunks;
            var __musoqTableSourceRows = aRows;
            this.OnPhaseChanged("compiled", QueryPhase.Where);
            this.OnPhaseChanged("compiled", QueryPhase.Select);
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.Generated.LoopInvariantSampleOuter, ResultRow0>(__musoqTableSourceRows, (a) => (a.VolatileValue > 0), (a) => new ResultRow0(a.VolatileValue, a.Value), token), token, onCompleted: () =>
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
            public ResultRow0(int __value0, int __value1)
            {
                a_VolatileValue = __value0;
                a_Value = __value1;
            }

            public override int Count => 2;
            public int a_Value { get; private set; }
            public int a_VolatileValue { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        a_VolatileValue = (int)value;
                        break;
                    case 1:
                        a_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "a.VolatileValue" => true,
                "a_VolatileValue" => true,
                "VolatileValue" => true,
                "a.Value" => true,
                "a_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)a_VolatileValue,
                1 => (object)a_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "a.VolatileValue" => (object)a_VolatileValue,
                "a_VolatileValue" => (object)a_VolatileValue,
                "VolatileValue" => (object)a_VolatileValue,
                "a.Value" => (object)a_Value,
                "a_Value" => (object)a_Value,
                "Value" => (object)a_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int a_VolatileValue, int a_Value)
            {
                this.a_VolatileValue = a_VolatileValue;
                this.a_Value = a_Value;
            }

            public int a_Value { get; }
            public int a_VolatileValue { get; }
        }
    }
}
