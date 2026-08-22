// === Parsed Query ===
/*
select label, metric, payload from #runtime.events() where 2 = runtimekey and runtimekey = 2 and true = enabled
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Label as label, ko3iko.Metric as metric, ko3iko.Payload as payload]
    Filter [(((2 = ko3iko.RuntimeKey) AND (ko3iko.RuntimeKey = 2)) AND (TRUE = ko3iko.Enabled))]
      SchemaScan [#runtime.events() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Label as label, ko3iko.Metric as metric, ko3iko.Payload as payload]
    PhysicalFilter [(((2 = ko3iko.RuntimeKey) AND (ko3iko.RuntimeKey = 2)) AND (TRUE = ko3iko.Enabled))]
      PhysicalSchemaScan [#runtime.events() as ko3iko] [pushdown: (2 = ko3iko.RuntimeKey), (ko3iko.RuntimeKey = 2), (TRUE = ko3iko.Enabled)]
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
      label: string <- field label
      metric: double <- field metric
      payload: string <- field payload

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeDynamicRow] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [runtimeKey: int = ko3iko.RuntimeKey]
      If [(((2 = runtimeKey) AND (runtimeKey = 2)) AND (TRUE = ko3iko.Enabled))]
        AppendShape [result <- ResultShape0(label: ko3iko.Label, metric: ko3iko.Metric, payload: ko3iko.Payload)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q232_PublicDynamicRootFilterProjection
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
            new Column("label", typeof(string), 0),
            new Column("metric", typeof(double), 1),
            new Column("payload", typeof(string), 2)
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
        public event QueryProgressEventHandler QueryProgress;
        public Table Run(CancellationToken token)
        {
            return QueryRows.DeferredTable<ResultRow0>("result", __columns_compiled_result_1, (queryToken) => ComputeRows_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, queryToken), token);
        }

        private IEnumerable<ResultRow0> ComputeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            foreach (var __musoqShapeRow in ComputeShapeRows_compiled_0(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token))
            {
                yield return new ResultRow0(__musoqShapeRow.label, __musoqShapeRow.metric, __musoqShapeRow.payload);
            }
        }

        private IEnumerable<ResultShape0> ComputeShapeRows_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            try
            {
                var __musoqExecutionState = ExecutionState.Capture(Parameters);
                ScriptParameterBinder.ValidateNoUnknownParameters(__musoqExecutionState.Parameters, Array.Empty<string>());
                OnPhaseChanged("compiled", QueryPhase.Begin);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __ko3ikoSchema = provider.GetSchema("#runtime");
                var ko3ikoRowsSource = __ko3ikoSchema.GetRowSource<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>("events", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), Array.Empty<object>());
                var ko3ikoRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>(ko3ikoRowsSource.Chunks, __musoqProgressContext, "ko3iko:1") : ko3ikoRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Where);
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var ko3ikoChunk in ko3ikoRows)
                {
                    if (ko3ikoChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow> ko3ikoChunkView)
                    {
                        if (ko3ikoChunkView.Source is Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow[] ko3ikoChunkViewArray)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewArray[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int runtimeKey = (int)(object)((dynamic)ko3iko).RuntimeKey;
                                if ((((2 == runtimeKey) && (runtimeKey == 2)) && (true == (bool)(object)((dynamic)ko3iko).Enabled)))
                                {
                                    yield return new ResultShape0(ko3iko.Label, (double)(object)((dynamic)ko3iko).Metric, (string)(object)((dynamic)ko3iko).Payload);
                                }
                            }

                            continue;
                        }

                        if (ko3ikoChunkView.Source is List<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow> ko3ikoChunkViewList)
                        {
                            int ko3ikoChunkViewOffset = ko3ikoChunkView.Offset;
                            for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunkView.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                            {
                                if ((ko3ikoIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var ko3iko = ko3ikoChunkViewList[ko3ikoChunkViewOffset + ko3ikoIndex];
                                int runtimeKey = (int)(object)((dynamic)ko3iko).RuntimeKey;
                                if ((((2 == runtimeKey) && (runtimeKey == 2)) && (true == (bool)(object)((dynamic)ko3iko).Enabled)))
                                {
                                    yield return new ResultShape0(ko3iko.Label, (double)(object)((dynamic)ko3iko).Metric, (string)(object)((dynamic)ko3iko).Payload);
                                }
                            }

                            continue;
                        }
                    }

                    for (int ko3ikoIndex = 0, ko3ikoIndexCount = ko3ikoChunk.Count; ko3ikoIndex < ko3ikoIndexCount; ++ko3ikoIndex)
                    {
                        if ((ko3ikoIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var ko3iko = ko3ikoChunk[ko3ikoIndex];
                        int runtimeKey = (int)(object)((dynamic)ko3iko).RuntimeKey;
                        if ((((2 == runtimeKey) && (runtimeKey == 2)) && (true == (bool)(object)((dynamic)ko3iko).Enabled)))
                        {
                            yield return new ResultShape0(ko3iko.Label, (double)(object)((dynamic)ko3iko).Metric, (string)(object)((dynamic)ko3iko).Payload);
                        }
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

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(string __value0, double __value1, string __value2)
            {
                label = __value0;
                metric = __value1;
                payload = __value2;
            }

            public override int Count => 3;
            public string label { get; private set; }
            public double metric { get; private set; }
            public string payload { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        label = (string)value;
                        break;
                    case 1:
                        metric = (double)value;
                        break;
                    case 2:
                        payload = (string)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "label" => true,
                "metric" => true,
                "payload" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)label,
                1 => (object)metric,
                2 => (object)payload,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "label" => (object)label,
                "metric" => (object)metric,
                "payload" => (object)payload,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(string label, double metric, string payload)
            {
                this.label = label;
                this.metric = metric;
                this.payload = payload;
            }

            public string label { get; }
            public double metric { get; }
            public string payload { get; }
        }
    }
}
