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
  Project [w.Value as Value]
    SchemaScan [#structured.wide(<IReadOnlyList`1>$items) as w]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [w.Value as Value]
    PhysicalSchemaScan [#structured.wide(<IReadOnlyList`1>$items) as w]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [w: StructuredWideRow]
      Value: int <- property Value
    Generated [ResultRow0]
      Value: int <- field Value

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_w_0: IReadOnlyList<StructuredWideInput> = convert<sequence<Musoq.Evaluator.Tests.StructuredSamples.StructuredWideInput>>($items)]
    PhaseBoundary [From]
    SourceScan [w: StructuredWideRow] -> wRows
    CreateShapeRows [result: ResultRow0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [w in wRows]
      AppendShape [result <- ResultRow0(Value: w.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultRow0]
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

    public sealed class CompiledQuery : BaseOperations, ITableRunnable, IQueryProgressSource, IParameterizedRunnable, ITypedParameterizedRunnable
    {
        private static readonly Column[] __columns_compiled_result_1 = new Column[]
        {
            new Column("Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_w_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
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
                                yield return new ResultRow0(w.Value);
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
                                yield return new ResultRow0(w.Value);
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
                        yield return new ResultRow0(w.Value);
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
                Value = __value0;
            }

            public override int Count => 1;
            public int Value { get; private set; }

            public override void AssignValue<T>(int columnNumber, T value)
            {
                global::Musoq.Evaluator.Tables.TypedColumnRead.Discard<T>(columnNumber switch
                {
                    0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value = global::Musoq.Evaluator.Tables.TypedColumnRead.Read<int, T>(value)),
                    _ => throw new ArgumentOutOfRangeException(nameof(columnNumber))
                });
            }

            public override T GetValue<T>(int columnIndex) => columnIndex switch
            {
                0 => global::Musoq.Evaluator.Tables.TypedColumnRead.Read<T, int>(this.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            public override bool HasColumn(string name) => name switch
            {
                "Value" => true,
                _ => false

            };
            public override void VisitColumns<TVisitor>(ref TVisitor visitor)
            {
                global::Musoq.Evaluator.Tables.TypedColumnVisit.Apply(0, this.Value, ref visitor);
            }
        }

        [global::System.Text.Json.Serialization.JsonConverter(typeof(__musoqStructuralCarrier_0_aJsonConverter))]
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

        private sealed class __musoqStructuralCarrier_0_aJsonConverter : global::System.Text.Json.Serialization.JsonConverter<__musoqStructuralCarrier_0_a>
        {
            public __musoqStructuralCarrier_0_aJsonConverter()
            {
            }

            public override __musoqStructuralCarrier_0_a Read(ref global::System.Text.Json.Utf8JsonReader reader, global::System.Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartObject)
                    throw new global::System.Text.Json.JsonException("A native record requires a JSON object.");
                int field0 = default;
                int field1 = default;
                int field2 = default;
                int field3 = default;
                int field4 = default;
                int field5 = default;
                int field6 = default;
                int field7 = default;
                int field8 = default;
                int field9 = default;
                int field10 = default;
                int field11 = default;
                int field12 = default;
                int field13 = default;
                int field14 = default;
                int field15 = default;
                int field16 = default;
                int field17 = default;
                int field18 = default;
                int field19 = default;
                int field20 = default;
                int field21 = default;
                int field22 = default;
                int field23 = default;
                int field24 = default;
                int field25 = default;
                int field26 = default;
                int field27 = default;
                int field28 = default;
                int field29 = default;
                int field30 = default;
                int field31 = default;
                int field32 = default;
                int field33 = default;
                int field34 = default;
                int field35 = default;
                int field36 = default;
                int field37 = default;
                int field38 = default;
                int field39 = default;
                int field40 = default;
                int field41 = default;
                int field42 = default;
                int field43 = default;
                int field44 = default;
                int field45 = default;
                int field46 = default;
                int field47 = default;
                int field48 = default;
                int field49 = default;
                int field50 = default;
                int field51 = default;
                int field52 = default;
                int field53 = default;
                int field54 = default;
                int field55 = default;
                int field56 = default;
                int field57 = default;
                int field58 = default;
                int field59 = default;
                int field60 = default;
                int field61 = default;
                int field62 = default;
                int field63 = default;
                int field64 = default;
                ulong p0 = 0;
                ulong p1 = 0;
                while (reader.Read())
                {
                    if (reader.TokenType == global::System.Text.Json.JsonTokenType.EndObject)
                    {
                        return new __musoqStructuralCarrier_0_a(field0, field1, field2, field3, field4, field5, field6, field7, field8, field9, field10, field11, field12, field13, field14, field15, field16, field17, field18, field19, field20, field21, field22, field23, field24, field25, field26, field27, field28, field29, field30, field31, field32, field33, field34, field35, field36, field37, field38, field39, field40, field41, field42, field43, field44, field45, field46, field47, field48, field49, field50, field51, field52, field53, field54, field55, field56, field57, field58, field59, field60, field61, field62, field63, field64, p0, p1);
                    }

                    if (reader.TokenType != global::System.Text.Json.JsonTokenType.PropertyName)
                        throw new global::System.Text.Json.JsonException("A native record requires named fields.");
                    if (reader.ValueTextEquals("F01"))
                    {
                        if ((p0 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field0 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F02"))
                    {
                        if ((p0 & 2UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field1 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F03"))
                    {
                        if ((p0 & 4UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field2 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F04"))
                    {
                        if ((p0 & 8UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field3 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 8UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F05"))
                    {
                        if ((p0 & 16UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field4 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 16UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F06"))
                    {
                        if ((p0 & 32UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field5 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 32UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F07"))
                    {
                        if ((p0 & 64UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field6 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 64UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F08"))
                    {
                        if ((p0 & 128UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field7 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 128UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F09"))
                    {
                        if ((p0 & 256UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field8 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 256UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F10"))
                    {
                        if ((p0 & 512UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field9 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 512UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F11"))
                    {
                        if ((p0 & 1024UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field10 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1024UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F12"))
                    {
                        if ((p0 & 2048UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field11 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2048UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F13"))
                    {
                        if ((p0 & 4096UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field12 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4096UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F14"))
                    {
                        if ((p0 & 8192UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field13 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 8192UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F15"))
                    {
                        if ((p0 & 16384UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field14 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 16384UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F16"))
                    {
                        if ((p0 & 32768UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field15 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 32768UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F17"))
                    {
                        if ((p0 & 65536UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field16 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 65536UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F18"))
                    {
                        if ((p0 & 131072UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field17 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 131072UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F19"))
                    {
                        if ((p0 & 262144UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field18 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 262144UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F20"))
                    {
                        if ((p0 & 524288UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field19 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 524288UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F21"))
                    {
                        if ((p0 & 1048576UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field20 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1048576UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F22"))
                    {
                        if ((p0 & 2097152UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field21 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2097152UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F23"))
                    {
                        if ((p0 & 4194304UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field22 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4194304UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F24"))
                    {
                        if ((p0 & 8388608UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field23 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 8388608UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F25"))
                    {
                        if ((p0 & 16777216UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field24 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 16777216UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F26"))
                    {
                        if ((p0 & 33554432UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field25 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 33554432UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F27"))
                    {
                        if ((p0 & 67108864UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field26 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 67108864UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F28"))
                    {
                        if ((p0 & 134217728UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field27 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 134217728UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F29"))
                    {
                        if ((p0 & 268435456UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field28 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 268435456UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F30"))
                    {
                        if ((p0 & 536870912UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field29 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 536870912UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F31"))
                    {
                        if ((p0 & 1073741824UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field30 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1073741824UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F32"))
                    {
                        if ((p0 & 2147483648UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field31 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2147483648UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F33"))
                    {
                        if ((p0 & 4294967296UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field32 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4294967296UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F34"))
                    {
                        if ((p0 & 8589934592UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field33 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 8589934592UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F35"))
                    {
                        if ((p0 & 17179869184UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field34 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 17179869184UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F36"))
                    {
                        if ((p0 & 34359738368UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field35 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 34359738368UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F37"))
                    {
                        if ((p0 & 68719476736UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field36 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 68719476736UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F38"))
                    {
                        if ((p0 & 137438953472UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field37 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 137438953472UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F39"))
                    {
                        if ((p0 & 274877906944UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field38 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 274877906944UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F40"))
                    {
                        if ((p0 & 549755813888UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field39 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 549755813888UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F41"))
                    {
                        if ((p0 & 1099511627776UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field40 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1099511627776UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F42"))
                    {
                        if ((p0 & 2199023255552UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field41 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2199023255552UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F43"))
                    {
                        if ((p0 & 4398046511104UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field42 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4398046511104UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F44"))
                    {
                        if ((p0 & 8796093022208UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field43 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 8796093022208UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F45"))
                    {
                        if ((p0 & 17592186044416UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field44 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 17592186044416UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F46"))
                    {
                        if ((p0 & 35184372088832UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field45 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 35184372088832UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F47"))
                    {
                        if ((p0 & 70368744177664UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field46 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 70368744177664UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F48"))
                    {
                        if ((p0 & 140737488355328UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field47 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 140737488355328UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F49"))
                    {
                        if ((p0 & 281474976710656UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field48 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 281474976710656UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F50"))
                    {
                        if ((p0 & 562949953421312UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field49 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 562949953421312UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F51"))
                    {
                        if ((p0 & 1125899906842624UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field50 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1125899906842624UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F52"))
                    {
                        if ((p0 & 2251799813685248UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field51 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2251799813685248UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F53"))
                    {
                        if ((p0 & 4503599627370496UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field52 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4503599627370496UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F54"))
                    {
                        if ((p0 & 9007199254740992UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field53 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 9007199254740992UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F55"))
                    {
                        if ((p0 & 18014398509481984UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field54 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 18014398509481984UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F56"))
                    {
                        if ((p0 & 36028797018963968UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field55 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 36028797018963968UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F57"))
                    {
                        if ((p0 & 72057594037927936UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field56 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 72057594037927936UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F58"))
                    {
                        if ((p0 & 144115188075855872UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field57 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 144115188075855872UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F59"))
                    {
                        if ((p0 & 288230376151711744UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field58 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 288230376151711744UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F60"))
                    {
                        if ((p0 & 576460752303423488UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field59 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 576460752303423488UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F61"))
                    {
                        if ((p0 & 1152921504606846976UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field60 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 1152921504606846976UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F62"))
                    {
                        if ((p0 & 2305843009213693952UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field61 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 2305843009213693952UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F63"))
                    {
                        if ((p0 & 4611686018427387904UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field62 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 4611686018427387904UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F64"))
                    {
                        if ((p0 & 9223372036854775808UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field63 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p0 |= 9223372036854775808UL;
                        continue;
                    }

                    if (reader.ValueTextEquals("F65"))
                    {
                        if ((p1 & 1UL) != 0)
                            throw new global::System.Text.Json.JsonException("A native field occurs more than once.");
                        reader.Read();
                        field64 = global::System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!;
                        p1 |= 1UL;
                        continue;
                    }

                    throw new global::System.Text.Json.JsonException("The native record contains an undeclared field.");
                }

                throw new global::System.Text.Json.JsonException("The native record is incomplete.");
            }

            public override void Write(global::System.Text.Json.Utf8JsonWriter writer, __musoqStructuralCarrier_0_a value, global::System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                if ((value.P0 & 1UL) != 0)
                {
                    writer.WritePropertyName("F01");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F0, options);
                }

                if ((value.P0 & 2UL) != 0)
                {
                    writer.WritePropertyName("F02");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F1, options);
                }

                if ((value.P0 & 4UL) != 0)
                {
                    writer.WritePropertyName("F03");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F2, options);
                }

                if ((value.P0 & 8UL) != 0)
                {
                    writer.WritePropertyName("F04");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F3, options);
                }

                if ((value.P0 & 16UL) != 0)
                {
                    writer.WritePropertyName("F05");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F4, options);
                }

                if ((value.P0 & 32UL) != 0)
                {
                    writer.WritePropertyName("F06");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F5, options);
                }

                if ((value.P0 & 64UL) != 0)
                {
                    writer.WritePropertyName("F07");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F6, options);
                }

                if ((value.P0 & 128UL) != 0)
                {
                    writer.WritePropertyName("F08");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F7, options);
                }

                if ((value.P0 & 256UL) != 0)
                {
                    writer.WritePropertyName("F09");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F8, options);
                }

                if ((value.P0 & 512UL) != 0)
                {
                    writer.WritePropertyName("F10");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F9, options);
                }

                if ((value.P0 & 1024UL) != 0)
                {
                    writer.WritePropertyName("F11");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F10, options);
                }

                if ((value.P0 & 2048UL) != 0)
                {
                    writer.WritePropertyName("F12");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F11, options);
                }

                if ((value.P0 & 4096UL) != 0)
                {
                    writer.WritePropertyName("F13");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F12, options);
                }

                if ((value.P0 & 8192UL) != 0)
                {
                    writer.WritePropertyName("F14");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F13, options);
                }

                if ((value.P0 & 16384UL) != 0)
                {
                    writer.WritePropertyName("F15");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F14, options);
                }

                if ((value.P0 & 32768UL) != 0)
                {
                    writer.WritePropertyName("F16");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F15, options);
                }

                if ((value.P0 & 65536UL) != 0)
                {
                    writer.WritePropertyName("F17");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F16, options);
                }

                if ((value.P0 & 131072UL) != 0)
                {
                    writer.WritePropertyName("F18");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F17, options);
                }

                if ((value.P0 & 262144UL) != 0)
                {
                    writer.WritePropertyName("F19");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F18, options);
                }

                if ((value.P0 & 524288UL) != 0)
                {
                    writer.WritePropertyName("F20");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F19, options);
                }

                if ((value.P0 & 1048576UL) != 0)
                {
                    writer.WritePropertyName("F21");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F20, options);
                }

                if ((value.P0 & 2097152UL) != 0)
                {
                    writer.WritePropertyName("F22");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F21, options);
                }

                if ((value.P0 & 4194304UL) != 0)
                {
                    writer.WritePropertyName("F23");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F22, options);
                }

                if ((value.P0 & 8388608UL) != 0)
                {
                    writer.WritePropertyName("F24");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F23, options);
                }

                if ((value.P0 & 16777216UL) != 0)
                {
                    writer.WritePropertyName("F25");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F24, options);
                }

                if ((value.P0 & 33554432UL) != 0)
                {
                    writer.WritePropertyName("F26");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F25, options);
                }

                if ((value.P0 & 67108864UL) != 0)
                {
                    writer.WritePropertyName("F27");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F26, options);
                }

                if ((value.P0 & 134217728UL) != 0)
                {
                    writer.WritePropertyName("F28");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F27, options);
                }

                if ((value.P0 & 268435456UL) != 0)
                {
                    writer.WritePropertyName("F29");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F28, options);
                }

                if ((value.P0 & 536870912UL) != 0)
                {
                    writer.WritePropertyName("F30");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F29, options);
                }

                if ((value.P0 & 1073741824UL) != 0)
                {
                    writer.WritePropertyName("F31");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F30, options);
                }

                if ((value.P0 & 2147483648UL) != 0)
                {
                    writer.WritePropertyName("F32");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F31, options);
                }

                if ((value.P0 & 4294967296UL) != 0)
                {
                    writer.WritePropertyName("F33");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F32, options);
                }

                if ((value.P0 & 8589934592UL) != 0)
                {
                    writer.WritePropertyName("F34");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F33, options);
                }

                if ((value.P0 & 17179869184UL) != 0)
                {
                    writer.WritePropertyName("F35");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F34, options);
                }

                if ((value.P0 & 34359738368UL) != 0)
                {
                    writer.WritePropertyName("F36");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F35, options);
                }

                if ((value.P0 & 68719476736UL) != 0)
                {
                    writer.WritePropertyName("F37");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F36, options);
                }

                if ((value.P0 & 137438953472UL) != 0)
                {
                    writer.WritePropertyName("F38");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F37, options);
                }

                if ((value.P0 & 274877906944UL) != 0)
                {
                    writer.WritePropertyName("F39");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F38, options);
                }

                if ((value.P0 & 549755813888UL) != 0)
                {
                    writer.WritePropertyName("F40");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F39, options);
                }

                if ((value.P0 & 1099511627776UL) != 0)
                {
                    writer.WritePropertyName("F41");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F40, options);
                }

                if ((value.P0 & 2199023255552UL) != 0)
                {
                    writer.WritePropertyName("F42");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F41, options);
                }

                if ((value.P0 & 4398046511104UL) != 0)
                {
                    writer.WritePropertyName("F43");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F42, options);
                }

                if ((value.P0 & 8796093022208UL) != 0)
                {
                    writer.WritePropertyName("F44");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F43, options);
                }

                if ((value.P0 & 17592186044416UL) != 0)
                {
                    writer.WritePropertyName("F45");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F44, options);
                }

                if ((value.P0 & 35184372088832UL) != 0)
                {
                    writer.WritePropertyName("F46");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F45, options);
                }

                if ((value.P0 & 70368744177664UL) != 0)
                {
                    writer.WritePropertyName("F47");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F46, options);
                }

                if ((value.P0 & 140737488355328UL) != 0)
                {
                    writer.WritePropertyName("F48");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F47, options);
                }

                if ((value.P0 & 281474976710656UL) != 0)
                {
                    writer.WritePropertyName("F49");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F48, options);
                }

                if ((value.P0 & 562949953421312UL) != 0)
                {
                    writer.WritePropertyName("F50");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F49, options);
                }

                if ((value.P0 & 1125899906842624UL) != 0)
                {
                    writer.WritePropertyName("F51");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F50, options);
                }

                if ((value.P0 & 2251799813685248UL) != 0)
                {
                    writer.WritePropertyName("F52");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F51, options);
                }

                if ((value.P0 & 4503599627370496UL) != 0)
                {
                    writer.WritePropertyName("F53");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F52, options);
                }

                if ((value.P0 & 9007199254740992UL) != 0)
                {
                    writer.WritePropertyName("F54");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F53, options);
                }

                if ((value.P0 & 18014398509481984UL) != 0)
                {
                    writer.WritePropertyName("F55");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F54, options);
                }

                if ((value.P0 & 36028797018963968UL) != 0)
                {
                    writer.WritePropertyName("F56");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F55, options);
                }

                if ((value.P0 & 72057594037927936UL) != 0)
                {
                    writer.WritePropertyName("F57");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F56, options);
                }

                if ((value.P0 & 144115188075855872UL) != 0)
                {
                    writer.WritePropertyName("F58");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F57, options);
                }

                if ((value.P0 & 288230376151711744UL) != 0)
                {
                    writer.WritePropertyName("F59");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F58, options);
                }

                if ((value.P0 & 576460752303423488UL) != 0)
                {
                    writer.WritePropertyName("F60");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F59, options);
                }

                if ((value.P0 & 1152921504606846976UL) != 0)
                {
                    writer.WritePropertyName("F61");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F60, options);
                }

                if ((value.P0 & 2305843009213693952UL) != 0)
                {
                    writer.WritePropertyName("F62");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F61, options);
                }

                if ((value.P0 & 4611686018427387904UL) != 0)
                {
                    writer.WritePropertyName("F63");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F62, options);
                }

                if ((value.P0 & 9223372036854775808UL) != 0)
                {
                    writer.WritePropertyName("F64");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F63, options);
                }

                if ((value.P1 & 1UL) != 0)
                {
                    writer.WritePropertyName("F65");
                    global::System.Text.Json.JsonSerializer.Serialize<int>(writer, value.F64, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}
