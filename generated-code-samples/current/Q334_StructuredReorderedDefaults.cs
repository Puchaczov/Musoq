// === Parsed Query ===
/*
select w.Value, w.Weight, w.Enabled
from #inputs.weighted(
    items: array {
        (Value: 10, Weight: 1.5),
        (Weight: 2.5, Value: 20, Enabled: false),
    }
) w
*/

// === Logical Plan ===
/*
MultiStatement
  Project [w.Value as w.Value, w.Weight as w.Weight, w.Enabled as w.Enabled]
    SchemaScan [#inputs.weighted(array { (Value: 10, Weight: 1,5), (Weight: 2,5, Value: 20, Enabled: FALSE) }) as w]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [w.Value as w.Value, w.Weight as w.Weight, w.Enabled as w.Enabled]
    PhysicalSchemaScan [#inputs.weighted(array { (Value: 10, Weight: 1,5), (Weight: 2,5, Value: 20, Enabled: FALSE) }) as w]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [w: WeightedRow]
      Value: int <- property Value
      Weight: decimal <- property Weight
      Enabled: bool <- property Enabled
    Generated [ResultRow0]
      w.Value: int <- field w_Value
      w.Weight: decimal <- field w_Weight
      w.Enabled: bool <- field w_Enabled
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.WeightedInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.WeightedInput@Musoq.Examples.DataSources.StructuredInputs(primitive:int32,primitive:decimal,primitive:bool); shape=(Value: int32, Weight: decimal); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,ExecutionLiteral { ReturnType = bool, Value = Boolean:0:0:1:0:0:Unspecified:0 }
    Musoq.Examples.DataSources.StructuredInputs.WeightedInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.WeightedInput@Musoq.Examples.DataSources.StructuredInputs(primitive:int32,primitive:decimal,primitive:bool); shape=(Weight: decimal, Value: int32, Enabled: bool = true); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,-

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_w_0: int = 10]
    Let [__musoqStructural_w_1: decimal = 1,5]
    PrepareStructuralInput [__musoqStructural_w_2: Musoq.Examples.DataSources.StructuredInputs.WeightedInput <- (Value: __musoqStructural_w_0, Weight: __musoqStructural_w_1); lifetime Inline; shape (Value: int32, Weight: decimal)]
    Let [__musoqStructural_w_3: decimal = 2,5]
    Let [__musoqStructural_w_4: int = 20]
    Let [__musoqStructural_w_5: bool = FALSE]
    PrepareStructuralInput [__musoqStructural_w_6: Musoq.Examples.DataSources.StructuredInputs.WeightedInput <- (Weight: __musoqStructural_w_3, Value: __musoqStructural_w_4, Enabled: __musoqStructural_w_5); lifetime Inline; shape (Weight: decimal, Value: int32, Enabled: bool = true)]
    Let [__musoqStructural_w_7: IReadOnlyList<WeightedInput> = array { __musoqStructural_w_2, __musoqStructural_w_6 }]
    PhaseBoundary [From]
    SourceScan [w: WeightedRow] -> wRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [w in wRows]
      AppendShape [result <- ResultShape0(w.Value: w.Value, w.Weight: w.Weight, w.Enabled: w.Enabled)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q334_StructuredReorderedDefaults
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
            new Column("w.Value", typeof(int), 0),
            new Column("w.Weight", typeof(decimal), 1),
            new Column("w.Enabled", typeof(bool), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_w_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0), new Column("Weight", typeof(decimal), 1), new Column("Enabled", typeof(bool), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.w_Value, __musoqShapeRow.w_Weight, __musoqShapeRow.w_Enabled);
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
                var __musoqFinalShapeRows = new List<ResultShape0>();
                OnPhaseChanged("compiled", QueryPhase.Begin);
                int __musoqStructural_w_0 = 10;
                decimal __musoqStructural_w_1 = 1.5m;
                Musoq.Examples.DataSources.StructuredInputs.WeightedInput __musoqStructural_w_2 = new Musoq.Examples.DataSources.StructuredInputs.WeightedInput(__musoqStructural_w_0, __musoqStructural_w_1, true);
                decimal __musoqStructural_w_3 = 2.5m;
                int __musoqStructural_w_4 = 20;
                bool __musoqStructural_w_5 = false;
                Musoq.Examples.DataSources.StructuredInputs.WeightedInput __musoqStructural_w_6 = new Musoq.Examples.DataSources.StructuredInputs.WeightedInput(__musoqStructural_w_4, __musoqStructural_w_3, __musoqStructural_w_5);
                IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.WeightedInput> __musoqStructural_w_7 = new Musoq.Examples.DataSources.StructuredInputs.WeightedInput[]
                {
                    __musoqStructural_w_2,
                    __musoqStructural_w_6
                };
                OnPhaseChanged("compiled", QueryPhase.From);
                var __wSchema = provider.GetSchema("#inputs");
                var wRowsSourceContext = new SourceExecutionContext("w:1", sourceExecutionPlans["w:1"], token, __schemaColumns_compiled_w_0, sourceRuntimeSettingsBySourceContextId["w:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.WeightedRow> wRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.WeightedSource wRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.WeightedSource(__musoqCheckStructural_4a1fea3135efdd7c(__musoqStructural_w_7, token), wRowsSourceContext);
                    wRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.WeightedSource, Musoq.Examples.DataSources.StructuredInputs.WeightedRow>(__wSchema, "weighted", wRowsSourceInstance, wRowsSourceContext, "#inputs", "w", "w:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "weighted", "w", "w:1", exception);
                }

                var wRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.WeightedRow>(wRowsSource.Chunks, __musoqProgressContext, "w:1") : wRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var wChunk in wRows)
                {
                    if (wChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.WeightedRow> wChunkView)
                    {
                        if (wChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.WeightedRow[] wChunkViewArray)
                        {
                            int wChunkViewOffset = wChunkView.Offset;
                            for (int wIndex = 0, wIndexCount = wChunkView.Count; wIndex < wIndexCount; ++wIndex)
                            {
                                if ((wIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var w = wChunkViewArray[wChunkViewOffset + wIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(w.Value, w.Weight, w.Enabled));
                            }

                            continue;
                        }

                        if (wChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.WeightedRow> wChunkViewList)
                        {
                            int wChunkViewOffset = wChunkView.Offset;
                            for (int wIndex = 0, wIndexCount = wChunkView.Count; wIndex < wIndexCount; ++wIndex)
                            {
                                if ((wIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var w = wChunkViewList[wChunkViewOffset + wIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(w.Value, w.Weight, w.Enabled));
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
                        __musoqFinalShapeRows.Add(new ResultShape0(w.Value, w.Weight, w.Enabled));
                    }
                }

                return __musoqFinalShapeRows;
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

        private static IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.WeightedInput> __musoqCheckStructural_4a1fea3135efdd7c(IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.WeightedInput> value, System.Threading.CancellationToken token)
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
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<Musoq.Examples.DataSources.StructuredInputs.WeightedInput>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
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

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "w.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "w:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0, decimal __value1, bool __value2)
            {
                w_Value = __value0;
                w_Weight = __value1;
                w_Enabled = __value2;
            }

            public override int Count => 3;
            public bool w_Enabled { get; private set; }
            public int w_Value { get; private set; }
            public decimal w_Weight { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        w_Value = (int)value;
                        break;
                    case 1:
                        w_Weight = (decimal)value;
                        break;
                    case 2:
                        w_Enabled = (bool)value;
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
                "w.Weight" => true,
                "w_Weight" => true,
                "Weight" => true,
                "w.Enabled" => true,
                "w_Enabled" => true,
                "Enabled" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)w_Value,
                1 => (object)w_Weight,
                2 => (object)w_Enabled,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "w.Value" => (object)w_Value,
                "w_Value" => (object)w_Value,
                "Value" => (object)w_Value,
                "w.Weight" => (object)w_Weight,
                "w_Weight" => (object)w_Weight,
                "Weight" => (object)w_Weight,
                "w.Enabled" => (object)w_Enabled,
                "w_Enabled" => (object)w_Enabled,
                "Enabled" => (object)w_Enabled,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int w_Value, decimal w_Weight, bool w_Enabled)
            {
                this.w_Value = w_Value;
                this.w_Weight = w_Weight;
                this.w_Enabled = w_Enabled;
            }

            public bool w_Enabled { get; }
            public int w_Value { get; }
            public decimal w_Weight { get; }
        }
    }
}
