// === Parsed Query ===
/*
select branch.measurement, branch.raw from #runtime.events() where branch is not null
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Branch.measurement as branch.measurement, ko3iko.Branch.raw as branch.raw]
    Filter [ko3iko.Branch IS NOT NULL]
      SchemaScan [#runtime.events() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Branch.measurement as branch.measurement, ko3iko.Branch.raw as branch.raw]
    PhysicalFilter [ko3iko.Branch IS NOT NULL]
      PhysicalSchemaScan [#runtime.events() as ko3iko] [pushdown: ko3iko.Branch IS NOT NULL]
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
      branch.measurement: double <- field branch_measurement
      branch.raw: ulong <- field branch_raw

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeDynamicRow] -> ko3ikoRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [branch: RuntimeDynamicBranch = ko3iko.Branch]
      If [branch IS NOT NULL]
        AppendShape [result <- ResultShape0(branch.measurement: branch.Measurement, branch.raw: branch.Raw)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q233_PublicDynamicNestedNullable
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
            new Column("branch.measurement", typeof(double), 0),
            new Column("branch.raw", typeof(ulong), 1)
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
                yield return new ResultRow0(__musoqShapeRow.branch_measurement, __musoqShapeRow.branch_raw);
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
                                Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = (Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch)(object)((dynamic)ko3iko).Branch;
                                if ((branch != null))
                                {
                                    yield return new ResultShape0((double)(object)((dynamic)branch).Measurement, (ulong)(object)((dynamic)branch).Raw);
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
                                Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = (Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch)(object)((dynamic)ko3iko).Branch;
                                if ((branch != null))
                                {
                                    yield return new ResultShape0((double)(object)((dynamic)branch).Measurement, (ulong)(object)((dynamic)branch).Raw);
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
                        Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = (Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch)(object)((dynamic)ko3iko).Branch;
                        if ((branch != null))
                        {
                            yield return new ResultShape0((double)(object)((dynamic)branch).Measurement, (ulong)(object)((dynamic)branch).Raw);
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
            public ResultRow0(double __value0, ulong __value1)
            {
                branch_measurement = __value0;
                branch_raw = __value1;
            }

            public override int Count => 2;
            public double branch_measurement { get; private set; }
            public ulong branch_raw { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        branch_measurement = (double)value;
                        break;
                    case 1:
                        branch_raw = (ulong)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "branch.measurement" => true,
                "branch_measurement" => true,
                "measurement" => true,
                "branch.raw" => true,
                "branch_raw" => true,
                "raw" => true,
                _ => false
            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)branch_measurement,
                1 => (object)branch_raw,
                _ => throw new IndexOutOfRangeException()};
            public override object this[string name] => name switch
            {
                "branch.measurement" => (object)branch_measurement,
                "branch_measurement" => (object)branch_measurement,
                "measurement" => (object)branch_measurement,
                "branch.raw" => (object)branch_raw,
                "branch_raw" => (object)branch_raw,
                "raw" => (object)branch_raw,
                _ => throw new KeyNotFoundException(name)};
        }

        private sealed class ResultShape0
        {
            public ResultShape0(double branch_measurement, ulong branch_raw)
            {
                this.branch_measurement = branch_measurement;
                this.branch_raw = branch_raw;
            }

            public double branch_measurement { get; }
            public ulong branch_raw { get; }
        }
    }
}
