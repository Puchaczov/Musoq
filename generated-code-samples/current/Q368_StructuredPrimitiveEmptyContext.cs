// === Parsed Query ===
/*
desc arguments #inputs.numbers(values: array {})
*/

// === Logical Plan ===
/*
Desc [#inputs.numbers()] [Arguments] []
*/

// === Physical Plan ===
/*
PhysicalDesc [#inputs.numbers()] [Arguments] []
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes

  Body
    PhaseBoundary [Begin]
    ReturnDesc [#inputs.numbers() Arguments]
    PhaseBoundary [Select]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q368_StructuredPrimitiveEmptyContext
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
    using Musoq.Schema.DataSources;
    using System.Linq;

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, IMetadataOnlyRunnable
    {
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
            return ComputeTable_compiled_0(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token);
        }

        private Table ComputeTable_compiled_0(ISchemaProvider provider, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId, IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans, ILogger logger, CancellationToken token)
        {
            QueryProgressEventHandler OnQueryProgress = QueryProgress;
            var __musoqProgressContext = OnQueryProgress == null ? null : new QueryRunContext(token, queryProgress: OnQueryProgress, sender: this, queryId: "compiled");
            Action<string, QueryPhase> OnPhaseChanged = this.OnPhaseChanged;
            var __musoqExecutionState = ExecutionState.Capture(Parameters);
            OnPhaseChanged("compiled", QueryPhase.Begin);
            var descSchema = provider.GetSchema("#inputs");
            var emptyInferred = Array.Empty<ISchemaColumn>();
            var descRuntimeCtx = new SourceExecutionContext("df8apb:1", SourceExecutionPlan.Empty(new SourceIdentity("#inputs", "numbers", "df8apb:1", "")), token, emptyInferred, sourceRuntimeSettingsBySourceContextId.TryGetValue("df8apb:1", out var descSourceRuntimeSettings) ? descSourceRuntimeSettings : new Dictionary<string, string>(), logger, OnDataSourceProgress);
            try
            {
                __musoqProgressContext?.CompleteQueryProgress();
            }
            finally
            {
                OnPhaseChanged("compiled", QueryPhase.End);
            }

            return EvaluationHelper.GetStructuralArgumentDescriptions(descSchema, "numbers", new global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription[] { new global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription(0, "values", "Collection", "int[]?", true, true, false, null, 32, 100000, 67108864L), new global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription(0, "values[]", "Scalar", "int", null, false, false, null, null, null, null) }, descRuntimeCtx);
            OnPhaseChanged("compiled", QueryPhase.Select);
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
    }
}
