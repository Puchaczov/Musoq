// === Parsed Query ===
/*
select branch.Measurement, branch.Raw from #runtime.events() where branch is not null
*/

// === Logical Plan ===
/*
MultiStatement
  Project [ko3iko.Branch.Measurement as branch.Measurement, ko3iko.Branch.Raw as branch.Raw]
    Filter [ko3iko.Branch IS NOT NULL]
      SchemaScan [#runtime.events() as ko3iko]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [ko3iko.Branch.Measurement as branch.Measurement, ko3iko.Branch.Raw as branch.Raw]
    PhysicalFilter [ko3iko.Branch IS NOT NULL]
      PhysicalSchemaScan [#runtime.events() as ko3iko] [pushdown: ko3iko.Branch IS NOT NULL]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [ko3iko: RuntimeDynamicRow]
      Branch: RuntimeDynamicBranch <- property Branch
      Branch.Measurement: double <- nested property Branch.Measurement
      Branch.Raw: ulong <- nested property Branch.Raw
    Generated [ResultRow0]
      branch.Measurement: double <- field branch_Measurement
      branch.Raw: ulong <- field branch_Raw

  Body
    PhaseBoundary [Begin]
    PhaseBoundary [From]
    SourceScan [ko3iko: RuntimeDynamicRow] -> ko3ikoRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Where]
    PhaseBoundary [Select]
    ChunkedForEach [ko3iko in ko3ikoRows]
      Let [branch: RuntimeDynamicBranch = ko3iko.Branch]
      If [branch IS NOT NULL]
        AppendShape [result <- ResultRow0(branch.Measurement: branch?.Measurement, branch.Raw: branch?.Raw)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("branch.Measurement", typeof(double), 0),
            new Column("branch.Raw", typeof(ulong), 1)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_ko3iko_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Branch", typeof(Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch), 0), new Column("Branch.Measurement", typeof(double), 1), new Column("Branch.Raw", typeof(ulong), 2) });
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
                var __ko3ikoSchema = provider.GetSchema("#runtime");
                var ko3ikoRowsSource = SourceInvocation<Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicRow>.Open(__ko3ikoSchema, "events", new SourceExecutionContext("ko3iko:1", sourceExecutionPlans["ko3iko:1"], token, __schemaColumns_compiled_ko3iko_0, sourceRuntimeSettingsBySourceContextId["ko3iko:1"], logger, OnDataSourceProgress), global::Musoq.Schema.SourceArguments.Empty);
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
                                Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = ko3iko.Branch;
                                if ((branch != null))
                                {
                                    yield return new ResultRow0((branch?.Measurement ?? default(double)), (branch?.Raw ?? default(ulong)));
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
                                Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = ko3iko.Branch;
                                if ((branch != null))
                                {
                                    yield return new ResultRow0((branch?.Measurement ?? default(double)), (branch?.Raw ?? default(ulong)));
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
                        Musoq.Evaluator.Tests.Schema.RuntimeDynamic.RuntimeDynamicBranch branch = ko3iko.Branch;
                        if ((branch != null))
                        {
                            yield return new ResultRow0((branch?.Measurement ?? default(double)), (branch?.Raw ?? default(ulong)));
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
                branch_Measurement = __value0;
                branch_Raw = __value1;
            }

            public override int Count => 2;
            public double branch_Measurement { get; private set; }
            public ulong branch_Raw { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.branch_Measurement = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<double, T>(value)),
                    1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.branch_Raw = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<ulong, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))});
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, double>(this.branch_Measurement),
                1 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, ulong>(this.branch_Raw),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))};
            public override bool HasColumn(string name) => name switch
            {
                "branch.Measurement" => true,
                "branch_Measurement" => true,
                "Measurement" => true,
                "branch.Raw" => true,
                "branch_Raw" => true,
                "Raw" => true,
                _ => false
            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.branch_Measurement, ref visitor);
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(1, this.branch_Raw, ref visitor);
            }
        }
    }
}
