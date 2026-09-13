// === Parsed Query ===
/*
let items = array {
    (F01: 1, F02: 2, F03: 3, F04: 4, F05: 5, F06: 6, F07: 7, F08: 8, F09: 9, F10: 10, F11: 11, F12: 12, F13: 13, F14: 14, F15: 15, F16: 16, F17: 17, F18: 18, F19: 19, F20: 20, F21: 21, F22: 22, F23: 23, F24: 24, F25: 25, F26: 26, F27: 27, F28: 28, F29: 29, F30: 30, F31: 31, F32: 32, F33: 33, F34: 34, F35: 35, F36: 36, F37: 37, F38: 38, F39: 39, F40: 40, F41: 41, F42: 42, F43: 43, F44: 44, F45: 45, F46: 46, F47: 47, F48: 48, F49: 49, F50: 50, F51: 51, F52: 52, F53: 53, F54: 54, F55: 55, F56: 56, F57: 57, F58: 58, F59: 59, F60: 60, F61: 61, F62: 62, F63: 63, F64: 64),
    (F65: 65),
};

select w.Value
from #structured.wide(items: $items) w
*/

// === Logical Plan ===
/*
MultiStatement
  Project [w.Value as w.Value]
    SchemaScan [#structured.wide(<IReadOnlyList`1>$items) as w]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [w.Value as w.Value]
    PhysicalSchemaScan [#structured.wide(<IReadOnlyList`1>$items) as w]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [w: StructuredWideRow]
      Value: int <- property Value
    Generated [ResultRow0]
      w.Value: int <- field w_Value

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_w_0: IReadOnlyList<StructuredWideInput> = convert<sequence<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput>>($items)]
    PhaseBoundary [From]
    SourceScan [w: StructuredWideRow] -> wRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [w in wRows]
      AppendShape [result <- ResultShape0(w.Value: w.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q352_StructuredWidePresence
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
            new Column("w.Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_w_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.w_Value);
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
                __musoqStructuralCarrier_0_a[] letItems = new __musoqStructuralCarrier_0_a[]
                {
                    new __musoqStructuralCarrier_0_a(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, default(int), 18446744073709551615UL, 0UL),
                    new __musoqStructuralCarrier_0_a(default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), default(int), 65, 0UL, 1UL)
                };
                OnPhaseChanged("compiled", QueryPhase.Begin);
                IReadOnlyList<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput> __musoqStructural_w_0 = __musoqStructuralAdapt_0_4199102667(letItems);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __wSchema = provider.GetSchema("#structured");
                var wRowsSourceContext = new SourceExecutionContext("w:1", sourceExecutionPlans["w:1"], token, __schemaColumns_compiled_w_0, sourceRuntimeSettingsBySourceContextId["w:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow> wRowsSource;
                try
                {
                    Musoq.Evaluator.Tests.StructuredSamples.StructuredWideSource wRowsSourceInstance = new Musoq.Evaluator.Tests.StructuredSamples.StructuredWideSource(__musoqCheckStructural_fc3359c19f3ef692(__musoqStructural_w_0, token), wRowsSourceContext);
                    wRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideSource, Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow>(__wSchema, "wide", wRowsSourceInstance, wRowsSourceContext, "#structured", "w", "w:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#structured", "wide", "w", "w:1", exception);
                }

                var wRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow>(wRowsSource.Chunks, __musoqProgressContext, "w:1") : wRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var wChunk in wRows)
                {
                    if (wChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow> wChunkView)
                    {
                        if (wChunkView.Source is Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow[] wChunkViewArray)
                        {
                            int wChunkViewOffset = wChunkView.Offset;
                            for (int wIndex = 0, wIndexCount = wChunkView.Count; wIndex < wIndexCount; ++wIndex)
                            {
                                if ((wIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var w = wChunkViewArray[wChunkViewOffset + wIndex];
                                yield return new ResultShape0(w.Value);
                            }

                            continue;
                        }

                        if (wChunkView.Source is List<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideRow> wChunkViewList)
                        {
                            int wChunkViewOffset = wChunkView.Offset;
                            for (int wIndex = 0, wIndexCount = wChunkView.Count; wIndex < wIndexCount; ++wIndex)
                            {
                                if ((wIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var w = wChunkViewList[wChunkViewOffset + wIndex];
                                yield return new ResultShape0(w.Value);
                            }

                            continue;
                        }
                    }

                    for (int wIndex = 0, wIndexCount = wChunk.Count; wIndex < wIndexCount; ++wIndex)
                    {
                        if ((wIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var w = wChunk[wIndex];
                        yield return new ResultShape0(w.Value);
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

        private static IReadOnlyList<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput> __musoqCheckStructural_fc3359c19f3ef692(IReadOnlyList<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput> value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            if (value != null)
            {
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                    if (__musoqStructural_collection1[__musoqStructural_index0] != null)
                    {
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                        __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                        if (3 > __musoqStructuralMaxDepth)
                            __musoqStructuralMaxDepth = 3;
                    }
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("let", "$items", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "w:1");
            return value;
        }

        private static IReadOnlyList<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput> __musoqStructuralAdapt_0_4199102667(__musoqStructuralCarrier_0_a[] source)
        {
            var result = new Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput[source.Length];
            for (var index = 0; index < source.Length; index++)
            {
                result[index] = new Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput(((source[index].P0 & 1UL) != 0UL) ? source[index].F0 : 0, ((source[index].P0 & 2UL) != 0UL) ? source[index].F1 : 0, ((source[index].P0 & 4UL) != 0UL) ? source[index].F2 : 0, ((source[index].P0 & 8UL) != 0UL) ? source[index].F3 : 0, ((source[index].P0 & 16UL) != 0UL) ? source[index].F4 : 0, ((source[index].P0 & 32UL) != 0UL) ? source[index].F5 : 0, ((source[index].P0 & 64UL) != 0UL) ? source[index].F6 : 0, ((source[index].P0 & 128UL) != 0UL) ? source[index].F7 : 0, ((source[index].P0 & 256UL) != 0UL) ? source[index].F8 : 0, ((source[index].P0 & 512UL) != 0UL) ? source[index].F9 : 0, ((source[index].P0 & 1024UL) != 0UL) ? source[index].F10 : 0, ((source[index].P0 & 2048UL) != 0UL) ? source[index].F11 : 0, ((source[index].P0 & 4096UL) != 0UL) ? source[index].F12 : 0, ((source[index].P0 & 8192UL) != 0UL) ? source[index].F13 : 0, ((source[index].P0 & 16384UL) != 0UL) ? source[index].F14 : 0, ((source[index].P0 & 32768UL) != 0UL) ? source[index].F15 : 0, ((source[index].P0 & 65536UL) != 0UL) ? source[index].F16 : 0, ((source[index].P0 & 131072UL) != 0UL) ? source[index].F17 : 0, ((source[index].P0 & 262144UL) != 0UL) ? source[index].F18 : 0, ((source[index].P0 & 524288UL) != 0UL) ? source[index].F19 : 0, ((source[index].P0 & 1048576UL) != 0UL) ? source[index].F20 : 0, ((source[index].P0 & 2097152UL) != 0UL) ? source[index].F21 : 0, ((source[index].P0 & 4194304UL) != 0UL) ? source[index].F22 : 0, ((source[index].P0 & 8388608UL) != 0UL) ? source[index].F23 : 0, ((source[index].P0 & 16777216UL) != 0UL) ? source[index].F24 : 0, ((source[index].P0 & 33554432UL) != 0UL) ? source[index].F25 : 0, ((source[index].P0 & 67108864UL) != 0UL) ? source[index].F26 : 0, ((source[index].P0 & 134217728UL) != 0UL) ? source[index].F27 : 0, ((source[index].P0 & 268435456UL) != 0UL) ? source[index].F28 : 0, ((source[index].P0 & 536870912UL) != 0UL) ? source[index].F29 : 0, ((source[index].P0 & 1073741824UL) != 0UL) ? source[index].F30 : 0, ((source[index].P0 & 2147483648UL) != 0UL) ? source[index].F31 : 0, ((source[index].P0 & 4294967296UL) != 0UL) ? source[index].F32 : 0, ((source[index].P0 & 8589934592UL) != 0UL) ? source[index].F33 : 0, ((source[index].P0 & 17179869184UL) != 0UL) ? source[index].F34 : 0, ((source[index].P0 & 34359738368UL) != 0UL) ? source[index].F35 : 0, ((source[index].P0 & 68719476736UL) != 0UL) ? source[index].F36 : 0, ((source[index].P0 & 137438953472UL) != 0UL) ? source[index].F37 : 0, ((source[index].P0 & 274877906944UL) != 0UL) ? source[index].F38 : 0, ((source[index].P0 & 549755813888UL) != 0UL) ? source[index].F39 : 0, ((source[index].P0 & 1099511627776UL) != 0UL) ? source[index].F40 : 0, ((source[index].P0 & 2199023255552UL) != 0UL) ? source[index].F41 : 0, ((source[index].P0 & 4398046511104UL) != 0UL) ? source[index].F42 : 0, ((source[index].P0 & 8796093022208UL) != 0UL) ? source[index].F43 : 0, ((source[index].P0 & 17592186044416UL) != 0UL) ? source[index].F44 : 0, ((source[index].P0 & 35184372088832UL) != 0UL) ? source[index].F45 : 0, ((source[index].P0 & 70368744177664UL) != 0UL) ? source[index].F46 : 0, ((source[index].P0 & 140737488355328UL) != 0UL) ? source[index].F47 : 0, ((source[index].P0 & 281474976710656UL) != 0UL) ? source[index].F48 : 0, ((source[index].P0 & 562949953421312UL) != 0UL) ? source[index].F49 : 0, ((source[index].P0 & 1125899906842624UL) != 0UL) ? source[index].F50 : 0, ((source[index].P0 & 2251799813685248UL) != 0UL) ? source[index].F51 : 0, ((source[index].P0 & 4503599627370496UL) != 0UL) ? source[index].F52 : 0, ((source[index].P0 & 9007199254740992UL) != 0UL) ? source[index].F53 : 0, ((source[index].P0 & 18014398509481984UL) != 0UL) ? source[index].F54 : 0, ((source[index].P0 & 36028797018963968UL) != 0UL) ? source[index].F55 : 0, ((source[index].P0 & 72057594037927936UL) != 0UL) ? source[index].F56 : 0, ((source[index].P0 & 144115188075855872UL) != 0UL) ? source[index].F57 : 0, ((source[index].P0 & 288230376151711744UL) != 0UL) ? source[index].F58 : 0, ((source[index].P0 & 576460752303423488UL) != 0UL) ? source[index].F59 : 0, ((source[index].P0 & 1152921504606846976UL) != 0UL) ? source[index].F60 : 0, ((source[index].P0 & 2305843009213693952UL) != 0UL) ? source[index].F61 : 0, ((source[index].P0 & 4611686018427387904UL) != 0UL) ? source[index].F62 : 0, ((source[index].P0 & 9223372036854775808UL) != 0UL) ? source[index].F63 : 0, ((source[index].P1 & 1UL) != 0UL) ? source[index].F64 : 0);
            }

            return result;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                w_Value = __value0;
            }

            public override int Count => 1;
            public int w_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        w_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "w.Value" => true,
                "w_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)w_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "w.Value" => (object)w_Value,
                "w_Value" => (object)w_Value,
                "Value" => (object)w_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int w_Value)
            {
                this.w_Value = w_Value;
            }

            public int w_Value { get; }
        }

        private readonly struct __musoqStructuralCarrier_0_a
        {
            public readonly int F0;
            public readonly int F1;
            public readonly int F2;
            public readonly int F3;
            public readonly int F4;
            public readonly int F5;
            public readonly int F6;
            public readonly int F7;
            public readonly int F8;
            public readonly int F9;
            public readonly int F10;
            public readonly int F11;
            public readonly int F12;
            public readonly int F13;
            public readonly int F14;
            public readonly int F15;
            public readonly int F16;
            public readonly int F17;
            public readonly int F18;
            public readonly int F19;
            public readonly int F20;
            public readonly int F21;
            public readonly int F22;
            public readonly int F23;
            public readonly int F24;
            public readonly int F25;
            public readonly int F26;
            public readonly int F27;
            public readonly int F28;
            public readonly int F29;
            public readonly int F30;
            public readonly int F31;
            public readonly int F32;
            public readonly int F33;
            public readonly int F34;
            public readonly int F35;
            public readonly int F36;
            public readonly int F37;
            public readonly int F38;
            public readonly int F39;
            public readonly int F40;
            public readonly int F41;
            public readonly int F42;
            public readonly int F43;
            public readonly int F44;
            public readonly int F45;
            public readonly int F46;
            public readonly int F47;
            public readonly int F48;
            public readonly int F49;
            public readonly int F50;
            public readonly int F51;
            public readonly int F52;
            public readonly int F53;
            public readonly int F54;
            public readonly int F55;
            public readonly int F56;
            public readonly int F57;
            public readonly int F58;
            public readonly int F59;
            public readonly int F60;
            public readonly int F61;
            public readonly int F62;
            public readonly int F63;
            public readonly int F64;
            public readonly ulong P0;
            public readonly ulong P1;
            public __musoqStructuralCarrier_0_a(int f0, int f1, int f2, int f3, int f4, int f5, int f6, int f7, int f8, int f9, int f10, int f11, int f12, int f13, int f14, int f15, int f16, int f17, int f18, int f19, int f20, int f21, int f22, int f23, int f24, int f25, int f26, int f27, int f28, int f29, int f30, int f31, int f32, int f33, int f34, int f35, int f36, int f37, int f38, int f39, int f40, int f41, int f42, int f43, int f44, int f45, int f46, int f47, int f48, int f49, int f50, int f51, int f52, int f53, int f54, int f55, int f56, int f57, int f58, int f59, int f60, int f61, int f62, int f63, int f64, ulong p0, ulong p1)
            {
                F0 = f0;
                F1 = f1;
                F2 = f2;
                F3 = f3;
                F4 = f4;
                F5 = f5;
                F6 = f6;
                F7 = f7;
                F8 = f8;
                F9 = f9;
                F10 = f10;
                F11 = f11;
                F12 = f12;
                F13 = f13;
                F14 = f14;
                F15 = f15;
                F16 = f16;
                F17 = f17;
                F18 = f18;
                F19 = f19;
                F20 = f20;
                F21 = f21;
                F22 = f22;
                F23 = f23;
                F24 = f24;
                F25 = f25;
                F26 = f26;
                F27 = f27;
                F28 = f28;
                F29 = f29;
                F30 = f30;
                F31 = f31;
                F32 = f32;
                F33 = f33;
                F34 = f34;
                F35 = f35;
                F36 = f36;
                F37 = f37;
                F38 = f38;
                F39 = f39;
                F40 = f40;
                F41 = f41;
                F42 = f42;
                F43 = f43;
                F44 = f44;
                F45 = f45;
                F46 = f46;
                F47 = f47;
                F48 = f48;
                F49 = f49;
                F50 = f50;
                F51 = f51;
                F52 = f52;
                F53 = f53;
                F54 = f54;
                F55 = f55;
                F56 = f56;
                F57 = f57;
                F58 = f58;
                F59 = f59;
                F60 = f60;
                F61 = f61;
                F62 = f62;
                F63 = f63;
                F64 = f64;
                P0 = p0;
                P1 = p1;
            }
        }
    }
}
