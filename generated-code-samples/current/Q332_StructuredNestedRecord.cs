// === Parsed Query ===
/*
select c.Enabled, c.Before, c.After
from #inputs.configure(
    options: (
        Enabled: true,
        Codes: array { 10, 20 },
        Window: (Before: 2, After: 3),
    )
) c
*/

// === Logical Plan ===
/*
MultiStatement
  Project [c.Enabled as c.Enabled, c.Before as c.Before, c.After as c.After]
    SchemaScan [#inputs.configure((Enabled: TRUE, Codes: array { 10, 20 }, window: (Before: 2, After: 3))) as c]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [c.Enabled as c.Enabled, c.Before as c.Before, c.After as c.After]
    PhysicalSchemaScan [#inputs.configure((Enabled: TRUE, Codes: array { 10, 20 }, window: (Before: 2, After: 3))) as c]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [c: ConfigureRow]
      Enabled: bool <- property Enabled
      Before: int <- property Before
      After: int <- property After
    Generated [ResultRow0]
      c.Enabled: bool <- field c_Enabled
      c.Before: int <- field c_Before
      c.After: int <- field c_After
  StructuralPreparation
    Musoq.Examples.DataSources.StructuredInputs.WindowInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.WindowInput@Musoq.Examples.DataSources.StructuredInputs(primitive:int32,primitive:int32); shape=(Before: int32, After: int32); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-
    Musoq.Examples.DataSources.StructuredInputs.OptionsInput; constructor=constructor:clr:Musoq.Examples.DataSources.StructuredInputs.OptionsInput@Musoq.Examples.DataSources.StructuredInputs(primitive:bool,array:1<primitive:int32>,clr:Musoq.Examples.DataSources.StructuredInputs.WindowInput@Musoq.Examples.DataSources.StructuredInputs); shape=(Enabled: bool, Codes: int32[], Window: (Before: int32, After: int32)); origin=Inline; lifetime=Inline; metrics=Runtime; ownership=ConstructFresh; limits=depth=32;nodes=100000;strings=67108864; defaults=-,-,-

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_c_0: bool = TRUE]
    Let [__musoqStructural_c_1: int[] = array { 10, 20 }]
    Let [__musoqStructural_c_2: int = 2]
    Let [__musoqStructural_c_3: int = 3]
    PrepareStructuralInput [__musoqStructural_c_4: Musoq.Examples.DataSources.StructuredInputs.WindowInput <- (Before: __musoqStructural_c_2, After: __musoqStructural_c_3); lifetime Inline; shape (Before: int32, After: int32)]
    PrepareStructuralInput [__musoqStructural_c_5: Musoq.Examples.DataSources.StructuredInputs.OptionsInput <- (Enabled: __musoqStructural_c_0, Codes: __musoqStructural_c_1, window: __musoqStructural_c_4); lifetime Inline; shape (Enabled: bool, Codes: int32[], Window: (Before: int32, After: int32))]
    PhaseBoundary [From]
    SourceScan [c: ConfigureRow] -> cRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [c in cRows]
      AppendShape [result <- ResultShape0(c.Enabled: c.Enabled, c.Before: c.Before, c.After: c.After)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q332_StructuredNestedRecord
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
            new Column("c.Enabled", typeof(bool), 0),
            new Column("c.Before", typeof(int), 1),
            new Column("c.After", typeof(int), 2)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_c_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Enabled", typeof(bool), 0), new Column("Before", typeof(int), 1), new Column("After", typeof(int), 2) });
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
                yield return new ResultRow0(__musoqShapeRow.c_Enabled, __musoqShapeRow.c_Before, __musoqShapeRow.c_After);
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
                bool __musoqStructural_c_0 = true;
                int[] __musoqStructural_c_1 = new int[]
                {
                    10,
                    20
                };
                int __musoqStructural_c_2 = 2;
                int __musoqStructural_c_3 = 3;
                Musoq.Examples.DataSources.StructuredInputs.WindowInput __musoqStructural_c_4 = new Musoq.Examples.DataSources.StructuredInputs.WindowInput(__musoqStructural_c_2, __musoqStructural_c_3);
                Musoq.Examples.DataSources.StructuredInputs.OptionsInput __musoqStructural_c_5 = new Musoq.Examples.DataSources.StructuredInputs.OptionsInput(__musoqStructural_c_0, __musoqStructural_c_1, __musoqStructural_c_4);
                OnPhaseChanged("compiled", QueryPhase.From);
                var __cSchema = provider.GetSchema("#inputs");
                var cRowsSourceContext = new SourceExecutionContext("c:1", sourceExecutionPlans["c:1"], token, __schemaColumns_compiled_c_0, sourceRuntimeSettingsBySourceContextId["c:1"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> cRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.ConfigureSource cRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.ConfigureSource(__musoqCheckStructural_d1a88d08bcd31867(__musoqStructural_c_5, token), cRowsSourceContext);
                    cRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.ConfigureSource, Musoq.Examples.DataSources.StructuredInputs.ConfigureRow>(__cSchema, "configure", cRowsSourceInstance, cRowsSourceContext, "#inputs", "c", "c:1");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "configure", "c", "c:1", exception);
                }

                var cRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow>(cRowsSource.Chunks, __musoqProgressContext, "c:1") : cRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var cChunk in cRows)
                {
                    if (cChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> cChunkView)
                    {
                        if (cChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.ConfigureRow[] cChunkViewArray)
                        {
                            int cChunkViewOffset = cChunkView.Offset;
                            for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                            {
                                if ((cIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var c = cChunkViewArray[cChunkViewOffset + cIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(c.Enabled, c.Before, c.After));
                            }

                            continue;
                        }

                        if (cChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.ConfigureRow> cChunkViewList)
                        {
                            int cChunkViewOffset = cChunkView.Offset;
                            for (int cIndex = 0, cIndexCount = cChunkView.Count; cIndex < cIndexCount; ++cIndex)
                            {
                                if ((cIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var c = cChunkViewList[cChunkViewOffset + cIndex];
                                __musoqFinalShapeRows.Add(new ResultShape0(c.Enabled, c.Before, c.After));
                            }

                            continue;
                        }
                    }

                    for (int cIndex = 0, cIndexCount = cChunk.Count; cIndex < cIndexCount; ++cIndex)
                    {
                        if ((cIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var c = cChunk[cIndex];
                        __musoqFinalShapeRows.Add(new ResultShape0(c.Enabled, c.Before, c.After));
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

        private static Musoq.Examples.DataSources.StructuredInputs.OptionsInput __musoqCheckStructural_d1a88d08bcd31867(Musoq.Examples.DataSources.StructuredInputs.OptionsInput value, System.Threading.CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            long __musoqStructuralNodes = 0L;
            long __musoqStructuralStrings = 0L;
            int __musoqStructuralMaxDepth = 0;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (1 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 1;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            if (value.Codes != null)
            {
                var __musoqStructural_collection1 = value.Codes;
                var __musoqStructural_count2 = __musoqStructural_collection1.Length;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (3 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 3;
                }
            }

            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (2 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 2;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (3 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 3;
            __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
            if (3 > __musoqStructuralMaxDepth)
                __musoqStructuralMaxDepth = 3;
            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "c.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "c:1");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(bool __value0, int __value1, int __value2)
            {
                c_Enabled = __value0;
                c_Before = __value1;
                c_After = __value2;
            }

            public override int Count => 3;
            public int c_After { get; private set; }
            public int c_Before { get; private set; }
            public bool c_Enabled { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        c_Enabled = (bool)value;
                        break;
                    case 1:
                        c_Before = (int)value;
                        break;
                    case 2:
                        c_After = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "c.Enabled" => true,
                "c_Enabled" => true,
                "Enabled" => true,
                "c.Before" => true,
                "c_Before" => true,
                "Before" => true,
                "c.After" => true,
                "c_After" => true,
                "After" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)c_Enabled,
                1 => (object)c_Before,
                2 => (object)c_After,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "c.Enabled" => (object)c_Enabled,
                "c_Enabled" => (object)c_Enabled,
                "Enabled" => (object)c_Enabled,
                "c.Before" => (object)c_Before,
                "c_Before" => (object)c_Before,
                "Before" => (object)c_Before,
                "c.After" => (object)c_After,
                "c_After" => (object)c_After,
                "After" => (object)c_After,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(bool c_Enabled, int c_Before, int c_After)
            {
                this.c_Enabled = c_Enabled;
                this.c_Before = c_Before;
                this.c_After = c_After;
            }

            public int c_After { get; }
            public int c_Before { get; }
            public bool c_Enabled { get; }
        }
    }
}
