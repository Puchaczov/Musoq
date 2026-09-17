// === Parsed Query ===
/*
with unused as (
    select p.Value
    from values { (Value: 100) } p
)
select n.Value
from #inputs.numbers(values: array {}) n
*/

// === Logical Plan ===
/*
MultiStatement
  Project [n.Value as n.Value]
    SchemaScan [#inputs.numbers(array {  }) as n]
*/

// === Physical Plan ===
/*
PhysicalMultiStatement
  PhysicalProject [n.Value as n.Value]
    PhysicalSchemaScan [#inputs.numbers(array {  }) as n]
*/

// === Execution Plan ===
/*
ExecutionPlan [compiled]
  Shapes
    SourceEntity [n: NumberRow]
      Value: int <- property Value
    Generated [ResultRow0]
      n.Value: int <- field n_Value

  Body
    PhaseBoundary [Begin]
    Let [__musoqStructural_n_0: IReadOnlyList<int> = array {  }]
    PhaseBoundary [From]
    SourceScan [n: NumberRow] -> nRows
    CreateShapeRows [result: ResultShape0 from ResultRow0]
    PhaseBoundary [Select]
    ChunkedForEach [n in nRows]
      AppendShape [result <- ResultShape0(n.Value: n.Value)]
    ReturnDeferredTable [result: ResultRow0 <- ResultShape0]
*/

// === Generated C# ===

// === SyntaxTree:  ===
namespace GeneratedSample_Q348_StructuredEmptyAndSuppressedDemand
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
            new Column("n.Value", typeof(int), 0)
        };
        private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_compiled_n_0 = Array.AsReadOnly(new ISchemaColumn[] { new Column("Value", typeof(int), 0) });
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
                yield return new ResultRow0(__musoqShapeRow.n_Value);
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
                IReadOnlyList<int> __musoqStructural_n_0 = new int[]
                {
                };
                OnPhaseChanged("compiled", QueryPhase.From);
                var __nSchema = provider.GetSchema("#inputs");
                var nRowsSourceContext = new SourceExecutionContext("n:2", sourceExecutionPlans["n:2"], token, __schemaColumns_compiled_n_0, sourceRuntimeSettingsBySourceContextId["n:2"], logger, OnDataSourceProgress);
                Musoq.Schema.DataSources.RowSource<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nRowsSource;
                try
                {
                    Musoq.Examples.DataSources.StructuredInputs.NumbersSource nRowsSourceInstance = new Musoq.Examples.DataSources.StructuredInputs.NumbersSource(__musoqCheckStructural_9da7a11753aabe60(__musoqStructural_n_0, token), nRowsSourceContext);
                    nRowsSource = DataSourceLifecycle.OpenTypedRowSource<Musoq.Examples.DataSources.StructuredInputs.NumbersSource, Musoq.Examples.DataSources.StructuredInputs.NumberRow>(__nSchema, "numbers", nRowsSourceInstance, nRowsSourceContext, "#inputs", "n", "n:2");
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
                    throw global::Musoq.Schema.Exceptions.DataSourceLifecycleException.ForOpen("#inputs", "numbers", "n", "n:2", exception);
                }

                var nRows = __musoqProgressContext != null ? QueryProgressRuntime.WrapChunks<Musoq.Examples.DataSources.StructuredInputs.NumberRow>(nRowsSource.Chunks, __musoqProgressContext, "n:2") : nRowsSource.Chunks;
                OnPhaseChanged("compiled", QueryPhase.Select);
                foreach (var nChunk in nRows)
                {
                    if (nChunk is global::Musoq.Schema.DataSources.RowChunk<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nChunkView)
                    {
                        if (nChunkView.Source is Musoq.Examples.DataSources.StructuredInputs.NumberRow[] nChunkViewArray)
                        {
                            int nChunkViewOffset = nChunkView.Offset;
                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var n = nChunkViewArray[nChunkViewOffset + nIndex];
                                yield return new ResultShape0(n.Value);
                            }

                            continue;
                        }

                        if (nChunkView.Source is List<Musoq.Examples.DataSources.StructuredInputs.NumberRow> nChunkViewList)
                        {
                            int nChunkViewOffset = nChunkView.Offset;
                            for (int nIndex = 0, nIndexCount = nChunkView.Count; nIndex < nIndexCount; ++nIndex)
                            {
                                if ((nIndex & 1023) == 0)
                                {
                                    token.ThrowIfCancellationRequested();
                                }

                                var n = nChunkViewList[nChunkViewOffset + nIndex];
                                yield return new ResultShape0(n.Value);
                            }

                            continue;
                        }
                    }

                    for (int nIndex = 0, nIndexCount = nChunk.Count; nIndex < nIndexCount; ++nIndex)
                    {
                        if ((nIndex & 1023) == 0)
                        {
                            token.ThrowIfCancellationRequested();
                        }

                        var n = nChunk[nIndex];
                        yield return new ResultShape0(n.Value);
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

        private static IReadOnlyList<int> __musoqCheckStructural_9da7a11753aabe60(IReadOnlyList<int> value, System.Threading.CancellationToken token)
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
                var __musoqStructural_collection1 = (System.Collections.Generic.IReadOnlyList<int>)value;
                var __musoqStructural_count2 = __musoqStructural_collection1.Count;
                for (var __musoqStructural_index0 = 0; __musoqStructural_index0 < __musoqStructural_count2; __musoqStructural_index0++)
                {
                    if ((__musoqStructural_index0 & 1023) == 0)
                        token.ThrowIfCancellationRequested();
                    __musoqStructuralNodes = checked(__musoqStructuralNodes + 1L);
                    if (2 > __musoqStructuralMaxDepth)
                        __musoqStructuralMaxDepth = 2;
                }
            }

            global::Musoq.Evaluator.Helpers.StructuralInputLimitRuntime.ThrowIfExceeded("inline", "n.argument[0]", new global::Musoq.Schema.StructuralInputs.StructuralInputMetrics(__musoqStructuralMaxDepth, __musoqStructuralNodes, __musoqStructuralStrings), 32, 100000L, 67108864L, token, "n:2");
            return value;
        }

        private sealed class ResultRow0 : Row
        {
            public ResultRow0(int __value0)
            {
                n_Value = __value0;
            }

            public override int Count => 1;
            public int n_Value { get; private set; }

            public override void AssignValue(int columnNumber, object value)
            {
                switch (columnNumber)
                {
                    case 0:
                        n_Value = (int)value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public override bool HasColumn(string name) => name switch
            {
                "n.Value" => true,
                "n_Value" => true,
                "Value" => true,
                _ => false

            };
            public override object this[int columnNumber] => columnNumber switch
            {
                0 => (object)n_Value,
                _ => throw new IndexOutOfRangeException()
            };
            public override object this[string name] => name switch
            {
                "n.Value" => (object)n_Value,
                "n_Value" => (object)n_Value,
                "Value" => (object)n_Value,
                _ => throw new KeyNotFoundException(name)
            };
        }

        private sealed class ResultShape0
        {
            public ResultShape0(int n_Value)
            {
                this.n_Value = n_Value;
            }

            public int n_Value { get; }
        }
    }
}
