// === Parsed Query ===
/*
select 1 as Marker from #runtime.events()
*/

// === Logical Plan ===
/*
MultiStatement
  Project [1 as Marker]
    SchemaScan [#runtime.events() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [1 as Marker]
    PhysicalSchemaScan [#runtime.events() as ko3iko]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeDynamicRow]
      Label: string <- property Label
      RuntimeKey: int <- runtime dynamic member "RuntimeKey"
      Enabled: bool <- runtime dynamic member "Enabled"
      Metric: double <- runtime dynamic member "Metric"
      Payload: string <- runtime dynamic member "Payload"
      Branch: RuntimeDynamicBranch <- runtime dynamic member "Branch"
      Branch.Measurement: double <- runtime dynamic member "Branch.Measurement"
      Branch.Raw: ulong <- runtime dynamic member "Branch.Raw"
      StaticBranch: RuntimeDynamicBranch <- property StaticBranch
      StaticBranch.Measurement: double <- runtime dynamic member "StaticBranch.Measurement"
      StaticBranch.Raw: ulong <- runtime dynamic member "StaticBranch.Raw"
    Generated [ResultRow0]
      Marker: int <- field Marker

  Body
    SourceScan [ko3iko: RuntimeDynamicRow] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    ChunkedForEach [ko3iko in ko3ikoRows]
      AppendShape [result <- ResultShape0(Marker: 1)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q231_PublicDynamicRootConstant
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Marker", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Label", typeof(string), 0), new Column("RuntimeKey", typeof(int), 1), new Column("Enabled", typeof(bool), 2), new Column("Metric", typeof(double), 3), new Column("Payload", typeof(string), 4), new Column("Branch", typeof(Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch), 5), new Column("Branch.Measurement", typeof(double), 6), new Column("Branch.Raw", typeof(ulong), 7), new Column("StaticBranch", typeof(Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch), 8), new Column("StaticBranch.Measurement", typeof(double), 9), new Column("StaticBranch.Raw", typeof(ulong), 10) });
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
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            OnPhaseChanged("compiled", QueryPhase.Begin);
            OnPhaseChanged("compiled", QueryPhase.From);
            OnPhaseChanged("compiled", QueryPhase.Select);
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
            var __ko3ikoSchema = provider.GetSchema("#runtime");
            var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>("events", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
            var ko3ikoRows = ko3ikoRowsSource.Chunks;
            var __musoqTableSourceRows = ko3ikoRows;
            return new QueryTableEnumerable<ResultRow0>((_) => TableProjectionRows.ProjectRowsSerial<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow, ResultRow0>(__musoqTableSourceRows, (ko3iko) => true, (ko3iko) => new ResultRow0(1), token), token, onCompleted: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }, onDisposed: () =>
            {
                OnPhaseChanged("compiled", QueryPhase.End);
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
            public ResultRow0(int __value0)
            {
                Marker = __value0;
            }

            public override int Count => 1;
            public int Marker { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        Marker = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "Marker" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)Marker,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "Marker" => (object)Marker,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int Marker)
            {
                this.Marker = Marker;
            }

            public int Marker { get; }
        }
    }
}
